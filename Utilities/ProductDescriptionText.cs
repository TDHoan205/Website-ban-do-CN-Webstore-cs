using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Webstore.Utilities
{
    public static partial class ProductDescriptionText
    {
        private static readonly HashSet<string> AllowedDescriptionTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "p", "br", "ul", "ol", "li", "strong", "em", "b", "i", "u"
        };

        public static string? NormalizePlainText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var normalized = DecodeEntitiesDeep(input);
            normalized = TryRepairMojibake(normalized);
            normalized = normalized.Replace("\0", string.Empty, StringComparison.Ordinal);
            return normalized.Trim();
        }

        public static string? SanitizeDescriptionHtmlNullable(string? input)
        {
            var sanitized = SanitizeDescriptionHtml(input);
            return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
        }

        public static string SanitizeDescriptionHtml(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var normalized = NormalizePlainText(input) ?? string.Empty;

            // Remove dangerous element blocks completely before processing the remaining tags.
            normalized = DangerousBlockTagRegex().Replace(normalized, string.Empty);
            normalized = HtmlCommentRegex().Replace(normalized, string.Empty);

            var sb = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in HtmlTagRegex().Matches(normalized))
            {
                if (match.Index > lastIndex)
                {
                    var rawText = normalized[lastIndex..match.Index];
                    sb.Append(HtmlEncoder.Default.Encode(rawText));
                }

                var tagName = match.Groups["name"].Value;
                var isClosing = match.Groups["closing"].Success;

                if (AllowedDescriptionTags.Contains(tagName))
                {
                    var safeTag = tagName.ToLowerInvariant();
                    if (safeTag == "br")
                    {
                        sb.Append("<br />");
                    }
                    else if (isClosing)
                    {
                        sb.Append($"</{safeTag}>");
                    }
                    else
                    {
                        sb.Append($"<{safeTag}>");
                    }
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < normalized.Length)
            {
                var tail = normalized[lastIndex..];
                sb.Append(HtmlEncoder.Default.Encode(tail));
            }

            var output = sb.ToString();
            output = NewlineRegex().Replace(output, "<br />");
            output = RepeatedBreakRegex().Replace(output, "<br /><br />");
            output = output.Trim();

            if (string.IsNullOrWhiteSpace(output))
            {
                return string.Empty;
            }

            // Wrap plain content in a paragraph so it renders consistently across pages.
            // But skip for very short strings (like single escape characters '\') to avoid EF Core translation bugs.
            if (output.Length > 1 && !BlockTagRegex().IsMatch(output))
            {
                output = $"<p>{output}</p>";
            }

            return output;
        }

        public static string ToPlainText(string? descriptionHtml, int maxLength = 0)
        {
            var normalized = NormalizePlainText(descriptionHtml);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            var withoutTags = AnyHtmlTagRegex().Replace(normalized, " ");
            withoutTags = WebUtility.HtmlDecode(withoutTags);
            withoutTags = MultiWhitespaceRegex().Replace(withoutTags, " ").Trim();

            if (maxLength > 0 && withoutTags.Length > maxLength)
            {
                return withoutTags[..maxLength].TrimEnd() + "...";
            }

            return withoutTags;
        }

        private static string DecodeEntitiesDeep(string input)
        {
            var current = input;
            for (var i = 0; i < 3; i++)
            {
                var decoded = WebUtility.HtmlDecode(current);
                if (string.Equals(decoded, current, StringComparison.Ordinal))
                {
                    break;
                }

                current = decoded;
            }

            return current;
        }

        private static string TryRepairMojibake(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var best = input;
            for (var i = 0; i < 2; i++)
            {
                if (!LooksLikeMojibake(best) && best.IndexOf('\uFFFD') < 0)
                {
                    break;
                }

                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    var legacyEncoding = Encoding.GetEncoding(1252);
                    var bytes = legacyEncoding.GetBytes(best);
                    var utf8 = Encoding.UTF8.GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(utf8))
                    {
                        best = utf8;
                    }
                }
                catch
                {
                    try
                    {
                        var bytes = Encoding.Latin1.GetBytes(best);
                        var utf8 = Encoding.UTF8.GetString(bytes);
                        if (!string.IsNullOrWhiteSpace(utf8))
                        {
                            best = utf8;
                        }
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            return best;
        }

        private static bool LooksLikeMojibake(string input)
        {
            return input.Contains("Ã", StringComparison.Ordinal)
                || input.Contains("á»", StringComparison.Ordinal)
                || input.Contains("áº", StringComparison.Ordinal);
        }

        [GeneratedRegex(@"<\s*(?<closing>/)?\s*(?<name>[a-zA-Z0-9]+)(?:\s+[^>]*)?>", RegexOptions.Compiled)]
        private static partial Regex HtmlTagRegex();

        [GeneratedRegex(@"<\s*(script|style|iframe|object|embed|svg|math)[^>]*>[\s\S]*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DangerousBlockTagRegex();

        [GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.Compiled)]
        private static partial Regex HtmlCommentRegex();

        [GeneratedRegex(@"\r\n|\r|\n", RegexOptions.Compiled)]
        private static partial Regex NewlineRegex();

        [GeneratedRegex(@"(?:<br\s*/?>\s*){3,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex RepeatedBreakRegex();

        [GeneratedRegex(@"<(p|ul|ol|li|br)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex BlockTagRegex();

        [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
        private static partial Regex AnyHtmlTagRegex();

        [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
        private static partial Regex MultiWhitespaceRegex();
    }
}