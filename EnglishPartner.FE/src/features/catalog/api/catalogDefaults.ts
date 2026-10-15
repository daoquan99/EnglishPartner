import type {
  AiModelOptionDto,
  CefrLevelPromptDto,
  ConversationModePromptDto,
  ScoringRubricDto,
  SpeechSpeedOptionDto,
  TeacherVoiceDto,
  TopicDto,
} from '@/shared/api/openapi.types';

/**
 * Fallback Seed Catalogs mirroring Backend DatabaseSeeder.cs so the UI renders
 * immediately even before SQL Server is started, while seamlessly syncing with
 * real Database data via TanStack Query when Backend is online.
 */
export const DEFAULT_SPEECH_SPEEDS: SpeechSpeedOptionDto[] = [
  {
    value: 0.75,
    label: '0.75x — Chậm & Rõ từng từ',
    descriptionVi: 'Phù hợp cho người mới bắt đầu hoặc nghe từ mới khó',
    recommendedCefrRange: 'A1 - A2',
    isDefault: false,
  },
  {
    value: 0.9,
    label: '0.90x — Vừa phải, dễ bắt kịp',
    descriptionVi: 'Nhịp độ sư phạm rõ ràng, dễ nghe trọng âm và nối âm',
    recommendedCefrRange: 'A2 - B1',
    isDefault: false,
  },
  {
    value: 1.0,
    label: '1.00x — Tự nhiên chuẩn bản xứ',
    descriptionVi: 'Tốc độ hội thoại tiêu chuẩn hằng ngày và phỏng vấn',
    recommendedCefrRange: 'B1 - B2',
    isDefault: true,
  },
  {
    value: 1.15,
    label: '1.15x — Nhanh & Năng động',
    descriptionVi: 'Luyện phản xạ nghe nhanh trong họp Scrum hoặc tranh luận',
    recommendedCefrRange: 'B2 - C1',
    isDefault: false,
  },
  {
    value: 1.25,
    label: '1.25x — Thử thách tốc độ cao',
    descriptionVi: 'Mô phỏng áp lực cao và người bản xứ nói nhanh',
    recommendedCefrRange: 'C1 - C2',
    isDefault: false,
  },
];

export const DEFAULT_CEFR_PROMPTS: CefrLevelPromptDto[] = [
  {
    id: '40000000-0000-0000-0000-000000000001',
    cefrLevel: 'A1',
    displayName: 'CEFR A1 (Beginner)',
    levelGuidance: 'Speak very slowly and clearly using basic everyday words and short present-tense sentences (5-8 words).',
    displayOrder: 1,
    isActive: true,
  },
  {
    id: '40000000-0000-0000-0000-000000000002',
    cefrLevel: 'A2',
    displayName: 'CEFR A2 (Elementary)',
    levelGuidance: 'Use simple, high-frequency vocabulary and clear sentence structures about daily routines, hobbies, and work basics.',
    displayOrder: 2,
    isActive: true,
  },
  {
    id: '40000000-0000-0000-0000-000000000003',
    cefrLevel: 'B1',
    displayName: 'CEFR B1 (Intermediate)',
    levelGuidance: 'Use natural conversational pacing on familiar topics (work, study, travel, ambitions). Introduce useful phrasal verbs.',
    displayOrder: 3,
    isActive: true,
  },
  {
    id: '40000000-0000-0000-0000-000000000004',
    cefrLevel: 'B2',
    displayName: 'CEFR B2 (Upper-Intermediate)',
    levelGuidance: 'Engage in fluent, spontaneous discussion on concrete and abstract topics. Challenge the learner to justify opinions.',
    displayOrder: 4,
    isActive: true,
  },
  {
    id: '40000000-0000-0000-0000-000000000005',
    cefrLevel: 'C1',
    displayName: 'CEFR C1 (Advanced)',
    levelGuidance: 'Converse at natural native speed using nuanced vocabulary, collocations, and complex grammatical structures.',
    displayOrder: 5,
    isActive: true,
  },
  {
    id: '40000000-0000-0000-0000-000000000006',
    cefrLevel: 'C2',
    displayName: 'CEFR C2 (Proficiency)',
    levelGuidance: 'Engage at a near-native intellectual level with idiomatic subtlety, rhetorical nuance, and stylistic precision.',
    displayOrder: 6,
    isActive: true,
  },
];

