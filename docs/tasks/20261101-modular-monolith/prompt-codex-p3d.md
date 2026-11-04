# P3d — tách module Speech thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **Speech**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Pronunciation` | `Modules/Speech/EnglishLiveTutor.Modules.Speech/Domain/Pronunciation` |
| `Features/Pronunciation` (trừ `Contracts/`; gồm `Rubrics/`) | `.../Modules.Speech/Features/Pronunciation` |
| `Features/Pronunciation/Contracts` | `Modules/Speech/EnglishLiveTutor.Modules.Speech.Contracts/Features/Pronunciation/Contracts` |
| `Application/Modules/Speech/ISpeechDbContext.cs` | `.../Modules.Speech/Modules/Speech/` |
| Common: `Interfaces/IPronunciationSynthesisLock.cs`, phần Speech của `Interfaces/IAiServices.cs` | module project (Contracts nếu module khác dùng) |

Kiểm chứng và report như P3b.
