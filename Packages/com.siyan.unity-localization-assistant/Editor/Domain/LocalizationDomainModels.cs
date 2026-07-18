using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum UpdatePolicy
    {
        PreserveExisting,
        FillMissing,
        Overwrite
    }

    [Flags]
    public enum ChangeKind
    {
        None = 0,
        CreateKey = 1 << 0,
        AddLocaleValue = 1 << 1,
        UpdateLocaleValue = 1 << 2,
        AssignReference = 1 << 3,
        RenameKey = 1 << 4
    }

    [Serializable]
    public sealed class LocalizationDiagnostic
    {
        [SerializeField] private DiagnosticSeverity severity;
        [SerializeField] private string code = string.Empty;
        [SerializeField] private string message = string.Empty;
        [SerializeField] private string assetPath = string.Empty;
        [SerializeField] private string propertyPath = string.Empty;
        [SerializeField] private string localeIdentifier = string.Empty;
        [SerializeField] private string key = string.Empty;
        [SerializeField] private string suggestedFix = string.Empty;

        public LocalizationDiagnostic(
            DiagnosticSeverity severity,
            string code,
            string message,
            string assetPath = "",
            string propertyPath = "",
            string localeIdentifier = "",
            string key = "",
            string suggestedFix = "")
        {
            this.severity = severity;
            this.code = code ?? string.Empty;
            this.message = message ?? string.Empty;
            this.assetPath = assetPath ?? string.Empty;
            this.propertyPath = propertyPath ?? string.Empty;
            this.localeIdentifier = localeIdentifier ?? string.Empty;
            this.key = key ?? string.Empty;
            this.suggestedFix = suggestedFix ?? string.Empty;
        }

        public DiagnosticSeverity Severity => severity;
        public string Code => code;
        public string Message => message;
        public string AssetPath => assetPath;
        public string PropertyPath => propertyPath;
        public string LocaleIdentifier => localeIdentifier;
        public string Key => key;
        public string SuggestedFix => suggestedFix;
    }

    [Serializable]
    public sealed class LocalizationDraftLocaleValue
    {
        [SerializeField] private string localeIdentifier = string.Empty;
        [SerializeField] private string existingValue = string.Empty;
        [SerializeField] private string proposedValue = string.Empty;
        [SerializeField] private ChangeKind changeKind;
        [SerializeField] private bool tableExists;
        [SerializeField] private bool entryExists;

        public string LocaleIdentifier { get => localeIdentifier; set => localeIdentifier = value ?? string.Empty; }
        public string ExistingValue { get => existingValue; set => existingValue = value ?? string.Empty; }
        public string ProposedValue { get => proposedValue; set => proposedValue = value ?? string.Empty; }
        public ChangeKind ChangeKind { get => changeKind; set => changeKind = value; }
        public bool TableExists { get => tableExists; set => tableExists = value; }
        public bool EntryExists { get => entryExists; set => entryExists = value; }
    }

    [Serializable]
    public sealed class LocalizationDraftEntry
    {
        [SerializeField] private string sourceAssetPath = string.Empty;
        [SerializeField] private string sourceIdentity = string.Empty;
        [SerializeField] private string propertyPath = string.Empty;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string elementIdentity = string.Empty;
        [SerializeField] private string suggestedKey = string.Empty;
        [SerializeField] private string existingKey = string.Empty;
        [SerializeField] private TableReference existingTableReference;
        [SerializeField] private TableEntryReference existingEntryReference;
        [SerializeField] private List<LocalizationDraftLocaleValue> localeValues = new List<LocalizationDraftLocaleValue>();
        [SerializeField] private ChangeKind changeKind;
        [SerializeField] private List<LocalizationDiagnostic> diagnostics = new List<LocalizationDiagnostic>();
        [SerializeField] private bool enabled;

        public string SourceAssetPath { get => sourceAssetPath; set => sourceAssetPath = value ?? string.Empty; }
        public string SourceIdentity { get => sourceIdentity; set => sourceIdentity = value ?? string.Empty; }
        public string PropertyPath { get => propertyPath; set => propertyPath = value ?? string.Empty; }
        public string TargetId { get => targetId; set => targetId = value ?? string.Empty; }
        public string ElementIdentity { get => elementIdentity; set => elementIdentity = value ?? string.Empty; }
        public string SuggestedKey { get => suggestedKey; set => suggestedKey = value ?? string.Empty; }
        public string ExistingKey { get => existingKey; set => existingKey = value ?? string.Empty; }
        public TableReference ExistingTableReference { get => existingTableReference; set => existingTableReference = value; }
        public TableEntryReference ExistingEntryReference { get => existingEntryReference; set => existingEntryReference = value; }
        public List<LocalizationDraftLocaleValue> LocaleValues => localeValues;
        public ChangeKind ChangeKind { get => changeKind; set => changeKind = value; }
        public List<LocalizationDiagnostic> Diagnostics => diagnostics;
        public bool Enabled { get => enabled; set => enabled = value; }
    }
}
