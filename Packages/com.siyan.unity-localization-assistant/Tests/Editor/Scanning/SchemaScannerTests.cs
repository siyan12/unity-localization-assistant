using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests.Scanning
{
    public sealed class SchemaScannerTests
    {
        private const string TestRoot = "Assets/UnityLocalizationAssistantScannerTests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder("Assets", "UnityLocalizationAssistantScannerTests");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Scan_ReadsPrivateTopLevelAndNestedLocalizedStrings()
        {
            var assetPath = CreateSourceAsset(
                "Catalog.asset",
                "item-01",
                new LocalizedString("Catalog Strings", "legacy.title"),
                new NestedValue("primary", new LocalizedString("Catalog Strings", "legacy.primary")),
                new NestedValue("secondary", new LocalizedString("Catalog Strings", "legacy.secondary")));
            var schema = CreateSchema(
                new LocalizationTargetDefinition(
                    "title",
                    "title",
                    string.Empty,
                    "{sourceId}.{targetId}",
                    true,
                    Array.Empty<string>(),
                    UpdatePolicy.PreserveExisting),
                new LocalizationTargetDefinition(
                    "detail",
                    "nestedValues[].text",
                    "stableId",
                    "{sourceId}.{targetId}.{elementId}",
                    true,
                    Array.Empty<string>(),
                    UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(3));
            Assert.That(result.Entries.Select(entry => entry.SourceAssetPath), Is.All.EqualTo(assetPath));
            Assert.That(result.Entries.Select(entry => entry.PropertyPath), Is.EqualTo(new[]
            {
                "nestedValues.Array.data[0].text",
                "nestedValues.Array.data[1].text",
                "title"
            }));
            Assert.That(result.Entries[0].ElementIdentity, Is.EqualTo("primary"));
            Assert.That(result.Entries[0].SuggestedKey, Is.EqualTo("item-01.detail.primary"));
            Assert.That(result.Entries[0].ExistingKey, Is.EqualTo("legacy.primary"));
            Assert.That(result.Entries[2].SuggestedKey, Is.EqualTo("item-01.title"));
            Assert.That(result.Entries[2].ExistingKey, Is.EqualTo("legacy.title"));
        }

        [Test]
        public void Scan_UsesDeterministicAssetAndPropertyOrder()
        {
            AssetDatabase.CreateFolder(TestRoot, "FolderB");
            AssetDatabase.CreateFolder(TestRoot, "FolderA");
            CreateSourceAsset("FolderB/Zeta.asset", "zeta", new LocalizedString(), new NestedValue("b", new LocalizedString()));
            CreateSourceAsset("FolderA/Alpha.asset", "alpha", new LocalizedString(), new NestedValue("a", new LocalizedString()));
            var schema = CreateSchema(
                new LocalizationTargetDefinition(
                    "title", "title", string.Empty, "{sourceId}.{targetId}", true,
                    Array.Empty<string>(), UpdatePolicy.PreserveExisting),
                new LocalizationTargetDefinition(
                    "detail", "nestedValues[].text", "stableId", "{sourceId}.{targetId}.{elementId}", true,
                    Array.Empty<string>(), UpdatePolicy.PreserveExisting),
                new[] { TestRoot + "/FolderB", TestRoot + "/FolderA" });
            var scanner = new SchemaScanner();

            var first = scanner.Scan(schema).Entries.Select(ToStableSignature).ToArray();
            var second = scanner.Scan(schema).Entries.Select(ToStableSignature).ToArray();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Is.EqualTo(first.OrderBy(value => value, StringComparer.Ordinal).ToArray()));
            Assert.That(first[0], Does.StartWith(TestRoot + "/FolderA/Alpha.asset|"));
            Assert.That(first[2], Does.StartWith(TestRoot + "/FolderB/Zeta.asset|"));
        }

        [Test]
        public void Scan_InvalidPropertyPath_ProducesStableDiagnostic()
        {
            CreateSourceAsset(
                "Catalog.asset",
                "item-01",
                new LocalizedString(),
                new NestedValue("primary", new LocalizedString()));
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "missing", "nestedValues[].doesNotExist", "stableId", "{sourceId}.{targetId}.{elementId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(ScanningDiagnosticCodes.PropertyPathInvalid));
            Assert.That(result.Diagnostics[0].PropertyPath, Is.EqualTo("nestedValues[].doesNotExist"));
        }

        [Test]
        public void Scan_EmptyValidCollection_ProducesNoInvalidPathDiagnostic()
        {
            CreateSourceAsset("Catalog.asset", "item-01", new LocalizedString());
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "detail", "nestedValues[].text", "stableId", "{sourceId}.{targetId}.{elementId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        }

        [Test]
        public void Scan_EmptyCollectionWithInvalidElementPath_ProducesDiagnostic()
        {
            CreateSourceAsset("Catalog.asset", "item-01", new LocalizedString());
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "detail", "nestedValues[].doesNotExist", "stableId", "{sourceId}.{targetId}.{elementId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Entries, Is.Empty);
            Assert.That(result.Diagnostics.Select(value => value.Code),
                Does.Contain(ScanningDiagnosticCodes.PropertyPathInvalid));
        }

        [Test]
        public void Scan_DirectLocalizedStringCollection_ProducesEntries()
        {
            var assetPath = CreateSourceAsset("Catalog.asset", "item-01", new LocalizedString());
            var asset = AssetDatabase.LoadAssetAtPath<ScannerTestCatalog>(assetPath);
            var serializedObject = new SerializedObject(asset);
            var directTexts = serializedObject.FindProperty("directTexts");
            directTexts.arraySize = 2;
            directTexts.GetArrayElementAtIndex(0).boxedValue = new LocalizedString("Scanner Test Strings", "direct.one");
            directTexts.GetArrayElementAtIndex(1).boxedValue = new LocalizedString("Scanner Test Strings", "direct.two");
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "direct", "directTexts[]", string.Empty, "{sourceId}.{targetId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Entries.Select(value => value.PropertyPath), Is.EqualTo(new[]
            {
                "directTexts.Array.data[0]",
                "directTexts.Array.data[1]"
            }));
        }

        [Test]
        public void Scan_ResolvesExistingKeyAndLocaleValue()
        {
            var locale = Locale.CreateLocale("en");
            AssetDatabase.CreateAsset(locale, TestRoot + "/English.asset");
            var collection = LocalizationEditorSettings.CreateStringTableCollection(
                "Scanner Test Strings",
                TestRoot,
                new List<Locale> { locale });
            var table = collection.GetTable(locale.Identifier) as StringTable;
            Assert.That(table, Is.Not.Null);
            var tableEntry = table.AddEntry("item-01.title", "Existing title");
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            var tablePath = AssetDatabase.GetAssetPath(table);
            var sharedDataPath = AssetDatabase.GetAssetPath(collection.SharedData);
            var tableBytesBefore = File.ReadAllBytes(Path.GetFullPath(tablePath));
            var sharedDataBytesBefore = File.ReadAllBytes(Path.GetFullPath(sharedDataPath));
            CreateSourceAsset(
                "Catalog.asset",
                "item-01",
                new LocalizedString(collection.TableCollectionNameReference, tableEntry.KeyId));
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "title", "title", string.Empty, "{sourceId}.{targetId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Entries, Has.Count.EqualTo(1));
            Assert.That(result.Entries[0].ExistingKey, Is.EqualTo("item-01.title"));
            Assert.That(result.Entries[0].ExistingTableReference.ReferenceType, Is.EqualTo(TableReference.Type.Guid));
            Assert.That(result.Entries[0].ExistingEntryReference.KeyId, Is.EqualTo(tableEntry.KeyId));
            Assert.That(result.Entries[0].LocaleValues, Has.Count.EqualTo(1));
            Assert.That(result.Entries[0].LocaleValues[0].LocaleIdentifier, Is.EqualTo("en"));
            Assert.That(result.Entries[0].LocaleValues[0].ExistingValue, Is.EqualTo("Existing title"));
            Assert.That(result.Entries[0].ChangeKind, Is.EqualTo(ChangeKind.None));
            Assert.That(File.ReadAllBytes(Path.GetFullPath(tablePath)), Is.EqualTo(tableBytesBefore));
            Assert.That(File.ReadAllBytes(Path.GetFullPath(sharedDataPath)), Is.EqualTo(sharedDataBytesBefore));
            Assert.That(EditorUtility.IsDirty(table), Is.False);
            Assert.That(EditorUtility.IsDirty(collection.SharedData), Is.False);
        }

        [Test]
        public void Scan_WrongCollectionWithMatchingKey_RequiresReferenceAssignment()
        {
            CreateSourceAsset(
                "Catalog.asset",
                "item-01",
                new LocalizedString("Wrong Strings", "item-01.title"));
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "title", "title", string.Empty, "{sourceId}.{targetId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var entry = new SchemaScanner().Scan(schema).Entries.Single();

            Assert.That(entry.ExistingTableReference.TableCollectionName, Is.EqualTo("Wrong Strings"));
            Assert.That(entry.ExistingEntryReference.Key, Is.EqualTo("item-01.title"));
            Assert.That((entry.ChangeKind & ChangeKind.AssignReference) != 0, Is.True);
        }

        [Test]
        public void Scan_UnresolvedIdReference_ProducesDiagnosticWithoutInventingKey()
        {
            CreateSourceAsset(
                "Catalog.asset",
                "item-01",
                new LocalizedString("Missing Strings", 42L));
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "title", "title", string.Empty, "{sourceId}.{targetId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));

            var entry = new SchemaScanner().Scan(schema).Entries.Single();

            Assert.That(entry.ExistingKey, Is.Empty);
            Assert.That(entry.ExistingEntryReference.KeyId, Is.EqualTo(42));
            Assert.That(entry.ChangeKind, Is.EqualTo(ChangeKind.CreateKey | ChangeKind.AssignReference));
            Assert.That(entry.Diagnostics.Select(value => value.Code),
                Does.Contain(ScanningDiagnosticCodes.LocalizedEntryUnresolved));
        }

        [Test]
        public void Scan_DryRunDoesNotChangeSourceAssetBytesOrDirtyState()
        {
            var assetPath = CreateSourceAsset("Catalog.asset", "item-01", new LocalizedString());
            var schema = CreateSchema(new LocalizationTargetDefinition(
                "title", "title", string.Empty, "{sourceId}.{targetId}", true,
                Array.Empty<string>(), UpdatePolicy.PreserveExisting));
            var absolutePath = Path.GetFullPath(assetPath);
            var bytesBefore = File.ReadAllBytes(absolutePath);
            var asset = AssetDatabase.LoadAssetAtPath<ScannerTestCatalog>(assetPath);
            Assert.That(EditorUtility.IsDirty(asset), Is.False);

            var result = new SchemaScanner().Scan(schema);

            Assert.That(result.Entries, Has.Count.EqualTo(1));
            Assert.That(File.ReadAllBytes(absolutePath), Is.EqualTo(bytesBefore));
            Assert.That(EditorUtility.IsDirty(asset), Is.False);
        }

        private static LocalizationSchemaDefinition CreateSchema(
            LocalizationTargetDefinition target,
            IReadOnlyList<string> sourceFolders = null)
        {
            return CreateSchema(new[] { target }, sourceFolders);
        }

        private static LocalizationSchemaDefinition CreateSchema(
            LocalizationTargetDefinition first,
            LocalizationTargetDefinition second,
            IReadOnlyList<string> sourceFolders = null)
        {
            return CreateSchema(new[] { first, second }, sourceFolders);
        }

        private static LocalizationSchemaDefinition CreateSchema(
            IReadOnlyList<LocalizationTargetDefinition> targets,
            IReadOnlyList<string> sourceFolders = null)
        {
            return new LocalizationSchemaDefinition(
                LocalizationSchemaAsset.CurrentVersion,
                typeof(ScannerTestCatalog).AssemblyQualifiedName,
                sourceFolders ?? new[] { TestRoot },
                "stableId",
                "Scanner Test Strings",
                new[] { "en" },
                LocalizationSchemaAsset.DefaultKeyTemplate,
                UpdatePolicy.PreserveExisting,
                targets,
                new LocalizationValidationDefinition(true, true, true),
                Array.Empty<LocalizationTerminologyDefinition>());
        }

        private static string CreateSourceAsset(
            string relativePath,
            string stableId,
            LocalizedString title,
            params NestedValue[] nestedValues)
        {
            var assetPath = TestRoot + "/" + relativePath;
            var asset = ScriptableObject.CreateInstance<ScannerTestCatalog>();
            AssetDatabase.CreateAsset(asset, assetPath);
            var serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("stableId").stringValue = stableId;
            serializedObject.FindProperty("title").boxedValue = title;
            var nestedProperty = serializedObject.FindProperty("nestedValues");
            nestedProperty.arraySize = nestedValues.Length;
            for (var index = 0; index < nestedValues.Length; index++)
            {
                var element = nestedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("stableId").stringValue = nestedValues[index].StableId;
                element.FindPropertyRelative("text").boxedValue = nestedValues[index].Text;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return assetPath;
        }

        private static string ToStableSignature(LocalizationDraftEntry entry)
        {
            return $"{entry.SourceAssetPath}|{entry.PropertyPath}|{entry.TargetId}|{entry.SuggestedKey}";
        }
    }

    public sealed class ScannerTestCatalog : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private LocalizedString title = new LocalizedString();
        [SerializeField] private List<NestedValue> nestedValues = new List<NestedValue>();
        [SerializeField] private List<LocalizedString> directTexts = new List<LocalizedString>();
    }

    [Serializable]
    public sealed class NestedValue
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private LocalizedString text = new LocalizedString();

        public NestedValue(string stableId, LocalizedString text)
        {
            this.stableId = stableId;
            this.text = text;
        }

        public string StableId => stableId;
        public LocalizedString Text => text;
    }
}
