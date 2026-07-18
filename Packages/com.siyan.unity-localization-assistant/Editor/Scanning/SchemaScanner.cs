using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class SchemaScanResult
    {
        private readonly List<LocalizationDraftEntry> entries = new List<LocalizationDraftEntry>();
        private readonly List<LocalizationDiagnostic> diagnostics = new List<LocalizationDiagnostic>();

        public IReadOnlyList<LocalizationDraftEntry> Entries => entries;
        public IReadOnlyList<LocalizationDiagnostic> Diagnostics => diagnostics;
        internal List<LocalizationDraftEntry> MutableEntries => entries;
        internal List<LocalizationDiagnostic> MutableDiagnostics => diagnostics;
    }

    public sealed class SchemaScanner
    {
        private readonly LocalizedReferenceResolver referenceResolver;

        public SchemaScanner()
            : this(new LocalizedReferenceResolver())
        {
        }

        public SchemaScanner(LocalizedReferenceResolver referenceResolver)
        {
            this.referenceResolver = referenceResolver ?? throw new ArgumentNullException(nameof(referenceResolver));
        }

        public SchemaScanResult Scan(LocalizationSchemaDefinition schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            var result = new SchemaScanResult();
            var sourceType = ResolveSourceType(schema.SourceType);
            if (sourceType == null)
            {
                result.MutableDiagnostics.Add(new LocalizationDiagnostic(
                    DiagnosticSeverity.Error,
                    ScanningDiagnosticCodes.SourceTypeNotFound,
                    $"ScriptableObject source type '{schema.SourceType}' could not be resolved."));
                return result;
            }

            foreach (var assetPath in FindSourceAssetPaths(sourceType, schema.SourceFolders))
            {
                var sourceAsset = AssetDatabase.LoadAssetAtPath(assetPath, sourceType) as ScriptableObject;
                if (sourceAsset == null)
                    continue;

                ScanAsset(schema, sourceAsset, assetPath, result);
            }

            result.MutableEntries.Sort(CompareEntries);
            return result;
        }

        private void ScanAsset(
            LocalizationSchemaDefinition schema,
            ScriptableObject sourceAsset,
            string assetPath,
            SchemaScanResult result)
        {
            var serializedObject = new SerializedObject(sourceAsset);
            var identityProperty = serializedObject.FindProperty(schema.IdentityPath);
            if (!TryReadScalar(identityProperty, out var sourceIdentity) || string.IsNullOrWhiteSpace(sourceIdentity))
            {
                result.MutableDiagnostics.Add(new LocalizationDiagnostic(
                    DiagnosticSeverity.Error,
                    ScanningDiagnosticCodes.SourceIdentityInvalid,
                    $"Identity property '{schema.IdentityPath}' is missing or empty.",
                    assetPath,
                    schema.IdentityPath));
                return;
            }

            foreach (var target in schema.Targets ?? Array.Empty<LocalizationTargetDefinition>())
            {
                if (!IsSerializedPathValid(sourceAsset.GetType(), target.PropertyPath))
                {
                    result.MutableDiagnostics.Add(new LocalizationDiagnostic(
                        DiagnosticSeverity.Error,
                        ScanningDiagnosticCodes.PropertyPathInvalid,
                        $"Property path '{target.PropertyPath}' is not valid for '{sourceAsset.GetType().FullName}'.",
                        assetPath,
                        target.PropertyPath));
                    continue;
                }

                var matches = ResolveTargetProperties(
                    serializedObject,
                    target.PropertyPath);

                foreach (var match in matches)
                    CreateDraftEntry(schema, target, assetPath, sourceIdentity, match, result);
            }
        }

        private void CreateDraftEntry(
            LocalizationSchemaDefinition schema,
            LocalizationTargetDefinition target,
            string assetPath,
            string sourceIdentity,
            ResolvedTargetProperty match,
            SchemaScanResult result)
        {
            if (!referenceResolver.TryResolve(match.Property, schema.RequiredLocales, out var snapshot))
            {
                result.MutableDiagnostics.Add(new LocalizationDiagnostic(
                    DiagnosticSeverity.Error,
                    ScanningDiagnosticCodes.PropertyNotLocalizedString,
                    $"Property '{match.Property.propertyPath}' is not a serialized LocalizedString.",
                    assetPath,
                    match.Property.propertyPath));
                return;
            }

            var elementIdentity = string.Empty;
            if (target.IsCollection && !string.IsNullOrEmpty(target.ElementIdPath))
            {
                var elementIdentityProperty = match.CollectionElement?.FindPropertyRelative(target.ElementIdPath);
                if (!TryReadScalar(elementIdentityProperty, out elementIdentity) || string.IsNullOrWhiteSpace(elementIdentity))
                {
                    result.MutableDiagnostics.Add(new LocalizationDiagnostic(
                        DiagnosticSeverity.Error,
                        ScanningDiagnosticCodes.ElementIdentityInvalid,
                        $"Element identity path '{target.ElementIdPath}' is missing or empty.",
                        assetPath,
                        match.Property.propertyPath));
                    return;
                }
            }

            var keyTemplate = string.IsNullOrEmpty(target.KeyTemplate) ? schema.KeyTemplate : target.KeyTemplate;
            var suggestedKey = keyTemplate
                .Replace("{sourceId}", sourceIdentity)
                .Replace("{targetId}", target.TargetId)
                .Replace("{elementId}", elementIdentity);
            var changeKind = ChangeKind.None;
            if (snapshot.EntryReference.ReferenceType == TableEntryReference.Type.Empty || !snapshot.EntryResolved)
                changeKind = ChangeKind.CreateKey | ChangeKind.AssignReference;
            else if (!string.Equals(snapshot.ExistingKey, suggestedKey, StringComparison.Ordinal))
                changeKind = ChangeKind.RenameKey | ChangeKind.AssignReference;
            if (!ReferencesSameCollection(snapshot.TableReference, schema.TableCollection))
                changeKind |= ChangeKind.AssignReference;

            var entry = new LocalizationDraftEntry
            {
                SourceAssetPath = assetPath,
                SourceIdentity = sourceIdentity,
                PropertyPath = match.Property.propertyPath,
                TargetId = target.TargetId,
                ElementIdentity = elementIdentity,
                SuggestedKey = suggestedKey,
                ExistingKey = snapshot.ExistingKey,
                ExistingTableReference = snapshot.TableReference,
                ExistingEntryReference = snapshot.EntryReference,
                ChangeKind = changeKind,
                Enabled = false
            };
            entry.LocaleValues.AddRange(snapshot.LocaleValues);
            if (!snapshot.TableResolved)
            {
                entry.Diagnostics.Add(new LocalizationDiagnostic(
                    DiagnosticSeverity.Error,
                    ScanningDiagnosticCodes.LocalizedTableUnresolved,
                    "The existing LocalizedString table reference could not be resolved.",
                    assetPath,
                    match.Property.propertyPath));
            }
            if (!snapshot.EntryResolved)
            {
                entry.Diagnostics.Add(new LocalizationDiagnostic(
                    DiagnosticSeverity.Error,
                    ScanningDiagnosticCodes.LocalizedEntryUnresolved,
                    "The existing LocalizedString entry reference could not be resolved.",
                    assetPath,
                    match.Property.propertyPath));
            }
            result.MutableEntries.Add(entry);
        }

        private static bool ReferencesSameCollection(TableReference existing, TableReference expected)
        {
            if (existing.Equals(expected))
                return true;
            if (existing.ReferenceType == TableReference.Type.Empty || expected.ReferenceType == TableReference.Type.Empty)
                return false;

            var existingCollection = LocalizationEditorSettings.GetStringTableCollection(existing);
            var expectedCollection = LocalizationEditorSettings.GetStringTableCollection(expected);
            return existingCollection != null
                && expectedCollection != null
                && existingCollection.TableCollectionNameReference.Equals(expectedCollection.TableCollectionNameReference);
        }

        private static Type ResolveSourceType(string sourceTypeName)
        {
            if (string.IsNullOrWhiteSpace(sourceTypeName))
                return null;

            var directType = Type.GetType(sourceTypeName, false);
            if (IsSupportedSourceType(directType))
                return directType;

            return TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(IsSupportedSourceType)
                .Where(type => string.Equals(type.FullName, sourceTypeName, StringComparison.Ordinal)
                    || string.Equals(type.AssemblyQualifiedName, sourceTypeName, StringComparison.Ordinal))
                .OrderBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static bool IsSupportedSourceType(Type type)
        {
            return type != null && !type.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(type);
        }

        private static IReadOnlyList<string> FindSourceAssetPaths(
            Type sourceType,
            IReadOnlyList<string> sourceFolders)
        {
            var folders = (sourceFolders ?? Array.Empty<string>())
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .Select(folder => folder.Replace('\\', '/').TrimEnd('/'))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(folder => folder, StringComparer.Ordinal)
                .ToArray();
            // Unity's type-name filters can omit ScriptableObjects declared in test or
            // dynamically loaded assemblies. Enumerate folder assets, then enforce the
            // configured type after loading each candidate.
            var guids = AssetDatabase.FindAssets(string.Empty, folders);
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Where(path => AssetDatabase.LoadAssetAtPath(path, sourceType) != null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static List<ResolvedTargetProperty> ResolveTargetProperties(
            SerializedObject serializedObject,
            string configuredPath)
        {
            var results = new List<ResolvedTargetProperty>();
            if (serializedObject == null || string.IsNullOrWhiteSpace(configuredPath))
                return results;

            var segments = configuredPath.Split('.');
            ResolvePathSegment(serializedObject, null, null, segments, 0, results);
            return results;
        }

        private static void ResolvePathSegment(
            SerializedObject serializedObject,
            SerializedProperty parent,
            SerializedProperty collectionElement,
            IReadOnlyList<string> segments,
            int segmentIndex,
            ICollection<ResolvedTargetProperty> results)
        {
            if (segmentIndex >= segments.Count)
                return;

            var segment = segments[segmentIndex];
            var isCollection = segment.EndsWith("[]", StringComparison.Ordinal);
            var propertyName = isCollection ? segment.Substring(0, segment.Length - 2) : segment;
            var property = parent == null
                ? serializedObject.FindProperty(propertyName)
                : parent.FindPropertyRelative(propertyName);
            if (property == null)
                return;

            if (isCollection)
            {
                if (!property.isArray || property.propertyType == SerializedPropertyType.String)
                    return;

                for (var index = 0; index < property.arraySize; index++)
                {
                    var element = property.GetArrayElementAtIndex(index);
                    if (segmentIndex == segments.Count - 1)
                        results.Add(new ResolvedTargetProperty(element.Copy(), element.Copy()));
                    else
                        ResolvePathSegment(serializedObject, element, element, segments, segmentIndex + 1, results);
                }
                return;
            }

            if (segmentIndex == segments.Count - 1)
            {
                results.Add(new ResolvedTargetProperty(property.Copy(), collectionElement?.Copy()));
                return;
            }

            ResolvePathSegment(serializedObject, property, collectionElement, segments, segmentIndex + 1, results);
        }

        private static bool IsSerializedPathValid(Type sourceType, string configuredPath)
        {
            if (sourceType == null || string.IsNullOrWhiteSpace(configuredPath))
                return false;

            var currentType = sourceType;
            foreach (var segment in configuredPath.Split('.'))
            {
                var isCollection = segment.EndsWith("[]", StringComparison.Ordinal);
                var fieldName = isCollection ? segment.Substring(0, segment.Length - 2) : segment;
                var field = FindSerializedField(currentType, fieldName);
                if (field == null)
                    return false;

                currentType = field.FieldType;
                if (isCollection && !TryGetCollectionElementType(currentType, out currentType))
                    return false;
            }

            return true;
        }

        private static FieldInfo FindSerializedField(Type type, string fieldName)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null || field.IsStatic || field.IsInitOnly)
                    continue;
                if (field.IsPublic
                    || field.GetCustomAttribute<SerializeField>() != null
                    || field.GetCustomAttribute<SerializeReference>() != null)
                    return field;
            }

            return null;
        }

        private static bool TryGetCollectionElementType(Type collectionType, out Type elementType)
        {
            if (collectionType.IsArray)
            {
                elementType = collectionType.GetElementType();
                return elementType != null;
            }

            var listType = collectionType.IsGenericType
                && collectionType.GetGenericTypeDefinition() == typeof(List<>)
                ? collectionType
                : collectionType.GetInterfaces()
                    .FirstOrDefault(type => type.IsGenericType
                        && type.GetGenericTypeDefinition() == typeof(IList<>));
            if (listType != null)
            {
                elementType = listType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool TryReadScalar(SerializedProperty property, out string value)
        {
            value = string.Empty;
            if (property == null)
                return false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    value = property.stringValue;
                    return true;
                case SerializedPropertyType.Integer:
                    value = property.longValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Enum:
                    value = property.enumNames[property.enumValueIndex];
                    return true;
                case SerializedPropertyType.ObjectReference:
                    value = property.objectReferenceValue == null ? string.Empty : property.objectReferenceValue.name;
                    return true;
                default:
                    return false;
            }
        }

        private static int CompareEntries(LocalizationDraftEntry left, LocalizationDraftEntry right)
        {
            var pathComparison = string.Compare(left.SourceAssetPath, right.SourceAssetPath, StringComparison.Ordinal);
            if (pathComparison != 0)
                return pathComparison;
            var propertyComparison = string.Compare(left.PropertyPath, right.PropertyPath, StringComparison.Ordinal);
            if (propertyComparison != 0)
                return propertyComparison;
            return string.Compare(left.TargetId, right.TargetId, StringComparison.Ordinal);
        }

        private sealed class ResolvedTargetProperty
        {
            public ResolvedTargetProperty(SerializedProperty property, SerializedProperty collectionElement)
            {
                Property = property;
                CollectionElement = collectionElement;
            }

            public SerializedProperty Property { get; }
            public SerializedProperty CollectionElement { get; }
        }
    }
}
