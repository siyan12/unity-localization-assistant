using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    [CreateAssetMenu(
        fileName = "LocalizationSchema",
        menuName = "Localization Assistant/Schema")]
    public sealed class LocalizationSchemaAsset : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public const string DefaultKeyTemplate = "{sourceId}.{targetId}";

        [SerializeField] private int schemaVersion = CurrentVersion;
        [SerializeField] private string sourceType = string.Empty;
        [SerializeField] private List<string> sourceFolders = new List<string>();
        [SerializeField] private string identityPath = string.Empty;
        [SerializeField] private TableReference tableCollection;
        [SerializeField] private List<string> requiredLocales = new List<string>();
        [SerializeField] private string keyTemplate = DefaultKeyTemplate;
        [SerializeField] private UpdatePolicy defaultUpdatePolicy = UpdatePolicy.PreserveExisting;
        [SerializeField] private List<LocalizationSchemaTarget> targets = new List<LocalizationSchemaTarget>();
        [SerializeField] private LocalizationValidationRules validationRules = new LocalizationValidationRules();
        [SerializeField] private List<LocalizationTerminologyRule> terminologyRules = new List<LocalizationTerminologyRule>();

        public int SchemaVersion { get => schemaVersion; set => schemaVersion = value; }
        public string SourceType { get => sourceType; set => sourceType = value ?? string.Empty; }
        public List<string> SourceFolders => sourceFolders;
        public string IdentityPath { get => identityPath; set => identityPath = value ?? string.Empty; }
        public TableReference TableCollection { get => tableCollection; set => tableCollection = value; }
        public List<string> RequiredLocales => requiredLocales;
        public string KeyTemplate { get => keyTemplate; set => keyTemplate = value ?? string.Empty; }
        public UpdatePolicy DefaultUpdatePolicy { get => defaultUpdatePolicy; set => defaultUpdatePolicy = value; }
        public List<LocalizationSchemaTarget> Targets => targets;
        public LocalizationValidationRules ValidationRules => validationRules;
        public List<LocalizationTerminologyRule> TerminologyRules => terminologyRules;
    }

    [Serializable]
    public sealed class LocalizationSchemaTarget
    {
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string propertyPath = string.Empty;
        [SerializeField] private string elementIdPath = string.Empty;
        [SerializeField] private string keyTemplate = string.Empty;
        [SerializeField] private bool required = true;
        [SerializeField] private List<string> placeholderContract = new List<string>();
        [SerializeField] private UpdatePolicy updatePolicy = UpdatePolicy.PreserveExisting;

        public string TargetId { get => targetId; set => targetId = value ?? string.Empty; }
        public string PropertyPath { get => propertyPath; set => propertyPath = value ?? string.Empty; }
        public string ElementIdPath { get => elementIdPath; set => elementIdPath = value ?? string.Empty; }
        public string KeyTemplate { get => keyTemplate; set => keyTemplate = value ?? string.Empty; }
        public bool Required { get => required; set => required = value; }
        public List<string> PlaceholderContract => placeholderContract;
        public UpdatePolicy UpdatePolicy { get => updatePolicy; set => updatePolicy = value; }
    }

    [Serializable]
    public sealed class LocalizationValidationRules
    {
        [SerializeField] private bool requireStableElementIdentity = true;
        [SerializeField] private bool requireRequiredLocaleValues = true;
        [SerializeField] private bool validatePlaceholderParity = true;

        public bool RequireStableElementIdentity { get => requireStableElementIdentity; set => requireStableElementIdentity = value; }
        public bool RequireRequiredLocaleValues { get => requireRequiredLocaleValues; set => requireRequiredLocaleValues = value; }
        public bool ValidatePlaceholderParity { get => validatePlaceholderParity; set => validatePlaceholderParity = value; }
    }

    [Serializable]
    public sealed class LocalizationTerminologyRule
    {
        [SerializeField] private string sourceTerm = string.Empty;
        [SerializeField] private string targetLocale = string.Empty;
        [SerializeField] private string expectedTerm = string.Empty;
        [SerializeField] private bool caseSensitive;

        public string SourceTerm { get => sourceTerm; set => sourceTerm = value ?? string.Empty; }
        public string TargetLocale { get => targetLocale; set => targetLocale = value ?? string.Empty; }
        public string ExpectedTerm { get => expectedTerm; set => expectedTerm = value ?? string.Empty; }
        public bool CaseSensitive { get => caseSensitive; set => caseSensitive = value; }
    }
}
