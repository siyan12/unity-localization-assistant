using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class SchemaReadResult
    {
        public SchemaReadResult(
            LocalizationSchemaDefinition schema,
            IReadOnlyList<LocalizationDiagnostic> diagnostics)
        {
            Schema = schema;
            Diagnostics = diagnostics;
        }

        public LocalizationSchemaDefinition Schema { get; }
        public IReadOnlyList<LocalizationDiagnostic> Diagnostics { get; }
        public bool IsValid => Schema != null && Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
    }

    public static class LocalizationSchemaReader
    {
        private static readonly Regex TargetIdPattern =
            new Regex("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.CultureInvariant);

        private static readonly Regex FixedArrayIndexPattern =
            new Regex(@"\[\d+\]", RegexOptions.CultureInvariant);

        private static readonly Regex TokenPattern =
            new Regex(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);

        private static readonly HashSet<string> AllowedTokens = new HashSet<string>(StringComparer.Ordinal)
        {
            "sourceId",
            "targetId",
            "elementId"
        };

        public static SchemaReadResult Read(LocalizationSchemaAsset asset)
        {
            var diagnostics = new List<LocalizationDiagnostic>();
            if (asset == null)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.AssetRequired,
                    "A localization schema asset is required.",
                    "Select or create a LocalizationSchemaAsset."));
                return new SchemaReadResult(null, diagnostics.AsReadOnly());
            }

            ValidateRoot(asset, diagnostics);
            var targets = ReadTargets(asset, diagnostics);
            var terminology = ReadTerminology(asset, diagnostics);
            var validationRules = asset.ValidationRules ?? new LocalizationValidationRules();

            var definition = new LocalizationSchemaDefinition(
                asset.SchemaVersion,
                Normalize(asset.SourceType),
                NormalizeDistinct(asset.SourceFolders, StringComparer.Ordinal),
                Normalize(asset.IdentityPath),
                asset.TableCollection,
                NormalizeDistinct(asset.RequiredLocales, StringComparer.OrdinalIgnoreCase),
                Normalize(asset.KeyTemplate),
                asset.DefaultUpdatePolicy,
                targets.AsReadOnly(),
                new LocalizationValidationDefinition(
                    validationRules.RequireStableElementIdentity,
                    validationRules.RequireRequiredLocaleValues,
                    validationRules.ValidatePlaceholderParity),
                terminology.AsReadOnly());

            return new SchemaReadResult(definition, diagnostics.AsReadOnly());
        }

        private static void ValidateRoot(
            LocalizationSchemaAsset asset,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (asset.SchemaVersion <= 0)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.VersionRequired,
                    "Schema version must be specified.",
                    $"Set schemaVersion to {LocalizationSchemaAsset.CurrentVersion}."));
            }
            else if (asset.SchemaVersion != LocalizationSchemaAsset.CurrentVersion)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.VersionUnsupported,
                    $"Schema version {asset.SchemaVersion} is not supported.",
                    $"Use schema version {LocalizationSchemaAsset.CurrentVersion}."));
            }

            if (string.IsNullOrWhiteSpace(asset.SourceType))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.SourceTypeRequired,
                    "Source type is required.",
                    "Use an assembly-qualified ScriptableObject type name."));
            }
            else
            {
                var sourceType = Type.GetType(asset.SourceType.Trim(), false);
                if (sourceType == null || !typeof(ScriptableObject).IsAssignableFrom(sourceType))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.SourceTypeInvalid,
                        $"Source type '{asset.SourceType}' could not be resolved as a ScriptableObject.",
                        "Use 'Namespace.TypeName, AssemblyName'."));
                }
            }

            ValidateSourceFolders(asset.SourceFolders, diagnostics);

            if (string.IsNullOrWhiteSpace(asset.IdentityPath))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.IdentityPathRequired,
                    "Source identity path is required.",
                    "Select a stable serialized identity field."));
            }

            if (asset.TableCollection.ReferenceType == TableReference.Type.Empty)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.TableCollectionRequired,
                    "A target String Table Collection is required.",
                    "Select a String Table Collection."));
            }

            ValidateLocales(asset.RequiredLocales, diagnostics);

            if (string.IsNullOrWhiteSpace(asset.KeyTemplate))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateRequired,
                    "The schema key template is required.",
                    $"Use {LocalizationSchemaAsset.DefaultKeyTemplate}."));
            }
        }

        private static void ValidateSourceFolders(
            IReadOnlyCollection<string> sourceFolders,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (sourceFolders == null || sourceFolders.Count == 0)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.SourceFolderRequired,
                    "At least one source folder is required.",
                    "Add an Assets-relative source folder."));
                return;
            }

            foreach (var folder in sourceFolders)
            {
                var normalized = Normalize(folder).Replace('\\', '/');
                if (string.IsNullOrEmpty(normalized) ||
                    (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal)))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.SourceFolderInvalid,
                        $"Source folder '{folder}' must be Assets-relative.",
                        "Use Assets or a path beginning with Assets/."));
                }
            }
        }

        private static void ValidateLocales(
            IReadOnlyCollection<string> locales,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (locales == null || locales.Count == 0)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.RequiredLocaleRequired,
                    "At least one required locale is required.",
                    "Add a locale identifier such as en."));
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var locale in locales)
            {
                var normalized = Normalize(locale);
                if (string.IsNullOrEmpty(normalized))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.RequiredLocaleInvalid,
                        "Required locale identifiers cannot be empty.",
                        "Remove the empty entry or enter a locale identifier."));
                }
                else if (!seen.Add(normalized))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.RequiredLocaleDuplicate,
                        $"Required locale '{normalized}' is duplicated.",
                        "Keep each locale identifier once."));
                }
            }
        }

        private static List<LocalizationTargetDefinition> ReadTargets(
            LocalizationSchemaAsset asset,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            var definitions = new List<LocalizationTargetDefinition>();
            if (asset.Targets == null || asset.Targets.Count == 0)
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.TargetRequired,
                    "At least one localization target is required.",
                    "Add a target field mapping."));
                ValidateKeyTemplate(asset.KeyTemplate, false, string.Empty, diagnostics);
                return definitions;
            }

            var targetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in asset.Targets)
            {
                if (target == null)
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.TargetRequired,
                        "A target entry is null.",
                        "Remove or replace the null target entry."));
                    continue;
                }

                var targetId = Normalize(target.TargetId);
                var propertyPath = Normalize(target.PropertyPath);
                var elementIdPath = Normalize(target.ElementIdPath);
                var effectiveTemplate = string.IsNullOrWhiteSpace(target.KeyTemplate)
                    ? Normalize(asset.KeyTemplate)
                    : Normalize(target.KeyTemplate);

                ValidateTargetIdentity(targetId, targetIds, propertyPath, diagnostics);
                var isCollection = propertyPath.Contains("[]");
                if (isCollection && string.IsNullOrEmpty(elementIdPath))
                {
                    diagnostics.Add(new LocalizationDiagnostic(
                        DiagnosticSeverity.Warning,
                        SchemaDiagnosticCodes.ElementIdPathRequired,
                        $"Collection target '{targetId}' has no stable element identity path.",
                        propertyPath: propertyPath,
                        suggestedFix: "Set elementIdPath to a stable field on each collection element."));
                }
                else if (!isCollection && !string.IsNullOrEmpty(elementIdPath))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.ElementIdPathUnexpected,
                        $"Non-collection target '{targetId}' cannot define elementIdPath.",
                        "Remove elementIdPath or use a collection property path.",
                        propertyPath));
                }

                ValidateKeyTemplate(effectiveTemplate, isCollection, propertyPath, diagnostics);
                var placeholders = ValidateAndNormalizePlaceholders(target.PlaceholderContract, propertyPath, diagnostics);

                definitions.Add(new LocalizationTargetDefinition(
                    targetId,
                    propertyPath,
                    elementIdPath,
                    effectiveTemplate,
                    target.Required,
                    placeholders,
                    target.UpdatePolicy));
            }

            return definitions;
        }

        private static void ValidateTargetIdentity(
            string targetId,
            ISet<string> targetIds,
            string propertyPath,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.TargetIdRequired,
                    "Target ID is required.",
                    "Enter a stable semantic target ID.",
                    propertyPath));
            }
            else if (!TargetIdPattern.IsMatch(targetId))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.TargetIdInvalid,
                    $"Target ID '{targetId}' contains unsupported characters.",
                    "Use letters, digits, period, underscore, or hyphen.",
                    propertyPath));
            }
            else if (!targetIds.Add(targetId))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.TargetIdDuplicate,
                    $"Target ID '{targetId}' is duplicated.",
                    "Use a unique target ID within the schema.",
                    propertyPath));
            }

            if (string.IsNullOrEmpty(propertyPath))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.PropertyPathRequired,
                    $"Target '{targetId}' requires a property path.",
                    "Enter a serialized property path."));
            }
            else if (FixedArrayIndexPattern.IsMatch(propertyPath) || propertyPath.Contains(".."))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.PropertyPathInvalid,
                    $"Property path '{propertyPath}' is not stable.",
                    "Use [] for collections and avoid fixed array indices.",
                    propertyPath));
            }
        }

        private static void ValidateKeyTemplate(
            string template,
            bool isCollection,
            string propertyPath,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateRequired,
                    "A key template is required.",
                    $"Use {LocalizationSchemaAsset.DefaultKeyTemplate}.",
                    propertyPath));
                return;
            }

            var matches = TokenPattern.Matches(template);
            var remainder = TokenPattern.Replace(template, string.Empty);
            if (remainder.Contains("{") || remainder.Contains("}"))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateMalformed,
                    $"Key template '{template}' has malformed braces.",
                    "Use balanced {token} placeholders.",
                    propertyPath));
            }

            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in matches)
            {
                var token = match.Groups[1].Value;
                if (string.IsNullOrEmpty(token))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.KeyTemplateMalformed,
                        "Key templates cannot contain an empty token.",
                        "Remove {} or replace it with a supported token.",
                        propertyPath));
                }
                else if (!AllowedTokens.Contains(token))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.KeyTemplateUnknownToken,
                        $"Key template token '{{{token}}}' is not supported.",
                        "Use {sourceId}, {targetId}, or {elementId}.",
                        propertyPath));
                }
                else
                {
                    tokens.Add(token);
                }
            }

            if (!tokens.Contains("sourceId"))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateSourceTokenRequired,
                    "Key templates must contain {sourceId}.",
                    "Add {sourceId} to the template.",
                    propertyPath));
            }
            if (!tokens.Contains("targetId"))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateTargetTokenRequired,
                    "Key templates must contain {targetId}.",
                    "Add {targetId} to the template.",
                    propertyPath));
            }
            if (isCollection && !tokens.Contains("elementId"))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateElementTokenRequired,
                    "Collection key templates must contain {elementId}.",
                    "Add {elementId} to the collection target template.",
                    propertyPath));
            }
            if (!isCollection && tokens.Contains("elementId"))
            {
                diagnostics.Add(Error(
                    SchemaDiagnosticCodes.KeyTemplateElementTokenUnexpected,
                    "Non-collection key templates cannot contain {elementId}.",
                    "Remove {elementId} or use a collection property path.",
                    propertyPath));
            }
        }

        private static List<string> ValidateAndNormalizePlaceholders(
            IReadOnlyCollection<string> placeholders,
            string propertyPath,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            var normalized = new List<string>();
            if (placeholders == null)
                return normalized;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placeholder in placeholders)
            {
                var value = Normalize(placeholder);
                if (string.IsNullOrEmpty(value))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.PlaceholderInvalid,
                        "Placeholder names cannot be empty.",
                        "Remove the empty placeholder.",
                        propertyPath));
                }
                else if (!seen.Add(value))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.PlaceholderDuplicate,
                        $"Placeholder '{value}' is duplicated.",
                        "Keep each placeholder once.",
                        propertyPath));
                }
                else
                {
                    normalized.Add(value);
                }
            }

            return normalized;
        }

        private static List<LocalizationTerminologyDefinition> ReadTerminology(
            LocalizationSchemaAsset asset,
            ICollection<LocalizationDiagnostic> diagnostics)
        {
            var definitions = new List<LocalizationTerminologyDefinition>();
            if (asset.TerminologyRules == null)
                return definitions;

            foreach (var rule in asset.TerminologyRules)
            {
                if (rule == null ||
                    string.IsNullOrWhiteSpace(rule.SourceTerm) ||
                    string.IsNullOrWhiteSpace(rule.TargetLocale) ||
                    string.IsNullOrWhiteSpace(rule.ExpectedTerm))
                {
                    diagnostics.Add(Error(
                        SchemaDiagnosticCodes.TerminologyRuleInvalid,
                        "Terminology rules require source term, target locale, and expected term.",
                        "Complete or remove the terminology rule."));
                    continue;
                }

                definitions.Add(new LocalizationTerminologyDefinition(
                    Normalize(rule.SourceTerm),
                    Normalize(rule.TargetLocale),
                    Normalize(rule.ExpectedTerm),
                    rule.CaseSensitive));
            }

            return definitions;
        }

        private static List<string> NormalizeDistinct(
            IEnumerable<string> values,
            IEqualityComparer<string> comparer)
        {
            if (values == null)
                return new List<string>();

            return values
                .Select(Normalize)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(comparer)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }

        private static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        private static LocalizationDiagnostic Error(
            string code,
            string message,
            string suggestedFix,
            string propertyPath = "")
        {
            return new LocalizationDiagnostic(
                DiagnosticSeverity.Error,
                code,
                message,
                propertyPath: propertyPath,
                suggestedFix: suggestedFix);
        }
    }
}
