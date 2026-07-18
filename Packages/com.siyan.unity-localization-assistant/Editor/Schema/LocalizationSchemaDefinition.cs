using System.Collections.Generic;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class LocalizationSchemaDefinition
    {
        public LocalizationSchemaDefinition(
            int schemaVersion,
            string sourceType,
            IReadOnlyList<string> sourceFolders,
            string identityPath,
            TableReference tableCollection,
            IReadOnlyList<string> requiredLocales,
            string keyTemplate,
            UpdatePolicy defaultUpdatePolicy,
            IReadOnlyList<LocalizationTargetDefinition> targets,
            LocalizationValidationDefinition validationRules,
            IReadOnlyList<LocalizationTerminologyDefinition> terminologyRules)
        {
            SchemaVersion = schemaVersion;
            SourceType = sourceType;
            SourceFolders = sourceFolders;
            IdentityPath = identityPath;
            TableCollection = tableCollection;
            RequiredLocales = requiredLocales;
            KeyTemplate = keyTemplate;
            DefaultUpdatePolicy = defaultUpdatePolicy;
            Targets = targets;
            ValidationRules = validationRules;
            TerminologyRules = terminologyRules;
        }

        public int SchemaVersion { get; }
        public string SourceType { get; }
        public IReadOnlyList<string> SourceFolders { get; }
        public string IdentityPath { get; }
        public TableReference TableCollection { get; }
        public IReadOnlyList<string> RequiredLocales { get; }
        public string KeyTemplate { get; }
        public UpdatePolicy DefaultUpdatePolicy { get; }
        public IReadOnlyList<LocalizationTargetDefinition> Targets { get; }
        public LocalizationValidationDefinition ValidationRules { get; }
        public IReadOnlyList<LocalizationTerminologyDefinition> TerminologyRules { get; }
    }

    public sealed class LocalizationTargetDefinition
    {
        public LocalizationTargetDefinition(
            string targetId,
            string propertyPath,
            string elementIdPath,
            string keyTemplate,
            bool required,
            IReadOnlyList<string> placeholderContract,
            UpdatePolicy updatePolicy)
        {
            TargetId = targetId;
            PropertyPath = propertyPath;
            ElementIdPath = elementIdPath;
            KeyTemplate = keyTemplate;
            Required = required;
            PlaceholderContract = placeholderContract;
            UpdatePolicy = updatePolicy;
        }

        public string TargetId { get; }
        public string PropertyPath { get; }
        public string ElementIdPath { get; }
        public string KeyTemplate { get; }
        public bool Required { get; }
        public IReadOnlyList<string> PlaceholderContract { get; }
        public UpdatePolicy UpdatePolicy { get; }
        public bool IsCollection => PropertyPath.Contains("[]");
    }

    public sealed class LocalizationValidationDefinition
    {
        public LocalizationValidationDefinition(
            bool requireStableElementIdentity,
            bool requireRequiredLocaleValues,
            bool validatePlaceholderParity)
        {
            RequireStableElementIdentity = requireStableElementIdentity;
            RequireRequiredLocaleValues = requireRequiredLocaleValues;
            ValidatePlaceholderParity = validatePlaceholderParity;
        }

        public bool RequireStableElementIdentity { get; }
        public bool RequireRequiredLocaleValues { get; }
        public bool ValidatePlaceholderParity { get; }
    }

    public sealed class LocalizationTerminologyDefinition
    {
        public LocalizationTerminologyDefinition(
            string sourceTerm,
            string targetLocale,
            string expectedTerm,
            bool caseSensitive)
        {
            SourceTerm = sourceTerm;
            TargetLocale = targetLocale;
            ExpectedTerm = expectedTerm;
            CaseSensitive = caseSensitive;
        }

        public string SourceTerm { get; }
        public string TargetLocale { get; }
        public string ExpectedTerm { get; }
        public bool CaseSensitive { get; }
    }
}
