using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class LocalizationKeyOwner
    {
        public LocalizationKeyOwner(
            string sourceAssetPath,
            string propertyPath,
            string sourceIdentity,
            string targetId,
            string elementIdentity)
        {
            SourceAssetPath = sourceAssetPath ?? string.Empty;
            PropertyPath = propertyPath ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            ElementIdentity = elementIdentity ?? string.Empty;
        }

        public string SourceAssetPath { get; }
        public string PropertyPath { get; }
        public string SourceIdentity { get; }
        public string TargetId { get; }
        public string ElementIdentity { get; }
    }

    public sealed class LocalizationKeyOwnershipIndex
    {
        private readonly SortedDictionary<string, IReadOnlyList<LocalizationKeyOwner>> owners;

        internal LocalizationKeyOwnershipIndex(
            SortedDictionary<string, IReadOnlyList<LocalizationKeyOwner>> owners)
        {
            this.owners = owners;
        }

        public IReadOnlyList<string> Keys => owners.Keys.ToArray();

        public IReadOnlyList<LocalizationKeyOwner> GetOwners(string key)
        {
            return key != null && owners.TryGetValue(key, out var values)
                ? values
                : Array.Empty<LocalizationKeyOwner>();
        }
    }

    public sealed class LocalizationKeyService
    {
        public string Expand(
            string template,
            string sourceIdentity,
            string targetId,
            string elementIdentity)
        {
            var expanded = (template ?? string.Empty)
                .Replace("{sourceId}", sourceIdentity ?? string.Empty)
                .Replace("{targetId}", targetId ?? string.Empty)
                .Replace("{elementId}", elementIdentity ?? string.Empty);
            return Normalize(expanded);
        }

        public string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
            var builder = new StringBuilder(normalized.Length);
            var replacementPending = false;
            for (var index = 0; index < normalized.Length; index++)
            {
                var character = normalized[index];
                if (char.IsHighSurrogate(character)
                    && index + 1 < normalized.Length
                    && char.IsLowSurrogate(normalized[index + 1]))
                {
                    if (IsSupplementaryLetterOrDigit(normalized, index))
                    {
                        if (replacementPending && builder.Length > 0 && !IsSeparator(builder[builder.Length - 1]))
                            builder.Append('-');
                        builder.Append(character);
                        builder.Append(normalized[++index]);
                        replacementPending = false;
                    }
                    else
                    {
                        index++;
                        replacementPending = true;
                    }
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    if (replacementPending && builder.Length > 0 && !IsSeparator(builder[builder.Length - 1]))
                        builder.Append('-');
                    builder.Append(character);
                    replacementPending = false;
                }
                else if (character == '.' || character == '_' || character == '-')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != character)
                        builder.Append(character);
                    replacementPending = false;
                }
                else
                {
                    replacementPending = true;
                }
            }

            return builder.ToString().Trim('.', '_', '-');
        }

        public LocalizationKeyOwnershipIndex BuildOwnershipIndex(
            IEnumerable<LocalizationDraftEntry> entries)
        {
            var grouped = new SortedDictionary<string, List<LocalizationKeyOwner>>(StringComparer.Ordinal);
            foreach (var entry in entries ?? Array.Empty<LocalizationDraftEntry>())
            {
                if (entry == null || string.IsNullOrEmpty(entry.SuggestedKey))
                    continue;
                if (!grouped.TryGetValue(entry.SuggestedKey, out var owners))
                {
                    owners = new List<LocalizationKeyOwner>();
                    grouped.Add(entry.SuggestedKey, owners);
                }
                owners.Add(new LocalizationKeyOwner(
                    entry.SourceAssetPath,
                    entry.PropertyPath,
                    entry.SourceIdentity,
                    entry.TargetId,
                    entry.ElementIdentity));
            }

            var result = new SortedDictionary<string, IReadOnlyList<LocalizationKeyOwner>>(StringComparer.Ordinal);
            foreach (var pair in grouped)
            {
                pair.Value.Sort(CompareOwners);
                result.Add(pair.Key, pair.Value.AsReadOnly());
            }
            return new LocalizationKeyOwnershipIndex(result);
        }

        private static bool IsSeparator(char value)
        {
            return value == '.' || value == '_' || value == '-';
        }

        private static bool IsSupplementaryLetterOrDigit(string value, int index)
        {
            switch (CharUnicodeInfo.GetUnicodeCategory(value, index))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                    return true;
                default:
                    return false;
            }
        }

        private static int CompareOwners(LocalizationKeyOwner left, LocalizationKeyOwner right)
        {
            var result = string.Compare(left.SourceAssetPath, right.SourceAssetPath, StringComparison.Ordinal);
            if (result != 0)
                return result;
            result = string.Compare(left.PropertyPath, right.PropertyPath, StringComparison.Ordinal);
            if (result != 0)
                return result;
            return string.Compare(left.TargetId, right.TargetId, StringComparison.Ordinal);
        }
    }
}
