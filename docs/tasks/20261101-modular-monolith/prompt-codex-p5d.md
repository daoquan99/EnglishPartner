# P5d — module Speech thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **Speech**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Speech/Domain/**` → `Modules.Speech.Domain` |
| Application | đổi tên → `Modules.Speech.Application` |
| Infrastructure | `Repositories/{AudioRecording,GeneratedPronunciationAudio,PronunciationAssessment,ScoringRubric}Repository.cs`, `Ai/{GeminiLivePronunciationTtsSynthesizer,GeminiPronunciationAiAssessor,PronunciationSynthesisLock}.cs`, `PronunciationConfigurations.cs`, `Persistence/LessonRubricSeeder.cs` (nếu seeder composition gọi nó, giữ lời gọi), `Persistence/Modules/SpeechDbContext.cs`, `Migrations/Speech/**` |
| Presentation | `Api/Endpoints/PronunciationEndpoints.cs`, `Api/Endpoints/LessonRubricEndpoints.cs` |

File Infrastructure/Api của Speech không có trong bảng nhưng `ModuleMap` xếp vào Speech → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
