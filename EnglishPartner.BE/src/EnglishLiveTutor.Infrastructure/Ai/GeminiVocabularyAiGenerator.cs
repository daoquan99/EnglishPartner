using System.Net.Http.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class GeminiVocabularyAiGenerator : IVocabularyAiGenerator
{
    private readonly HttpClient _http;

    public GeminiVocabularyAiGenerator(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GeneratedVocabularyRawSense>> GenerateVocabularyAsync(
        string apiKey,
        string modelCode,
        string cefrLevel,
        string topic,
        int count,
        List<string> excludedWords,
        CancellationToken ct = default)
    {
        string prompt = $@"You are an expert English lexicographer and curriculum designer for Vietnamese learners.
Generate {count} distinct, high-utility English vocabulary items tailored to CEFR level {cefrLevel} and topic '{topic}'.
Do NOT include any of these already-existing words: {string.Join(", ", excludedWords)}.

Strict Rules:
1. Return ONLY valid JSON matching the requested schema (a JSON array of objects).
2. Provide accurate UK and US IPA transcriptions, syllable counts, and stress patterns only when confident.
3. Provide clear English definitions (`definitionEn`), accurate Vietnamese meanings (`meaningVi`), synonyms, antonyms, word family members, natural collocations with Vietnamese meanings, 2 practical example sentences (`sentenceEn` + `sentenceVi`), and grammar patterns.

JSON Schema per item in array:
[
  {{
    ""word"": ""string"",
    ""partOfSpeech"": ""noun | verb | adjective | adverb | phrase"",
    ""cefrLevel"": ""{cefrLevel}"",
    ""definitionEn"": ""string"",
    ""meaningVi"": ""string"",
    ""descriptionEn"": ""string"",
    ""ipaUk"": ""/string/"",
    ""ipaUs"": ""/string/"",
    ""syllableCount"": 3,
    ""stressPattern"": ""string"",
    ""synonyms"": [""string""],
    ""antonyms"": [""string""],
    ""wordFamilies"": [{{ ""relatedWord"": ""string"", ""partOfSpeech"": ""string"" }}],
    ""collocations"": [{{ ""phraseEn"": ""string"", ""meaningVi"": ""string"" }}],
    ""examples"": [{{ ""sentenceEn"": ""string"", ""sentenceVi"": ""string"" }}],
    ""grammarPatterns"": [{{ ""pattern"": ""string"", ""explanation"": ""string"", ""example"": ""string"" }}]
  }}
]";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelCode}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";

        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.4,
                responseMimeType = "application/json"
            }
        };

        using var resp = await _http.PostAsJsonAsync(url, payload, ct);
        string rawBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Google AI Studio trả về lỗi {(int)resp.StatusCode}: {rawBody}");
        }

        var root = JsonNode.Parse(rawBody);
        string? jsonText = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("AI không trả về dữ liệu JSON hợp lệ.");
        }

        jsonText = CleanJsonFence(jsonText);
        var itemsArray = JsonNode.Parse(jsonText)?.AsArray();
        if (itemsArray is null)
        {
            throw new InvalidOperationException("Cấu trúc JSON từ AI không phải là mảng từ vựng.");
        }

        var results = new List<GeneratedVocabularyRawSense>();

        foreach (var itemNode in itemsArray)
        {
            if (itemNode is null) continue;

            string word = itemNode["word"]?.GetValue<string>()?.Trim() ?? "";
            string pos = itemNode["partOfSpeech"]?.GetValue<string>()?.Trim() ?? "noun";
            string defEn = itemNode["definitionEn"]?.GetValue<string>()?.Trim() ?? "";
            string meanVi = itemNode["meaningVi"]?.GetValue<string>()?.Trim() ?? "";
            string? descEn = itemNode["descriptionEn"]?.GetValue<string>()?.Trim();
            string? ipaUk = itemNode["ipaUk"]?.GetValue<string>()?.Trim();
            string? ipaUs = itemNode["ipaUs"]?.GetValue<string>()?.Trim();

            var synonyms = ReadStringList(itemNode["synonyms"]);
            var antonyms = ReadStringList(itemNode["antonyms"]);

            var wordFamilies = new List<GeneratedWordFamilyRaw>();
            var wfArr = itemNode["wordFamilies"]?.AsArray();
            if (wfArr is not null)
            {
                foreach (var wf in wfArr)
                {
                    string rw = wf?["relatedWord"]?.GetValue<string>()?.Trim() ?? "";
                    string rpos = wf?["partOfSpeech"]?.GetValue<string>()?.Trim() ?? "noun";
                    if (!string.IsNullOrWhiteSpace(rw))
                    {
                        wordFamilies.Add(new GeneratedWordFamilyRaw(rw, rpos));
                    }
                }
            }

            var collocations = new List<GeneratedCollocationRaw>();
            var colArr = itemNode["collocations"]?.AsArray();
            if (colArr is not null)
            {
                foreach (var c in colArr)
                {
                    string ph = c?["phraseEn"]?.GetValue<string>()?.Trim() ?? "";
                    string m = c?["meaningVi"]?.GetValue<string>()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(ph))
                    {
                        collocations.Add(new GeneratedCollocationRaw(ph, m));
                    }
                }
            }

            var examples = new List<GeneratedExampleRaw>();
            var exArr = itemNode["examples"]?.AsArray();
            if (exArr is not null)
            {
                foreach (var ex in exArr)
                {
                    string senEn = ex?["sentenceEn"]?.GetValue<string>()?.Trim() ?? "";
                    string senVi = ex?["sentenceVi"]?.GetValue<string>()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(senEn))
                    {
                        examples.Add(new GeneratedExampleRaw(senEn, senVi));
                    }
                }
            }

            var grammarPatterns = new List<GeneratedGrammarPatternRaw>();
            var gpArr = itemNode["grammarPatterns"]?.AsArray();
            if (gpArr is not null)
            {
                foreach (var gp in gpArr)
                {
                    string pat = gp?["pattern"]?.GetValue<string>()?.Trim() ?? "";
                    string exp = gp?["explanation"]?.GetValue<string>()?.Trim() ?? "";
                    string exm = gp?["example"]?.GetValue<string>()?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(pat))
                    {
                        grammarPatterns.Add(new GeneratedGrammarPatternRaw(pat, exp, exm));
                    }
                }
            }

            results.Add(new GeneratedVocabularyRawSense(
                Word: word,
                PartOfSpeech: pos,
                CefrLevel: cefrLevel,
                DefinitionEn: defEn,
                MeaningVi: meanVi,
                DescriptionEn: descEn,
                IpaUs: ipaUs,
                IpaUk: ipaUk,
                Synonyms: synonyms,
                Antonyms: antonyms,
                WordFamilies: wordFamilies,
                Collocations: collocations,
                Examples: examples,
                GrammarPatterns: grammarPatterns
            ));
        }

        return results;
    }

    private static List<string> ReadStringList(JsonNode? node)
    {
        var list = new List<string>();
        if (node is JsonArray arr)
        {
            foreach (var el in arr)
            {
                string? s = el?.GetValue<string>()?.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    list.Add(s);
                }
            }
        }
        return list;
    }

    private static string CleanJsonFence(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith("```"))
        {
            int firstNewline = t.IndexOf('\n');
            if (firstNewline >= 0) t = t[(firstNewline + 1)..];
            if (t.EndsWith("```")) t = t[..^3];
        }
        return t.Trim();
    }
}
