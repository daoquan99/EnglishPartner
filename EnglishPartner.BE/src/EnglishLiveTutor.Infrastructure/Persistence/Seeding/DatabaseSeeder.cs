using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Identity.Permissions;
using EnglishLiveTutor.Domain.Identity.Roles;
using EnglishLiveTutor.Domain.Identity.Users;
using EnglishLiveTutor.Domain.Prompts.CefrLevelPrompts;
using EnglishLiveTutor.Domain.Prompts.ConversationModePrompts;
using EnglishLiveTutor.Domain.Prompts.SpeechSpeeds;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    /// <summary>
    /// Applies relational migrations first; fails fast if schema migration fails.
    /// Then executes idempotent catalog/reference seeding that preserves administrator edits
    /// and works on both empty and partially initialized databases.
    /// </summary>
    public static async Task EnsureSchemaAndSeedAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        AdminUserSeedOptions? adminOptions = null,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        await EnsureSchemaMigratedAsync(dbFactory, ct);

        try
        {
            await SeedReferenceCatalogsAsync(dbFactory, adminOptions, ct);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Database reference seeding encountered an error after schema migration completed.");
            throw;
        }
    }

    public static async Task EnsureSchemaMigratedAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }
    }

    public static async Task SeedReferenceCatalogsAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        AdminUserSeedOptions? adminOptions = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ActorOverride = "system-seeder";

        // 1. Seed AI Model Catalog idempotently by ModelCode/Id (preserves admin edits and soft-deleted items)
        var existingModelCodes = await db.AiModelCatalog
            .IgnoreQueryFilters()
            .Select(m => m.ModelCode)
            .ToListAsync(ct);
        var existingModelSet = existingModelCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingModels = BuildDefaultAiModels()
            .Where(m => !existingModelSet.Contains(m.ModelCode))
            .ToList();
        if (missingModels.Count > 0)
        {
            db.AiModelCatalog.AddRange(missingModels);
            await db.SaveChangesAsync(ct);
        }

        // 2. Seed Default Pronunciation Scoring Rubric ENGLISH_PRONUNCIATION_V1
        if (!await db.ScoringRubrics.IgnoreQueryFilters().AnyAsync(r => r.Code == "ENGLISH_PRONUNCIATION_V1", ct))
        {
            var rubricId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            var rubric = new ScoringRubricEntity(
                code: "ENGLISH_PRONUNCIATION_V1",
                name: "Tiêu chuẩn chấm phát âm tiếng Anh v1 (Accuracy 40% · Clarity 30% · Fluency 30%)",
                description: "Đánh giá phát âm từ vựng và câu ví dụ dựa trên độ chính xác âm vị, độ rõ ràng và độ trôi chảy.",
                version: 1,
                assessmentType: "Pronunciation",
                isActive: true,
                id: rubricId);

            rubric.AddCriterion(
                code: "Accuracy",
                name: "Accuracy (Độ chính xác âm vị & trọng âm)",
                description: "Phonetic accuracy of vowel and consonant sounds and stress patterns.",
                weight: 40.00m,
                displayOrder: 1,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000101"));

            rubric.AddCriterion(
                code: "Clarity",
                name: "Clarity (Độ rõ chữ & khẩu hình)",
                description: "Vocal projection, articulation clarity, and audibility consistency.",
                weight: 30.00m,
                displayOrder: 2,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000102"));

            rubric.AddCriterion(
                code: "Fluency",
                name: "Fluency (Nhịp điệu & độ trôi chảy)",
                description: "Speaking rhythm, natural flow, and absence of hesitation pauses.",
                weight: 30.00m,
                displayOrder: 3,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000103"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
            await db.SaveChangesAsync(ct);
        }

        // 3. Seed Starter Vocabulary Catalog idempotently by Id / NormalizedWord
        var existingVocabWords = (await db.Vocabularies
            .IgnoreQueryFilters()
            .Select(v => v.NormalizedWord)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingVocabs = BuildStarterVocabularyCatalog()
            .Where(v => !existingVocabWords.Contains(v.NormalizedWord))
            .ToList();
        if (missingVocabs.Count > 0)
        {
            db.Vocabularies.AddRange(missingVocabs);
            await db.SaveChangesAsync(ct);
        }

        // 4. Seed CEFR Level Prompts & Conversation Mode Prompts idempotently
        var existingCefrLevels = (await db.CefrLevelPrompts
            .IgnoreQueryFilters()
            .Select(p => p.CefrLevel)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingCefrPrompts = CefrPromptBuilder.GetDefaultCefrLevelPrompts()
            .Where(p => !existingCefrLevels.Contains(p.CefrLevel))
            .ToList();
        if (missingCefrPrompts.Count > 0)
        {
            db.CefrLevelPrompts.AddRange(missingCefrPrompts);
            await db.SaveChangesAsync(ct);
        }

        var existingModeCodes = (await db.ConversationModePrompts
            .IgnoreQueryFilters()
            .Select(m => m.ModeCode)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingModePrompts = CefrPromptBuilder.GetDefaultConversationModePrompts()
            .Where(m => !existingModeCodes.Contains(m.ModeCode))
            .ToList();
        if (missingModePrompts.Count > 0)
        {
            db.ConversationModePrompts.AddRange(missingModePrompts);
            await db.SaveChangesAsync(ct);
        }

        // 5. Seed Speech Speeds idempotently
        var existingSpeedCodes = (await db.SpeechSpeeds
            .IgnoreQueryFilters()
            .Select(s => s.Code)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingSpeeds = BuildDefaultSpeechSpeeds()
            .Where(s => !existingSpeedCodes.Contains(s.Code))
            .ToList();
        if (missingSpeeds.Count > 0)
        {
            db.SpeechSpeeds.AddRange(missingSpeeds);
            await db.SaveChangesAsync(ct);
        }

        // 6. Seed Teacher Voices idempotently
        var existingVoiceIds = (await db.TeacherVoices
            .IgnoreQueryFilters()
            .Select(v => v.Id)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingVoices = BuildDefaultTeacherVoices()
            .Where(v => !existingVoiceIds.Contains(v.Id))
            .ToList();
        if (missingVoices.Count > 0)
        {
            db.TeacherVoices.AddRange(missingVoices);
            await db.SaveChangesAsync(ct);
        }

        // 7. Seed Curated Topics idempotently
        var existingTopicCodes = (await db.Topics
            .IgnoreQueryFilters()
            .Select(t => t.Code)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingTopics = BuildDefaultTopics()
            .Where(t => !existingTopicCodes.Contains(t.Code))
            .ToList();
        if (missingTopics.Count > 0)
        {
            db.Topics.AddRange(missingTopics);
            await db.SaveChangesAsync(ct);
        }

        // 8. Seed Default Permissions, Roles, and Starter Accounts idempotently
        await SeedIdentityAsync(db, adminOptions, ct);
    }

    public static List<AiModelConfigEntity> BuildDefaultAiModels() => new()
    {
        new(
            displayName: "Gemini 3.8 Flash (Recommended)",
            modelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Model đa phương thức thế hệ mới nhất, hỗ trợ cả tạo từ vựng JSON và chấm điểm phát âm trực tiếp từ âm thanh.",
            displayOrder: 1,
            id: Guid.Parse("10000000-0000-0000-0000-000000000001")),
        new(
            displayName: "Gemini 3.6 Flash",
            modelCode: "gemini-3.6-flash",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Hỗ trợ tạo từ vựng và đánh giá phát âm bằng âm thanh.",
            displayOrder: 2,
            id: Guid.Parse("10000000-0000-0000-0000-000000000002")),
        new(
            displayName: "Gemini 3.5 Flash Lite",
            modelCode: "gemini-3.5-flash-lite",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Tốc độ phản hồi cực nhanh, chi phí thấp.",
            displayOrder: 3,
            id: Guid.Parse("10000000-0000-0000-0000-000000000003")),
        new(
            displayName: "Gemini 3.1 Flash Lite",
            modelCode: "gemini-3.1-flash-lite",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Phiên bản nhẹ hỗ trợ cả tạo từ vựng và chấm phát âm.",
            displayOrder: 4,
            id: Guid.Parse("10000000-0000-0000-0000-000000000004")),
        new(
            displayName: "Gemini 3 Flash Preview",
            modelCode: "gemini-3-flash-preview",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Bản preview dòng Gemini 3 Flash.",
            displayOrder: 5,
            id: Guid.Parse("10000000-0000-0000-0000-000000000005")),
        new(
            displayName: "Gemini 2.5 Flash",
            modelCode: "gemini-2.5-flash",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Model ổn định phổ biến; lưu ý một số tài khoản mới có thể giới hạn quyền truy cập dòng 2.5.",
            displayOrder: 6,
            id: Guid.Parse("10000000-0000-0000-0000-000000000006")),
        new(
            displayName: "Gemini 2.5 Flash Lite",
            modelCode: "gemini-2.5-flash-lite",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Phiên bản Lite của dòng Gemini 2.5.",
            displayOrder: 7,
            id: Guid.Parse("10000000-0000-0000-0000-000000000007")),
        new(
            displayName: "Gemini 3.7 Flash (Deprecated → 3.8 Flash)",
            modelCode: "gemini-3.7-flash",
            routedModelCode: "gemini-3.8-flash",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: true,
            supportsPronunciationAssessment: true,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: true,
            isEnabled: true,
            capabilityNote: "Đã ngừng khuyến nghị (Deprecated) và tự động định tuyến sang gemini-3.8-flash.",
            displayOrder: 8,
            id: Guid.Parse("10000000-0000-0000-0000-000000000008")),
        new(
            displayName: "Gemma 4 26B (Text Only)",
            modelCode: "gemma-4-26b-a4b-it",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: false,
            supportsPronunciationAssessment: false,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Chỉ hỗ trợ tạo từ vựng văn bản (Text Generation); KHÔNG hỗ trợ đầu vào âm thanh để chấm phát âm.",
            displayOrder: 9,
            id: Guid.Parse("10000000-0000-0000-0000-000000000009")),
        new(
            displayName: "Gemma 4 31B (Text Only)",
            modelCode: "gemma-4-31b-it",
            supportsTextGeneration: true,
            supportsStructuredOutput: true,
            supportsAudioInput: false,
            supportsPronunciationAssessment: false,
            supportsStreaming: true,
            isAvailable: true,
            isDeprecated: false,
            isEnabled: true,
            capabilityNote: "Chỉ hỗ trợ tạo từ vựng văn bản (Text Generation); KHÔNG hỗ trợ đầu vào âm thanh để chấm phát âm.",
            displayOrder: 10,
            id: Guid.Parse("10000000-0000-0000-0000-000000000010")),
        new(
            displayName: "Gemini 3.5 Transcribe Live",
            modelCode: "gemini-3.5-transcribe-live",
            supportsLiveAudio: true,
            supportsTranscription: true,
            supportsStreaming: true,
            isAvailable: true,
            isEnabled: true,
            capabilityNote: "Chuyên dụng cho Hub 2 Realtime Transcription.",
            displayOrder: 11,
            id: Guid.Parse("10000000-0000-0000-0000-000000000011")),
        new(
            displayName: "Gemini 3.5 Live Translate Preview",
            modelCode: "gemini-3.5-live-translate-preview",
            supportsLiveAudio: true,
            supportsTranslation: true,
            supportsStreaming: true,
            isAvailable: true,
            isEnabled: true,
            capabilityNote: "Chuyên dụng cho Hub 3 Realtime Vietnamese Translation.",
            displayOrder: 12,
            id: Guid.Parse("10000000-0000-0000-0000-000000000012")),
        new(
            displayName: "Gemini 3.8 Live",
            modelCode: "gemini-3.8-live",
            supportsLiveAudio: true,
            supportsStreaming: true,
            isAvailable: true,
            isEnabled: true,
            capabilityNote: "Chuyên dụng cho Hub 1 Live Voice Tutor.",
            displayOrder: 13,
            id: Guid.Parse("10000000-0000-0000-0000-000000000013")),
        new(
            displayName: "Gemini 3.8 Live Extended Thinking",
            modelCode: "gemini-3.8-live-extended-thinking",
            supportsLiveAudio: true,
            supportsStreaming: true,
            isAvailable: true,
            isEnabled: true,
            capabilityNote: "Chuyên dụng cho Hub 1 Live Voice Tutor (Thinking Mode).",
            displayOrder: 14,
            id: Guid.Parse("10000000-0000-0000-0000-000000000014"))
    };

    private static async Task SeedIdentityAsync(
        AppDbContext db,
        AdminUserSeedOptions? adminOptions,
        CancellationToken ct = default)
    {
        var defaultPermissions = new List<PermissionEntity>
        {
            new("conversations.access", "Access Live Tutor Conversations", "Conversations", "Create, view, and stream live AI tutor conversations", id: Guid.Parse("60000000-0000-0000-0000-000000000001")),
            new("vocabularies.read", "View Vocabulary & Flashcards", "Vocabulary", "Access vocabulary catalog and daily flashcards", id: Guid.Parse("60000000-0000-0000-0000-000000000002")),
            new("vocabularies.generate", "Generate Vocabulary with AI", "Vocabulary", "Generate new structured vocabulary items via Gemini/Gemma", id: Guid.Parse("60000000-0000-0000-0000-000000000003")),
            new("pronunciation.assess", "Submit Pronunciation Assessments", "Pronunciation", "Record and score spoken English pronunciation", id: Guid.Parse("60000000-0000-0000-0000-000000000004")),
            new("rubrics.manage", "Manage Pronunciation Scoring Rubrics", "Pronunciation", "Update scoring rubric criteria and weights", id: Guid.Parse("60000000-0000-0000-0000-000000000005")),
            new("prompts.manage", "Manage CEFR & Session Mode Prompts", "Prompts", "Create and update CEFR level guidance and coaching mode directives", id: Guid.Parse("60000000-0000-0000-0000-000000000006")),
            new("voices.manage", "Manage Teacher Voices Catalog", "Voices", "Update and configure prebuilt teacher voices", id: Guid.Parse("60000000-0000-0000-0000-000000000007")),
            new("users.read", "View Users", "Identity", "List and inspect user profiles", id: Guid.Parse("60000000-0000-0000-0000-000000000008")),
            new("users.manage", "Manage Users & Role Assignments", "Identity", "Assign roles and activate/deactivate users", id: Guid.Parse("60000000-0000-0000-0000-000000000009")),
            new("roles.manage", "Manage Roles & Permissions", "Identity", "Create roles and configure role-permission mappings", id: Guid.Parse("60000000-0000-0000-0000-000000000010")),
            new("topics.manage", "Manage Topics Catalog", "Topics", "Create, update, and curate conversation & vocabulary topics", id: Guid.Parse("60000000-0000-0000-0000-000000000011")),
            new("vocabularies.manage", "Manage Vocabulary & Flashcards Catalog", "Vocabulary", "Create, update, and delete vocabulary flashcards in database", id: Guid.Parse("60000000-0000-0000-0000-000000000012"))
        };

        var existingPermCodes = (await db.Permissions
            .IgnoreQueryFilters()
            .Select(p => p.Code)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingPerms = defaultPermissions.Where(p => !existingPermCodes.Contains(p.Code)).ToList();
        if (missingPerms.Count > 0)
        {
            db.Permissions.AddRange(missingPerms);
            await db.SaveChangesAsync(ct);
        }

        var existingRoleCodes = (await db.Roles
            .IgnoreQueryFilters()
            .Select(r => r.Code)
            .ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existingRoleCodes.Count == 0)
        {
            var allPermissions = await db.Permissions.AsNoTracking().ToListAsync(ct);

            var adminRole = new RoleEntity(
                code: "ADMIN",
                name: "System Administrator",
                description: "Toàn quyền quản trị hệ thống, người dùng, prompt, giọng đọc và rubric.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000001"));
            foreach (var p in allPermissions)
            {
                adminRole.GrantPermission(p.Id);
            }

            var teacherRole = new RoleEntity(
                code: "TEACHER",
                name: "Curriculum & Coaching Teacher",
                description: "Quản lý từ vựng, tiêu chí chấm phát âm, cấu hình CEFR prompt và giọng đọc giáo viên.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach (var p in allPermissions.Where(x => x.Module != "Identity"))
            {
                teacherRole.GrantPermission(p.Id);
            }

            var learnerRole = new RoleEntity(
                code: "LEARNER",
                name: "English Learner",
                description: "Học viên luyện hội thoại trực tiếp, học flashcard từ vựng hằng ngày và chấm phát âm.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000003"));
            foreach (var p in allPermissions.Where(x =>
                x.Code is "conversations.access" or "vocabularies.read" or "vocabularies.generate" or "pronunciation.assess"))
            {
                learnerRole.GrantPermission(p.Id);
            }

            db.Roles.AddRange(adminRole, teacherRole, learnerRole);
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(ct))
        {
            var opts = adminOptions ?? new AdminUserSeedOptions();
            var hasher = new Pbkdf2PasswordHasher();
            var adminRoleId = Guid.Parse("70000000-0000-0000-0000-000000000001");
            var learnerRoleId = Guid.Parse("70000000-0000-0000-0000-000000000003");

            var adminUser = UserEntity.Create(
                username: string.IsNullOrWhiteSpace(opts.Username) ? "admin" : opts.Username.Trim(),
                email: string.IsNullOrWhiteSpace(opts.Email) ? "admin@lingomint.local" : opts.Email.Trim(),
                fullName: string.IsNullOrWhiteSpace(opts.FullName) ? "LingoMint Administrator" : opts.FullName.Trim(),
                passwordHash: hasher.HashPassword(string.IsNullOrWhiteSpace(opts.Password) ? "Admin@123" : opts.Password),
                preferredCefrLevel: string.IsNullOrWhiteSpace(opts.PreferredCefrLevel) ? "C1" : opts.PreferredCefrLevel.Trim(),
                preferredVoiceName: string.IsNullOrWhiteSpace(opts.PreferredVoiceName) ? "Aoede" : opts.PreferredVoiceName.Trim(),
                id: Guid.Parse("80000000-0000-0000-0000-000000000001"));
            adminUser.AssignRole(adminRoleId);

            var defaultLearner = UserEntity.Create(
                username: "default-learner",
                email: "learner@lingomint.local",
                fullName: "Học viên Mặc định",
                passwordHash: hasher.HashPassword("Learner@123"),
                preferredCefrLevel: "B1",
                preferredVoiceName: "Aoede",
                id: Guid.Parse("80000000-0000-0000-0000-000000000002"));
            defaultLearner.AssignRole(learnerRoleId);

            db.Users.AddRange(adminUser, defaultLearner);
            await db.SaveChangesAsync(ct);
        }
    }

    public static List<VocabularyEntity> BuildStarterVocabularyCatalog()
    {
        var list = new List<VocabularyEntity>();

        // Word 1: beautiful (A1)
        var vBeautiful = VocabularyEntity.Create(
            word: "beautiful",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 320,
            topicCategory: "Daily Life & Descriptions",
            id: Guid.Parse("30000000-0000-0000-0000-000000000001"));
        vBeautiful.AddPronunciation("UK", "/ˈbjuː.tɪ.fəl/", syllableCount: 3, stressPattern: "BEAU-ti-ful (Ooo)");
        vBeautiful.AddPronunciation("US", "/ˈbjuː.t̬ə.fəl/", syllableCount: 3, stressPattern: "BEAU-ti-ful (Ooo)");
        vBeautiful.AddWordFamily("beauty", "noun");
        vBeautiful.AddWordFamily("beautify", "verb");
        vBeautiful.AddWordFamily("beautifully", "adverb");

        var sBeautiful = vBeautiful.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Pleasing the senses or mind aesthetically; very attractive.",
            meaningVi: "Đẹp, xinh đẹp, tuyệt đẹp",
            cefrLevel: "A1",
            descriptionEn: "Used to describe people, places, scenery, weather, or experiences that give great pleasure to see or hear.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000101"));
        sBeautiful.AddRelation("Synonym", "gorgeous", 1);
        sBeautiful.AddRelation("Synonym", "stunning", 2);
        sBeautiful.AddRelation("Synonym", "attractive", 3);
        sBeautiful.AddRelation("Antonym", "ugly", 1);
        sBeautiful.AddRelation("Antonym", "unattractive", 2);
        sBeautiful.AddCollocation("beautiful scenery", "phong cảnh tuyệt đẹp", 1);
        sBeautiful.AddCollocation("beautiful smile", "nụ cười rạng rỡ", 2);
        sBeautiful.AddCollocation("absolutely beautiful", "đẹp hoàn hảo / cực kỳ đẹp", 3);
        sBeautiful.AddExample(
            "Da Nang is famous for its beautiful beaches and friendly people.",
            "Đà Nẵng nổi tiếng với những bãi biển tuyệt đẹp và con người thân thiện.",
            "A1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001001"));
        sBeautiful.AddExample(
            "She played a beautiful melody on the piano.",
            "Cô ấy đã chơi một giai điệu tuyệt đẹp trên đàn dương cầm.",
            "A2",
            2,
            id: Guid.Parse("30000000-0000-0000-0000-000000001002"));
        sBeautiful.AddGrammarPattern("beautiful + noun", "Đứng trước danh từ để bổ nghĩa", "a beautiful morning");
        sBeautiful.AddGrammarPattern("be / look / sound + beautiful", "Đứng sau động từ nối (linking verbs)", "The sunset looks beautiful.");
        list.Add(vBeautiful);

        // Word 2: bank (Multi-sense example: Sense 1 A2 financial, Sense 2 B1 river bank)
        var vBank = VocabularyEntity.Create(
            word: "bank",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 410,
            topicCategory: "Finance & Geography (Multi-Sense)",
            id: Guid.Parse("30000000-0000-0000-0000-000000000002"));
        vBank.AddPronunciation("UK", "/bæŋk/", syllableCount: 1, stressPattern: "bank (O)");
        vBank.AddPronunciation("US", "/bæŋk/", syllableCount: 1, stressPattern: "bank (O)");
        vBank.AddWordFamily("banker", "noun");
        vBank.AddWordFamily("banking", "noun");

        var sBank1 = vBank.AddSense(
            partOfSpeech: "noun",
            definitionEn: "An organization where people and businesses can invest or borrow money and exchange currency.",
            meaningVi: "Ngân hàng (tổ chức tài chính)",
            cefrLevel: "A2",
            descriptionEn: "Sense 1: Financial institution where you keep money in an account or apply for loans.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000102"));
        sBank1.AddRelation("Synonym", "financial institution", 1);
        sBank1.AddRelation("Synonym", "lender", 2);
        sBank1.AddCollocation("open a bank account", "mở tài khoản ngân hàng", 1);
        sBank1.AddCollocation("bank transfer", "chuyển khoản ngân hàng", 2);
        sBank1.AddExample(
            "I need to go to the bank to open a savings account.",
            "Tôi cần đến ngân hàng để mở một tài khoản tiết kiệm.",
            "A2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001003"));
        sBank1.AddGrammarPattern("deposit / withdraw money from a bank", "Gửi / rút tiền từ ngân hàng", "She withdrew cash from the bank.");

        var sBank2 = vBank.AddSense(
            partOfSpeech: "noun",
            definitionEn: "Sloping raised land, especially along the sides of a river or lake.",
            meaningVi: "Bờ sông, bờ hồ",
            cefrLevel: "B1",
            descriptionEn: "Sense 2: The land alongside a body of water such as a river or canal.",
            isPrimary: false,
            displayOrder: 2,
            id: Guid.Parse("30000000-0000-0000-0000-000000000103"));
        sBank2.AddRelation("Synonym", "riverbank", 1);
        sBank2.AddRelation("Synonym", "shore", 2);
        sBank2.AddCollocation("along the river bank", "dọc theo bờ sông", 1);
        sBank2.AddCollocation("steep grassy bank", "bờ cỏ dốc", 2);
        sBank2.AddExample(
            "We walked peacefully along the grassy bank of the Red River at sunset.",
            "Chúng tôi đi dạo yên bình dọc theo bờ cỏ của sông Hồng lúc hoàng hôn.",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001004"));
        sBank2.AddGrammarPattern("on the bank of + the river", "Trên bờ của con sông", "A small village sits on the bank of the river.");
        list.Add(vBank);

        // Word 3: collaborate (B1)
        var vCollab = VocabularyEntity.Create(
            word: "collaborate",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 1450,
            topicCategory: "Workplace & Teamwork",
            id: Guid.Parse("30000000-0000-0000-0000-000000000003"));
        vCollab.AddPronunciation("UK", "/kəˈlæb.ə.reɪt/", syllableCount: 4, stressPattern: "col-LAB-o-rate (oOoo)");
        vCollab.AddPronunciation("US", "/kəˈlæb.ə.reɪt/", syllableCount: 4, stressPattern: "col-LAB-o-rate (oOoo)");
        vCollab.AddWordFamily("collaboration", "noun");
        vCollab.AddWordFamily("collaborative", "adjective");
        vCollab.AddWordFamily("collaborator", "noun");

        var sCollab = vCollab.AddSense(
            partOfSpeech: "verb",
            definitionEn: "To work together with one or more people or organizations to achieve a shared goal.",
            meaningVi: "Hợp tác, cộng tác làm việc cùng nhau",
            cefrLevel: "B1",
            descriptionEn: "Essential verb for describing cross-functional teamwork in interviews and daily standups.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000104"));
        sCollab.AddRelation("Synonym", "cooperate", 1);
        sCollab.AddRelation("Synonym", "team up", 2);
        sCollab.AddRelation("Antonym", "work alone", 1);
        sCollab.AddCollocation("collaborate closely with", "hợp tác chặt chẽ với", 1);
        sCollab.AddCollocation("collaborate on a project", "cộng tác trong một dự án", 2);
        sCollab.AddExample(
            "Our backend engineers collaborate closely with the frontend team every sprint.",
            "Các kỹ sư backend của chúng tôi hợp tác chặt chẽ với đội frontend trong mỗi sprint.",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001005"));
        sCollab.AddGrammarPattern("collaborate with [somebody] on [something]", "Hợp tác với ai về việc gì", "We collaborated with the QA team on automated testing.");
        list.Add(vCollab);

        // Word 4: scalable (B2)
        var vScalable = VocabularyEntity.Create(
            word: "scalable",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 2800,
            topicCategory: "Software Engineering & System Design",
            id: Guid.Parse("30000000-0000-0000-0000-000000000004"));
        vScalable.AddPronunciation("UK", "/ˈskeɪ.lə.bəl/", syllableCount: 3, stressPattern: "SCAL-a-ble (Ooo)");
        vScalable.AddPronunciation("US", "/ˈskeɪ.lə.bəl/", syllableCount: 3, stressPattern: "SCAL-a-ble (Ooo)");
        vScalable.AddWordFamily("scalability", "noun");
        vScalable.AddWordFamily("scale", "verb");

        var sScalable = vScalable.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Able to grow, expand, or handle a rapidly increasing amount of work or traffic smoothly.",
            meaningVi: "Có khả năng mở rộng quy mô / chịu tải cao",
            cefrLevel: "B2",
            descriptionEn: "Frequently used in System Design and Technical Interviews to describe cloud architectures and databases.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000105"));
        sScalable.AddRelation("Synonym", "expandable", 1);
        sScalable.AddRelation("Synonym", "extensible", 2);
        sScalable.AddRelation("Antonym", "rigid", 1);
        sScalable.AddCollocation("highly scalable architecture", "kiến trúc có khả năng mở rộng cao", 1);
        sScalable.AddCollocation("scalable cloud solution", "giải pháp đám mây dễ mở rộng", 2);
        sScalable.AddExample(
            "We designed a highly scalable microservices architecture to handle ten thousand concurrent users.",
            "Chúng tôi đã thiết kế một kiến trúc microservices có khả năng mở rộng cao để xử lý mười nghìn người dùng đồng thời.",
            "B2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001006"));
        sScalable.AddGrammarPattern("highly / easily + scalable", "Đi kèm trạng từ chỉ mức độ", "The database cluster is easily scalable.");
        list.Add(vScalable);

        // Word 5: itinerary (B1)
        var vItinerary = VocabularyEntity.Create(
            word: "itinerary",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 3400,
            topicCategory: "Travel & Planning",
            id: Guid.Parse("30000000-0000-0000-0000-000000000005"));
        vItinerary.AddPronunciation("UK", "/aɪˈtɪn.ər.ər.i/", syllableCount: 5, stressPattern: "i-TIN-er-ar-y (oOooo)");
        vItinerary.AddPronunciation("US", "/aɪˈtɪn.ə.rer.i/", syllableCount: 5, stressPattern: "i-TIN-er-ar-y (oOooo)");
        vItinerary.AddWordFamily("itinerant", "adjective");

        var sItinerary = vItinerary.AddSense(
            partOfSpeech: "noun",
            definitionEn: "A detailed plan or route of a journey, including places to visit and times.",
            meaningVi: "Lịch trình chuyến đi, hành trình du lịch",
            cefrLevel: "B1",
            descriptionEn: "Commonly used when discussing travel plans, business trips, and tour schedules.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000106"));
        sItinerary.AddRelation("Synonym", "travel schedule", 1);
        sItinerary.AddRelation("Synonym", "route plan", 2);
        sItinerary.AddCollocation("plan a travel itinerary", "lên lịch trình chuyến đi", 1);
        sItinerary.AddCollocation("packed itinerary", "lịch trình dày đặc", 2);
        sItinerary.AddExample(
            "Could you send me the detailed itinerary for our business trip to Singapore?",
            "Bạn có thể gửi cho tôi lịch trình chi tiết cho chuyến công tác tới Singapore của chúng ta không?",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001007"));
        sItinerary.AddGrammarPattern("on the itinerary", "Có trong lịch trình", "Visiting the old quarter is next on our itinerary.");
        list.Add(vItinerary);

        // Word 6: resilient (B2)
        var vResilient = VocabularyEntity.Create(
            word: "resilient",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 2650,
            topicCategory: "Personal Growth & System Engineering",
            id: Guid.Parse("30000000-0000-0000-0000-000000000006"));
        vResilient.AddPronunciation("UK", "/rɪˈzɪl.i.ənt/", syllableCount: 4, stressPattern: "re-SIL-i-ent (oOoo)");
        vResilient.AddPronunciation("US", "/rɪˈzɪl.jənt/", syllableCount: 3, stressPattern: "re-SIL-ient (oOo)");
        vResilient.AddWordFamily("resilience", "noun");
        vResilient.AddWordFamily("resiliently", "adverb");

        var sResilient = vResilient.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Able to recover quickly from difficult conditions, setbacks, or system failures.",
            meaningVi: "Kiên cường, bền bỉ; có khả năng tự phục hồi lỗi",
            cefrLevel: "B2",
            descriptionEn: "Great word for both HR behavioral interviews (personal resilience) and System Design interviews (fault-tolerant systems).",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000107"));
        sResilient.AddRelation("Synonym", "adaptable", 1);
        sResilient.AddRelation("Synonym", "fault-tolerant", 2);
        sResilient.AddRelation("Antonym", "fragile", 1);
        sResilient.AddCollocation("resilient to failure", "có khả năng chống chịu lỗi", 1);
        sResilient.AddCollocation("stay resilient under pressure", "giữ vững sự kiên cường dưới áp lực", 2);
        sResilient.AddExample(
            "Adding automatic retry policies made our payment service much more resilient to network timeouts.",
            "Việc bổ sung cơ chế tự động thử lại đã giúp dịch vụ thanh toán của chúng tôi chịu lỗi mất mạng tốt hơn nhiều.",
            "B2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001008"));
        sResilient.AddGrammarPattern("be resilient to + noun", "Chống chịu tốt trước điều gì", "The system is resilient to sudden traffic spikes.");
        list.Add(vResilient);

        return list;
    }
    public static List<TeacherVoiceEntity> BuildDefaultTeacherVoices() => new()
    {
        // Featured 6 primary Teacher Voices with custom Vietnamese coaching style descriptions
        new(
            id: "Aoede",
            name: "Aoede",
            displayName: "Aoede",
            type: "prebuilt",
            gender: "Female",
            persona: "Breezy",
            description: "Breezy, natural female voice suited for narration.",
            label: "Aoede (Nữ)",
            style: "Truyền cảm, ấm áp & rõ chữ",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 1),
        new(
            id: "Puck",
            name: "Puck",
            displayName: "Puck",
            type: "prebuilt",
            gender: "Male",
            persona: "Upbeat",
            description: "Upbeat and lively male voice, ideal for conversational interaction.",
            label: "Puck (Nam)",
            style: "Thân thiện, tự nhiên & năng động",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 2),
        new(
            id: "Kore",
            name: "Kore",
            displayName: "Kore",
            type: "prebuilt",
            gender: "Female",
            persona: "Firm",
            description: "Firm, articulate female voice suitable for general conversation.",
            label: "Kore (Nữ)",
            style: "Điềm tĩnh, chuẩn mực sư phạm",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 3),
        new(
            id: "Charon",
            name: "Charon",
            displayName: "Charon",
            type: "prebuilt",
            gender: "Male",
            persona: "Informative",
            description: "Calm and informative male voice, well suited for instruction.",
            label: "Charon (Nam)",
            style: "Trầm ấm, phong thái học thuật",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 4),
        new(
            id: "Fenrir",
            name: "Fenrir",
            displayName: "Fenrir",
            type: "prebuilt",
            gender: "Male",
            persona: "Excitable",
            description: "Excitable and energetic male voice with crisp delivery.",
            label: "Fenrir (Nam)",
            style: "Dứt khoát, rõ ràng, tốc độ tốt",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 5),
        new(
            id: "Leda",
            name: "Leda",
            displayName: "Leda",
            type: "prebuilt",
            gender: "Female",
            persona: "Youthful",
            description: "Youthful and soft female voice, great for patient guidance.",
            label: "Leda (Nữ)",
            style: "Nhẹ nhàng, kiên nhẫn cho A1-B1",
            isFeatured: true,
            isEnabled: true,
            displayOrder: 6),

        // Remaining 24 Google Prebuilt Voices (Total = 30 Voices)
        new(
            id: "Zephyr",
            name: "Zephyr",
            displayName: "Zephyr",
            type: "prebuilt",
            gender: "Female",
            persona: "Bright",
            description: "Bright and clear female voice.",
            label: "Zephyr (Nữ)",
            style: "Tươi sáng, phát âm trong trẻo",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 7),
        new(
            id: "Orus",
            name: "Orus",
            displayName: "Orus",
            type: "prebuilt",
            gender: "Male",
            persona: "Firm",
            description: "Firm, confident and direct male voice.",
            label: "Orus (Nam)",
            style: "Vững vàng, tự tin & trực diện",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 8),
        new(
            id: "Callirrhoe",
            name: "Callirrhoe",
            displayName: "Callirrhoe",
            type: "prebuilt",
            gender: "Female",
            persona: "Easy-going",
            description: "Easy-going and pleasant female voice.",
            label: "Callirrhoe (Nữ)",
            style: "Thoải mái, dễ chịu & tự nhiên",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 9),
        new(
            id: "Autonoe",
            name: "Autonoe",
            displayName: "Autonoe",
            type: "prebuilt",
            gender: "Female",
            persona: "Bright",
            description: "Bright and expressive female voice.",
            label: "Autonoe (Nữ)",
            style: "Tươi sáng & giàu biểu cảm",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 10),
        new(
            id: "Enceladus",
            name: "Enceladus",
            displayName: "Enceladus",
            type: "prebuilt",
            gender: "Male",
            persona: "Breathy",
            description: "Soft and breathy male voice.",
            label: "Enceladus (Nam)",
            style: "Trầm nhẹ, thư thái",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 11),
        new(
            id: "Iapetus",
            name: "Iapetus",
            displayName: "Iapetus",
            type: "prebuilt",
            gender: "Male",
            persona: "Clear",
            description: "Clear and clean male voice with casual tone.",
            label: "Iapetus (Nam)",
            style: "Rõ ràng, gần gũi đời thường",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 12),
        new(
            id: "Umbriel",
            name: "Umbriel",
            displayName: "Umbriel",
            type: "prebuilt",
            gender: "Male",
            persona: "Easy-going",
            description: "Easy-going and relaxed male voice.",
            label: "Umbriel (Nam)",
            style: "Thư giãn, nhịp độ thong thả",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 13),
        new(
            id: "Algieba",
            name: "Algieba",
            displayName: "Algieba",
            type: "prebuilt",
            gender: "Female",
            persona: "Smooth",
            description: "Smooth and articulate female voice.",
            label: "Algieba (Nữ)",
            style: "Mượt mà, tròn vành rõ chữ",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 14),
        new(
            id: "Despina",
            name: "Despina",
            displayName: "Despina",
            type: "prebuilt",
            gender: "Female",
            persona: "Smooth",
            description: "Smooth and soothing female voice.",
            label: "Despina (Nữ)",
            style: "Êm dịu, tạo cảm giác thoải mái",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 15),
        new(
            id: "Erinome",
            name: "Erinome",
            displayName: "Erinome",
            type: "prebuilt",
            gender: "Female",
            persona: "Clear",
            description: "Clear and poised female voice.",
            label: "Erinome (Nữ)",
            style: "Rõ nét, đĩnh đạc & chuyên nghiệp",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 16),
        new(
            id: "Algenib",
            name: "Algenib",
            displayName: "Algenib",
            type: "prebuilt",
            gender: "Male",
            persona: "Gravelly",
            description: "Gravelly, textured male voice.",
            label: "Algenib (Nam)",
            style: "Trầm khàn cá tính",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 17),
        new(
            id: "Rasalgethi",
            name: "Rasalgethi",
            displayName: "Rasalgethi",
            type: "prebuilt",
            gender: "Male",
            persona: "Informative",
            description: "Informative and measured male voice.",
            label: "Rasalgethi (Nam)",
            style: "Điềm đạm, chuẩn mực thuyết trình",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 18),
        new(
            id: "Laomedeia",
            name: "Laomedeia",
            displayName: "Laomedeia",
            type: "prebuilt",
            gender: "Female",
            persona: "Upbeat",
            description: "Upbeat and cheerful female voice.",
            label: "Laomedeia (Nữ)",
            style: "Vui tươi, truyền năng lượng tích cực",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 19),
        new(
            id: "Achernar",
            name: "Achernar",
            displayName: "Achernar",
            type: "prebuilt",
            gender: "Female",
            persona: "Soft",
            description: "Soft and gentle female voice.",
            label: "Achernar (Nữ)",
            style: "Dịu dàng, nhẹ nhàng",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 20),
        new(
            id: "Alnilam",
            name: "Alnilam",
            displayName: "Alnilam",
            type: "prebuilt",
            gender: "Male",
            persona: "Firm",
            description: "Firm and resonant male voice.",
            label: "Alnilam (Nam)",
            style: "Vang sáng, dứt khoát",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 21),
        new(
            id: "Schedar",
            name: "Schedar",
            displayName: "Schedar",
            type: "prebuilt",
            gender: "Male",
            persona: "Even",
            description: "Even and balanced male voice.",
            label: "Schedar (Nam)",
            style: "Cân bằng, nhịp điệu ổn định",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 22),
        new(
            id: "Gacrux",
            name: "Gacrux",
            displayName: "Gacrux",
            type: "prebuilt",
            gender: "Male",
            persona: "Mature",
            description: "Mature and grounded male voice.",
            label: "Gacrux (Nam)",
            style: "Trưởng thành, điềm tĩnh",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 23),
        new(
            id: "Pulcherrima",
            name: "Pulcherrima",
            displayName: "Pulcherrima",
            type: "prebuilt",
            gender: "Female",
            persona: "Forward",
            description: "Forward and engaging female voice.",
            label: "Pulcherrima (Nữ)",
            style: "Chủ động, cuốn hút người nghe",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 24),
        new(
            id: "Achird",
            name: "Achird",
            displayName: "Achird",
            type: "prebuilt",
            gender: "Male",
            persona: "Friendly",
            description: "Friendly and approachable male voice.",
            label: "Achird (Nam)",
            style: "Thân thiện, dễ bắt chuyện",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 25),
        new(
            id: "Zubenelgenubi",
            name: "Zubenelgenubi",
            displayName: "Zubenelgenubi",
            type: "prebuilt",
            gender: "Male",
            persona: "Casual",
            description: "Casual and conversational male voice.",
            label: "Zubenelgenubi (Nam)",
            style: "Tự nhiên, giao tiếp hằng ngày",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 26),
        new(
            id: "Vindemiatrix",
            name: "Vindemiatrix",
            displayName: "Vindemiatrix",
            type: "prebuilt",
            gender: "Female",
            persona: "Gentle",
            description: "Gentle and compassionate female voice.",
            label: "Vindemiatrix (Nữ)",
            style: "Ân cần, khích lệ học viên",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 27),
        new(
            id: "Sadachbia",
            name: "Sadachbia",
            displayName: "Sadachbia",
            type: "prebuilt",
            gender: "Female",
            persona: "Lively",
            description: "Lively and energetic female voice.",
            label: "Sadachbia (Nữ)",
            style: "Sôi nổi, giàu nhiệt huyết",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 28),
        new(
            id: "Sadaltager",
            name: "Sadaltager",
            displayName: "Sadaltager",
            type: "prebuilt",
            gender: "Male",
            persona: "Knowledgeable",
            description: "Knowledgeable and articulate male voice.",
            label: "Sadaltager (Nam)",
            style: "Uyên bác, giải thích sâu sắc",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 29),
        new(
            id: "Sulafat",
            name: "Sulafat",
            displayName: "Sulafat",
            type: "prebuilt",
            gender: "Female",
            persona: "Warm",
            description: "Warm and inviting female voice.",
            label: "Sulafat (Nữ)",
            style: "Ấm áp, truyền cảm hứng",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 30)
    };

    public static List<TopicEntity> BuildDefaultTopics() => new()
    {
        // --- Software Engineering & Technical Interview Topics ---
        new(
            code: "net10_clean_arch_ddd",
            name: ".NET 10, Clean Architecture, DDD & CQRS",
            displayNameVi: "Kiến trúc Clean Architecture, DDD & CQRS trên .NET 10",
            category: "Software Engineering",
            modeCode: "technical_interview",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Discuss Aggregate Roots, Domain Events, EF Core Interceptors, CQRS handlers, and Pragmatic Clean Architecture trade-offs.",
            isFeatured: true,
            displayOrder: 1,
            id: Guid.Parse("90000000-0000-0000-0000-000000000001")),
        new(
            code: "sql_server_indexing_tuning",
            name: "SQL Server Indexing, Query Tuning & Deadlocks",
            displayNameVi: "Tối ưu truy vấn SQL Server, Indexing & xử lý Deadlock",
            category: "Software Engineering",
            modeCode: "technical_interview",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Clustered vs non-clustered indexes, execution plans, key lookups, isolation levels, and high-concurrency deadlock mitigation.",
            isFeatured: true,
            displayOrder: 2,
            id: Guid.Parse("90000000-0000-0000-0000-000000000002")),
        new(
            code: "react_typescript_performance",
            name: "React 19, TypeScript, TanStack Query & Frontend Architecture",
            displayNameVi: "Kiến trúc Frontend: React 19, TanStack Query, Zustand & RBAC",
            category: "Software Engineering",
            modeCode: "technical_interview",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Feature-based architecture, server state vs client state, single-flight token refresh, and rendering optimization.",
            isFeatured: true,
            displayOrder: 3,
            id: Guid.Parse("90000000-0000-0000-0000-000000000003")),
        new(
            code: "signalr_realtime_streaming",
            name: "Realtime Audio Streaming with SignalR & WebSockets",
            displayNameVi: "Streaming âm thanh thời gian thực với SignalR & WebSockets",
            category: "Software Engineering",
            modeCode: "technical_discussion",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Low-latency bidirectional audio streaming, backpressure, PCM 16kHz/24kHz audio pipelines, and connection resilience.",
            isFeatured: false,
            displayOrder: 4,
            id: Guid.Parse("90000000-0000-0000-0000-000000000004")),

        // --- System Design Topics ---
        new(
            code: "sysdesign_payment_gateway",
            name: "Designing a High-Concurrency Payment & Order System",
            displayNameVi: "Thiết kế hệ thống Thanh toán & Đơn hàng chịu tải cao",
            category: "System Design",
            modeCode: "system_design",
            suggestedCefrLevel: "C1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Idempotency keys, distributed transactions, outbox pattern, Redis caching, message queues, and fault tolerance.",
            isFeatured: true,
            displayOrder: 5,
            id: Guid.Parse("90000000-0000-0000-0000-000000000005")),
        new(
            code: "sysdesign_realtime_chat",
            name: "Designing a Global Realtime Chat & Voice Tutoring Platform",
            displayNameVi: "Thiết kế nền tảng Chat & Gia sư giọng nói AI toàn cầu",
            category: "System Design",
            modeCode: "system_design",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "WebSocket gateway scaling, Redis pub/sub backplane, session affinity, transcript persistence, and AI model routing.",
            isFeatured: false,
            displayOrder: 6,
            id: Guid.Parse("90000000-0000-0000-0000-000000000006")),
        new(
            code: "sysdesign_rate_limiter",
            name: "Distributed Rate Limiter & API Gateway Security",
            displayNameVi: "Thiết kế Rate Limiter phân tán & Bảo mật API Gateway",
            category: "System Design",
            modeCode: "stress_interview",
            suggestedCefrLevel: "C1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Token bucket vs sliding window algorithms, Redis Lua scripts, graceful degradation, and DDoS protection.",
            isFeatured: false,
            displayOrder: 7,
            id: Guid.Parse("90000000-0000-0000-0000-000000000007")),

        // --- HR, Behavioral (STAR) & Career Topics ---
        new(
            code: "career_self_intro_goals",
            name: "Self-Introduction, Career Goals & Why This Role",
            displayNameVi: "Giới thiệu bản thân, Mục tiêu nghề nghiệp & Lý do ứng tuyển",
            category: "HR & Career",
            modeCode: "hr_interview",
            suggestedCefrLevel: "B1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Craft a compelling 90-second elevator pitch, articulate career aspirations, and align strengths with company culture.",
            isFeatured: true,
            displayOrder: 8,
            id: Guid.Parse("90000000-0000-0000-0000-000000000008")),
        new(
            code: "star_production_outage",
            name: "STAR Story: Resolving a Critical Production Incident Under Pressure",
            displayNameVi: "Kể chuyện STAR: Xử lý sự cố Production nghiêm trọng dưới áp lực thời gian",
            category: "HR & Career",
            modeCode: "behavioral_star",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Practice structuring Situation, Task, Action, and measurable Result when describing a high-stakes outage.",
            isFeatured: true,
            displayOrder: 9,
            id: Guid.Parse("90000000-0000-0000-0000-000000000009")),
        new(
            code: "star_team_conflict",
            name: "STAR Story: Navigating Technical Disagreement & Code Review Conflict",
            displayNameVi: "Kể chuyện STAR: Giải quyết bất đồng kỹ thuật khi Review Code",
            category: "HR & Career",
            modeCode: "behavioral_star",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Demonstrate empathy, data-driven persuasion, and collaborative leadership in cross-functional teams.",
            isFeatured: false,
            displayOrder: 10,
            id: Guid.Parse("90000000-0000-0000-0000-000000000010")),
        new(
            code: "salary_negotiation_offer",
            name: "Salary Expectations & Compensation Package Negotiation",
            displayNameVi: "Đàm phán lương & Phúc lợi khi nhận Offer quốc tế",
            category: "HR & Career",
            modeCode: "practice_interview",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Practice polite, confident phrases for discussing total compensation, remote work flexibility, and growth path.",
            isFeatured: false,
            displayOrder: 11,
            id: Guid.Parse("90000000-0000-0000-0000-000000000011")),
        new(
            code: "cv_flagship_project",
            name: "Deep Dive into My Flagship Full-Stack Project on CV",
            displayNameVi: "Phân tích chuyên sâu Dự án nổi bật nhất trong CV",
            category: "HR & Career",
            modeCode: "resume_deep_dive",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: false,
            description: "Explain your architecture ownership, performance bottlenecks solved, and business metrics improved.",
            isFeatured: false,
            displayOrder: 12,
            id: Guid.Parse("90000000-0000-0000-0000-000000000012")),

        // --- Workplace, Agile & Client Communication Topics ---
        new(
            code: "agile_sprint_standup_blocker",
            name: "Daily Standup: Sprint Progress, API Integration & Blocker Escalation",
            displayNameVi: "Họp Daily Standup: Báo cáo tiến độ Sprint & nêu Blocker",
            category: "Workplace & Agile",
            modeCode: "daily_standup",
            suggestedCefrLevel: "B1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Give crisp updates on yesterday's PRs, today's tasks, and raise cross-team API blockers clearly.",
            isFeatured: true,
            displayOrder: 13,
            id: Guid.Parse("90000000-0000-0000-0000-000000000013")),
        new(
            code: "bug_memory_leak_debugging",
            name: "Pair Debugging: Investigating a Memory Leak & Slow API Endpoint",
            displayNameVi: "Cùng đồng nghiệp Debug lỗi rò rỉ bộ nhớ & API phản hồi chậm",
            category: "Workplace & Agile",
            modeCode: "problem_solving",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Brainstorm root causes, analyze telemetry/logs, propose a safe hotfix, and plan regression tests.",
            isFeatured: false,
            displayOrder: 14,
            id: Guid.Parse("90000000-0000-0000-0000-000000000014")),
        new(
            code: "client_scope_timeline_negotiation",
            name: "Client Meeting: Clarifying Requirements & Managing Scope Creep",
            displayNameVi: "Họp với Khách hàng: Làm rõ yêu cầu & Đàm phán tiến độ dự án",
            category: "Workplace & Agile",
            modeCode: "client_meeting",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Translate technical constraints into business impact and propose phased delivery milestones to a client.",
            isFeatured: true,
            displayOrder: 15,
            id: Guid.Parse("90000000-0000-0000-0000-000000000015")),
        new(
            code: "roleplay_performance_review",
            name: "Roleplay: Annual 1-on-1 Performance Review with Engineering Manager",
            displayNameVi: "Đóng vai: Buổi đánh giá hiệu suất 1-1 cuối năm với Sếp",
            category: "Workplace & Agile",
            modeCode: "roleplay_mode",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Highlight key deliverables, discuss promotion readiness to Senior Engineer, and ask for constructive feedback.",
            isFeatured: false,
            displayOrder: 16,
            id: Guid.Parse("90000000-0000-0000-0000-000000000016")),

        // --- Daily Conversation, Travel & Lifestyle Topics ---
        new(
            code: "daily_routines_work_life",
            name: "Daily Routines, Remote Work Habits & Work-Life Balance",
            displayNameVi: "Thói quen hằng ngày, Làm việc từ xa & Cân bằng cuộc sống",
            category: "Daily Life & Culture",
            modeCode: "daily_conversation",
            suggestedCefrLevel: "A2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Talk naturally about morning routines, coffee habits, staying productive at home, and unwinding after work.",
            isFeatured: true,
            displayOrder: 17,
            id: Guid.Parse("90000000-0000-0000-0000-000000000017")),
        new(
            code: "travel_street_food_vietnam",
            name: "Travel Adventures & Introducing Vietnamese Street Food to Foreign Friends",
            displayNameVi: "Du lịch khám phá & Giới thiệu ẩm thực đường phố Việt Nam",
            category: "Daily Life & Culture",
            modeCode: "daily_conversation",
            suggestedCefrLevel: "B1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Describe flavors, textures, cultural traditions, and memorable travel destinations in vivid English.",
            isFeatured: true,
            displayOrder: 18,
            id: Guid.Parse("90000000-0000-0000-0000-000000000018")),
        new(
            code: "movies_books_podcasts",
            name: "Favorite Movies, Sci-Fi Shows, Books & Tech Podcasts",
            displayNameVi: "Bộ phim, Cuốn sách & Podcast yêu thích",
            category: "Daily Life & Culture",
            modeCode: "english_coach",
            suggestedCefrLevel: "B1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Practice summarizing plots, sharing personal takeaways, and using expressive adjectives.",
            isFeatured: false,
            displayOrder: 19,
            id: Guid.Parse("90000000-0000-0000-0000-000000000019")),
        new(
            code: "health_fitness_mindfulness",
            name: "Health, Running, Gym Habits & Mental Recharge",
            displayNameVi: "Sức khỏe, Chạy bộ, Tập luyện & Tái tạo năng lượng",
            category: "Daily Life & Culture",
            modeCode: "rapid_fire",
            suggestedCefrLevel: "A2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Build quick speaking reflexes around exercise routines, sleep quality, nutrition, and stress management.",
            isFeatured: false,
            displayOrder: 20,
            id: Guid.Parse("90000000-0000-0000-0000-000000000020")),

        // --- Debate, Pronunciation & Academic/Vocabulary Specialized Topics ---
        new(
            code: "debate_ai_software_engineers",
            name: "Debate: Will AI Agents Replace or Empower Software Engineers?",
            displayNameVi: "Tranh luận: AI Agent sẽ thay thế hay nâng tầm Kỹ sư phần mềm?",
            category: "Debate & Critical Thinking",
            modeCode: "debate_mode",
            suggestedCefrLevel: "C1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Practice structured argumentation, hedging, counter-arguments, and C1-level analytical discourse markers.",
            isFeatured: true,
            displayOrder: 21,
            id: Guid.Parse("90000000-0000-0000-0000-000000000021")),
        new(
            code: "debate_monolith_vs_microservices",
            name: "Deep Dive: Modular Monolith vs Microservices for Growing Startups",
            displayNameVi: "Đào sâu: Modular Monolith hay Microservices cho Startup đang tăng trưởng?",
            category: "Debate & Critical Thinking",
            modeCode: "deep_dive",
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Answer multi-layered Why / How / What-if questions about deployment complexity, team autonomy, and data consistency.",
            isFeatured: false,
            displayOrder: 22,
            id: Guid.Parse("90000000-0000-0000-0000-000000000022")),
        new(
            code: "pronunciation_linking_final_sounds",
            name: "Connected Speech, Word Stress & Tricky Ending Consonants (/s/, /t/, /d/, /θ/)",
            displayNameVi: "Luyện nối âm, Trọng âm từ & Âm cuối tiếng Anh chuẩn xác",
            category: "Pronunciation & Fluency",
            modeCode: "pronunciation_coach",
            suggestedCefrLevel: "B1",
            supportsConversation: true,
            supportsVocabulary: false,
            description: "Practice sentence rhythm, intonation patterns, and crisp articulation for natural spoken English.",
            isFeatured: false,
            displayOrder: 23,
            id: Guid.Parse("90000000-0000-0000-0000-000000000023")),
        new(
            code: "vocab_cloud_devops_k8s",
            name: "Cloud Computing, Docker, Kubernetes & CI/CD Pipelines",
            displayNameVi: "Từ vựng Cloud Computing, Docker, Kubernetes & CI/CD",
            category: "Vocabulary Specialization",
            modeCode: null,
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "High-value terminology for DevOps, container orchestration, observability, and zero-downtime deployments.",
            isFeatured: true,
            displayOrder: 24,
            id: Guid.Parse("90000000-0000-0000-0000-000000000024")),
        new(
            code: "vocab_business_idioms_phrasal_verbs",
            name: "Essential Workplace Phrasal Verbs & Executive Idioms",
            displayNameVi: "Cụm động từ (Phrasal Verbs) & Thành ngữ công sở thông dụng",
            category: "Vocabulary Specialization",
            modeCode: null,
            suggestedCefrLevel: "B2",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Master natural expressions like 'circle back', 'iron out bottlenecks', 'get the ball rolling', and 'on the same page'.",
            isFeatured: true,
            displayOrder: 25,
            id: Guid.Parse("90000000-0000-0000-0000-000000000025")),
        new(
            code: "vocab_ielts_academic_discourse",
            name: "IELTS Speaking Part 3 & Academic Discourse Connectors",
            displayNameVi: "Từ vựng học thuật IELTS Speaking Part 3 & Từ nối lập luận",
            category: "Vocabulary Specialization",
            modeCode: null,
            suggestedCefrLevel: "C1",
            supportsConversation: true,
            supportsVocabulary: true,
            description: "Elevate lexical resource with precise collocations for technology, education, environment, and urbanization.",
            isFeatured: false,
            displayOrder: 26,
            id: Guid.Parse("90000000-0000-0000-0000-000000000026"))
    };

    public static List<SpeechSpeedEntity> BuildDefaultSpeechSpeeds() => new()
    {
        new(
            code: "0.75x",
            speedValue: 0.75,
            label: "0.75x — Chậm & Rõ từng từ",
            descriptionVi: "Phù hợp cho người mới bắt đầu hoặc nghe từ mới khó",
            recommendedCefrRange: "A1 - A2",
            isDefault: false,
            isActive: true,
            displayOrder: 1,
            id: Guid.Parse("45000000-0000-0000-0000-000000000001")),
        new(
            code: "0.90x",
            speedValue: 0.90,
            label: "0.90x — Vừa phải, dễ bắt kịp",
            descriptionVi: "Nhịp độ sư phạm rõ ràng, dễ nghe trọng âm và nối âm",
            recommendedCefrRange: "A2 - B1",
            isDefault: false,
            isActive: true,
            displayOrder: 2,
            id: Guid.Parse("45000000-0000-0000-0000-000000000002")),
        new(
            code: "1.00x",
            speedValue: 1.00,
            label: "1.00x — Tự nhiên chuẩn bản xứ",
            descriptionVi: "Tốc độ hội thoại tiêu chuẩn hằng ngày và phỏng vấn",
            recommendedCefrRange: "B1 - B2",
            isDefault: true,
            isActive: true,
            displayOrder: 3,
            id: Guid.Parse("45000000-0000-0000-0000-000000000003")),
        new(
            code: "1.15x",
            speedValue: 1.15,
            label: "1.15x — Nhanh & Năng động",
            descriptionVi: "Luyện phản xạ nghe nhanh trong họp Scrum hoặc tranh luận",
            recommendedCefrRange: "B2 - C1",
            isDefault: false,
            isActive: true,
            displayOrder: 4,
            id: Guid.Parse("45000000-0000-0000-0000-000000000004")),
        new(
            code: "1.25x",
            speedValue: 1.25,
            label: "1.25x — Thử thách tốc độ cao",
            descriptionVi: "Mô phỏng áp lực cao và người bản xứ nói nhanh",
            recommendedCefrRange: "C1 - C2",
            isDefault: false,
            isActive: true,
            displayOrder: 5,
            id: Guid.Parse("45000000-0000-0000-0000-000000000005"))
    };

}

