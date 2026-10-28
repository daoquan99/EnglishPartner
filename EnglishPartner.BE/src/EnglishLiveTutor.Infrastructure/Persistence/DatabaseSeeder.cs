using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Voices;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task EnsureSchemaAndSeedAsync(
        IDbContextFactory<AppDbContext> dbFactory,
        AdminUserSeedOptions? adminOptions = null,
        DemoLearnerSeedOptions? demoLearnerOptions = null,
        IHostEnvironment? environment = null,
        ILogger? logger = null,
        bool migrate = true,
        bool seed = true)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        // 1. Ensure all Vocabulary, Daily Learning, AI Model Catalog, and Pronunciation tables exist in SQL Server
        if (migrate)
        {
            await db.Database.MigrateAsync();
        }

        if (!seed)
        {
            return;
        }

        // 2. Seed AI Model Catalog idempotently
        if (!await db.AiModelCatalog.AnyAsync())
        {
            var models = new List<AiModelConfigEntity>
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
                    capabilityNote: "Model Ä‘a phÆ°Æ¡ng thá»©c tháº¿ há»‡ má»›i nháº¥t, há»— trá»£ cáº£ táº¡o tá»« vá»±ng JSON vÃ  cháº¥m Ä‘iá»ƒm phÃ¡t Ã¢m trá»±c tiáº¿p tá»« Ã¢m thanh.",
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
                    capabilityNote: "Há»— trá»£ táº¡o tá»« vá»±ng vÃ  Ä‘Ã¡nh giÃ¡ phÃ¡t Ã¢m báº±ng Ã¢m thanh.",
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
                    capabilityNote: "Tá»‘c Ä‘á»™ pháº£n há»“i cá»±c nhanh, chi phÃ­ tháº¥p.",
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
                    capabilityNote: "PhiÃªn báº£n nháº¹ há»— trá»£ cáº£ táº¡o tá»« vá»±ng vÃ  cháº¥m phÃ¡t Ã¢m.",
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
                    capabilityNote: "Báº£n preview dÃ²ng Gemini 3 Flash.",
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
                    capabilityNote: "Model á»•n Ä‘á»‹nh phá»• biáº¿n; lÆ°u Ã½ má»™t sá»‘ tÃ i khoáº£n má»›i cÃ³ thá»ƒ giá»›i háº¡n quyá»n truy cáº­p dÃ²ng 2.5.",
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
                    capabilityNote: "PhiÃªn báº£n Lite cá»§a dÃ²ng Gemini 2.5.",
                    displayOrder: 7,
                    id: Guid.Parse("10000000-0000-0000-0000-000000000007")),
                new(
                    displayName: "Gemini 3.7 Flash (Deprecated â†’ 3.8 Flash)",
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
                    capabilityNote: "ÄÃ£ ngá»«ng khuyáº¿n nghá»‹ (Deprecated) vÃ  tá»± Ä‘á»™ng Ä‘á»‹nh tuyáº¿n sang gemini-3.8-flash.",
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
                    capabilityNote: "Chá»‰ há»— trá»£ táº¡o tá»« vá»±ng vÄƒn báº£n (Text Generation); KHÃ”NG há»— trá»£ Ä‘áº§u vÃ o Ã¢m thanh Ä‘á»ƒ cháº¥m phÃ¡t Ã¢m.",
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
                    capabilityNote: "Chá»‰ há»— trá»£ táº¡o tá»« vá»±ng vÄƒn báº£n (Text Generation); KHÃ”NG há»— trá»£ Ä‘áº§u vÃ o Ã¢m thanh Ä‘á»ƒ cháº¥m phÃ¡t Ã¢m.",
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
                    capabilityNote: "ChuyÃªn dá»¥ng cho Hub 2 Realtime Transcription.",
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
                    capabilityNote: "ChuyÃªn dá»¥ng cho Hub 3 Realtime Vietnamese Translation.",
                    displayOrder: 12,
                    id: Guid.Parse("10000000-0000-0000-0000-000000000012")),
                new(
                    displayName: "Gemini 3.8 Live",
                    modelCode: "gemini-3.8-live",
                    supportsLiveAudio: true,
                    supportsStreaming: true,
                    isAvailable: true,
                    isEnabled: true,
                    capabilityNote: "ChuyÃªn dá»¥ng cho Hub 1 Live Voice Tutor.",
                    displayOrder: 13,
                    id: Guid.Parse("10000000-0000-0000-0000-000000000013")),
                new(
                    displayName: "Gemini 3.8 Live Extended Thinking",
                    modelCode: "gemini-3.8-live-extended-thinking",
                    supportsLiveAudio: true,
                    supportsStreaming: true,
                    isAvailable: true,
                    isEnabled: true,
                    capabilityNote: "ChuyÃªn dá»¥ng cho Hub 1 Live Voice Tutor (Thinking Mode).",
                    displayOrder: 14,
                    id: Guid.Parse("10000000-0000-0000-0000-000000000014"))
            };

            db.AiModelCatalog.AddRange(models);
            await db.SaveChangesAsync();
        }

        // Ensure Antigravity CLI Local models exist in catalog
        var agyCatalogSeed = new (string Code, string Name, string Note, int Order, Guid Id)[]
        {
            ("gemini-3.8-flash-medium", "Gemini 3.8 Flash Medium (AGY Local)", "Model Antigravity CLI cục bộ trên máy, cân bằng tốc độ và chất lượng cho sinh bài học, từ vựng và chấm bài viết.", 20, Guid.Parse("10000000-0000-0000-0000-000000000020")),
            ("gemini-3.8-flash-low", "Gemini 3.8 Flash Low (AGY Local)", "Model Antigravity CLI cục bộ trên máy với tốc độ phản hồi nhanh nhất.", 21, Guid.Parse("10000000-0000-0000-0000-000000000021")),
            ("gemini-3.8-flash-high", "Gemini 3.8 Flash High (AGY Local)", "Model Antigravity CLI cục bộ trên máy với tư duy suy luận sâu (Thinking Mode) cho chấm điểm bài luận và phân tích ngữ pháp.", 22, Guid.Parse("10000000-0000-0000-0000-000000000022")),
        };

        bool anyAgyAdded = false;
        foreach (var item in agyCatalogSeed)
        {
            if (!await db.AiModelCatalog.AnyAsync(m => m.ModelCode == item.Code))
            {
                db.AiModelCatalog.Add(new AiModelConfigEntity(
                    displayName: item.Name,
                    modelCode: item.Code,
                    supportsTextGeneration: true,
                    supportsStructuredOutput: true,
                    supportsAudioInput: false,
                    supportsPronunciationAssessment: true,
                    supportsStreaming: false,
                    isAvailable: true,
                    isEnabled: true,
                    capabilityNote: item.Note,
                    displayOrder: item.Order,
                    id: item.Id));
                anyAgyAdded = true;
            }
        }
        if (anyAgyAdded)
        {
            await db.SaveChangesAsync();
        }

        // 3. Seed Default Pronunciation Scoring Rubric ENGLISH_PRONUNCIATION_V1
        if (!await db.ScoringRubrics.AnyAsync(r => r.Code == "ENGLISH_PRONUNCIATION_V1"))
        {
            var rubricId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            var rubric = new ScoringRubricEntity(
                code: "ENGLISH_PRONUNCIATION_V1",
                name: "TiÃªu chuáº©n cháº¥m phÃ¡t Ã¢m tiáº¿ng Anh v1 (Accuracy 40% Â· Clarity 30% Â· Fluency 30%)",
                description: "ÄÃ¡nh giÃ¡ phÃ¡t Ã¢m tá»« vá»±ng vÃ  cÃ¢u vÃ­ dá»¥ dá»±a trÃªn Ä‘á»™ chÃ­nh xÃ¡c Ã¢m vá»‹, Ä‘á»™ rÃµ rÃ ng vÃ  Ä‘á»™ trÃ´i cháº£y.",
                version: 1,
                assessmentType: "Pronunciation",
                isActive: true,
                id: rubricId);

            rubric.AddCriterion(
                code: "Accuracy",
                name: "Accuracy (Äá»™ chÃ­nh xÃ¡c Ã¢m vá»‹ & trá»ng Ã¢m)",
                description: "Phonetic accuracy of vowel and consonant sounds and stress patterns.",
                weight: 40.00m,
                displayOrder: 1,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000101"));

            rubric.AddCriterion(
                code: "Clarity",
                name: "Clarity (Äá»™ rÃµ chá»¯ & kháº©u hÃ¬nh)",
                description: "Vocal projection, articulation clarity, and audibility consistency.",
                weight: 30.00m,
                displayOrder: 2,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000102"));

            rubric.AddCriterion(
                code: "Fluency",
                name: "Fluency (Nhá»‹p Ä‘iá»‡u & Ä‘á»™ trÃ´i cháº£y)",
                description: "Speaking rhythm, natural flow, and absence of hesitation pauses.",
                weight: 30.00m,
                displayOrder: 3,
                minimumScore: 0m,
                maximumScore: 100m,
                isEnabled: true,
                id: Guid.Parse("20000000-0000-0000-0000-000000000103"));

            rubric.ValidateWeights();
            db.ScoringRubrics.Add(rubric);
            await db.SaveChangesAsync();
        }

        // 3b. Seed Default Lesson Exercise AI Rubric Templates
        await LessonRubricSeeder.SeedAsync(db);

        // 4. Seed Starter Vocabulary Catalog (A1, A2, B1, B2) idempotently
        if (!await db.Vocabularies.AnyAsync())
        {
            var starterVocabs = BuildStarterVocabularyCatalog();
            db.Vocabularies.AddRange(starterVocabs);
            await db.SaveChangesAsync();
        }

        // 5. Seed CEFR Level Prompts & 19 Conversation Mode Prompts (CefrPromptBuilder in DB)
        if (!await db.CefrLevelPrompts.AnyAsync())
        {
            db.CefrLevelPrompts.AddRange(CefrPromptBuilder.GetDefaultCefrLevelPrompts());
            await db.SaveChangesAsync();
        }

        if (!await db.ConversationModePrompts.AnyAsync())
        {
            db.ConversationModePrompts.AddRange(CefrPromptBuilder.GetDefaultConversationModePrompts());
            await db.SaveChangesAsync();
        }

        // 5b. Seed Teacher Speech Speeds into Database
        if (!await db.SpeechSpeeds.AnyAsync())
        {
            db.SpeechSpeeds.AddRange(BuildDefaultSpeechSpeeds());
            await db.SaveChangesAsync();
        }

        // 6. Seed 30 Google Prebuilt Teacher Voices
        if (!await db.TeacherVoices.AnyAsync())
        {
            db.TeacherVoices.AddRange(BuildDefaultTeacherVoices());
            await db.SaveChangesAsync();
        }

        // 6b. Seed Curated Topics Catalog (Conversation & Vocabulary Topics)
        if (!await db.Topics.AnyAsync())
        {
            db.Topics.AddRange(BuildDefaultTopics());
            await db.SaveChangesAsync();
        }

        // 7. Seed Default Permissions, Roles (ADMIN, TEACHER, LEARNER), and Starter Accounts
        await SeedIdentityAsync(db, adminOptions, demoLearnerOptions, environment, logger);
    }

    private static List<VocabularyEntity> BuildStarterVocabularyCatalog()
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
        vBeautiful.AddPronunciation("UK", "/ËˆbjuË.tÉª.fÉ™l/", syllableCount: 3, stressPattern: "BEAU-ti-ful (Ooo)");
        vBeautiful.AddPronunciation("US", "/ËˆbjuË.tÌ¬É™.fÉ™l/", syllableCount: 3, stressPattern: "BEAU-ti-ful (Ooo)");
        vBeautiful.AddWordFamily("beauty", "noun");
        vBeautiful.AddWordFamily("beautify", "verb");
        vBeautiful.AddWordFamily("beautifully", "adverb");

        var sBeautiful = vBeautiful.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Pleasing the senses or mind aesthetically; very attractive.",
            meaningVi: "Äáº¹p, xinh Ä‘áº¹p, tuyá»‡t Ä‘áº¹p",
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
        sBeautiful.AddCollocation("beautiful scenery", "phong cáº£nh tuyá»‡t Ä‘áº¹p", 1);
        sBeautiful.AddCollocation("beautiful smile", "ná»¥ cÆ°á»i ráº¡ng rá»¡", 2);
        sBeautiful.AddCollocation("absolutely beautiful", "Ä‘áº¹p hoÃ n háº£o / cá»±c ká»³ Ä‘áº¹p", 3);
        sBeautiful.AddExample(
            "Da Nang is famous for its beautiful beaches and friendly people.",
            "ÄÃ  Náºµng ná»•i tiáº¿ng vá»›i nhá»¯ng bÃ£i biá»ƒn tuyá»‡t Ä‘áº¹p vÃ  con ngÆ°á»i thÃ¢n thiá»‡n.",
            "A1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001001"));
        sBeautiful.AddExample(
            "She played a beautiful melody on the piano.",
            "CÃ´ áº¥y Ä‘Ã£ chÆ¡i má»™t giai Ä‘iá»‡u tuyá»‡t Ä‘áº¹p trÃªn Ä‘Ã n dÆ°Æ¡ng cáº§m.",
            "A2",
            2,
            id: Guid.Parse("30000000-0000-0000-0000-000000001002"));
        sBeautiful.AddGrammarPattern("beautiful + noun", "Äá»©ng trÆ°á»›c danh tá»« Ä‘á»ƒ bá»• nghÄ©a", "a beautiful morning");
        sBeautiful.AddGrammarPattern("be / look / sound + beautiful", "Äá»©ng sau Ä‘á»™ng tá»« ná»‘i (linking verbs)", "The sunset looks beautiful.");
        list.Add(vBeautiful);

        // Word 2: bank (Multi-sense example: Sense 1 A2 financial, Sense 2 B1 river bank)
        var vBank = VocabularyEntity.Create(
            word: "bank",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 410,
            topicCategory: "Finance & Geography (Multi-Sense)",
            id: Guid.Parse("30000000-0000-0000-0000-000000000002"));
        vBank.AddPronunciation("UK", "/bÃ¦Å‹k/", syllableCount: 1, stressPattern: "bank (O)");
        vBank.AddPronunciation("US", "/bÃ¦Å‹k/", syllableCount: 1, stressPattern: "bank (O)");
        vBank.AddWordFamily("banker", "noun");
        vBank.AddWordFamily("banking", "noun");

        var sBank1 = vBank.AddSense(
            partOfSpeech: "noun",
            definitionEn: "An organization where people and businesses can invest or borrow money and exchange currency.",
            meaningVi: "NgÃ¢n hÃ ng (tá»• chá»©c tÃ i chÃ­nh)",
            cefrLevel: "A2",
            descriptionEn: "Sense 1: Financial institution where you keep money in an account or apply for loans.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000102"));
        sBank1.AddRelation("Synonym", "financial institution", 1);
        sBank1.AddRelation("Synonym", "lender", 2);
        sBank1.AddCollocation("open a bank account", "má»Ÿ tÃ i khoáº£n ngÃ¢n hÃ ng", 1);
        sBank1.AddCollocation("bank transfer", "chuyá»ƒn khoáº£n ngÃ¢n hÃ ng", 2);
        sBank1.AddExample(
            "I need to go to the bank to open a savings account.",
            "TÃ´i cáº§n Ä‘áº¿n ngÃ¢n hÃ ng Ä‘á»ƒ má»Ÿ má»™t tÃ i khoáº£n tiáº¿t kiá»‡m.",
            "A2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001003"));
        sBank1.AddGrammarPattern("deposit / withdraw money from a bank", "Gá»­i / rÃºt tiá»n tá»« ngÃ¢n hÃ ng", "She withdrew cash from the bank.");

        var sBank2 = vBank.AddSense(
            partOfSpeech: "noun",
            definitionEn: "Sloping raised land, especially along the sides of a river or lake.",
            meaningVi: "Bá» sÃ´ng, bá» há»“",
            cefrLevel: "B1",
            descriptionEn: "Sense 2: The land alongside a body of water such as a river or canal.",
            isPrimary: false,
            displayOrder: 2,
            id: Guid.Parse("30000000-0000-0000-0000-000000000103"));
        sBank2.AddRelation("Synonym", "riverbank", 1);
        sBank2.AddRelation("Synonym", "shore", 2);
        sBank2.AddCollocation("along the river bank", "dá»c theo bá» sÃ´ng", 1);
        sBank2.AddCollocation("steep grassy bank", "bá» cá» dá»‘c", 2);
        sBank2.AddExample(
            "We walked peacefully along the grassy bank of the Red River at sunset.",
            "ChÃºng tÃ´i Ä‘i dáº¡o yÃªn bÃ¬nh dá»c theo bá» cá» cá»§a sÃ´ng Há»“ng lÃºc hoÃ ng hÃ´n.",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001004"));
        sBank2.AddGrammarPattern("on the bank of + the river", "TrÃªn bá» cá»§a con sÃ´ng", "A small village sits on the bank of the river.");
        list.Add(vBank);

        // Word 3: collaborate (B1)
        var vCollab = VocabularyEntity.Create(
            word: "collaborate",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 1450,
            topicCategory: "Workplace & Teamwork",
            id: Guid.Parse("30000000-0000-0000-0000-000000000003"));
        vCollab.AddPronunciation("UK", "/kÉ™ËˆlÃ¦b.É™.reÉªt/", syllableCount: 4, stressPattern: "col-LAB-o-rate (oOoo)");
        vCollab.AddPronunciation("US", "/kÉ™ËˆlÃ¦b.É™.reÉªt/", syllableCount: 4, stressPattern: "col-LAB-o-rate (oOoo)");
        vCollab.AddWordFamily("collaboration", "noun");
        vCollab.AddWordFamily("collaborative", "adjective");
        vCollab.AddWordFamily("collaborator", "noun");

        var sCollab = vCollab.AddSense(
            partOfSpeech: "verb",
            definitionEn: "To work together with one or more people or organizations to achieve a shared goal.",
            meaningVi: "Há»£p tÃ¡c, cá»™ng tÃ¡c lÃ m viá»‡c cÃ¹ng nhau",
            cefrLevel: "B1",
            descriptionEn: "Essential verb for describing cross-functional teamwork in interviews and daily standups.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000104"));
        sCollab.AddRelation("Synonym", "cooperate", 1);
        sCollab.AddRelation("Synonym", "team up", 2);
        sCollab.AddRelation("Antonym", "work alone", 1);
        sCollab.AddCollocation("collaborate closely with", "há»£p tÃ¡c cháº·t cháº½ vá»›i", 1);
        sCollab.AddCollocation("collaborate on a project", "cá»™ng tÃ¡c trong má»™t dá»± Ã¡n", 2);
        sCollab.AddExample(
            "Our backend engineers collaborate closely with the frontend team every sprint.",
            "CÃ¡c ká»¹ sÆ° backend cá»§a chÃºng tÃ´i há»£p tÃ¡c cháº·t cháº½ vá»›i Ä‘á»™i frontend trong má»—i sprint.",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001005"));
        sCollab.AddGrammarPattern("collaborate with [somebody] on [something]", "Há»£p tÃ¡c vá»›i ai vá» viá»‡c gÃ¬", "We collaborated with the QA team on automated testing.");
        list.Add(vCollab);

        // Word 4: scalable (B2)
        var vScalable = VocabularyEntity.Create(
            word: "scalable",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 2800,
            topicCategory: "Software Engineering & System Design",
            id: Guid.Parse("30000000-0000-0000-0000-000000000004"));
        vScalable.AddPronunciation("UK", "/ËˆskeÉª.lÉ™.bÉ™l/", syllableCount: 3, stressPattern: "SCAL-a-ble (Ooo)");
        vScalable.AddPronunciation("US", "/ËˆskeÉª.lÉ™.bÉ™l/", syllableCount: 3, stressPattern: "SCAL-a-ble (Ooo)");
        vScalable.AddWordFamily("scalability", "noun");
        vScalable.AddWordFamily("scale", "verb");

        var sScalable = vScalable.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Able to grow, expand, or handle a rapidly increasing amount of work or traffic smoothly.",
            meaningVi: "CÃ³ kháº£ nÄƒng má»Ÿ rá»™ng quy mÃ´ / chá»‹u táº£i cao",
            cefrLevel: "B2",
            descriptionEn: "Frequently used in System Design and Technical Interviews to describe cloud architectures and databases.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000105"));
        sScalable.AddRelation("Synonym", "expandable", 1);
        sScalable.AddRelation("Synonym", "extensible", 2);
        sScalable.AddRelation("Antonym", "rigid", 1);
        sScalable.AddCollocation("highly scalable architecture", "kiáº¿n trÃºc cÃ³ kháº£ nÄƒng má»Ÿ rá»™ng cao", 1);
        sScalable.AddCollocation("scalable cloud solution", "giáº£i phÃ¡p Ä‘Ã¡m mÃ¢y dá»… má»Ÿ rá»™ng", 2);
        sScalable.AddExample(
            "We designed a highly scalable microservices architecture to handle ten thousand concurrent users.",
            "ChÃºng tÃ´i Ä‘Ã£ thiáº¿t káº¿ má»™t kiáº¿n trÃºc microservices cÃ³ kháº£ nÄƒng má»Ÿ rá»™ng cao Ä‘á»ƒ xá»­ lÃ½ mÆ°á»i nghÃ¬n ngÆ°á»i dÃ¹ng Ä‘á»“ng thá»i.",
            "B2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001006"));
        sScalable.AddGrammarPattern("highly / easily + scalable", "Äi kÃ¨m tráº¡ng tá»« chá»‰ má»©c Ä‘á»™", "The database cluster is easily scalable.");
        list.Add(vScalable);

        // Word 5: itinerary (B1)
        var vItinerary = VocabularyEntity.Create(
            word: "itinerary",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 3400,
            topicCategory: "Travel & Planning",
            id: Guid.Parse("30000000-0000-0000-0000-000000000005"));
        vItinerary.AddPronunciation("UK", "/aÉªËˆtÉªn.É™r.É™r.i/", syllableCount: 5, stressPattern: "i-TIN-er-ar-y (oOooo)");
        vItinerary.AddPronunciation("US", "/aÉªËˆtÉªn.É™.rer.i/", syllableCount: 5, stressPattern: "i-TIN-er-ar-y (oOooo)");
        vItinerary.AddWordFamily("itinerant", "adjective");

        var sItinerary = vItinerary.AddSense(
            partOfSpeech: "noun",
            definitionEn: "A detailed plan or route of a journey, including places to visit and times.",
            meaningVi: "Lá»‹ch trÃ¬nh chuyáº¿n Ä‘i, hÃ nh trÃ¬nh du lá»‹ch",
            cefrLevel: "B1",
            descriptionEn: "Commonly used when discussing travel plans, business trips, and tour schedules.",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000106"));
        sItinerary.AddRelation("Synonym", "travel schedule", 1);
        sItinerary.AddRelation("Synonym", "route plan", 2);
        sItinerary.AddCollocation("plan a travel itinerary", "lÃªn lá»‹ch trÃ¬nh chuyáº¿n Ä‘i", 1);
        sItinerary.AddCollocation("packed itinerary", "lá»‹ch trÃ¬nh dÃ y Ä‘áº·c", 2);
        sItinerary.AddExample(
            "Could you send me the detailed itinerary for our business trip to Singapore?",
            "Báº¡n cÃ³ thá»ƒ gá»­i cho tÃ´i lá»‹ch trÃ¬nh chi tiáº¿t cho chuyáº¿n cÃ´ng tÃ¡c tá»›i Singapore cá»§a chÃºng ta khÃ´ng?",
            "B1",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001007"));
        sItinerary.AddGrammarPattern("on the itinerary", "CÃ³ trong lá»‹ch trÃ¬nh", "Visiting the old quarter is next on our itinerary.");
        list.Add(vItinerary);

        // Word 6: resilient (B2)
        var vResilient = VocabularyEntity.Create(
            word: "resilient",
            languageCode: "en",
            sourceType: "Seeded",
            frequencyRank: 2650,
            topicCategory: "Personal Growth & System Engineering",
            id: Guid.Parse("30000000-0000-0000-0000-000000000006"));
        vResilient.AddPronunciation("UK", "/rÉªËˆzÉªl.i.É™nt/", syllableCount: 4, stressPattern: "re-SIL-i-ent (oOoo)");
        vResilient.AddPronunciation("US", "/rÉªËˆzÉªl.jÉ™nt/", syllableCount: 3, stressPattern: "re-SIL-ient (oOo)");
        vResilient.AddWordFamily("resilience", "noun");
        vResilient.AddWordFamily("resiliently", "adverb");

        var sResilient = vResilient.AddSense(
            partOfSpeech: "adjective",
            definitionEn: "Able to recover quickly from difficult conditions, setbacks, or system failures.",
            meaningVi: "KiÃªn cÆ°á»ng, bá»n bá»‰; cÃ³ kháº£ nÄƒng tá»± phá»¥c há»“i lá»—i",
            cefrLevel: "B2",
            descriptionEn: "Great word for both HR behavioral interviews (personal resilience) and System Design interviews (fault-tolerant systems).",
            isPrimary: true,
            displayOrder: 1,
            id: Guid.Parse("30000000-0000-0000-0000-000000000107"));
        sResilient.AddRelation("Synonym", "adaptable", 1);
        sResilient.AddRelation("Synonym", "fault-tolerant", 2);
        sResilient.AddRelation("Antonym", "fragile", 1);
        sResilient.AddCollocation("resilient to failure", "cÃ³ kháº£ nÄƒng chá»‘ng chá»‹u lá»—i", 1);
        sResilient.AddCollocation("stay resilient under pressure", "giá»¯ vá»¯ng sá»± kiÃªn cÆ°á»ng dÆ°á»›i Ã¡p lá»±c", 2);
        sResilient.AddExample(
            "Adding automatic retry policies made our payment service much more resilient to network timeouts.",
            "Viá»‡c bá»• sung cÆ¡ cháº¿ tá»± Ä‘á»™ng thá»­ láº¡i Ä‘Ã£ giÃºp dá»‹ch vá»¥ thanh toÃ¡n cá»§a chÃºng tÃ´i chá»‹u lá»—i máº¥t máº¡ng tá»‘t hÆ¡n nhiá»u.",
            "B2",
            1,
            id: Guid.Parse("30000000-0000-0000-0000-000000001008"));
        sResilient.AddGrammarPattern("be resilient to + noun", "Chá»‘ng chá»‹u tá»‘t trÆ°á»›c Ä‘iá»u gÃ¬", "The system is resilient to sudden traffic spikes.");
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
            label: "Aoede (Ná»¯)",
            style: "Truyá»n cáº£m, áº¥m Ã¡p & rÃµ chá»¯",
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
            style: "ThÃ¢n thiá»‡n, tá»± nhiÃªn & nÄƒng Ä‘á»™ng",
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
            label: "Kore (Ná»¯)",
            style: "Äiá»m tÄ©nh, chuáº©n má»±c sÆ° pháº¡m",
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
            style: "Tráº§m áº¥m, phong thÃ¡i há»c thuáº­t",
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
            style: "Dá»©t khoÃ¡t, rÃµ rÃ ng, tá»‘c Ä‘á»™ tá»‘t",
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
            label: "Leda (Ná»¯)",
            style: "Nháº¹ nhÃ ng, kiÃªn nháº«n cho A1-B1",
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
            label: "Zephyr (Ná»¯)",
            style: "TÆ°Æ¡i sÃ¡ng, phÃ¡t Ã¢m trong tráº»o",
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
            style: "Vá»¯ng vÃ ng, tá»± tin & trá»±c diá»‡n",
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
            label: "Callirrhoe (Ná»¯)",
            style: "Thoáº£i mÃ¡i, dá»… chá»‹u & tá»± nhiÃªn",
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
            label: "Autonoe (Ná»¯)",
            style: "TÆ°Æ¡i sÃ¡ng & giÃ u biá»ƒu cáº£m",
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
            style: "Tráº§m nháº¹, thÆ° thÃ¡i",
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
            style: "RÃµ rÃ ng, gáº§n gÅ©i Ä‘á»i thÆ°á»ng",
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
            style: "ThÆ° giÃ£n, nhá»‹p Ä‘á»™ thong tháº£",
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
            label: "Algieba (Ná»¯)",
            style: "MÆ°á»£t mÃ , trÃ²n vÃ nh rÃµ chá»¯",
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
            label: "Despina (Ná»¯)",
            style: "ÃŠm dá»‹u, táº¡o cáº£m giÃ¡c thoáº£i mÃ¡i",
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
            label: "Erinome (Ná»¯)",
            style: "RÃµ nÃ©t, Ä‘Ä©nh Ä‘áº¡c & chuyÃªn nghiá»‡p",
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
            style: "Tráº§m khÃ n cÃ¡ tÃ­nh",
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
            style: "Äiá»m Ä‘áº¡m, chuáº©n má»±c thuyáº¿t trÃ¬nh",
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
            label: "Laomedeia (Ná»¯)",
            style: "Vui tÆ°Æ¡i, truyá»n nÄƒng lÆ°á»£ng tÃ­ch cá»±c",
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
            label: "Achernar (Ná»¯)",
            style: "Dá»‹u dÃ ng, nháº¹ nhÃ ng",
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
            style: "Vang sÃ¡ng, dá»©t khoÃ¡t",
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
            style: "CÃ¢n báº±ng, nhá»‹p Ä‘iá»‡u á»•n Ä‘á»‹nh",
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
            style: "TrÆ°á»Ÿng thÃ nh, Ä‘iá»m tÄ©nh",
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
            label: "Pulcherrima (Ná»¯)",
            style: "Chá»§ Ä‘á»™ng, cuá»‘n hÃºt ngÆ°á»i nghe",
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
            style: "ThÃ¢n thiá»‡n, dá»… báº¯t chuyá»‡n",
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
            style: "Tá»± nhiÃªn, giao tiáº¿p háº±ng ngÃ y",
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
            label: "Vindemiatrix (Ná»¯)",
            style: "Ã‚n cáº§n, khÃ­ch lá»‡ há»c viÃªn",
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
            label: "Sadachbia (Ná»¯)",
            style: "SÃ´i ná»•i, giÃ u nhiá»‡t huyáº¿t",
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
            style: "UyÃªn bÃ¡c, giáº£i thÃ­ch sÃ¢u sáº¯c",
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
            label: "Sulafat (Ná»¯)",
            style: "áº¤m Ã¡p, truyá»n cáº£m há»©ng",
            isFeatured: false,
            isEnabled: true,
            displayOrder: 30)
    };

    public static async Task SeedIdentityAsync(
        AppDbContext db,
        AdminUserSeedOptions? adminOptions = null,
        DemoLearnerSeedOptions? demoLearnerOptions = null,
        IHostEnvironment? environment = null,
        ILogger? logger = null)
    {
        if (!await db.Permissions.AnyAsync())
        {
            var permissions = new List<PermissionEntity>
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
                new("vocabularies.manage", "Manage Vocabulary & Flashcards Catalog", "Vocabulary", "Create, update, and delete vocabulary flashcards in database", id: Guid.Parse("60000000-0000-0000-0000-000000000012")),
                new("lessons.read", "View Lessons & Catalog", "Lessons", "Access published lessons, exercises, and catalog", id: Guid.Parse("60000000-0000-0000-0000-000000000013")),
                new("lessons.manage", "Manage Lessons & Exercises", "Lessons", "Create, update, and manage draft lessons and exercises", id: Guid.Parse("60000000-0000-0000-0000-000000000014")),
                new("lessons.publish", "Publish & Archive Lessons", "Lessons", "Publish validated draft lessons and archive deprecated lessons", id: Guid.Parse("60000000-0000-0000-0000-000000000015")),
                new("lessons.generate", "AI Lesson Generation & Prompt Builder", "Lessons", "Generate candidate lessons with AI and build prompts", id: Guid.Parse("60000000-0000-0000-0000-000000000016")),
                new("lesson-exercises.attempt", "Submit Lesson Attempts & Role Play", "Lessons", "Start attempts, submit answers, and participate in role play", id: Guid.Parse("60000000-0000-0000-0000-000000000017")),
                new("lesson-rubrics.manage", "Manage Lesson Scoring Rubrics", "Lessons", "Create and version rubrics for lesson exercises", id: Guid.Parse("60000000-0000-0000-0000-000000000018"))
            };
            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();
        }
        else
        {
            var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
            var missingPermissions = new List<PermissionEntity>();
            void EnsurePerm(string code, string name, string module, string desc, Guid id)
            {
                if (!existingCodes.Contains(code))
                    missingPermissions.Add(new PermissionEntity(code, name, module, desc, id: id));
            }
            EnsurePerm("lessons.read", "View Lessons & Catalog", "Lessons", "Access published lessons, exercises, and catalog", Guid.Parse("60000000-0000-0000-0000-000000000013"));
            EnsurePerm("lessons.manage", "Manage Lessons & Exercises", "Lessons", "Create, update, and manage draft lessons and exercises", Guid.Parse("60000000-0000-0000-0000-000000000014"));
            EnsurePerm("lessons.publish", "Publish & Archive Lessons", "Lessons", "Publish validated draft lessons and archive deprecated lessons", Guid.Parse("60000000-0000-0000-0000-000000000015"));
            EnsurePerm("lessons.generate", "AI Lesson Generation & Prompt Builder", "Lessons", "Generate candidate lessons with AI and build prompts", Guid.Parse("60000000-0000-0000-0000-000000000016"));
            EnsurePerm("lesson-exercises.attempt", "Submit Lesson Attempts & Role Play", "Lessons", "Start attempts, submit answers, and participate in role play", Guid.Parse("60000000-0000-0000-0000-000000000017"));
            EnsurePerm("lesson-rubrics.manage", "Manage Lesson Scoring Rubrics", "Lessons", "Create and version rubrics for lesson exercises", Guid.Parse("60000000-0000-0000-0000-000000000018"));

            if (missingPermissions.Count > 0)
            {
                db.Permissions.AddRange(missingPermissions);
                await db.SaveChangesAsync();
            }
        }

        if (!await db.Roles.AnyAsync())
        {
            var allPermissions = await db.Permissions.AsNoTracking().ToListAsync();

            var adminRole = new RoleEntity(
                code: "ADMIN",
                name: "System Administrator",
                description: "ToÃ n quyá»n quáº£n trá»‹ há»‡ thá»‘ng, ngÆ°á»i dÃ¹ng, prompt, giá»ng Ä‘á»c vÃ  rubric.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000001"));
            foreach (var p in allPermissions)
            {
                adminRole.GrantPermission(p.Id);
            }

            var teacherRole = new RoleEntity(
                code: "TEACHER",
                name: "Curriculum & Coaching Teacher",
                description: "Quáº£n lÃ½ tá»« vá»±ng, tiÃªu chÃ­ cháº¥m phÃ¡t Ã¢m, cáº¥u hÃ¬nh CEFR prompt vÃ  giá»ng Ä‘á»c giÃ¡o viÃªn.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000002"));
            foreach (var p in allPermissions.Where(x => x.Module != "Identity"))
            {
                teacherRole.GrantPermission(p.Id);
            }

            var learnerRole = new RoleEntity(
                code: "LEARNER",
                name: "English Learner",
                description: "Há»c viÃªn luyá»‡n há»™i thoáº¡i trá»±c tiáº¿p, há»c flashcard tá»« vá»±ng háº±ng ngÃ y vÃ  cháº¥m phÃ¡t Ã¢m.",
                isSystemRole: true,
                id: Guid.Parse("70000000-0000-0000-0000-000000000003"));
            foreach (var p in allPermissions.Where(x =>
                x.Code is "conversations.access" or "vocabularies.read" or "vocabularies.generate" or "pronunciation.assess" or "lessons.read" or "lesson-exercises.attempt"))
            {
                learnerRole.GrantPermission(p.Id);
            }

            db.Roles.AddRange(adminRole, teacherRole, learnerRole);
            await db.SaveChangesAsync();
        }
        else
        {
            var allPermissions = await db.Permissions.AsNoTracking().ToListAsync();
            var existingRoles = await db.Roles.Include(r => r.RolePermissions).ToListAsync();

            var adminRole = existingRoles.FirstOrDefault(r => r.Code == "ADMIN");
            if (adminRole != null)
            {
                foreach (var p in allPermissions)
                {
                    adminRole.GrantPermission(p.Id);
                }
            }

            var teacherRole = existingRoles.FirstOrDefault(r => r.Code == "TEACHER");
            if (teacherRole != null)
            {
                foreach (var p in allPermissions.Where(x => x.Module != "Identity"))
                {
                    teacherRole.GrantPermission(p.Id);
                }
            }

            var learnerRole = existingRoles.FirstOrDefault(r => r.Code == "LEARNER");
            if (learnerRole != null)
            {
                foreach (var p in allPermissions.Where(x =>
                    x.Code is "conversations.access" or "vocabularies.read" or "vocabularies.generate" or "pronunciation.assess" or "lessons.read" or "lesson-exercises.attempt"))
                {
                    learnerRole.GrantPermission(p.Id);
                }
            }

            await db.SaveChangesAsync();
        }

        var hasher = new Pbkdf2PasswordHasher();
        var adminRoleId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var learnerRoleId = Guid.Parse("70000000-0000-0000-0000-000000000003");

        string adminUsername = adminOptions?.Username ?? "admin";
        if (!await db.Users.AnyAsync(u => u.Username == adminUsername))
        {
            if (string.IsNullOrWhiteSpace(adminOptions?.Password))
            {
                logger?.LogWarning("Admin user seeding skipped: AdminUser:Password is not configured. Configure 'AdminUser:Password' via user-secrets or environment variables.");
            }
            else if (adminOptions.Password.Length < 8)
            {
                logger?.LogWarning("Admin user seeding skipped: AdminUser:Password must be at least 8 characters long.");
            }
            else
            {
                var adminUser = UserEntity.Create(
                    username: adminUsername,
                    email: adminOptions.Email,
                    fullName: adminOptions.FullName,
                    passwordHash: hasher.HashPassword(adminOptions.Password),
                    preferredCefrLevel: adminOptions.PreferredCefrLevel,
                    preferredVoiceName: adminOptions.PreferredVoiceName,
                    id: Guid.Parse("80000000-0000-0000-0000-000000000001"));
                adminUser.AssignRole(adminRoleId);
                db.Users.Add(adminUser);
                await db.SaveChangesAsync();
                logger?.LogInformation("Admin user '{Username}' seeded successfully.", adminUsername);
            }
        }

        bool isDev = environment?.IsDevelopment() ?? false;
        if (isDev)
        {
            string learnerUsername = demoLearnerOptions?.Username ?? "default-learner";
            if (!await db.Users.AnyAsync(u => u.Username == learnerUsername))
            {
                if (string.IsNullOrWhiteSpace(demoLearnerOptions?.Password))
                {
                    logger?.LogInformation("Demo learner seeding skipped: DemoLearner:Password is not configured.");
                }
                else if (demoLearnerOptions.Password.Length < 8)
                {
                    logger?.LogWarning("Demo learner seeding skipped: DemoLearner:Password must be at least 8 characters long.");
                }
                else
                {
                    var defaultLearner = UserEntity.Create(
                        username: learnerUsername,
                        email: demoLearnerOptions.Email,
                        fullName: demoLearnerOptions.FullName,
                        passwordHash: hasher.HashPassword(demoLearnerOptions.Password),
                        preferredCefrLevel: demoLearnerOptions.PreferredCefrLevel,
                        preferredVoiceName: demoLearnerOptions.PreferredVoiceName,
                        id: Guid.Parse("80000000-0000-0000-0000-000000000002"));
                    defaultLearner.AssignRole(learnerRoleId);
                    db.Users.Add(defaultLearner);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Demo learner '{Username}' seeded successfully.", learnerUsername);
                }
            }
        }
    }

    public static AdminUserSeedOptions GetAdminUserOptions(IConfiguration? configuration = null)
    {
        var options = new AdminUserSeedOptions();
        if (configuration != null)
        {
            configuration.GetSection(AdminUserSeedOptions.SectionName).Bind(options);
        }
        return options;
    }

    public static DemoLearnerSeedOptions GetDemoLearnerOptions(IConfiguration? configuration = null)
    {
        var options = new DemoLearnerSeedOptions();
        if (configuration != null)
        {
            configuration.GetSection(DemoLearnerSeedOptions.SectionName).Bind(options);
        }
        return options;
    }


    public static List<TopicEntity> BuildDefaultTopics() => new()
    {
        // --- Software Engineering & Technical Interview Topics ---
        new(
            code: "net10_clean_arch_ddd",
            name: ".NET 10, Clean Architecture, DDD & CQRS",
            displayNameVi: "Kiáº¿n trÃºc Clean Architecture, DDD & CQRS trÃªn .NET 10",
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
            displayNameVi: "Tá»‘i Æ°u truy váº¥n SQL Server, Indexing & xá»­ lÃ½ Deadlock",
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
            displayNameVi: "Kiáº¿n trÃºc Frontend: React 19, TanStack Query, Zustand & RBAC",
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
            displayNameVi: "Streaming Ã¢m thanh thá»i gian thá»±c vá»›i SignalR & WebSockets",
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
            displayNameVi: "Thiáº¿t káº¿ há»‡ thá»‘ng Thanh toÃ¡n & ÄÆ¡n hÃ ng chá»‹u táº£i cao",
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
            displayNameVi: "Thiáº¿t káº¿ ná»n táº£ng Chat & Gia sÆ° giá»ng nÃ³i AI toÃ n cáº§u",
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
            displayNameVi: "Thiáº¿t káº¿ Rate Limiter phÃ¢n tÃ¡n & Báº£o máº­t API Gateway",
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
            displayNameVi: "Giá»›i thiá»‡u báº£n thÃ¢n, Má»¥c tiÃªu nghá» nghiá»‡p & LÃ½ do á»©ng tuyá»ƒn",
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
            displayNameVi: "Ká»ƒ chuyá»‡n STAR: Xá»­ lÃ½ sá»± cá»‘ Production nghiÃªm trá»ng dÆ°á»›i Ã¡p lá»±c thá»i gian",
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
            displayNameVi: "Ká»ƒ chuyá»‡n STAR: Giáº£i quyáº¿t báº¥t Ä‘á»“ng ká»¹ thuáº­t khi Review Code",
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
            displayNameVi: "ÄÃ m phÃ¡n lÆ°Æ¡ng & PhÃºc lá»£i khi nháº­n Offer quá»‘c táº¿",
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
            displayNameVi: "PhÃ¢n tÃ­ch chuyÃªn sÃ¢u Dá»± Ã¡n ná»•i báº­t nháº¥t trong CV",
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
            displayNameVi: "Há»p Daily Standup: BÃ¡o cÃ¡o tiáº¿n Ä‘á»™ Sprint & nÃªu Blocker",
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
            displayNameVi: "CÃ¹ng Ä‘á»“ng nghiá»‡p Debug lá»—i rÃ² rá»‰ bá»™ nhá»› & API pháº£n há»“i cháº­m",
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
            displayNameVi: "Há»p vá»›i KhÃ¡ch hÃ ng: LÃ m rÃµ yÃªu cáº§u & ÄÃ m phÃ¡n tiáº¿n Ä‘á»™ dá»± Ã¡n",
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
            displayNameVi: "ÄÃ³ng vai: Buá»•i Ä‘Ã¡nh giÃ¡ hiá»‡u suáº¥t 1-1 cuá»‘i nÄƒm vá»›i Sáº¿p",
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
            displayNameVi: "ThÃ³i quen háº±ng ngÃ y, LÃ m viá»‡c tá»« xa & CÃ¢n báº±ng cuá»™c sá»‘ng",
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
            displayNameVi: "Du lá»‹ch khÃ¡m phÃ¡ & Giá»›i thiá»‡u áº©m thá»±c Ä‘Æ°á»ng phá»‘ Viá»‡t Nam",
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
            displayNameVi: "Bá»™ phim, Cuá»‘n sÃ¡ch & Podcast yÃªu thÃ­ch",
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
            displayNameVi: "Sá»©c khá»e, Cháº¡y bá»™, Táº­p luyá»‡n & TÃ¡i táº¡o nÄƒng lÆ°á»£ng",
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
            displayNameVi: "Tranh luáº­n: AI Agent sáº½ thay tháº¿ hay nÃ¢ng táº§m Ká»¹ sÆ° pháº§n má»m?",
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
            displayNameVi: "ÄÃ o sÃ¢u: Modular Monolith hay Microservices cho Startup Ä‘ang tÄƒng trÆ°á»Ÿng?",
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
            name: "Connected Speech, Word Stress & Tricky Ending Consonants (/s/, /t/, /d/, /Î¸/)",
            displayNameVi: "Luyá»‡n ná»‘i Ã¢m, Trá»ng Ã¢m tá»« & Ã‚m cuá»‘i tiáº¿ng Anh chuáº©n xÃ¡c",
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
            displayNameVi: "Tá»« vá»±ng Cloud Computing, Docker, Kubernetes & CI/CD",
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
            displayNameVi: "Cá»¥m Ä‘á»™ng tá»« (Phrasal Verbs) & ThÃ nh ngá»¯ cÃ´ng sá»Ÿ thÃ´ng dá»¥ng",
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
            displayNameVi: "Tá»« vá»±ng há»c thuáº­t IELTS Speaking Part 3 & Tá»« ná»‘i láº­p luáº­n",
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
            label: "0.75x â€” Cháº­m & RÃµ tá»«ng tá»«",
            descriptionVi: "PhÃ¹ há»£p cho ngÆ°á»i má»›i báº¯t Ä‘áº§u hoáº·c nghe tá»« má»›i khÃ³",
            recommendedCefrRange: "A1 - A2",
            isDefault: false,
            isActive: true,
            displayOrder: 1,
            id: Guid.Parse("45000000-0000-0000-0000-000000000001")),
        new(
            code: "0.90x",
            speedValue: 0.90,
            label: "0.90x â€” Vá»«a pháº£i, dá»… báº¯t ká»‹p",
            descriptionVi: "Nhá»‹p Ä‘á»™ sÆ° pháº¡m rÃµ rÃ ng, dá»… nghe trá»ng Ã¢m vÃ  ná»‘i Ã¢m",
            recommendedCefrRange: "A2 - B1",
            isDefault: false,
            isActive: true,
            displayOrder: 2,
            id: Guid.Parse("45000000-0000-0000-0000-000000000002")),
        new(
            code: "1.00x",
            speedValue: 1.00,
            label: "1.00x â€” Tá»± nhiÃªn chuáº©n báº£n xá»©",
            descriptionVi: "Tá»‘c Ä‘á»™ há»™i thoáº¡i tiÃªu chuáº©n háº±ng ngÃ y vÃ  phá»ng váº¥n",
            recommendedCefrRange: "B1 - B2",
            isDefault: true,
            isActive: true,
            displayOrder: 3,
            id: Guid.Parse("45000000-0000-0000-0000-000000000003")),
        new(
            code: "1.15x",
            speedValue: 1.15,
            label: "1.15x â€” Nhanh & NÄƒng Ä‘á»™ng",
            descriptionVi: "Luyá»‡n pháº£n xáº¡ nghe nhanh trong há»p Scrum hoáº·c tranh luáº­n",
            recommendedCefrRange: "B2 - C1",
            isDefault: false,
            isActive: true,
            displayOrder: 4,
            id: Guid.Parse("45000000-0000-0000-0000-000000000004")),
        new(
            code: "1.25x",
            speedValue: 1.25,
            label: "1.25x â€” Thá»­ thÃ¡ch tá»‘c Ä‘á»™ cao",
            descriptionVi: "MÃ´ phá»ng Ã¡p lá»±c cao vÃ  ngÆ°á»i báº£n xá»© nÃ³i nhanh",
            recommendedCefrRange: "C1 - C2",
            isDefault: false,
            isActive: true,
            displayOrder: 5,
            id: Guid.Parse("45000000-0000-0000-0000-000000000005"))
    };

}
