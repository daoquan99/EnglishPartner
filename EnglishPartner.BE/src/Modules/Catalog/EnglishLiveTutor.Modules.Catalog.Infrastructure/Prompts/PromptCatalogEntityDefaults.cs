using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Domain.Prompts;

namespace EnglishLiveTutor.Application.Features.Prompts.Services;

public static class PromptCatalogEntityDefaults
{
    public static List<CefrLevelPromptEntity> GetDefaultCefrLevelPrompts() => new()
    {
        new(
            cefrLevel: "A1",
            displayName: "CEFR A1 (Beginner)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("A1"),
            displayOrder: 1,
            id: Guid.Parse("40000000-0000-0000-0000-000000000001")),
        new(
            cefrLevel: "A2",
            displayName: "CEFR A2 (Elementary)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("A2"),
            displayOrder: 2,
            id: Guid.Parse("40000000-0000-0000-0000-000000000002")),
        new(
            cefrLevel: "B1",
            displayName: "CEFR B1 (Intermediate)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("B1"),
            displayOrder: 3,
            id: Guid.Parse("40000000-0000-0000-0000-000000000003")),
        new(
            cefrLevel: "B2",
            displayName: "CEFR B2 (Upper-Intermediate)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("B2"),
            displayOrder: 4,
            id: Guid.Parse("40000000-0000-0000-0000-000000000004")),
        new(
            cefrLevel: "C1",
            displayName: "CEFR C1 (Advanced)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("C1"),
            displayOrder: 5,
            id: Guid.Parse("40000000-0000-0000-0000-000000000005")),
        new(
            cefrLevel: "C2",
            displayName: "CEFR C2 (Proficiency)",
            levelGuidance: PromptCatalogDefaults.GetLevelGuidance("C2"),
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
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("practice_interview"),
            displayOrder: 1,
            id: Guid.Parse("50000000-0000-0000-0000-000000000001")),
        new(
            modeCode: "real_interview",
            category: "Interview & Coaching",
            displayName: "Real Interview Mode (Phỏng vấn thực tế, không ngắt lời, đánh giá cuối buổi)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("real_interview"),
            displayOrder: 2,
            id: Guid.Parse("50000000-0000-0000-0000-000000000002")),
        new(
            modeCode: "hr_interview",
            category: "Interview & Coaching",
            displayName: "HR / Talent Acquisition Interview Mode",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("hr_interview"),
            displayOrder: 3,
            id: Guid.Parse("50000000-0000-0000-0000-000000000003")),
        new(
            modeCode: "technical_interview",
            category: "Interview & Coaching",
            displayName: "Technical Software Engineering Interview Mode (C#, .NET, SQL Server, API, Architecture)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("technical_interview"),
            displayOrder: 4,
            id: Guid.Parse("50000000-0000-0000-0000-000000000004")),
        new(
            modeCode: "system_design",
            category: "Interview & Coaching",
            displayName: "System Design Interview Mode",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("system_design"),
            displayOrder: 5,
            id: Guid.Parse("50000000-0000-0000-0000-000000000005")),
        new(
            modeCode: "behavioral_star",
            category: "Interview & Coaching",
            displayName: "Behavioral Interview Mode (STAR Method: Situation, Task, Action, Result)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("behavioral_star"),
            displayOrder: 6,
            id: Guid.Parse("50000000-0000-0000-0000-000000000006")),
        new(
            modeCode: "stress_interview",
            category: "Interview & Coaching",
            displayName: "Stress Interview Mode (Phỏng vấn áp lực cao & phản biện)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("stress_interview"),
            displayOrder: 7,
            id: Guid.Parse("50000000-0000-0000-0000-000000000007")),
        new(
            modeCode: "rapid_fire",
            category: "Interview & Coaching",
            displayName: "Rapid Fire Mode (Hỏi nhanh – Đáp gọn trong 15–30 giây)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("rapid_fire"),
            displayOrder: 8,
            id: Guid.Parse("50000000-0000-0000-0000-000000000008")),
        new(
            modeCode: "deep_dive",
            category: "Interview & Coaching",
            displayName: "Deep Dive Mode (Đào sâu bằng Why? How? What if?)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("deep_dive"),
            displayOrder: 9,
            id: Guid.Parse("50000000-0000-0000-0000-000000000009")),
        new(
            modeCode: "english_coach",
            category: "Interview & Coaching",
            displayName: "English Coach Mode (Sửa ngữ pháp, từ vựng & độ trôi chảy)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("english_coach"),
            displayOrder: 10,
            id: Guid.Parse("50000000-0000-0000-0000-000000000010")),
        new(
            modeCode: "pronunciation_coach",
            category: "Interview & Coaching",
            displayName: "Pronunciation, Stress & Intonation Coach Mode",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("pronunciation_coach"),
            displayOrder: 11,
            id: Guid.Parse("50000000-0000-0000-0000-000000000011")),
        new(
            modeCode: "resume_deep_dive",
            category: "Interview & Coaching",
            displayName: "Resume / CV & Past Projects Deep Dive Mode",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("resume_deep_dive"),
            displayOrder: 12,
            id: Guid.Parse("50000000-0000-0000-0000-000000000012")),

        // --- 7 COMMUNICATION & WORKPLACE MODES ---
        new(
            modeCode: "daily_conversation",
            category: "Communication & Workplace",
            displayName: "Daily Conversation Mode (Trò chuyện cuộc sống hằng ngày)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("daily_conversation"),
            displayOrder: 13,
            isDefault: true,
            id: Guid.Parse("50000000-0000-0000-0000-000000000013")),
        new(
            modeCode: "daily_standup",
            category: "Communication & Workplace",
            displayName: "Daily Standup Scrum Mode (Giả lập họp Daily Scrum)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("daily_standup"),
            displayOrder: 14,
            id: Guid.Parse("50000000-0000-0000-0000-000000000014")),
        new(
            modeCode: "technical_discussion",
            category: "Communication & Workplace",
            displayName: "Technical Discussion with Colleague Mode (Tranh luận giải pháp kỹ thuật)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("technical_discussion"),
            displayOrder: 15,
            id: Guid.Parse("50000000-0000-0000-0000-000000000015")),
        new(
            modeCode: "problem_solving",
            category: "Communication & Workplace",
            displayName: "Problem Solving & Bug Investigation Mode (Cùng phân tích bug & tìm hướng xử lý)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("problem_solving"),
            displayOrder: 16,
            id: Guid.Parse("50000000-0000-0000-0000-000000000016")),
        new(
            modeCode: "client_meeting",
            category: "Communication & Workplace",
            displayName: "Client Meeting & Requirement Gathering Mode (Trao đổi yêu cầu với khách hàng)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("client_meeting"),
            displayOrder: 17,
            id: Guid.Parse("50000000-0000-0000-0000-000000000017")),
        new(
            modeCode: "debate_mode",
            category: "Communication & Workplace",
            displayName: "Debate & Critical Thinking Mode (Tranh luận luyện phản xạ & lập luận)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("debate_mode"),
            displayOrder: 18,
            id: Guid.Parse("50000000-0000-0000-0000-000000000018")),
        new(
            modeCode: "roleplay_mode",
            category: "Communication & Workplace",
            displayName: "Custom Roleplay Scenario Mode (Đóng vai khách hàng, sếp, đồng nghiệp)",
            directiveTemplate: PromptCatalogDefaults.GetModeDirectiveTemplate("roleplay_mode"),
            displayOrder: 19,
            id: Guid.Parse("50000000-0000-0000-0000-000000000019"))
    };
}
