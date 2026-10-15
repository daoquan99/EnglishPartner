using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Infrastructure.Realtime;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Analyzes learner utterances AND extracts spoken teacher corrections for the Right Panel
/// ("Sửa Lỗi Chính Tả, Từ Vựng & Ngữ Pháp").
/// NEVER modifies or replaces TranscriptLine1 in the live conversation chat feed!
/// </summary>
public sealed class GrammarCorrectionAnalyzerService
{
    private readonly HttpClient _http;
    private readonly FastTextTranslatorFallback _translator;

    public GrammarCorrectionAnalyzerService(HttpClient http, FastTextTranslatorFallback translator)
    {
        _http = http;
        _translator = translator;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Extracts a correction card if the AI English Teacher explicitly points out a correction
    /// in its spoken response (e.g. 'Just a small correction: be sure to say "new places"' or
    /// 'Just try saying "places" to make it plural' as seen in Screenshots 1 & 2).
    /// </summary>
    public LanguageCorrectionDto? TryExtractCorrectionFromTeacherReply(
        string conversationId,
        string turnId,
        string? lastUserUtterance,
        string aiTeacherReply,
        string cefrLevel)
    {
        if (string.IsNullOrWhiteSpace(aiTeacherReply))
            return null;

        // Look for patterns where the teacher quotes a correction:
        // e.g. be sure to say "new places", try saying "places", say "an idea" instead
        var match = Regex.Match(
            aiTeacherReply,
            @"(?:correction|try saying|be sure to say|remember to say|instead of|should be|make it plural)[^""']*?[""']([^""']+)[""']",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string quotedFix = match.Groups[1].Value.Trim();
            string original = !string.IsNullOrWhiteSpace(lastUserUtterance)
                ? lastUserUtterance.Trim()
                : "Câu nói trước của bạn";

            return new LanguageCorrectionDto(
                Id: Guid.NewGuid().ToString("N")[..12],
                ConversationId: conversationId,
                TurnId: turnId,
                OriginalText: original,
                CorrectedText: quotedFix,
                ErrorCategory: "Teacher Live Correction",
                ExplanationVi: $"Giáo viên AI vừa nhắc bạn chỉnh lại thành \"{quotedFix}\" (lưu ý danh từ số nhiều / cấu trúc tự nhiên trong câu).",
                CefrTip: $"[{cefrLevel}] Lặp lại to cụm \"{quotedFix}\" theo hướng dẫn của giáo viên để rèn phản xạ.",
                Timestamp: DateTimeOffset.UtcNow
            );
        }

        return null;
    }

    public async Task<LanguageCorrectionDto?> AnalyzeUserUtteranceAsync(
        string apiKey,
        string conversationId,
        string turnId,
        string userTranscript,
        string cefrLevel,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userTranscript))
            return null;

        string cleaned = SmartTextDeduplicator.CollapseNearDuplicateSentences(userTranscript.Trim());
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return null;

        // 1. Fast deterministic rule check (catches "to new place", "I enjoy traveling. to new place", "a idea", etc. in 0ms)
        var ruleHit = DetectCommonMistakesFallback(conversationId, turnId, cleaned, cefrLevel);
        if (ruleHit is not null)
            return ruleHit;

        // 2. Query auto-discovered Google AI Studio Gemini models
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var models = await _translator.GetAvailableGeminiModelsAsync(apiKey, ct);
            foreach (string model in models.Take(3))
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";
                    string prompt = string.Join("\n", new[]
                    {
                        $"You are an expert English teacher evaluating a spoken utterance from a Vietnamese learner (CEFR Level: {cefrLevel}).",
                        $"Student utterance: \"{cleaned}\"",
                        "",
                        "If there is ANY grammar error, missing plural -s (e.g. 'to new place' -> 'to new places'), missing article, wrong tense, wrong preposition, unnatural punctuation/fragment, or word choice issue, return a JSON correction card.",
                        "Even if the grammar is okay, provide a 'Speaking Upgrade' suggestion so the learner gets helpful feedback in the correction panel.",
                        "Respond ONLY with a valid JSON object (no markdown fences):",
                        "{",
                        "  \"originalText\": \"exact faulty phrase or sentence from the student\",",
                        "  \"correctedText\": \"natural, grammatically correct English version\",",
                        "  \"errorCategory\": \"Grammar | Plural & Article | Word Choice | Speaking Upgrade\",",
                        "  \"explanationVi\": \"Giải thích chi tiết, dễ hiểu bằng tiếng Việt\",",
                        $"  \"cefrTip\": \"Mẹo luyện nói thực tế bằng tiếng Việt cho trình độ {cefrLevel}\"",
                        "}"
                    });