export const DEFAULT_CONVERSATION_MODES: ConversationModePromptDto[] = [
  { id: '50000000-0000-0000-0000-000000000013', modeCode: 'daily_conversation', category: 'Communication & Workplace', displayName: 'Daily Conversation Mode (Trò chuyện cuộc sống hằng ngày)', directiveTemplate: 'MODE: Daily Conversation Mode.', displayOrder: 1, isDefault: true, isActive: true },
  { id: '50000000-0000-0000-0000-000000000001', modeCode: 'practice_interview', category: 'Interview & Coaching', displayName: 'Practice Interview Mode (Phỏng vấn & Sửa lỗi sau mỗi câu)', directiveTemplate: 'MODE: Practice Interview Mode.', displayOrder: 2, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000002', modeCode: 'real_interview', category: 'Interview & Coaching', displayName: 'Real Interview Mode (Phỏng vấn thực tế, đánh giá cuối buổi)', directiveTemplate: 'MODE: Real Interview Mode.', displayOrder: 3, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000003', modeCode: 'hr_interview', category: 'Interview & Coaching', displayName: 'HR / Talent Acquisition Interview Mode', directiveTemplate: 'MODE: HR Interview Mode.', displayOrder: 4, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000004', modeCode: 'technical_interview', category: 'Interview & Coaching', displayName: 'Technical Software Engineering Interview (.NET, SQL, React, API)', directiveTemplate: 'MODE: Technical Interview Mode.', displayOrder: 5, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000005', modeCode: 'system_design', category: 'Interview & Coaching', displayName: 'System Design Interview Mode (Kiến trúc hệ thống lớn)', directiveTemplate: 'MODE: System Design Interview Mode.', displayOrder: 6, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000006', modeCode: 'behavioral_star', category: 'Interview & Coaching', displayName: 'Behavioral Interview Mode (Phương pháp STAR)', directiveTemplate: 'MODE: Behavioral STAR Mode.', displayOrder: 7, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000007', modeCode: 'stress_interview', category: 'Interview & Coaching', displayName: 'Stress Interview Mode (Phỏng vấn áp lực cao & phản biện)', directiveTemplate: 'MODE: Stress Interview Mode.', displayOrder: 8, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000008', modeCode: 'rapid_fire', category: 'Interview & Coaching', displayName: 'Rapid Fire Mode (Hỏi nhanh – Đáp gọn 15–30s)', directiveTemplate: 'MODE: Rapid Fire Mode.', displayOrder: 9, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000009', modeCode: 'deep_dive', category: 'Interview & Coaching', displayName: 'Deep Dive Mode (Đào sâu Why? How? What if?)', directiveTemplate: 'MODE: Deep Dive Mode.', displayOrder: 10, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000010', modeCode: 'english_coach', category: 'Interview & Coaching', displayName: 'English Coach Mode (Sửa ngữ pháp, từ vựng & độ trôi chảy)', directiveTemplate: 'MODE: English Coach Mode.', displayOrder: 11, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000011', modeCode: 'pronunciation_coach', category: 'Interview & Coaching', displayName: 'Pronunciation, Stress & Intonation Coach Mode', directiveTemplate: 'MODE: Pronunciation Coach Mode.', displayOrder: 12, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000012', modeCode: 'resume_deep_dive', category: 'Interview & Coaching', displayName: 'Resume / CV & Past Projects Deep Dive Mode', directiveTemplate: 'MODE: Resume Deep Dive Mode.', displayOrder: 13, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000014', modeCode: 'daily_standup', category: 'Communication & Workplace', displayName: 'Daily Standup Scrum Mode (Giả lập họp Daily Scrum)', directiveTemplate: 'MODE: Daily Standup Scrum Mode.', displayOrder: 14, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000015', modeCode: 'technical_discussion', category: 'Communication & Workplace', displayName: 'Technical Discussion with Colleague (Tranh luận giải pháp)', directiveTemplate: 'MODE: Technical Discussion Mode.', displayOrder: 15, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000016', modeCode: 'problem_solving', category: 'Communication & Workplace', displayName: 'Problem Solving & Bug Investigation (Phân tích bug)', directiveTemplate: 'MODE: Problem Solving Mode.', displayOrder: 16, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000017', modeCode: 'client_meeting', category: 'Communication & Workplace', displayName: 'Client Meeting & Requirement Gathering (Làm việc với KH)', directiveTemplate: 'MODE: Client Meeting Mode.', displayOrder: 17, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000018', modeCode: 'debate_mode', category: 'Communication & Workplace', displayName: 'Debate & Critical Thinking Mode (Tranh luận phản xạ)', directiveTemplate: 'MODE: Debate Mode.', displayOrder: 18, isDefault: false, isActive: true },
  { id: '50000000-0000-0000-0000-000000000019', modeCode: 'roleplay_mode', category: 'Communication & Workplace', displayName: 'Custom Roleplay Scenario Mode (Đóng vai thực tế)', directiveTemplate: 'MODE: Custom Roleplay Mode.', displayOrder: 19, isDefault: false, isActive: true },
];

