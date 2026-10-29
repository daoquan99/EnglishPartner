using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Rules;

public static class GrammarContentHasher
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string ComputeHash(ExerciseType type, string contentJson, string? gradingJson)
    {
        string canonicalContent = CanonicalizeJson(contentJson);
        string canonicalGrading = CanonicalizeJson(gradingJson);

        string normalizedPayload = $"{type}|{canonicalContent}|{canonicalGrading}";
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPayload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string CanonicalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node == null)
            {
                return "{}";
            }

            var sb = new StringBuilder();
            WriteCanonicalNode(node, sb);
            return sb.ToString();
        }
        catch
        {
            // Fallback if parsing fails: normalize as raw string
            return NormalizeStringValue(json);
        }
    }

    private static void WriteCanonicalNode(JsonNode node, StringBuilder sb)
    {
        switch (node)
        {
            case JsonObject obj:
                sb.Append('{');
                var sortedProps = obj
                    .OrderBy(p => p.Key, StringComparer.Ordinal)
                    .ToList();

                for (int i = 0; i < sortedProps.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(sortedProps[i].Key).Append("\":");
                    if (sortedProps[i].Value == null)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        WriteCanonicalNode(sortedProps[i].Value!, sb);
                    }
                }
                sb.Append('}');
                break;

            case JsonArray arr:
                sb.Append('[');
                for (int i = 0; i < arr.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (arr[i] == null)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        WriteCanonicalNode(arr[i]!, sb);
                    }
                }
                sb.Append(']');
                break;

            case JsonValue val:
                if (val.TryGetValue<string>(out var strVal))
                {
                    string norm = NormalizeStringValue(strVal);
                    sb.Append(JsonSerializer.Serialize(norm, (JsonSerializerOptions?)null));
                }
                else if (val.TryGetValue<bool>(out var boolVal))
                {
                    sb.Append(boolVal ? "true" : "false");
                }
                else if (val.TryGetValue<decimal>(out var decVal))
                {
                    sb.Append(decVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else if (val.TryGetValue<double>(out var dblVal))
                {
                    sb.Append(dblVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else if (val.TryGetValue<long>(out var longVal))
                {
                    sb.Append(longVal.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    string raw = val.ToJsonString();
                    sb.Append(NormalizeStringValue(raw));
                }
                break;

            default:
                sb.Append("null");
                break;
        }
    }

    private static string NormalizeStringValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        string collapsed = WhitespaceRegex.Replace(trimmed, " ");
        return collapsed.ToLowerInvariant();
    }
}
