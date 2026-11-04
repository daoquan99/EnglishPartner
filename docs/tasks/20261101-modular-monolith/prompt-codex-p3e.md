# P3e — tách module Vocabulary thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **Vocabulary**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Vocabulary`, `Domain/Learning`, `Domain/VocabularyGames` | `Modules/Vocabulary/EnglishLiveTutor.Modules.Vocabulary/Domain/...` |
| `Features/Vocabulary`, `Features/Learning`, `Features/VocabularyGames` (trừ `Contracts/`) | `.../Modules.Vocabulary/Features/...` |
| `Contracts/` của 3 feature trên (nếu có) | `Modules/Vocabulary/EnglishLiveTutor.Modules.Vocabulary.Contracts/Features/...` |
| `Application/Modules/Vocabulary/IVocabularyDbContext.cs` | `.../Modules.Vocabulary/Modules/Vocabulary/` |
| Common: `Models/VocabularyDtos.cs`, `Models/ManualVocabularyGenerationDtos.cs`, `Interfaces/IVocabularyGenerationContent.cs`, phần Vocabulary của `IAiServices.cs` | module project (Contracts nếu module khác dùng) |
| Lưu ý | Vocabulary implement port của Speech (`IPronunciationTargetTextSource`, `IPronunciationAssessmentObserver`) → reference `Modules.Speech.Contracts` |

Kiểm chứng và report như P3b.
