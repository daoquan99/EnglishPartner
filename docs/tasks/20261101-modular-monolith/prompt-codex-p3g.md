# P3g — tách module Grammar thành project

Áp dụng **đúng "Quy tắc chung tách một module X"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p3b.md` (đọc lại nếu cần) cho module **Grammar**:

| Nguồn | Đích |
| --- | --- |
| `Domain/Grammar` | `Modules/Grammar/EnglishLiveTutor.Modules.Grammar/Domain/Grammar` |
| `Features/Grammar` (trừ `Contracts/`) | `.../Modules.Grammar/Features/Grammar` |
| `Features/Grammar/Contracts` (nếu có) | `Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Contracts/Features/Grammar/Contracts` |
| `Application/Modules/Grammar/IGrammarDbContext.cs` | `.../Modules.Grammar/Modules/Grammar/` |

Kiểm chứng và report như P3b.
