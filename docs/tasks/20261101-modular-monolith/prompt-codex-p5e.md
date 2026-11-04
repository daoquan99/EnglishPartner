# P5e — module Vocabulary thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **Vocabulary**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Vocabulary/Domain/**` → `Modules.Vocabulary.Domain` |
| Application | đổi tên → `Modules.Vocabulary.Application` |
| Infrastructure | `Infrastructure/VocabularyGames/**` (worker hết hạn round, pool reader, options), `Repositories/{DailyVocabularyPlan,FallingWordGame,UserVocabularyProgress,Vocabulary}Repository.cs`, `Ai/{GeminiVocabularyAiGenerator,VocabularyGenerationContent,VocabularyGenerationOptions}.cs`, configurations `Vocabulary`, `Learning`, `FallingWordGame`, `Persistence/Modules/VocabularyDbContext.cs`, `Migrations/Vocabulary/**` |
| Presentation | `Api/Endpoints/{Vocabulary,DailyLearning,VocabularyGames}Endpoints.cs` |

File Infrastructure/Api của Vocabulary không có trong bảng nhưng `ModuleMap` xếp vào Vocabulary → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
