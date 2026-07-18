using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests.Validation
{
    public sealed class LocalizationValidationServiceTests
    {
        private const string TestRoot = "Assets/UnityLocalizationAssistantValidationTests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder("Assets", "UnityLocalizationAssistantValidationTests");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh();
        }

        [Test]
        public void Validate_DuplicateSourceIdentityAndKey_ReportsEveryOwner()
        {
            var target = Target("title");
            var schema = Schema("Validation Strings", new[] { target }, Array.Empty<string>(), false, false);
            var first = Draft("Assets/A.asset", "duplicate", "shared.title", "title");
            var second = Draft("Assets/B.asset", "duplicate", "shared.title", "title");

            var report = Validate(schema, first, second);

            Assert.That(report.Diagnostics.Count(value =>
                value.Code == ValidationDiagnosticCodes.SourceIdentityDuplicate), Is.EqualTo(2));
            Assert.That(report.Diagnostics.Count(value =>
                value.Code == ValidationDiagnosticCodes.DuplicateKey), Is.EqualTo(2));
            Assert.That(report.KeyOwnership.GetOwners("shared.title"), Has.Count.EqualTo(2));
        }

        [Test]
        public void Validate_RequiredLocaleWithoutTable_ReportsMissingTable()
        {
            var collection = CreateCollection("Missing Locale Strings", "en");
            var schema = Schema(collection.TableCollectionNameReference, new[] { Target("title") },
                new[] { "zh-Hans" }, true, false);

            var report = Validate(schema, Draft("Assets/A.asset", "a", "a.title", "title"));

            AssertCode(report, ValidationDiagnosticCodes.RequiredLocaleTableMissing, "zh-Hans");
        }

        [Test]
        public void Validate_RequiredLocaleWithoutEntry_ReportsMissingEntry()
        {
            var collection = CreateCollection("Missing Entry Strings", "en");
            var schema = Schema(collection.TableCollectionNameReference, new[] { Target("title") },
                new[] { "en" }, true, false);

            var report = Validate(schema, Draft("Assets/A.asset", "a", "a.title", "title"));

            AssertCode(report, ValidationDiagnosticCodes.RequiredLocaleEntryMissing, "en");
        }

        [Test]
        public void Validate_RequiredLocaleWithWhitespaceValue_ReportsMissingValue()
        {
            var collection = CreateCollection("Empty Value Strings", "en");
            var table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            var tableEntry = table.AddEntry("a.title", "   ");
            var draft = Draft("Assets/A.asset", "a", "a.title", "title");
            draft.ExistingTableReference = collection.TableCollectionNameReference;
            draft.ExistingEntryReference = tableEntry.KeyId;
            var schema = Schema(collection.TableCollectionNameReference, new[] { Target("title") },
                new[] { "en" }, true, false);

            var report = Validate(schema, draft);

            AssertCode(report, ValidationDiagnosticCodes.RequiredLocaleValueMissing, "en");
        }

        [Test]
        public void Validate_WrongCollection_ReportsReferenceError()
        {
            var expected = CreateCollection("Expected Strings", "en");
            var wrong = CreateCollection("Wrong Strings", "en", "WrongEnglish.asset");
            var draft = Draft("Assets/A.asset", "a", "a.title", "title");
            draft.ExistingTableReference = wrong.TableCollectionNameReference;
            draft.ExistingEntryReference = "a.title";
            var schema = Schema(expected.TableCollectionNameReference, new[] { Target("title") },
                Array.Empty<string>(), false, false);

            var report = Validate(schema, draft);

            AssertCode(report, ValidationDiagnosticCodes.WrongTableCollection);
        }

        [Test]
        public void PlaceholderParser_HandlesEscapesSelectorsAndNestedFormats()
        {
            var parser = new SmartStringPlaceholderParser();

            var result = parser.Parse("{{literal}} {user.Name} has {count:plural:{count} item|{count} items}");

            Assert.That(result.IsValid, Is.True, result.Error);
            Assert.That(result.Placeholders, Is.EqualTo(new[] { "count", "user" }));
        }

        [Test]
        public void PlaceholderParser_InvalidSyntax_ReturnsError()
        {
            var result = new SmartStringPlaceholderParser().Parse("Broken {count");

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Error, Is.Not.Empty);
        }

        [Test]
        public void Validate_PlaceholderMismatchAndInvalidSyntax_AreLocaleScoped()
        {
            var target = Target("title", placeholders: new[] { "amount" });
            var schema = Schema("Placeholder Strings", new[] { target }, Array.Empty<string>(), false, true);
            var draft = Draft("Assets/A.asset", "a", "a.title", "title");
            draft.LocaleValues.Add(Locale("en", "Damage {count}"));
            draft.LocaleValues.Add(Locale("zh-Hans", "Damage {amount"));

            var report = Validate(schema, draft);

            AssertCode(report, ValidationDiagnosticCodes.PlaceholderParityMismatch, "en");
            AssertCode(report, ValidationDiagnosticCodes.PlaceholderSyntaxInvalid, "zh-Hans");
        }

        [Test]
        public void Validate_Placeholders_UseSchemaSuggestedKeyWhenReferenceIsEmpty()
        {
            var collection = CreateCollection("Suggested Placeholder Strings", "en");
            var table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            table.AddEntry("a.title", "Damage {count}");
            var schema = Schema(collection.TableCollectionNameReference,
                new[] { Target("title", placeholders: new[] { "amount" }) },
                Array.Empty<string>(), false, true);

            var report = Validate(schema, Draft("Assets/A.asset", "a", "a.title", "title"));

            AssertCode(report, ValidationDiagnosticCodes.PlaceholderParityMismatch, "en");
        }

        [Test]
        public void Validate_Placeholders_PreferSchemaSuggestedKeyOverWrongCollectionSnapshot()
        {
            var collection = CreateCollection("Target Placeholder Strings", "en");
            var table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            table.AddEntry("a.title", "Damage {amount}");
            var draft = Draft("Assets/A.asset", "a", "a.title", "title");
            draft.ExistingTableReference = "Wrong Placeholder Strings";
            draft.LocaleValues.Add(Locale("en", "Damage {count}"));
            var schema = Schema(collection.TableCollectionNameReference,
                new[] { Target("title", placeholders: new[] { "amount" }) },
                Array.Empty<string>(), false, true);

            var report = Validate(schema, draft);

            Assert.That(report.Diagnostics.Any(value =>
                value.Code == ValidationDiagnosticCodes.PlaceholderParityMismatch), Is.False);
            AssertCode(report, ValidationDiagnosticCodes.WrongTableCollection);
        }

        [Test]
        public void Validate_CollectionWithoutElementIdentity_ReportsEvenWithoutEntries()
        {
            var target = Target(
                "detail",
                propertyPath: "items[].description",
                elementIdPath: string.Empty,
                keyTemplate: "{sourceId}.{targetId}.{elementId}");
            var schema = Schema("Element Strings", new[] { target }, Array.Empty<string>(), false, false,
                requireStableElementIdentity: true);

            var report = Validate(schema);

            AssertCode(report, ValidationDiagnosticCodes.ElementIdentityMissing);
            Assert.That(report.Diagnostics.Single(value =>
                value.Code == ValidationDiagnosticCodes.ElementIdentityMissing).Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
        }

        [Test]
        public void Validate_ReportCountsAndOrdering_AreDeterministic()
        {
            var scan = Scan();
            AddScanDiagnostic(scan, new LocalizationDiagnostic(
                DiagnosticSeverity.Error, "Z_ERROR", "error", "Assets/Z.asset"));
            AddScanDiagnostic(scan, new LocalizationDiagnostic(
                DiagnosticSeverity.Info, "B_INFO", "info", "Assets/B.asset"));
            AddScanDiagnostic(scan, new LocalizationDiagnostic(
                DiagnosticSeverity.Warning, "A_WARNING", "warning", "Assets/A.asset"));
            var schema = Schema("Report Strings", Array.Empty<LocalizationTargetDefinition>(),
                Array.Empty<string>(), false, false);

            var report = new LocalizationValidationService().Validate(schema, scan);

            Assert.That(report.InfoCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Diagnostics.Select(value => value.Code),
                Is.EqualTo(new[] { "B_INFO", "A_WARNING", "Z_ERROR" }));
        }

        private static LocalizationValidationReport Validate(
            LocalizationSchemaDefinition schema,
            params LocalizationDraftEntry[] entries)
        {
            var scan = Scan(entries);
            return new LocalizationValidationService().Validate(schema, scan);
        }

        private static SchemaScanResult Scan(params LocalizationDraftEntry[] entries)
        {
            var result = new SchemaScanResult();
            var field = typeof(SchemaScanResult).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "SchemaScanResult.entries backing field changed.");
            ((List<LocalizationDraftEntry>)field.GetValue(result)).AddRange(entries);
            return result;
        }

        private static void AddScanDiagnostic(SchemaScanResult result, LocalizationDiagnostic diagnostic)
        {
            var field = typeof(SchemaScanResult).GetField("diagnostics", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "SchemaScanResult.diagnostics backing field changed.");
            ((List<LocalizationDiagnostic>)field.GetValue(result)).Add(diagnostic);
        }

        private static LocalizationSchemaDefinition Schema(
            TableReference table,
            IReadOnlyList<LocalizationTargetDefinition> targets,
            IReadOnlyList<string> locales,
            bool requireLocales,
            bool validatePlaceholders,
            bool requireStableElementIdentity = true)
        {
            return new LocalizationSchemaDefinition(
                LocalizationSchemaAsset.CurrentVersion,
                typeof(LocalizationValidationServiceTests).AssemblyQualifiedName,
                new[] { TestRoot },
                "stableId",
                table,
                locales,
                LocalizationSchemaAsset.DefaultKeyTemplate,
                UpdatePolicy.PreserveExisting,
                targets,
                new LocalizationValidationDefinition(
                    requireStableElementIdentity,
                    requireLocales,
                    validatePlaceholders),
                Array.Empty<LocalizationTerminologyDefinition>());
        }

        private static LocalizationTargetDefinition Target(
            string targetId,
            string propertyPath = "title",
            string elementIdPath = "",
            string keyTemplate = "{sourceId}.{targetId}",
            IReadOnlyList<string> placeholders = null)
        {
            return new LocalizationTargetDefinition(
                targetId,
                propertyPath,
                elementIdPath,
                keyTemplate,
                true,
                placeholders ?? Array.Empty<string>(),
                UpdatePolicy.PreserveExisting);
        }

        private static LocalizationDraftEntry Draft(
            string assetPath,
            string sourceIdentity,
            string key,
            string targetId)
        {
            return new LocalizationDraftEntry
            {
                SourceAssetPath = assetPath,
                SourceIdentity = sourceIdentity,
                PropertyPath = targetId,
                TargetId = targetId,
                SuggestedKey = key
            };
        }

        private static LocalizationDraftLocaleValue Locale(string locale, string value)
        {
            return new LocalizationDraftLocaleValue
            {
                LocaleIdentifier = locale,
                ExistingValue = value
            };
        }

        private static StringTableCollection CreateCollection(
            string name,
            string localeCode,
            string localeAssetName = "English.asset")
        {
            var locale = UnityEngine.Localization.Locale.CreateLocale(localeCode);
            AssetDatabase.CreateAsset(locale, TestRoot + "/" + localeAssetName);
            return LocalizationEditorSettings.CreateStringTableCollection(
                name,
                TestRoot,
                new List<UnityEngine.Localization.Locale> { locale });
        }

        private static void AssertCode(
            LocalizationValidationReport report,
            string code,
            string locale = null)
        {
            Assert.That(report.Diagnostics.Any(value =>
                    value.Code == code
                    && (locale == null || value.LocaleIdentifier == locale)),
                Is.True,
                "Expected diagnostic " + code + (locale == null ? string.Empty : " for " + locale));
        }
    }
}
