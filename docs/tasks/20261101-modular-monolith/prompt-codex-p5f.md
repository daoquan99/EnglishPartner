# P5f — module Lessons thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **Lessons**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Lessons/Domain/**` → `Modules.Lessons.Domain` |
| Application | đổi tên → `Modules.Lessons.Application` |
| Infrastructure | `Infrastructure/Lessons/**` (grader, sanitizer, prompt builder), `Infrastructure/Storage/**`, `Repositories/LessonRolePlayRepository.cs`, `Ai/{GeminiLessonAiGenerator,GeminiRolePlayReplyProvider,LessonAiOptions}.cs`, configurations `Lesson`, `LessonRolePlay`, `Persistence/Modules/LessonsDbContext.cs`, `Migrations/Lessons/**` |
| Presentation | `Api/Endpoints/{Lesson,LessonAttempt,LessonMedia}Endpoints.cs` |

File Infrastructure/Api của Lessons không có trong bảng nhưng `ModuleMap` xếp vào Lessons → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