export const DEFAULT_TEACHER_VOICES: TeacherVoiceDto[] = [
  { id: 'Aoede', name: 'Aoede', displayName: 'Aoede', label: 'Aoede (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Breezy', style: 'Truyền cảm, ấm áp & rõ chữ', description: 'Breezy, natural female voice suited for narration.', isFeatured: true, isEnabled: true, displayOrder: 1 },
  { id: 'Puck', name: 'Puck', displayName: 'Puck', label: 'Puck (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Upbeat', style: 'Thân thiện, tự nhiên & năng động', description: 'Upbeat and lively male voice, ideal for conversational interaction.', isFeatured: true, isEnabled: true, displayOrder: 2 },
  { id: 'Kore', name: 'Kore', displayName: 'Kore', label: 'Kore (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Firm', style: 'Điềm tĩnh, chuẩn mực sư phạm', description: 'Firm, articulate female voice suitable for general conversation.', isFeatured: true, isEnabled: true, displayOrder: 3 },
  { id: 'Charon', name: 'Charon', displayName: 'Charon', label: 'Charon (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Informative', style: 'Trầm ấm, phong thái học thuật', description: 'Calm and informative male voice, well suited for instruction.', isFeatured: true, isEnabled: true, displayOrder: 4 },
  { id: 'Fenrir', name: 'Fenrir', displayName: 'Fenrir', label: 'Fenrir (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Excitable', style: 'Dứt khoát, rõ ràng, tốc độ tốt', description: 'Excitable and energetic male voice with crisp delivery.', isFeatured: true, isEnabled: true, displayOrder: 5 },
  { id: 'Leda', name: 'Leda', displayName: 'Leda', label: 'Leda (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Youthful', style: 'Nhẹ nhàng, kiên nhẫn cho A1-B1', description: 'Youthful and soft female voice, great for patient guidance.', isFeatured: true, isEnabled: true, displayOrder: 6 },
  { id: 'Zephyr', name: 'Zephyr', displayName: 'Zephyr', label: 'Zephyr (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Bright', style: 'Tươi sáng, phát âm trong trẻo', description: 'Bright and clear female voice.', isFeatured: false, isEnabled: true, displayOrder: 7 },
  { id: 'Orus', name: 'Orus', displayName: 'Orus', label: 'Orus (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Firm', style: 'Vững vàng, tự tin & trực diện', description: 'Firm, confident and direct male voice.', isFeatured: false, isEnabled: true, displayOrder: 8 },
  { id: 'Callirrhoe', name: 'Callirrhoe', displayName: 'Callirrhoe', label: 'Callirrhoe (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Easy-going', style: 'Thoải mái, dễ chịu & tự nhiên', description: 'Easy-going and pleasant female voice.', isFeatured: false, isEnabled: true, displayOrder: 9 },
  { id: 'Autonoe', name: 'Autonoe', displayName: 'Autonoe', label: 'Autonoe (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Bright', style: 'Tươi sáng & giàu biểu cảm', description: 'Bright and expressive female voice.', isFeatured: false, isEnabled: true, displayOrder: 10 },
  { id: 'Enceladus', name: 'Enceladus', displayName: 'Enceladus', label: 'Enceladus (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Breathy', style: 'Trầm nhẹ, thư thái', description: 'Soft and breathy male voice.', isFeatured: false, isEnabled: true, displayOrder: 11 },
  { id: 'Iapetus', name: 'Iapetus', displayName: 'Iapetus', label: 'Iapetus (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Clear', style: 'Rõ ràng, gần gũi đời thường', description: 'Clear and clean male voice with casual tone.', isFeatured: false, isEnabled: true, displayOrder: 12 },
  { id: 'Umbriel', name: 'Umbriel', displayName: 'Umbriel', label: 'Umbriel (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Easy-going', style: 'Thư giãn, nhịp độ thong thả', description: 'Easy-going and relaxed male voice.', isFeatured: false, isEnabled: true, displayOrder: 13 },
  { id: 'Algieba', name: 'Algieba', displayName: 'Algieba', label: 'Algieba (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Smooth', style: 'Mượt mà, tròn vành rõ chữ', description: 'Smooth and articulate female voice.', isFeatured: false, isEnabled: true, displayOrder: 14 },
  { id: 'Despina', name: 'Despina', displayName: 'Despina', label: 'Despina (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Smooth', style: 'Êm dịu, nhẹ nhàng', description: 'Smooth and soothing female voice.', isFeatured: false, isEnabled: true, displayOrder: 15 },
  { id: 'Erinome', name: 'Erinome', displayName: 'Erinome', label: 'Erinome (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Clear', style: 'Chuẩn mực, điềm đạm', description: 'Clear and poised female voice.', isFeatured: false, isEnabled: true, displayOrder: 16 },
  { id: 'Algenib', name: 'Algenib', displayName: 'Algenib', label: 'Algenib (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Gravelly', style: 'Trầm khàn, cá tính', description: 'Gravelly, textured male voice.', isFeatured: false, isEnabled: true, displayOrder: 17 },
  { id: 'Rasalgethi', name: 'Rasalgethi', displayName: 'Rasalgethi', label: 'Rasalgethi (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Informative', style: 'Điềm tĩnh, giàu thông tin', description: 'Informative and measured male voice.', isFeatured: false, isEnabled: true, displayOrder: 18 },
  { id: 'Laomedeia', name: 'Laomedeia', displayName: 'Laomedeia', label: 'Laomedeia (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Upbeat', style: 'Vui tươi, truyền năng lượng', description: 'Upbeat and cheerful female voice.', isFeatured: false, isEnabled: true, displayOrder: 19 },
  { id: 'Achernar', name: 'Achernar', displayName: 'Achernar', label: 'Achernar (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Soft', style: 'Dịu dàng, ân cần', description: 'Soft and gentle female voice.', isFeatured: false, isEnabled: true, displayOrder: 20 },
  { id: 'Alnilam', name: 'Alnilam', displayName: 'Alnilam', label: 'Alnilam (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Firm', style: 'Vang, chắc khỏe & quyết đoán', description: 'Firm and resonant male voice.', isFeatured: false, isEnabled: true, displayOrder: 21 },
  { id: 'Schedar', name: 'Schedar', displayName: 'Schedar', label: 'Schedar (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Even', style: 'Cân bằng, nhịp nhàng', description: 'Even and balanced male voice.', isFeatured: false, isEnabled: true, displayOrder: 22 },
  { id: 'Gacrux', name: 'Gacrux', displayName: 'Gacrux', label: 'Gacrux (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Mature', style: 'Trưởng thành, đĩnh đạc', description: 'Mature and grounded male voice.', isFeatured: false, isEnabled: true, displayOrder: 23 },
  { id: 'Pulcherrima', name: 'Pulcherrima', displayName: 'Pulcherrima', label: 'Pulcherrima (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Forward', style: 'Chủ động, cuốn hút', description: 'Forward and engaging female voice.', isFeatured: false, isEnabled: true, displayOrder: 24 },
  { id: 'Achird', name: 'Achird', displayName: 'Achird', label: 'Achird (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Friendly', style: 'Thân thiện, dễ gần', description: 'Friendly and approachable male voice.', isFeatured: false, isEnabled: true, displayOrder: 25 },
  { id: 'Zubenelgenubi', name: 'Zubenelgenubi', displayName: 'Zubenelgenubi', label: 'Zubenelgenubi (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Casual', style: 'Tự nhiên, thoải mái', description: 'Casual and conversational male voice.', isFeatured: false, isEnabled: true, displayOrder: 26 },
  { id: 'Vindemiatrix', name: 'Vindemiatrix', displayName: 'Vindemiatrix', label: 'Vindemiatrix (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Gentle', style: 'Nhẹ nhàng, điềm đạm', description: 'Gentle and composed female voice.', isFeatured: false, isEnabled: true, displayOrder: 27 },
  { id: 'Sadachbia', name: 'Sadachbia', displayName: 'Sadachbia', label: 'Sadachbia (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Lively', style: 'Sôi nổi, giàu biểu cảm', description: 'Lively and expressive male voice.', isFeatured: false, isEnabled: true, displayOrder: 28 },
  { id: 'Sadaltager', name: 'Sadaltager', displayName: 'Sadaltager', label: 'Sadaltager (Nam)', type: 'prebuilt', gender: 'Male', persona: 'Knowledgeable', style: 'Uyên bác, thuyết phục', description: 'Knowledgeable and authoritative male voice.', isFeatured: false, isEnabled: true, displayOrder: 29 },
  { id: 'Sulafat', name: 'Sulafat', displayName: 'Sulafat', label: 'Sulafat (Nữ)', type: 'prebuilt', gender: 'Female', persona: 'Warm', style: 'Ấm áp, khích lệ người học', description: 'Warm and encouraging female voice.', isFeatured: false, isEnabled: true, displayOrder: 30 },
];

