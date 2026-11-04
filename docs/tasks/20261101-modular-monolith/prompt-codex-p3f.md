# P3f — tách module Lessons thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **Lessons**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Lessons`, `Domain/LessonAttempts` | `Modules/Lessons/EnglishLiveTutor.Modules.Lessons/Domain/...` |
| `Features/Lessons` (trừ `Contracts/`) | `.../Modules.Lessons/Features/Lessons` |
| `Features/Lessons/Contracts` (nếu có) | `Modules/Lessons/EnglishLiveTutor.Modules.Lessons.Contracts/Features/Lessons/Contracts` |
| `Application/Modules/Lessons/ILessonsDbContext.cs` | `.../Modules.Lessons/Modules/Lessons/` |
| Common: `Models/LessonDtos.cs`, `Models/LessonAttemptDtos.cs`, `Interfaces/{ILessonAiGenerator,ILessonCandidateValidator,ILessonMediaStorage,ILessonPromptBuilder,ILessonServices,IRolePlayReplyProvider}.cs` | module project (Contracts nếu module khác dùng) |
| `Common/Exercises/` | chỉ chuyển nếu có type riêng của Lessons; nếu chỉ dùng enum Shared (`Domain/Common/Exercises`) và Grammar cũng dùng → giữ ở SharedKernel |

Kiểm chứng và report như P3b.
