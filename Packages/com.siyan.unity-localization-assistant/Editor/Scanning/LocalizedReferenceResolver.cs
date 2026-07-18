using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class LocalizedReferenceSnapshot
    {
        public LocalizedReferenceSnapshot(
            TableReference tableReference,
            TableEntryReference entryReference,
            string existingKey,
            IReadOnlyList<LocalizationDraftLocaleValue> localeValues,
            bool tableResolved,
            bool entryResolved)
        {
            TableReference = tableReference;
            EntryReference = entryReference;
            ExistingKey = existingKey ?? string.Empty;
            LocaleValues = localeValues ?? Array.Empty<LocalizationDraftLocaleValue>();
            TableResolved = tableResolved;
            EntryResolved = entryResolved;
        }

        public TableReference TableReference { get; }
        public TableEntryReference EntryReference { get; }
        public string ExistingKey { get; }
        public IReadOnlyList<LocalizationDraftLocaleValue> LocaleValues { get; }
        public bool TableResolved { get; }
        public bool EntryResolved { get; }
    }

    public sealed class LocalizedReferenceResolver
    {
        public bool TryResolve(
            SerializedProperty property,
            IReadOnlyList<string> requiredLocales,
            out LocalizedReferenceSnapshot snapshot)
        {
            snapshot = null;
            if (property == null)
                return false;

            LocalizedString localizedString;
            try
            {
                localizedString = property.boxedValue as LocalizedString;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (localizedString == null)
                return false;

            var collection = localizedString.TableReference.ReferenceType == TableReference.Type.Empty
                ? null
                : LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);
            var tableResolved = localizedString.TableReference.ReferenceType == TableReference.Type.Empty || collection != null;
            var sharedEntry = collection?.SharedData.GetEntryFromReference(localizedString.TableEntryReference);
            var entryResolved = localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty
                || sharedEntry != null;
            var existingKey = localizedString.TableEntryReference.ReferenceType == TableEntryReference.Type.Name
                ? localizedString.TableEntryReference.Key
                : sharedEntry?.Key ?? string.Empty;
            var localeValues = ReadLocaleValues(collection, localizedString.TableEntryReference, requiredLocales);
            snapshot = new LocalizedReferenceSnapshot(
                localizedString.TableReference,
                localizedString.TableEntryReference,
                existingKey,
                localeValues,
                tableResolved,
                entryResolved);
            return true;
        }

        private static IReadOnlyList<LocalizationDraftLocaleValue> ReadLocaleValues(
            StringTableCollection collection,
            TableEntryReference entryReference,
            IReadOnlyList<string> requiredLocales)
        {
            var localeCodes = new HashSet<string>(StringComparer.Ordinal);
            if (requiredLocales != null)
            {
                foreach (var locale in requiredLocales)
                {
                    if (!string.IsNullOrWhiteSpace(locale))
                        localeCodes.Add(locale.Trim());
                }
            }

            if (collection != null)
            {
                foreach (var table in collection.StringTables)
                {
                    if (table != null)
                        localeCodes.Add(table.LocaleIdentifier.Code);
                }
            }

            var values = new List<LocalizationDraftLocaleValue>();
            foreach (var localeCode in localeCodes.OrderBy(value => value, StringComparer.Ordinal))
            {
                var table = collection?.GetTable(new LocaleIdentifier(localeCode)) as StringTable;
                var entry = table?.GetEntryFromReference(entryReference);
                values.Add(new LocalizationDraftLocaleValue
                {
                    LocaleIdentifier = localeCode,
                    ExistingValue = entry?.LocalizedValue ?? string.Empty,
                    ProposedValue = string.Empty,
                    ChangeKind = ChangeKind.None
                });
            }

            return values;
        }
    }
}