                    var payload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role = "user",
                                parts = new[] { new { text = prompt } }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = 0.15
                        }
                    };

                    using var resp = await _http.PostAsJsonAsync(url, payload, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        var root = await resp.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
                        string? rawJson = root?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()?.Trim();
                        if (!string.IsNullOrWhiteSpace(rawJson))
                        {
                            int startBrace = rawJson.IndexOf('{');
                            int endBrace = rawJson.LastIndexOf('}');
                            if (startBrace >= 0 && endBrace > startBrace)
                            {
                                rawJson = rawJson[startBrace..(endBrace + 1)];
                                var parsed = JsonNode.Parse(rawJson);
                                string orig = parsed?["originalText"]?.GetValue<string>()?.Trim() ?? cleaned;
                                string corr = parsed?["correctedText"]?.GetValue<string>()?.Trim() ?? "";

                                if (!string.IsNullOrWhiteSpace(corr))
                                {
                                    return new LanguageCorrectionDto(
                                        Id: Guid.NewGuid().ToString("N")[..12],
                                        ConversationId: conversationId,
                                        TurnId: turnId,
                                        OriginalText: orig,
                                        CorrectedText: corr,
                                        ErrorCategory: parsed?["errorCategory"]?.GetValue<string>() ?? "Grammar & Expression",
                                        ExplanationVi: parsed?["explanationVi"]?.GetValue<string>() ?? "Câu nói đã được phân tích và gợi ý cách diễn đạt chuẩn xác hơn.",
                                        CefrTip: parsed?["cefrTip"]?.GetValue<string>() ?? $"[{cefrLevel}] Đọc to câu gợi ý 2 lần để ghi nhớ phản xạ.",
                                        Timestamp: DateTimeOffset.UtcNow
                                    );
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // 3. Guaranteed Speaking Feedback Card for any 3+ word utterance
        if (words.Length >= 3)
        {
            return new LanguageCorrectionDto(
                Id: Guid.NewGuid().ToString("N")[..12],
                ConversationId: conversationId,
                TurnId: turnId,
                OriginalText: cleaned,
                CorrectedText: PolishSpokenSentence(cleaned),
                ErrorCategory: "Speaking & Fluency Feedback",
                ExplanationVi: "Câu nói của bạn có cấu trúc tốt! Hãy chú ý nối âm cuối (ending sounds /s/, /t/, /d/) và giữ nhịp điệu liền mạch không ngắt quãng giữa câu.",
                CefrTip: $"[{cefrLevel}] Mở rộng câu bằng cách thêm lý do 'because...' hoặc ví dụ 'for example...'.",
                Timestamp: DateTimeOffset.UtcNow
            );
        }

        return null;
    }

    private static string PolishSpokenSentence(string input)
    {
        string s = Regex.Replace(input, @"\b(\w+)(\s+\1\b)+", "$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\.\s+([a-z])", " $1"); // Fix mid-sentence period like "I enjoy traveling. to new place"
        s = Regex.Replace(s, @"\bto\s+new\s+place\b", "to new places", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\ba\s+([aeiouAEIOU]\w+)\b", "an $1");
        return s.Trim();
    }

    private static LanguageCorrectionDto? DetectCommonMistakesFallback(
        string conversationId,
        string turnId,
        string text,
        string cefrLevel)
    {
        var rules = new (string Pattern, string Replacement, string Category, string ExplanationVi, string Tip)[]
        {
            (@"\bto\s+new\s+place\b", "to new places", "Grammar (Plural Noun)",
                "Khi nói chung về việc đi du lịch đến \"những vùng đất/địa điểm mới\", danh từ đếm được \"place\" phải ở dạng số nhiều là \"new places\" (hoặc \"a new place\").",
                "Chú ý phát âm rõ âm đuôi /ɪz/ trong 'places' (/ˈpleɪ.sɪz/)."),
            (@"\b(traveling|taking\s+many\s+photos)\.\s+(to\b)", "$1 $2", "Fluency (Mid-Sentence Pause)",
                "Bạn bị ngắt nhịp quá lâu giữa câu khiến câu bị tách thành 2 mệnh đề rời rạc. Hãy đọc liền mạch cụm động từ và bổ ngữ chỉ mục đích/nơi chốn.",
                "Luyện đọc liền hơi cả cụm: 'I enjoy traveling to new places' và 'take many photos to remember my trips'."),
            (@"\ba\s+([aeiouAEIOU]\w+)\b", "an $1", "Grammar (Article a/an)",
                "Trước danh từ/tính từ bắt đầu bằng nguyên âm (u, e, o, a, i), ta phải dùng mạo từ \"an\" thay vì \"a\".",
                "Luôn nối âm 'an + nguyên âm' khi nói (ví dụ: an_idea, an_apple)."),
            (@"\bdiscussed\s+about\b", "discussed", "Grammar & Collocation",
                "Động từ \"discuss\" là ngoại động từ, đi trực tiếp với tân ngữ mà không dùng giới từ \"about\".",
                "Ghi nhớ: discuss something = talk about something."),
            (@"\bmanager\s+(?:and\s+manager\s+)?high\s+traffic\s+loading\b", "managed high traffic loads", "Word Choice & Tense",
                "\"Manager\" là danh từ (người quản lý). Để diễn đạt hành động \"đã quản lý tải lưu lượng cao\", cần dùng động từ quá khứ \"managed high traffic loads\".",
                "Phân biệt rõ động từ 'manage' (quản lý) và danh từ 'manager' (người quản lý)."),
            (@"\bpretty\s+my\s+neck\b", "practicing my speaking", "Pronunciation & Word Choice",
                "Cụm \"pretty my neck\" là lỗi phát âm khiến máy nhận diện nhầm. Hãy phát âm rõ âm cuối khi nói \"practicing my English / speaking\".",
                "Chú ý trọng âm đầu của từ PRAC-ti-cing (/ˈpræk.tɪ.sɪŋ/)."),
            (@"\btoo\s+many\s+informations\b", "too much information", "Uncountable Noun",
                "\"Information\" là danh từ không đếm được nên không thêm 's' và đi với \"too much\".",
                "Các danh từ không đếm được cần nhớ: information, advice, equipment, feedback."),
            (@"\bfor\s+(buy|reduce|improve|make|get|learn|practice|fix|solve|manage|recieve|receive|remember)\b", "to $1", "Grammar (Purpose 'to + V')",
                "Để chỉ mục đích \"để làm gì\", tiếng Anh dùng cấu trúc \"to + V nguyên mẫu\", không dùng \"for + V nguyên mẫu\".",
                "Dùng 'to + Verb' (ví dụ: to remember my trips, to improve my English)."),
            (@"\b(two|three|four|five|many|several|different)\s+(time|night|day|year|month|week|problem|bug|query|place|photo|trip)\b(?!s)", "$1 $2s", "Grammar (Plural Noun)",
                "Sau số đếm từ 2 trở lên hoặc 'many / several / different', danh từ đếm được phải ở dạng số nhiều (thêm -s/-es).",
                "Đừng quên bật nhẹ âm /-s/, /-z/ hoặc /-ɪz/ ở cuối danh từ số nhiều khi nói."),
            (@"\b(\w+)(\s+\1\b)+", "$1", "Fluency & Self-Correction",
                "Bạn bị lặp từ khi đang suy nghĩ câu nói. Hãy giữ nhịp thở đều và nói liền mạch cụm từ đã hoàn chỉnh.",
                "Mẹo phản xạ: Thay vì lặp lại từ đầu câu, hãy tạm dừng 0.5 giây để chọn từ chính xác.")
        };

        foreach (var rule in rules)
        {
            var match = Regex.Match(text, rule.Pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string corrected = PolishSpokenSentence(Regex.Replace(text, rule.Pattern, rule.Replacement, RegexOptions.IgnoreCase));
                return new LanguageCorrectionDto(
                    Id: Guid.NewGuid().ToString("N")[..12],
                    ConversationId: conversationId,
                    TurnId: turnId,
                    OriginalText: text,
                    CorrectedText: corrected,
                    ErrorCategory: rule.Category,
                    ExplanationVi: rule.ExplanationVi,
                    CefrTip: $"[{cefrLevel}] {rule.Tip}",
                    Timestamp: DateTimeOffset.UtcNow
                );
            }
        }

        return null;
    }
}
