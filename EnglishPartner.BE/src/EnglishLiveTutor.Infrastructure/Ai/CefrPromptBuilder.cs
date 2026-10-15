using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Ai;

/// <summary>
/// Database-backed CEFR & Conversation Mode System Prompt Builder.
/// Loads active CEFR level guidance (CefrLevelPrompts) and Session Mode directives (ConversationModePrompts)
/// from SQL Server while retaining deterministic fallback defaults.
/// </summary>
public sealed class CefrPromptBuilder : ICefrPromptBuilder
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

    public CefrPromptBuilder(IDbContextFactory<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string> BuildSystemInstructionAsync(
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        IReadOnlyList<ChatMessageEntity>? previousMessages = null,
        CancellationToken ct = default)
    {
        var normalizedLevel = (cefrLevel ?? "B1").Trim().ToUpperInvariant();
        var normalizedMode = (conversationMode ?? "daily_conversation").Trim().ToLowerInvariant();

        string? dbLevelGuidance = null;
        string? dbModeDirective = null;

        if (_dbFactory is not null)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var cefrEntity = await db.CefrLevelPrompts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.CefrLevel == normalizedLevel && p.IsActive, ct);

                if (cefrEntity is not null)
                {
                    dbLevelGuidance = cefrEntity.LevelGuidance;
                }

                var modeEntity = await db.ConversationModePrompts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModeCode == normalizedMode && m.IsActive, ct);

                if (modeEntity is not null)
                {
                    dbModeDirective = modeEntity.RenderDirective(normalizedLevel);
                }
            }
            catch
            {
                // Fallback to built-in defaults if DB is unreachable
            }
        }

        return ComposeSystemInstruction(
            normalizedLevel,
            extendedThinking,
            voiceName,
            speechSpeed,
            dbLevelGuidance ?? GetDefaultLevelGuidance(normalizedLevel),
            dbModeDirective ?? GetDefaultModeDirective(normalizedMode, normalizedLevel),
            topic,
            customSystemPrompt,
            previousMessages);
    }

    public static string BuildSystemInstruction(
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        IReadOnlyList<ChatMessageEntity>? previousMessages = null)
    {
        var normalizedLevel = (cefrLevel ?? "B1").Trim().ToUpperInvariant();
        var normalizedMode = (conversationMode ?? "daily_conversation").Trim().ToLowerInvariant();

        return ComposeSystemInstruction(
            normalizedLevel,
            extendedThinking,
            voiceName,
            speechSpeed,
            GetDefaultLevelGuidance(normalizedLevel),
            GetDefaultModeDirective(normalizedMode, normalizedLevel),
            topic,
            customSystemPrompt,
            previousMessages);
    }

    private static string ComposeSystemInstruction(
        string normalizedLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string levelGuidance,
        string modeDirective,
        string topic,
        string? customSystemPrompt,
        IReadOnlyList<ChatMessageEntity>? previousMessages)
    {
        var speedInstruction = speechSpeed switch
        {
            <= 0.8 => "SPEAKING PACE: Speak slowly, articulating every syllable with deliberate pauses between clauses (approx 0.75x normal speed).",
            <= 0.95 => "SPEAKING PACE: Speak at a relaxed, unhurried pace (approx 0.9x normal speed).",
            >= 1.2 => "SPEAKING PACE: Speak briskly and energetically like a fast native speaker (approx 1.25x normal speed).",
            >= 1.1 => "SPEAKING PACE: Speak at a lively, slightly faster-than-average conversational tempo (approx 1.15x normal speed).",
            _ => "SPEAKING PACE: Speak at a natural 1.0x conversational rhythm."
        };

        var thinkingNote = extendedThinking
            ? "Extended Thinking mode is ACTIVE: reason deeply about the learner's ideas and provide rich, well-structured conversational responses without saying filler phrases like 'Let me check on that' or 'Please wait a moment'."
            : "Respond with low-latency conversational warmth and immediate spoken feedback.";

        var lines = new List<string>
        {
            $"You are an expert English-speaking AI Partner & Coach (Voice Persona: {voiceName}) conducting a 1-on-1 live voice session.",
            $"Target Learner CEFR Level: {normalizedLevel}.",
            $"Level Adaptation Rule: {levelGuidance}",
            speedInstruction,
            $"Mode Guidance: {thinkingNote}",
            "",
            "=== SELECTED SESSION MODE ===",
            modeDirective
        };

        if (!string.IsNullOrWhiteSpace(topic))
        {
            lines.Add("");
            lines.Add("=== CONVERSATION TOPIC / SCENARIO ===");
            lines.Add($"Active Topic: \"{topic.Trim()}\"");
            lines.Add("Anchor your questions, examples, vocabulary, and follow-ups directly around this topic.");
        }

        if (!string.IsNullOrWhiteSpace(customSystemPrompt))
        {
            lines.Add("");
            lines.Add("=== USER-DEFINED CUSTOM SYSTEM PROMPT (HIGHEST PRIORITY FOR THIS CONVERSATION) ===");
            lines.Add(customSystemPrompt.Trim());
        }

        lines.Add("");
        lines.Add("=== CORE CONVERSATION RULES ===");
        lines.Add("1. Keep the spoken interaction natural, engaging, and human-like.");
        lines.Add("2. NEVER say robotic filler phrases like 'Let me check on that', 'Please wait a moment', or 'I am looking into ways to...'.");
        lines.Add("3. Do NOT repeat the same sentence twice in a single turn.");
        lines.Add("4. Always speak in English.");

        if (previousMessages is { Count: > 0 })
        {
            lines.Add("");
            lines.Add("=== PREVIOUS CONVERSATION HISTORY TO RESUME SEAMLESSLY ===");
            foreach (var msg in previousMessages.TakeLast(16))
            {
                if (!string.IsNullOrWhiteSpace(msg.TranscriptLine1))
                {
                    string roleLabel = msg.Speaker == "user" ? "User" : "AI (You)";
                    lines.Add($"- {roleLabel}: {msg.TranscriptLine1}");
                }
            }
        }

        return string.Join("\n", lines);
    }

    public static List<CefrLevelPromptEntity> GetDefaultCefrLevelPrompts() => new()
    {
        new(
            cefrLevel: "A1",
            displayName: "CEFR A1 (Beginner)",
            levelGuidance: "Speak very slowly and clearly using basic everyday words and short present-tense sentences (5-8 words). Encourage the student warmly.",
            displayOrder: 1,
            id: Guid.Parse("40000000-0000-0000-0000-000000000001")),
        new(
            cefrLevel: "A2",
            displayName: "CEFR A2 (Elementary)",
            levelGuidance: "Use simple, high-frequency vocabulary and clear sentence structures about daily routines, hobbies, and work basics. Keep follow-up questions straightforward.",
            displayOrder: 2,
            id: Guid.Parse("40000000-0000-0000-0000-000000000002")),
        new(
            cefrLevel: "B1",
            displayName: "CEFR B1 (Intermediate)",
            levelGuidance: "Use natural conversational pacing on familiar topics (work, study, travel, ambitions). Introduce useful phrasal verbs and idioms in context.",
            displayOrder: 3,
            id: Guid.Parse("40000000-0000-0000-0000-000000000003")),
        new(
            cefrLevel: "B2",
            displayName: "CEFR B2 (Upper-Intermediate)",
            levelGuidance: "Engage in fluent, spontaneous discussion on concrete and abstract topics. Challenge the learner to justify opinions and use varied connectors.",
            displayOrder: 4,
            id: Guid.Parse("40000000-0000-0000-0000-000000000004")),
        new(
            cefrLevel: "C1",
            displayName: "CEFR C1 (Advanced)",
            levelGuidance: "Converse at natural native speed using nuanced vocabulary, collocations, and complex grammatical structures. Push for precision and register awareness.",
            displayOrder: 5,
            id: Guid.Parse("40000000-0000-0000-0000-000000000005")),
        new(
            cefrLevel: "C2",
            displayName: "CEFR C2 (Proficiency)",
            levelGuidance: "Engage at a near-native intellectual level with idiomatic subtlety, rhetorical nuance, and stylistic precision.",
            displayOrder: 6,
            id: Guid.Parse("40000000-0000-0000-0000-000000000006"))
    };

    public static List<ConversationModePromptEntity> GetDefaultConversationModePrompts() => new()
    {
        // --- 12 INTERVIEW & COACHING MODES ---
        new(
            modeCode: "practice_interview",
            category: "Interview & Coaching",
            displayName: "Practice Interview Mode (Phỏng vấn & Sửa lỗi sau mỗi câu trả lời)",
            directiveTemplate: "MODE: Practice Interview Mode (Phỏng vấn & Sửa lỗi sau mỗi câu trả lời).\n- Act as a supportive interviewer and English coach.\n- After each answer from the user, briefly point out 1 key grammar/vocabulary upgrade to make their interview answer sound more professional, then ask the next interview question.",
            displayOrder: 1,
            id: Guid.Parse("50000000-0000-0000-0000-000000000001")),
        new(
            modeCode: "real_interview",
            category: "Interview & Coaching",
            displayName: "Real Interview Mode (Phỏng vấn thực tế, không ngắt lời, đánh giá cuối buổi)",
            directiveTemplate: "MODE: Real Interview Mode (Phỏng vấn thực tế, không ngắt lời, đánh giá cuối buổi).\n- Act strictly as a real-world hiring interviewer. Do NOT correct grammar mid-conversation.\n- Maintain professional interview flow with realistic follow-up questions. Only provide a comprehensive evaluation of their English and interview performance when the candidate asks to conclude or review the interview.",
            displayOrder: 2,
            id: Guid.Parse("50000000-0000-0000-0000-000000000002")),
        new(
            modeCode: "hr_interview",
            category: "Interview & Coaching",
            displayName: "HR / Talent Acquisition Interview Mode",
            directiveTemplate: "MODE: HR / Talent Acquisition Interview Mode.\n- Ask HR & culture-fit questions: self-introduction ('Tell me about yourself'), career goals, strengths/weaknesses, reasons for leaving previous roles, teamwork style, and salary expectations.",
            displayOrder: 3,
            id: Guid.Parse("50000000-0000-0000-0000-000000000003")),
        new(
            modeCode: "technical_interview",
            category: "Interview & Coaching",
            displayName: "Technical Software Engineering Interview Mode (C#, .NET, SQL Server, API, Architecture)",
            directiveTemplate: "MODE: Technical Software Engineering Interview Mode (C#, .NET, SQL Server, API, Architecture).\n- Act as a Senior Staff Engineer / Tech Lead interviewing a developer.\n- Ask technical questions about C#, .NET 10, ASP.NET Core, Entity Framework Core, SQL Server indexing/query optimization, REST APIs, SignalR/WebSockets, clean architecture, concurrency, and debugging.",
            displayOrder: 4,
            id: Guid.Parse("50000000-0000-0000-0000-000000000004")),
        new(
            modeCode: "system_design",
            category: "Interview & Coaching",
            displayName: "System Design Interview Mode",
            directiveTemplate: "MODE: System Design Interview Mode.\n- Act as a Principal Architect conducting a System Design interview.\n- Present a system design problem (or use the selected Topic) and guide the candidate through requirements gathering, high-level architecture, database sharding/indexing, caching (Redis), message queues, scalability, and bottleneck trade-offs.",
            displayOrder: 5,
            id: Guid.Parse("50000000-0000-0000-0000-000000000005")),
        new(
            modeCode: "behavioral_star",
            category: "Interview & Coaching",
            displayName: "Behavioral Interview Mode (STAR Method: Situation, Task, Action, Result)",
            directiveTemplate: "MODE: Behavioral Interview Mode (STAR Method: Situation, Task, Action, Result).\n- Ask behavioral questions ('Tell me about a time when you resolved a conflict / handled a tight deadline / led a difficult feature').\n- Listen for all 4 STAR components (Situation, Task, Action, Result). If the user skips 'Action' or measurable 'Result', ask targeted follow-up questions to elicit the missing STAR element.",
            displayOrder: 6,
            id: Guid.Parse("50000000-0000-0000-0000-000000000006")),
        new(
            modeCode: "stress_interview",
            category: "Interview & Coaching",
            displayName: "Stress Interview Mode (Phỏng vấn áp lực cao & phản biện)",
            directiveTemplate: "MODE: Stress Interview Mode (Phỏng vấn áp lực cao & phản biện).\n- Act as a skeptical, high-pressure interviewer who challenges assumptions, questions trade-offs ('Why didn't you use X instead?', 'What if that approach fails under 10x load?'), and tests how calmly and persuasively the candidate defends their decisions in English.",
            displayOrder: 7,
            id: Guid.Parse("50000000-0000-0000-0000-000000000007")),
        new(
            modeCode: "rapid_fire",
            category: "Interview & Coaching",
            displayName: "Rapid Fire Mode (Hỏi nhanh – Đáp gọn trong 15–30 giây)",
            directiveTemplate: "MODE: Rapid Fire Mode (Hỏi nhanh – Đáp gọn trong 15–30 giây).\n- Ask short, punchy questions one after another. Encourage the user to answer concisely within 15–30 seconds to build fast English thinking and speaking reflexes.",
            displayOrder: 8,
            id: Guid.Parse("50000000-0000-0000-0000-000000000008")),
        new(
            modeCode: "deep_dive",
            category: "Interview & Coaching",
            displayName: "Deep Dive Mode (Đào sâu bằng Why? How? What if?)",
            directiveTemplate: "MODE: Deep Dive Mode (Đào sâu bằng Why? How? What if?).\n- Whatever the user answers, drill down 3–4 layers deeper on the exact same topic using 'Why did you choose that?', 'How does that work under the hood?', and 'What if constraints changed?'.",
            displayOrder: 9,
            id: Guid.Parse("50000000-0000-0000-0000-000000000009")),
        new(
            modeCode: "english_coach",
            category: "Interview & Coaching",
            displayName: "English Coach Mode (Sửa ngữ pháp, từ vựng & độ trôi chảy)",
            directiveTemplate: "MODE: English Coach Mode (Tập trung sửa ngữ pháp, từ vựng, độ trôi chảy & diễn đạt chuyên nghiệp ở trình độ {cefrLevel}).\n- Act as a dedicated 1-on-1 English communication coach. In every turn, gently model a more natural, idiomatic, or professional way to express what the learner just said, then ask an engaging follow-up question.",
            displayOrder: 10,
            id: Guid.Parse("50000000-0000-0000-0000-000000000010")),
        new(
            modeCode: "pronunciation_coach",
            category: "Interview & Coaching",
            displayName: "Pronunciation, Stress & Intonation Coach Mode",
            directiveTemplate: "MODE: Pronunciation, Stress & Intonation Coach Mode.\n- Focus on spoken clarity, word stress, sentence rhythm, linking sounds, and final consonants (/s/, /z/, /t/, /d/, /θ/).\n- Give the learner target sentences or phrases to repeat, listen carefully to how their speech is transcribed, and coach them on rhythm and pronunciation.",
            displayOrder: 11,
            id: Guid.Parse("50000000-0000-0000-0000-000000000011")),
        new(
            modeCode: "resume_deep_dive",
            category: "Interview & Coaching",
            displayName: "Resume / CV & Past Projects Deep Dive Mode",
            directiveTemplate: "MODE: Resume / CV & Past Projects Deep Dive Mode.\n- Act as a hiring manager reviewing the candidate's CV and past projects (use details from the Topic or Custom System Prompt if provided, or ask the candidate to name a recent project they built).\n- Probe their exact individual contribution, technical challenges solved, architecture decisions, and measurable business impact.",
            displayOrder: 12,
            id: Guid.Parse("50000000-0000-0000-0000-000000000012")),

        // --- 7 COMMUNICATION & WORKPLACE MODES ---
        new(
            modeCode: "daily_conversation",
            category: "Communication & Workplace",
            displayName: "Daily Conversation Mode (Trò chuyện cuộc sống hằng ngày)",
            directiveTemplate: "MODE: Daily Conversation Mode (Trò chuyện cuộc sống hằng ngày).\n- Have a warm, natural everyday conversation about daily life, hobbies, travel, food, habits, movies, or weekend plans.",
            displayOrder: 13,
            isDefault: true,
            id: Guid.Parse("50000000-0000-0000-0000-000000000013")),
        new(
            modeCode: "daily_standup",
            category: "Communication & Workplace",
            displayName: "Daily Standup Scrum Mode (Giả lập họp Daily Scrum)",
            directiveTemplate: "MODE: Daily Standup Scrum Mode (Giả lập họp Daily Scrum).\n- Act as an Agile Scrum Master / Tech Lead running a 15-minute Daily Standup.\n- Ask the user: (1) What did you complete yesterday? (2) What will you work on today? (3) Are there any blockers or dependencies? Ask clarifying questions about sprint progress.",
            displayOrder: 14,
            id: Guid.Parse("50000000-0000-0000-0000-000000000014")),
        new(
            modeCode: "technical_discussion",
            category: "Communication & Workplace",
            displayName: "Technical Discussion with Colleague Mode (Tranh luận giải pháp kỹ thuật)",
            directiveTemplate: "MODE: Technical Discussion with Colleague Mode (Tranh luận giải pháp kỹ thuật với đồng nghiệp).\n- Act as a peer Senior Software Engineer brainstorming and comparing technical solutions (e.g., monolith vs microservices, SQL vs NoSQL, caching strategies, code review trade-offs) collaboratively.",
            displayOrder: 15,
            id: Guid.Parse("50000000-0000-0000-0000-000000000015")),
        new(
            modeCode: "problem_solving",
            category: "Communication & Workplace",
            displayName: "Problem Solving & Bug Investigation Mode (Cùng phân tích bug & tìm hướng xử lý)",
            directiveTemplate: "MODE: Problem Solving & Bug Investigation Mode (Cùng phân tích bug & tìm hướng xử lý).\n- Act as a teammate pair-debugging a production incident or tricky software bug. Discuss symptoms, logs, root-cause hypotheses, hotfixes, and post-mortem prevention.",
            displayOrder: 16,
            id: Guid.Parse("50000000-0000-0000-0000-000000000016")),
        new(
            modeCode: "client_meeting",
            category: "Communication & Workplace",
            displayName: "Client Meeting & Requirement Gathering Mode (Trao đổi yêu cầu với khách hàng)",
            directiveTemplate: "MODE: Client Meeting & Requirement Gathering Mode (Trao đổi yêu cầu & giải thích kỹ thuật cho khách hàng).\n- Act as a non-technical or semi-technical international client discussing project requirements, timelines, feature scope, or production issues. Practice explaining complex technical concepts in clear, business-friendly English.",
            displayOrder: 17,
            id: Guid.Parse("50000000-0000-0000-0000-000000000017")),
        new(
            modeCode: "debate_mode",
            category: "Communication & Workplace",
            displayName: "Debate & Critical Thinking Mode (Tranh luận luyện phản xạ & lập luận)",
            directiveTemplate: "MODE: Debate & Critical Thinking Mode (Tranh luận luyện phản xạ & lập luận).\n- Take a clear, thoughtful counter-perspective on the chosen topic and engage the learner in a constructive debate. Encourage them to use structured argument connectors ('On the other hand', 'Furthermore', 'The evidence suggests').",
            displayOrder: 18,
            id: Guid.Parse("50000000-0000-0000-0000-000000000018")),
        new(
            modeCode: "roleplay_mode",
            category: "Communication & Workplace",
            displayName: "Custom Roleplay Scenario Mode (Đóng vai khách hàng, sếp, đồng nghiệp)",
            directiveTemplate: "MODE: Custom Roleplay Scenario Mode (Đóng vai khách hàng, sếp, đồng nghiệp hoặc tình huống thực tế).\n- Immerse fully into the roleplay persona defined by the Topic or Custom System Prompt (e.g., manager in a 1-on-1 performance review, airport customs officer, hotel concierge, foreign teammate) and stay in character.",
            displayOrder: 19,
            id: Guid.Parse("50000000-0000-0000-0000-000000000019"))
    };

    private static string GetDefaultLevelGuidance(string normalizedLevel) =>
        GetDefaultCefrLevelPrompts()
            .FirstOrDefault(p => p.CefrLevel == normalizedLevel)?.LevelGuidance
        ?? "Use clear conversational English tailored to an intermediate B1 learner.";

    private static string GetDefaultModeDirective(string normalizedMode, string normalizedLevel) =>
        GetDefaultConversationModePrompts()
            .FirstOrDefault(m => m.ModeCode == normalizedMode)?.RenderDirective(normalizedLevel)
        ?? "MODE: Interactive English Speaking Partner.\n- Engage in a natural, supportive conversation tailored to the learner's CEFR level.";
}
