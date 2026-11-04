using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Application.Common.Utilities;

public static class IdempotencyFingerprint
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Compute(object? payload, params (string Key, object? Value)[] routeParameters)
    {
        var root = new JsonObject();

        if (routeParameters != null && routeParameters.Length > 0)
        {
            var routesObj = new JsonObject();
            foreach (var (k, v) in routeParameters.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                routesObj[k] = v != null ? JsonValue.Create(v.ToString()) : null;
            }
            root["_routes"] = routesObj;
        }

        if (payload != null)
        {
            JsonNode? payloadNode;
            if (payload is string s)
            {
                try
                {
                    payloadNode = JsonNode.Parse(s);
                }
                catch
                {
                    payloadNode = JsonValue.Create(s);
                }
            }
            else if (payload is JsonNode jn)
            {
                payloadNode = JsonNode.Parse(jn.ToJsonString());
            }
            else
            {
                string json = JsonSerializer.Serialize(payload, SerializerOptions);
                payloadNode = JsonNode.Parse(json);
            }

            if (payloadNode != null)
            {
                payloadNode = CanonicalizeAndSanitize(payloadNode);
                root["_payload"] = payloadNode;
            }
        }

        string canonicalJson = SortJsonObject(root).ToJsonString();
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static JsonNode? CanonicalizeAndSanitize(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var sorted = new JsonObject();
            foreach (var kvp in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                // Redact sensitive secrets from fingerprint
                if (string.Equals(kvp.Key, "apiKey", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "password", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "token", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sorted[kvp.Key] = kvp.Value != null ? CanonicalizeAndSanitize(kvp.Value) : null;
            }
            return sorted;
        }

        if (node is JsonArray arr)
        {
            var sortedArr = new JsonArray();
            foreach (var item in arr)
            {
                sortedArr.Add(item != null ? CanonicalizeAndSanitize(item) : null);
            }
            return sortedArr;
        }

        return node.DeepClone();
    }

    private static JsonObject SortJsonObject(JsonObject obj)
    {
        var sorted = new JsonObject();
        foreach (var kvp in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            sorted[kvp.Key] = kvp.Value?.DeepClone();
        }
        return sorted;
    }
}
