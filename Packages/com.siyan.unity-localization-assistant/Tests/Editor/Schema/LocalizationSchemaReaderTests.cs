using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Siyan.UnityLocalizationAssistant.Editor.Tests
{
    public sealed class LocalizationSchemaReaderTests
    {
        private LocalizationSchemaAsset schema;

        [TearDown]
        public void TearDown()
        {
            if (schema != null)
                Object.DestroyImmediate(schema);
        }

        [Test]
        public void SchemaAsset_HasSafeNonNullDefaults()
        {
            schema = ScriptableObject.CreateInstance<LocalizationSchemaAsset>();

            Assert.That(schema.SchemaVersion, Is.EqualTo(LocalizationSchemaAsset.CurrentVersion));
            Assert.That(schema.DefaultUpdatePolicy, Is.EqualTo(UpdatePolicy.PreserveExisting));
            Assert.That(schema.SourceFolders, Is.Not.Null);
            Assert.That(schema.RequiredLocales, Is.Not.Null);
            Assert.That(schema.Targets, Is.Not.Null);
            Assert.That(schema.ValidationRules, Is.Not.Null);
            Assert.That(schema.TerminologyRules, Is.Not.Null);
        }

        [Test]
        public void Read_ValidSchema_ReturnsNormalizedDefinition()
        {
            schema = CreateValidSchema();
            schema.SourceFolders.Add(" Assets/Zeta ");
            schema.SourceFolders.Add("Assets/FictionalCatalog");
            schema.RequiredLocales[0] = " en ";

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Schema.SourceFolders,
                Is.EqualTo(new[] { "Assets/FictionalCatalog", "Assets/Zeta" }));
            Assert.That(result.Schema.RequiredLocales,
                Is.EqualTo(new[] { "en", "zh-Hans" }));
            Assert.That(result.Schema.Targets.Single().KeyTemplate,
                Is.EqualTo(LocalizationSchemaAsset.DefaultKeyTemplate));
        }

        [Test]
        public void Read_NullAsset_ReturnsStableDiagnosticInsteadOfThrowing()
        {
            var result = LocalizationSchemaReader.Read(null);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.AssetRequired), Is.True);
        }

        [Test]
        public void Read_UnsupportedVersion_ReturnsStableDiagnostic()
        {
            schema = CreateValidSchema();
            schema.SchemaVersion = 2;

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.VersionUnsupported), Is.True);
        }

        [Test]
        public void Read_MissingRootFields_ReturnsDeterministicDiagnostics()
        {
            schema = ScriptableObject.CreateInstance<LocalizationSchemaAsset>();

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.SourceTypeRequired));
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.SourceFolderRequired));
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.IdentityPathRequired));
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.TableCollectionRequired));
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.RequiredLocaleRequired));
            Assert.That(result.Diagnostics.Select(d => d.Code), Does.Contain(SchemaDiagnosticCodes.TargetRequired));
        }

        [Test]
        public void Read_UnresolvableSourceType_ReturnsStableDiagnostic()
        {
            schema = CreateValidSchema();
            schema.SourceType = "Fictional.MissingType, Fictional.MissingAssembly";

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.SourceTypeInvalid), Is.True);
        }

        [Test]
        public void Read_DuplicateTargetIds_ReturnsStableDiagnostic()
        {
            schema = CreateValidSchema();
            schema.Targets.Add(CreateTarget("name", "description"));

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.TargetIdDuplicate), Is.True);
        }

        [Test]
        public void Read_CollectionTargetWithStableIdentity_IsValid()
        {
            schema = CreateValidSchema();
            schema.Targets.Clear();
            schema.Targets.Add(new LocalizationSchemaTarget
            {
                TargetId = "bonus-description",
                PropertyPath = "bonuses[].description",
                ElementIdPath = "stableId",
                KeyTemplate = "{sourceId}.{targetId}.{elementId}",
                UpdatePolicy = UpdatePolicy.FillMissing
            });

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Schema.Targets.Single().IsCollection, Is.True);
        }

        [Test]
        public void Read_CollectionWithoutElementIdentity_ReturnsWarning()
        {
            schema = CreateValidSchema();
            schema.Targets.Clear();
            schema.Targets.Add(new LocalizationSchemaTarget
            {
                TargetId = "bonus-description",
                PropertyPath = "bonuses[].description",
                KeyTemplate = "{sourceId}.{targetId}.{elementId}"
            });

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.True);
            var diagnostic = result.Diagnostics.Single(d =>
                d.Code == SchemaDiagnosticCodes.ElementIdPathRequired);
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
        }

        [Test]
        public void Read_CollectionTemplateWithoutElementToken_ReturnsError()
        {
            schema = CreateValidSchema();
            schema.Targets.Clear();
            schema.Targets.Add(new LocalizationSchemaTarget
            {
                TargetId = "bonus-description",
                PropertyPath = "bonuses[].description",
                ElementIdPath = "stableId"
            });

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.KeyTemplateElementTokenRequired), Is.True);
        }

        [Test]
        public void Read_UnknownTemplateToken_ReturnsError()
        {
            schema = CreateValidSchema();
            schema.Targets.Single().KeyTemplate = "{sourceId}.{targetId}.{kind}";

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.KeyTemplateUnknownToken), Is.True);
        }

        [TestCase("{sourceId}.{targetId", SchemaDiagnosticCodes.KeyTemplateMalformed)]
        [TestCase("{targetId}", SchemaDiagnosticCodes.KeyTemplateSourceTokenRequired)]
        [TestCase("{sourceId}", SchemaDiagnosticCodes.KeyTemplateTargetTokenRequired)]
        public void Read_InvalidTemplate_ReturnsExpectedStableDiagnostic(
            string template,
            string expectedCode)
        {
            schema = CreateValidSchema();
            schema.Targets.Single().KeyTemplate = template;

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, expectedCode), Is.True);
        }

        [Test]
        public void Read_NonCollectionElementToken_ReturnsError()
        {
            schema = CreateValidSchema();
            schema.Targets.Single().KeyTemplate = "{sourceId}.{targetId}.{elementId}";

            var result = LocalizationSchemaReader.Read(schema);

            Assert.That(result.IsValid, Is.False);
            Assert.That(HasCode(result, SchemaDiagnosticCodes.KeyTemplateElementTokenUnexpected), Is.True);
        }

        [Test]
        public void SchemaAsset_EditorJsonRoundTrip_PreservesContract()
        {
            schema = CreateValidSchema();
            var json = EditorJsonUtility.ToJson(schema);
            var clone = ScriptableObject.CreateInstance<LocalizationSchemaAsset>();

            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, clone);
                var result = LocalizationSchemaReader.Read(clone);

                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Schema.SourceType, Is.EqualTo(schema.SourceType));
                Assert.That(result.Schema.Targets.Single().TargetId, Is.EqualTo("name"));
                Assert.That(result.Schema.TableCollection.ReferenceType,
                    Is.EqualTo(schema.TableCollection.ReferenceType));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        private LocalizationSchemaAsset CreateValidSchema()
        {
            var asset = ScriptableObject.CreateInstance<LocalizationSchemaAsset>();
            asset.SourceType = "UnityEngine.ScriptableObject, UnityEngine.CoreModule";
            asset.SourceFolders.Add("Assets/FictionalCatalog");
            asset.IdentityPath = "stableId";
            asset.TableCollection = "Fictional Item Strings";
            asset.RequiredLocales.Add("en");
            asset.RequiredLocales.Add("zh-Hans");
            asset.Targets.Add(CreateTarget("name", "displayName"));
            return asset;
        }

        private static LocalizationSchemaTarget CreateTarget(string targetId, string propertyPath)
        {
            return new LocalizationSchemaTarget
            {
                TargetId = targetId,
                PropertyPath = propertyPath,
                Required = true,
                UpdatePolicy = UpdatePolicy.FillMissing
            };
        }

        private static bool HasCode(SchemaReadResult result, string code)
        {
            return result.Diagnostics.Any(d => d.Code == code);
        }
    }
}
