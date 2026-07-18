using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class LocalizationValidationReport
    {
        public LocalizationValidationReport(
            IReadOnlyList<LocalizationDiagnostic> diagnostics,
            LocalizationKeyOwnershipIndex keyOwnership)
        {
            Diagnostics = diagnostics ?? Array.Empty<LocalizationDiagnostic>();
            KeyOwnership = keyOwnership ?? throw new ArgumentNullException(nameof(keyOwnership));
            ErrorCount = Diagnostics.Count(value => value.Severity == DiagnosticSeverity.Error);
            WarningCount = Diagnostics.Count(value => value.Severity == DiagnosticSeverity.Warning);
            InfoCount = Diagnostics.Count(value => value.Severity == DiagnosticSeverity.Info);
        }

        public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; }
        public LocalizationKeyOwnershipIndex KeyOwnership { get; }
        public int ErrorCount { get; }
        public int WarningCount { get; }
        public int InfoCount { get; }
        public bool IsValid => ErrorCount == 0;
    }

    public sealed class LocalizationValidationService
    {
        private readonly LocalizationKeyService keyService;
        private readonly SmartStringPlaceholderParser placeholderParser;

        public LocalizationValidationService()
            : this(new LocalizationKeyService(), new SmartStringPlaceholderParser())
        {
        }

        public LocalizationValidationService(
            LocalizationKeyService keyService,
            SmartStringPlaceholderParser placeholderParser)
        {
            this.keyService = keyService ?? throw new ArgumentNullException(nameof(keyService));
            this.placeholderParser = placeholderParser ?? throw new ArgumentNullException(nameof(placeholderParser));
        }

        public LocalizationValidationReport Validate(
            LocalizationSchemaDefinition schema,
            SchemaScanResult scanResult)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (scanResult == null)
                throw new ArgumentNullException(nameof(scanResult));

            var entries = scanResult.Entries.Where(value => value != null).ToArray();
            var diagnostics = new List<LocalizationDiagnostic>();
            diagnostics.AddRange(scanResult.Diagnostics);
            foreach (var entry in entries)
                diagnostics.AddRange(entry.Diagnostics);

            var ownership = keyService.BuildOwnershipIndex(entries);
            ValidateSourceIdentities(entries, diagnostics);
            ValidateKeys(entries, ownership, diagnostics);
            ValidateElementIdentities(schema, entries, diagnostics);
            ValidateReferences(schema, entries, diagnostics);
            ValidateRequiredLocales(schema, entries, diagnostics);
            ValidatePlaceholders(schema, entries, diagnostics);

            var ordered = diagnostics
                .Where(value => value != null)
                .GroupBy(ToDiagnosticIdentity, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(value => value.Severity)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .ThenBy(value => value.AssetPath, StringComparer.Ordinal)
                .ThenBy(value => value.PropertyPath, StringComparer.Ordinal)
                .ThenBy(value => value.LocaleIdentifier, StringComparer.Ordinal)
                .ThenBy(value => value.Key, StringComparer.Ordinal)
                .ThenBy(value => value.Message, StringComparer.Ordinal)
                .ToArray();
            return new LocalizationValidationReport(ordered, ownership);
        }

        private static void ValidateSourceIdentities(
            IReadOnlyList<LocalizationDraftEntry> entries,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            foreach (var group in entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.SourceIdentity))
                .GroupBy(entry => entry.SourceIdentity, StringComparer.Ordinal)
                .Where(group => group.Select(entry => entry.SourceAssetPath).Distinct(StringComparer.Ordinal).Skip(1).Any()))
            {
                foreach (var entry in group)
                {
                    diagnostics.Add(Error(
                        ValidationDiagnosticCodes.SourceIdentityDuplicate,
                        $"Source identity '{group.Key}' is owned by more than one asset.",
                        entry,
                        "Give every source asset a unique stable identity."));
                }
            }
        }

        private static void ValidateKeys(
            IReadOnlyList<LocalizationDraftEntry> entries,
            LocalizationKeyOwnershipIndex ownership,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            foreach (var entry in entries.Where(value => string.IsNullOrWhiteSpace(value.SuggestedKey)))
            {
                diagnostics.Add(Error(
                    ValidationDiagnosticCodes.SuggestedKeyEmpty,
                    "The expanded and normalized localization key is empty.",
                    entry,
                    "Provide non-empty stable identity values and a valid key template."));
            }

            foreach (var key in ownership.Keys.Where(value => ownership.GetOwners(value).Count > 1))
            {
                foreach (var owner in ownership.GetOwners(key))
                {
                    diagnostics.Add(new LocalizationDiagnostic(
                        DiagnosticSeverity.Error,
                        ValidationDiagnosticCodes.DuplicateKey,
                        $"Localization key '{key}' has more than one owner.",
                        owner.SourceAssetPath,
                        owner.PropertyPath,
                        key: key,
                        suggestedFix: "Change a source, target, or element identity so every key has one owner."));
                }
            }
        }

        private static void ValidateElementIdentities(
            LocalizationSchemaDefinition schema,
            IReadOnlyList<LocalizationDraftEntry> entries,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            foreach (var target in schema.Targets ?? Array.Empty<LocalizationTargetDefinition>())
            {
                var collectionDepth = CountCollectionMarkers(target.PropertyPath);
                if (collectionDepth > 1)
                {
                    diagnostics.Add(new LocalizationDiagnostic(
                        DiagnosticSeverity.Error,
                        ValidationDiagnosticCodes.NestedCollectionUnsupported,
                        $"Target '{target.TargetId}' contains {collectionDepth} collection levels; Schema v1 supports one stable element identity.",
                        propertyPath: target.PropertyPath,
                        suggestedFix: "Split the target or provide a project adapter for multi-level collection identity."));
                }

                if (!target.IsCollection || !string.IsNullOrWhiteSpace(target.ElementIdPath))
                    continue;
                var severity = schema.ValidationRules != null && schema.ValidationRules.RequireStableElementIdentity
                    ? DiagnosticSeverity.Error
                    : DiagnosticSeverity.Warning;
                diagnostics.Add(new LocalizationDiagnostic(
                    severity,
                    ValidationDiagnosticCodes.ElementIdentityMissing,
                    $"Collection target '{target.TargetId}' has no stable element identity path.",
                    propertyPath: target.PropertyPath,
                    suggestedFix: "Set elementIdPath to a stable serialized field; array indices are reorder-sensitive."));
            }

            foreach (var group in entries
                .Where(entry => !string.IsNullOrEmpty(entry.ElementIdentity))
                .GroupBy(
                    entry => entry.SourceAssetPath + "\n" + entry.TargetId + "\n" + entry.ElementIdentity,
                    StringComparer.Ordinal)
                .Where(group => group.Skip(1).Any()))
            {
                foreach (var entry in group)
                {
                    diagnostics.Add(Error(
                        ValidationDiagnosticCodes.ElementIdentityDuplicate,
                        $"Element identity '{entry.ElementIdentity}' is duplicated within target '{entry.TargetId}'.",
                        entry,
                        "Give every collection element a unique stable identity."));
                }
            }
        }

        private static void ValidateReferences(
            LocalizationSchemaDefinition schema,
            IReadOnlyList<LocalizationDraftEntry> entries,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            foreach (var entry in entries)
            {
                if (entry.ExistingTableReference.ReferenceType == TableReference.Type.Empty
                    || ReferencesSameCollection(entry.ExistingTableReference, schema.TableCollection))
                    continue;
                diagnostics.Add(Error(
                    ValidationDiagnosticCodes.WrongTableCollection,
                    "The LocalizedString points to a different table collection than the schema.",
                    entry,
                    "Assign the schema table collection before applying locale values."));
            }
        }

        private static void ValidateRequiredLocales(
            LocalizationSchemaDefinition schema,
            IReadOnlyList<LocalizationDraftEntry> entries,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (schema.ValidationRules == null || !schema.ValidationRules.RequireRequiredLocaleValues)
                return;

            var collection = LocalizationEditorSettings.GetStringTableCollection(schema.TableCollection);
            foreach (var entry in entries)
            {
                var target = FindTarget(schema, entry.TargetId);
                if (target == null || !target.Required)
                    continue;

                foreach (var localeCode in schema.RequiredLocales ?? Array.Empty<string>())
                {
                    var localeValue = entry.LocaleValues.FirstOrDefault(value =>
                        string.Equals(value.LocaleIdentifier, localeCode, StringComparison.OrdinalIgnoreCase));
                    var proposedValue = localeValue?.ProposedValue ?? string.Empty;
                    var table = collection?.GetTable(new LocaleIdentifier(localeCode)) as StringTable;
                    if (table == null)
                    {
                        diagnostics.Add(LocaleError(
                            ValidationDiagnosticCodes.RequiredLocaleTableMissing,
                            $"Required locale table '{localeCode}' is missing.",
                            entry,
                            localeCode,
                            "Add the locale table to the schema collection."));
                        continue;
                    }

                    StringTableEntry tableEntry = null;
                    if (ReferencesSameCollection(entry.ExistingTableReference, schema.TableCollection)
                        && entry.ExistingEntryReference.ReferenceType != TableEntryReference.Type.Empty)
                    {
                        tableEntry = table.GetEntryFromReference(entry.ExistingEntryReference);
                    }
                    if (tableEntry == null && !string.IsNullOrEmpty(entry.SuggestedKey))
                        tableEntry = table.GetEntry(entry.SuggestedKey);

                    if (tableEntry == null && string.IsNullOrWhiteSpace(proposedValue))
                    {
                        diagnostics.Add(LocaleError(
                            ValidationDiagnosticCodes.RequiredLocaleEntryMissing,
                            $"Required locale '{localeCode}' has no entry for key '{entry.SuggestedKey}'.",
                            entry,
                            localeCode,
                            "Create the entry and provide a localized value."));
                        continue;
                    }

                    var effectiveValue = !string.IsNullOrWhiteSpace(proposedValue)
                        ? proposedValue
                        : tableEntry?.LocalizedValue ?? localeValue?.ExistingValue ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(effectiveValue))
                    {
                        diagnostics.Add(LocaleError(
                            ValidationDiagnosticCodes.RequiredLocaleValueMissing,
                            $"Required locale '{localeCode}' has an empty value for key '{entry.SuggestedKey}'.",
                            entry,
                            localeCode,
                            "Provide a non-empty localized value."));
                    }
                }
            }
        }

        private void ValidatePlaceholders(
            LocalizationSchemaDefinition schema,
            IReadOnlyList<LocalizationDraftEntry> entries,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (schema.ValidationRules == null || !schema.ValidationRules.ValidatePlaceholderParity)
                return;

            var collection = LocalizationEditorSettings.GetStringTableCollection(schema.TableCollection);
            foreach (var entry in entries)
            {
                var target = FindTarget(schema, entry.TargetId);
                if (target == null)
                    continue;
                var expected = new HashSet<string>(
                    target.PlaceholderContract ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
                var localeCodes = new HashSet<string>(
                    entry.LocaleValues
                        .Where(value => !string.IsNullOrWhiteSpace(value.LocaleIdentifier))
                        .Select(value => value.LocaleIdentifier),
                    StringComparer.Ordinal);
                if (collection != null)
                {
                    foreach (var table in collection.StringTables.Where(value => value != null))
                        localeCodes.Add(table.LocaleIdentifier.Code);
                }

                foreach (var localeCode in localeCodes.OrderBy(value => value, StringComparer.Ordinal))
                {
                    var localeValue = entry.LocaleValues.FirstOrDefault(value =>
                        string.Equals(value.LocaleIdentifier, localeCode, StringComparison.Ordinal));
                    var proposedValue = localeValue?.ProposedValue ?? string.Empty;
                    var table = collection?.GetTable(new LocaleIdentifier(localeCode)) as StringTable;
                    var targetEntry = string.IsNullOrEmpty(entry.SuggestedKey)
                        ? null
                        : table?.GetEntry(entry.SuggestedKey);
                    var effectiveValue = !string.IsNullOrWhiteSpace(proposedValue)
                        ? proposedValue
                        : targetEntry?.LocalizedValue ?? localeValue?.ExistingValue ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(effectiveValue))
                        continue;

                    var parsed = placeholderParser.Parse(effectiveValue);
                    if (!parsed.IsValid)
                    {
                        diagnostics.Add(LocaleError(
                            ValidationDiagnosticCodes.PlaceholderSyntaxInvalid,
                            $"Smart String syntax is invalid: {parsed.Error}",
                            entry,
                            localeCode,
                            "Correct the Smart String braces and formatter syntax."));
                        continue;
                    }

                    var actual = new HashSet<string>(parsed.Placeholders, StringComparer.Ordinal);
                    if (actual.SetEquals(expected))
                        continue;
                    diagnostics.Add(LocaleError(
                        ValidationDiagnosticCodes.PlaceholderParityMismatch,
                        $"Placeholder contract mismatch. Expected [{Join(expected)}], found [{Join(actual)}].",
                        entry,
                        localeCode,
                        "Make the locale value placeholders exactly match placeholderContract."));
                }
            }
        }

        private static LocalizationTargetDefinition FindTarget(
            LocalizationSchemaDefinition schema,
            string targetId)
        {
            return (schema.Targets ?? Array.Empty<LocalizationTargetDefinition>())
                .FirstOrDefault(target => string.Equals(target.TargetId, targetId, StringComparison.Ordinal));
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

        private static int CountCollectionMarkers(string propertyPath)
        {
            var count = 0;
            var index = 0;
            while (!string.IsNullOrEmpty(propertyPath)
                && (index = propertyPath.IndexOf("[]", index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += 2;
            }
            return count;
        }

        private static string Join(IEnumerable<string> values)
        {
            return string.Join(", ", values.OrderBy(value => value, StringComparer.Ordinal));
        }

        private static LocalizationDiagnostic Error(
            string code,
            string message,
            LocalizationDraftEntry entry,
            string suggestedFix)
        {
            return new LocalizationDiagnostic(
                DiagnosticSeverity.Error,
                code,
                message,
                entry.SourceAssetPath,
                entry.PropertyPath,
                key: entry.SuggestedKey,
                suggestedFix: suggestedFix);
        }

        private static LocalizationDiagnostic LocaleError(
            string code,
            string message,
            LocalizationDraftEntry entry,
            string locale,
            string suggestedFix)
        {
            return new LocalizationDiagnostic(
                DiagnosticSeverity.Error,
                code,
                message,
                entry.SourceAssetPath,
                entry.PropertyPath,
                locale,
                entry.SuggestedKey,
                suggestedFix);
        }

        private static string ToDiagnosticIdentity(LocalizationDiagnostic value)
        {
            return string.Join("\u001f", new[]
            {
                ((int)value.Severity).ToString(), value.Code, value.Message, value.AssetPath,
                value.PropertyPath, value.LocaleIdentifier, value.Key, value.SuggestedFix
            });
        }
    }
}
