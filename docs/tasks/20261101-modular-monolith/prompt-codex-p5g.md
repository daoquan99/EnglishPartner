# P5g — module Grammar thành 5 project

Áp dụng **đúng "Quy tắc chung cho module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p5b.md` (đọc lại nếu cần) cho module **Grammar**:

| Layer | Nguồn → đích |
| --- | --- |
| Domain | `Modules.Grammar/Domain/**` → `Modules.Grammar.Domain` |
| Application | đổi tên → `Modules.Grammar.Application` |
| Infrastructure | `Infrastructure/Grammar/**` (content pack nhúng — **chuyển cả `EmbeddedResource` JSON và sửa đường dẫn resource trong csproj mới; kiểm test content pack đọc được resource sau khi đổi assembly**), `Ai/{GeminiGrammarExerciseAiGenerator,GrammarGenerationAiOptions}.cs`, `GrammarConfigurations.cs`, `Persistence/Modules/GrammarDbContext.cs`, `Migrations/Grammar/**` |
| Presentation | `Api/Endpoints/{Grammar,AdminGrammar}Endpoints.cs` |

File Infrastructure/Api của Grammar không có trong bảng nhưng `ModuleMap` xếp vào Grammar → cũng chuyển (liệt kê trong report). Kiểm chứng và report như P5b.
