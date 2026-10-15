using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Vocabulary;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class VocabularyGenerationContent : IVocabularyGenerationContent
{
    public string BuildPrompt(string cefrLevel, string topic, int count, IReadOnlyList<string> excludedWords)
    {
        string excludedWordsJoined = string.Join(", ", excludedWords);
        string prompt = $@"You are an expert English lexicographer and curriculum designer for Vietnamese learners.
Generate {count} distinct, high-utility English vocabulary items tailored to CEFR level {cefrLevel} and topic '{topic}'.
Do NOT include any of these already-existing words: {excludedWordsJoined}.

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
        return prompt;
    }

    public List<GeneratedVocabularyRawSense> ParseResponse(string jsonText, string cefrLevel)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new InvalidOperationException("Vui lòng dán response JSON của AI.");
        try
        {
            return ParseItems(jsonText, VocabularyGenerationDefaults.NormalizeCefr(cefrLevel));
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("JSON không hợp lệ. Hãy dán nguyên mảng JSON hoặc khối ```json của AI.", ex);
        }
    }

    private static List<GeneratedVocabularyRawSense> ParseItems(string jsonText, string cefrLevel)
    {
        jsonText = CleanJsonFence(jsonText);
        var itemsArray = JsonNode.Parse(jsonText) as JsonArray;
        if (itemsArray is null)
        {
            throw new InvalidOperationException("Cấu trúc JSON từ AI không phải là mảng từ vựng.");
        }

        if (itemsArray.Count == 0 || itemsArray.Count > VocabularyGenerationDefaults.MaxCount)
            throw new InvalidOperationException($"JSON phải chứa từ 1 đến {VocabularyGenerationDefaults.MaxCount} từ vựng.");

        var results = new List<GeneratedVocabularyRawSense>();

        foreach (var itemNode in itemsArray)
        {
            if (itemNode is not JsonObject item)
                throw new InvalidOperationException("Mỗi phần tử trong JSON phải là một object từ vựng.");

            string word = ReadText(item["word"], "word") ?? "";
            string pos = ReadText(item["partOfSpeech"], "partOfSpeech") ?? "noun";
            string defEn = ReadText(item["definitionEn"], "definitionEn") ?? "";
            string meanVi = ReadText(item["meaningVi"], "meaningVi") ?? "";
            string? descEn = ReadText(item["descriptionEn"], "descriptionEn");
            string? ipaUk = ReadText(item["ipaUk"], "ipaUk");
            string? ipaUs = ReadText(item["ipaUs"], "ipaUs");

            if (string.IsNullOrWhiteSpace(word) || string.IsNullOrWhiteSpace(defEn) || string.IsNullOrWhiteSpace(meanVi))
                throw new InvalidOperationException("Mỗi từ vựng phải có word, definitionEn và meaningVi không rỗng.");
            if (word.Length > 160 || defEn.Length > 1000 || meanVi.Length > 1000 || descEn?.Length > 1500)
                throw new InvalidOperationException("Nội dung từ vựng vượt quá độ dài cho phép (word: 160, definitionEn/meaningVi: 1000, descriptionEn: 1500).");

            var synonyms = ReadStringList(item["synonyms"]);
            var antonyms = ReadStringList(item["antonyms"]);

            var wordFamilies = new List<GeneratedWordFamilyRaw>();
            var wfArr = ReadArray(item["wordFamilies"], "wordFamilies");
            if (wfArr is not null)
            {
                foreach (var wf in wfArr)
                {
                    string rw = ReadText(wf?["relatedWord"], "relatedWord") ?? "";
                    string rpos = ReadText(wf?["partOfSpeech"], "partOfSpeech") ?? "noun";
                    if (!string.IsNullOrWhiteSpace(rw))
                    {
                        wordFamilies.Add(new GeneratedWordFamilyRaw(rw, rpos));
                    }
                }
            }

            var collocations = new List<GeneratedCollocationRaw>();
            var colArr = ReadArray(item["collocations"], "collocations");
            if (colArr is not null)
            {
                foreach (var c in colArr)
                {
                    string ph = ReadText(c?["phraseEn"], "phraseEn") ?? "";
                    string m = ReadText(c?["meaningVi"], "meaningVi") ?? "";
                    if (!string.IsNullOrWhiteSpace(ph))
                    {
                        collocations.Add(new GeneratedCollocationRaw(ph, m));
                    }
                }
            }

            var examples = new List<GeneratedExampleRaw>();
            var exArr = ReadArray(item["examples"], "examples");
            if (exArr is not null)
            {
                foreach (var ex in exArr)
                {
                    string senEn = ReadText(ex?["sentenceEn"], "sentenceEn") ?? "";
                    string senVi = ReadText(ex?["sentenceVi"], "sentenceVi") ?? "";
                    if (!string.IsNullOrWhiteSpace(senEn))
                    {
                        examples.Add(new GeneratedExampleRaw(senEn, senVi));
                    }
                }
            }

            var grammarPatterns = new List<GeneratedGrammarPatternRaw>();
            var gpArr = ReadArray(item["grammarPatterns"], "grammarPatterns");
            if (gpArr is not null)
            {
                foreach (var gp in gpArr)
                {
                    string pat = ReadText(gp?["pattern"], "pattern") ?? "";
                    string exp = ReadText(gp?["explanation"], "explanation") ?? "";
                    string exm = ReadText(gp?["example"], "example") ?? "";
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

        foreach (var sense in results)
        {
            CheckLength(sense.PartOfSpeech, 64, "partOfSpeech");
            CheckLength(sense.IpaUs, 160, "ipaUs");
            CheckLength(sense.IpaUk, 160, "ipaUk");
            foreach (var relatedWord in sense.Synonyms.Concat(sense.Antonyms))
                CheckLength(relatedWord, 160, "synonyms/antonyms");
            foreach (var family in sense.WordFamilies)
            {
                CheckLength(family.RelatedWord, 160, "wordFamilies.relatedWord");
                CheckLength(family.PartOfSpeech, 64, "wordFamilies.partOfSpeech");
            }
            foreach (var collocation in sense.Collocations)
            {
                CheckLength(collocation.PhraseEn, 300, "collocations.phraseEn");
                CheckLength(collocation.MeaningVi, 300, "collocations.meaningVi");
            }
            foreach (var example in sense.Examples)
            {
                CheckLength(example.SentenceEn, 600, "examples.sentenceEn");
                CheckLength(example.SentenceVi, 600, "examples.sentenceVi");
            }
            foreach (var pattern in sense.GrammarPatterns)
            {
                CheckLength(pattern.Pattern, 300, "grammarPatterns.pattern");
                CheckLength(pattern.Explanation, 600, "grammarPatterns.explanation");
                CheckLength(pattern.Example, 600, "grammarPatterns.example");
            }
        }
        return results;
    }

    private static void CheckLength(string? value, int maxLength, string field)
    {
        if (value?.Length > maxLength)
            throw new InvalidOperationException($"Trường '{field}' không được dài quá {maxLength} ký tự.");
    }

    private static JsonArray? ReadArray(JsonNode? node, string field)
    {
        if (node is null) return null;
        return node as JsonArray ?? throw new InvalidOperationException($"Trường '{field}' phải là một mảng JSON.");
    }

    private static string? ReadText(JsonNode? node, string field)
    {
        if (node is null) return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
            return text.Trim();
        throw new InvalidOperationException($"Trường '{field}' phải là một chuỗi.");
    }

    private static List<string> ReadStringList(JsonNode? node)
    {
        var list = new List<string>();
        var items = ReadArray(node, "synonyms/antonyms");
        if (items is null) return list;
        foreach (var item in items)
        {
            var text = ReadText(item, "synonyms/antonyms");
            if (!string.IsNullOrWhiteSpace(text)) list.Add(text);
        }
        return list;
    }

    private static string CleanJsonFence(string raw)
    {
        string text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            int firstNewline = text.IndexOf('\n');
            if (firstNewline < 0 || !text.EndsWith("```", StringComparison.Ordinal))
                throw new InvalidOperationException("Khối JSON Markdown phải có nội dung và đủ dấu ``` đóng/mở.");
            text = text[(firstNewline + 1)..^3];
        }
        return text.Trim();
    }
}
