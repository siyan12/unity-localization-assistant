using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Parsing;

namespace Siyan.UnityLocalizationAssistant.Editor
{
    public sealed class PlaceholderParseResult
    {
        public PlaceholderParseResult(IReadOnlyList<string> placeholders, string error)
        {
            Placeholders = placeholders ?? Array.Empty<string>();
            Error = error ?? string.Empty;
        }

        public IReadOnlyList<string> Placeholders { get; }
        public string Error { get; }
        public bool IsValid => string.IsNullOrEmpty(Error);
    }

    public sealed class SmartStringPlaceholderParser
    {
        public PlaceholderParseResult Parse(string value)
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            Format format = null;
            try
            {
                format = formatter.Parser.ParseFormat(
                    value ?? string.Empty,
                    formatter.GetNotEmptyFormatterExtensionNames());
                var placeholders = new HashSet<string>(StringComparer.Ordinal);
                Collect(format, placeholders);
                return new PlaceholderParseResult(
                    placeholders.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
                    string.Empty);
            }
            catch (Exception exception)
            {
                return new PlaceholderParseResult(Array.Empty<string>(), exception.Message);
            }
            finally
            {
                format?.ReleaseToPool();
            }
        }

        private static void Collect(Format format, ISet<string> placeholders)
        {
            if (format == null)
                return;

            foreach (var placeholder in format.Items.OfType<Placeholder>())
            {
                if (placeholder.Selectors.Count > 0)
                {
                    var name = placeholder.Selectors[0].RawText;
                    if (!string.IsNullOrEmpty(name))
                        placeholders.Add(name);
                }
                Collect(placeholder.Format, placeholders);
            }
        }
    }
}