export const DEFAULT_TOPICS: TopicDto[] = [
  { id: '90000000-0000-0000-0000-000000000001', code: 'net10_clean_arch_ddd', name: '.NET 10, Clean Architecture, DDD & CQRS', displayNameVi: 'Kiến trúc Clean Architecture, DDD & CQRS trên .NET 10', category: 'Software Engineering', modeCode: 'technical_interview', suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Aggregate Roots, Domain Events, EF Core Interceptors, CQRS handlers.', isFeatured: true, isActive: true, displayOrder: 1 },
  { id: '90000000-0000-0000-0000-000000000002', code: 'sql_server_indexing_tuning', name: 'SQL Server Indexing, Query Tuning & Deadlocks', displayNameVi: 'Tối ưu truy vấn SQL Server, Indexing & xử lý Deadlock', category: 'Software Engineering', modeCode: 'technical_interview', suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Clustered vs non-clustered indexes, execution plans, isolation levels.', isFeatured: true, isActive: true, displayOrder: 2 },
  { id: '90000000-0000-0000-0000-000000000003', code: 'react_typescript_performance', name: 'React 19, TypeScript, TanStack Query & Frontend Architecture', displayNameVi: 'Kiến trúc Frontend: React 19, TanStack Query, Zustand & RBAC', category: 'Software Engineering', modeCode: 'technical_interview', suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Feature-based architecture, server state vs client state, RBAC routing.', isFeatured: true, isActive: true, displayOrder: 3 },
  { id: '90000000-0000-0000-0000-000000000005', code: 'sysdesign_payment_gateway', name: 'Designing a High-Concurrency Payment & Order System', displayNameVi: 'Thiết kế hệ thống Thanh toán & Đơn hàng chịu tải cao', category: 'System Design', modeCode: 'system_design', suggestedCefrLevel: 'C1', supportsConversation: true, supportsVocabulary: true, description: 'Idempotency keys, distributed transactions, outbox pattern, Redis caching.', isFeatured: true, isActive: true, displayOrder: 5 },
  { id: '90000000-0000-0000-0000-000000000008', code: 'career_self_intro_goals', name: 'Self-Introduction, Career Goals & Why This Role', displayNameVi: 'Giới thiệu bản thân, Mục tiêu nghề nghiệp & Lý do ứng tuyển', category: 'HR & Career', modeCode: 'hr_interview', suggestedCefrLevel: 'B1', supportsConversation: true, supportsVocabulary: true, description: 'Craft a compelling 90-second elevator pitch and career story.', isFeatured: true, isActive: true, displayOrder: 8 },
  { id: '90000000-0000-0000-0000-000000000009', code: 'star_production_outage', name: 'STAR Story: Resolving a Critical Production Incident Under Pressure', displayNameVi: 'Kể chuyện STAR: Xử lý sự cố Production nghiêm trọng dưới áp lực', category: 'HR & Career', modeCode: 'behavioral_star', suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Situation, Task, Action, and measurable Result for high-stakes incidents.', isFeatured: true, isActive: true, displayOrder: 9 },
  { id: '90000000-0000-0000-0000-000000000013', code: 'agile_sprint_standup_blocker', name: 'Daily Standup: Sprint Progress, API Integration & Blocker Escalation', displayNameVi: 'Họp Daily Standup: Báo cáo tiến độ Sprint & nêu Blocker', category: 'Workplace & Agile', modeCode: 'daily_standup', suggestedCefrLevel: 'B1', supportsConversation: true, supportsVocabulary: true, description: 'Give crisp updates on yesterday PRs, today tasks, and blockers.', isFeatured: true, isActive: true, displayOrder: 13 },
  { id: '90000000-0000-0000-0000-000000000015', code: 'client_scope_timeline_negotiation', name: 'Client Meeting: Clarifying Requirements & Managing Scope Creep', displayNameVi: 'Họp với Khách hàng: Làm rõ yêu cầu & Đàm phán tiến độ dự án', category: 'Workplace & Agile', modeCode: 'client_meeting', suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Translate technical constraints into business impact for clients.', isFeatured: true, isActive: true, displayOrder: 15 },
  { id: '90000000-0000-0000-0000-000000000017', code: 'daily_routines_work_life', name: 'Daily Routines, Remote Work Habits & Work-Life Balance', displayNameVi: 'Thói quen hằng ngày, Làm việc từ xa & Cân bằng cuộc sống', category: 'Daily Life & Culture', modeCode: 'daily_conversation', suggestedCefrLevel: 'A2', supportsConversation: true, supportsVocabulary: true, description: 'Talk naturally about morning routines, coffee habits, and unwinding.', isFeatured: true, isActive: true, displayOrder: 17 },
  { id: '90000000-0000-0000-0000-000000000018', code: 'travel_street_food_vietnam', name: 'Travel Adventures & Introducing Vietnamese Street Food to Foreign Friends', displayNameVi: 'Du lịch khám phá & Giới thiệu ẩm thực đường phố Việt Nam', category: 'Daily Life & Culture', modeCode: 'daily_conversation', suggestedCefrLevel: 'B1', supportsConversation: true, supportsVocabulary: true, description: 'Describe flavors, textures, cultural traditions, and travel.', isFeatured: true, isActive: true, displayOrder: 18 },
  { id: '90000000-0000-0000-0000-000000000021', code: 'debate_ai_software_engineers', name: 'Debate: Will AI Agents Replace or Empower Software Engineers?', displayNameVi: 'Tranh luận: AI Agent sẽ thay thế hay nâng tầm Kỹ sư phần mềm?', category: 'Debate & Critical Thinking', modeCode: 'debate_mode', suggestedCefrLevel: 'C1', supportsConversation: true, supportsVocabulary: true, description: 'Practice structured argumentation, hedging, and counter-arguments.', isFeatured: true, isActive: true, displayOrder: 21 },
  { id: '90000000-0000-0000-0000-000000000024', code: 'vocab_cloud_devops_k8s', name: 'Cloud Computing, Docker, Kubernetes & CI/CD Pipelines', displayNameVi: 'Từ vựng Cloud Computing, Docker, Kubernetes & CI/CD', category: 'Vocabulary Specialization', modeCode: null, suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'High-value terminology for DevOps, containers, and observability.', isFeatured: true, isActive: true, displayOrder: 24 },
  { id: '90000000-0000-0000-0000-000000000025', code: 'vocab_business_idioms_phrasal_verbs', name: 'Essential Workplace Phrasal Verbs & Executive Idioms', displayNameVi: 'Cụm động từ (Phrasal Verbs) & Thành ngữ công sở thông dụng', category: 'Vocabulary Specialization', modeCode: null, suggestedCefrLevel: 'B2', supportsConversation: true, supportsVocabulary: true, description: 'Master expressions like circle back, iron out bottlenecks, on the same page.', isFeatured: true, isActive: true, displayOrder: 25 },
];

export const DEFAULT_AI_MODELS: AiModelOptionDto[] = [
  { id: '10000000-0000-0000-0000-000000000001', displayName: 'Gemini 3.8 Flash (Recommended)', modelCode: 'gemini-3.8-flash', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: true, supportsPronunciationAssessment: true, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Model đa phương thức thế hệ mới nhất, hỗ trợ cả tạo từ vựng JSON và chấm điểm phát âm trực tiếp từ âm thanh.', displayOrder: 1 },
  { id: '10000000-0000-0000-0000-000000000002', displayName: 'Gemini 3.6 Flash', modelCode: 'gemini-3.6-flash', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: true, supportsPronunciationAssessment: true, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Hỗ trợ tạo từ vựng và đánh giá phát âm bằng âm thanh.', displayOrder: 2 },
  { id: '10000000-0000-0000-0000-000000000003', displayName: 'Gemini 3.5 Flash Lite', modelCode: 'gemini-3.5-flash-lite', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: true, supportsPronunciationAssessment: true, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Tốc độ phản hồi cực nhanh, chi phí thấp.', displayOrder: 3 },
  { id: '10000000-0000-0000-0000-000000000004', displayName: 'Gemini 3.1 Flash Lite', modelCode: 'gemini-3.1-flash-lite', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: true, supportsPronunciationAssessment: true, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Phiên bản nhẹ hỗ trợ cả tạo từ vựng và chấm phát âm.', displayOrder: 4 },
  { id: '10000000-0000-0000-0000-000000000006', displayName: 'Gemini 2.5 Flash', modelCode: 'gemini-2.5-flash', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: true, supportsPronunciationAssessment: true, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Model ổn định phổ biến cho tạo từ vựng và chấm phát âm.', displayOrder: 6 },
  { id: '10000000-0000-0000-0000-000000000009', displayName: 'Gemma 4 26B (Text Only)', modelCode: 'gemma-4-26b-a4b-it', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: false, supportsPronunciationAssessment: false, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Chỉ hỗ trợ tạo từ vựng văn bản (Text Generation); KHÔNG hỗ trợ đầu vào âm thanh để chấm phát âm.', displayOrder: 9 },
  { id: '10000000-0000-0000-0000-000000000010', displayName: 'Gemma 4 31B (Text Only)', modelCode: 'gemma-4-31b-it', supportsTextGeneration: true, supportsStructuredOutput: true, supportsAudioInput: false, supportsPronunciationAssessment: false, isAvailable: true, isDeprecated: false, isEnabled: true, capabilityNote: 'Chỉ hỗ trợ tạo từ vựng văn bản (Text Generation); KHÔNG hỗ trợ đầu vào âm thanh để chấm phát âm.', displayOrder: 10 },
];

export const DEFAULT_SCORING_RUBRIC: ScoringRubricDto = {
  id: '20000000-0000-0000-0000-000000000001',
  code: 'ENGLISH_PRONUNCIATION_V1',
  name: 'Tiêu chuẩn chấm phát âm tiếng Anh v1 (Accuracy 40% · Clarity 30% · Fluency 30%)',
  description: 'Đánh giá phát âm từ vựng và câu ví dụ dựa trên độ chính xác âm vị, độ rõ ràng và độ trôi chảy.',
  version: 1,
  assessmentType: 'Pronunciation',
  isActive: true,
  criteria: [
    { id: '20000000-0000-0000-0000-000000000101', code: 'Accuracy', name: 'Accuracy (Độ chính xác âm vị & trọng âm)', description: 'Phonetic accuracy of vowel and consonant sounds and stress patterns.', weight: 40, displayOrder: 1, minimumScore: 0, maximumScore: 100, isEnabled: true },
    { id: '20000000-0000-0000-0000-000000000102', code: 'Clarity', name: 'Clarity (Độ rõ ràng & âm cuối)', description: 'Articulation clarity, intelligibility, and ending consonant completeness.', weight: 30, displayOrder: 2, minimumScore: 0, maximumScore: 100, isEnabled: true },
    { id: '20000000-0000-0000-0000-000000000103', code: 'Fluency', name: 'Fluency (Độ trôi chảy & nhịp điệu)', description: 'Smooth pacing, natural rhythm, linking, and absence of unnatural pauses.', weight: 30, displayOrder: 3, minimumScore: 0, maximumScore: 100, isEnabled: true },
  ],
};
