# Việc giao cho Codex: P1b — interface DbContext theo module

> Orchestrator đã sửa: đúng là 48 `DbSet` module; `IdempotencyRecords` thuộc Shared Kernel, không đưa vào interface module. Làm tiếp P1b theo bản đã sửa này.

## Mục tiêu

Tạo 7 interface `I<Module>DbContext` chỉ chứa `DbSet` của module, factory theo module, để `AppDbContext` hiện tại implement và DI trả về cùng instance scoped. Chưa chuyển handler nào. Liên quan: ADR-0006 phase P1.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, luật 13–15), `EnglishPartner.BE/AGENTS.md`
- `docs/adr/0006-modular-monolith.md`
- Skill: `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (mục "Per-module database access")
- Skill: `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`
- Code: `src/EnglishLiveTutor.Application/Common/Interfaces/IAppDbContext.cs` (interface hiện có, 48 `DbSet` + `IAppDbContextFactory`; `AppDbContext` có thêm `IdempotencyRecords` — Shared Kernel), `src/EnglishLiveTutor.Infrastructure/Persistence/AppDbContext.cs` (`AppDbContext`, `AppDbContextFactoryWrapper` dòng ~128), `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs:50-62` (đăng ký).

## Phạm vi

Chỉ được tạo/sửa:
- Tạo `src/EnglishLiveTutor.Application/Modules/IModuleDbContext.cs`, `src/EnglishLiveTutor.Application/Modules/IModuleDbContextFactory.cs`
- Tạo `src/EnglishLiveTutor.Application/Modules/<Module>/I<Module>DbContext.cs` cho 7 module
- Sửa `src/EnglishLiveTutor.Application/Common/Interfaces/IAppDbContext.cs`
- Sửa `src/EnglishLiveTutor.Infrastructure/Persistence/AppDbContext.cs` (chỉ khai báo kế thừa và class factory mới nếu cần)
- Sửa `src/EnglishLiveTutor.Infrastructure/DependencyInjection.cs` (chỉ thêm đăng ký)
- Sửa `tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleMap.cs` (map thư mục/namespace mới)
- Tạo `tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleDbContextRegistrationTests.cs`

Không được sửa: handler/query/service trong `Features/`, repository, migration, `AppDbContextModelSnapshot`, cấu hình EF (`Configurations/`), `appsettings*`, test hiện có khác, allowlist (trừ khi test stale báo dòng không còn — khi đó chỉ được xóa dòng).

Contract được phép đổi: không có.

## Yêu cầu

1. `IModuleDbContext : IAsyncDisposable, IDisposable` với `Task<int> SaveChangesAsync(CancellationToken ct = default);` — namespace `EnglishLiveTutor.Application.Modules`.
2. `IModuleDbContextFactory<TDb> where TDb : IModuleDbContext` với `Task<TDb> CreateDbContextAsync(CancellationToken ct = default);`.
3. 7 interface kế thừa `IModuleDbContext`, namespace `EnglishLiveTutor.Application.Modules.<Module>`. Chuyển **nguyên văn** khai báo `DbSet` (giữ tên property, kiểu) từ `IAppDbContext` sang:
   - `IIdentityDbContext`: Users, Roles, Permissions, UserRoles, RolePermissions, RefreshTokens
   - `ICatalogDbContext`: CefrLevelPrompts, ConversationModePrompts, SpeechSpeeds, TeacherVoices, Topics, AiModelCatalog
   - `ISpeechDbContext`: ScoringRubrics, ScoringRubricCriteria, AudioRecordings, PronunciationAssessments, PronunciationCriterionScores, GeneratedPronunciationAudios
   - `ILessonsDbContext`: Lessons, Exercises, LessonAttempts, ExerciseAnswers, LessonRolePlaySessions, LessonRolePlayTurns
   - `IGrammarDbContext`: GrammarSteps, GrammarExercises, UserGrammarStepProgress, GrammarAttempts, GrammarAttemptAnswers
   - `IVocabularyDbContext`: Vocabularies, VocabularySenses, VocabularyPronunciations, VocabularyRelations, VocabularyWordFamilies, VocabularyCollocations, VocabularyExamples, VocabularyGrammarPatterns, VocabularyGenerationAudits, UserLearningSettings, DailyVocabularyPlans, DailyVocabularyItems, UserVocabularyProgresses, VocabularyReviewAttempts, FallingWordGameSessions, FallingWordGameRounds
   - `ILiveTutorDbContext`: Conversations, Messages, Corrections
   - Tổng 48 `DbSet` (toàn bộ `DbSet` của `IAppDbContext`) — không thiếu, không trùng. `AppDbContext.IdempotencyRecords` giữ nguyên, không đưa vào interface module. Nếu thấy `DbSet` không nằm trong danh sách: dừng và ghi report.
4. `IAppDbContext` giữ nguyên tên/namespace, đổi thành `public interface IAppDbContext : IIdentityDbContext, ICatalogDbContext, ISpeechDbContext, ILessonsDbContext, IGrammarDbContext, IVocabularyDbContext, ILiveTutorDbContext` và xóa các `DbSet` đã chuyển; giữ `Task<int> SaveChangesAsync(...)` nếu cần để không vỡ caller. `IAppDbContextFactory` giữ nguyên. Mọi code đang dùng `IAppDbContext` phải build được không đổi.
5. Infrastructure: `AppDbContext` đã implement `IAppDbContext` nên tự implement 7 interface; chỉ sửa nếu compiler yêu cầu. Thêm `ModuleDbContextFactory<TDb>` (cùng file `AppDbContext.cs`, sealed) implement `IModuleDbContextFactory<TDb>`, tạo context bằng `IDbContextFactory<AppDbContext>` y như `AppDbContextFactoryWrapper` rồi trả `(TDb)(object)context`.
6. DI (`DependencyInjection.cs`, ngay sau dòng `AddScoped<IAppDbContext>`): `AddScoped<I<Module>DbContext>(sp => sp.GetRequiredService<AppDbContext>())` cho 7 interface; `AddSingleton(typeof(IModuleDbContextFactory<>), typeof(ModuleDbContextFactory<>))`. Không đổi đăng ký khác.
7. `ModuleMap`: thư mục `EnglishLiveTutor.Application/Modules/<Module>/` và namespace `EnglishLiveTutor.Application.Modules.<Module>` thuộc module `<Module>`; `Application/Modules/*.cs` cấp gốc và namespace `EnglishLiveTutor.Application.Modules` (không có segment module) là Shared. Cập nhật scanner nếu cần để namespace `EnglishLiveTutor.Application.Modules.<M>` được phân loại (vẫn là "internal" khi module khác dùng).
8. `ModuleDbContextRegistrationTests`: dựng `ServiceCollection` qua cùng extension đăng ký Infrastructure như các test DI hiện có (xem `CqrsDependencyInjectionTests.cs` để theo cách làm, dùng connection string giả, không mở kết nối) — trong một scope, 7 interface và `IAppDbContext` resolve ra **cùng một instance**; `IModuleDbContextFactory<IVocabularyDbContext>` resolve được. Thêm test reflection: hợp các `DbSet` property của 7 interface = 48 (đúng bằng số `DbSet` của `IAppDbContext` hiện tại), không trùng tên; `IdempotencyRecords` của `AppDbContext` là Shared Kernel, không thuộc interface module nào.

## Tiêu chí hoàn thành

- [ ] Build 0 lỗi.
- [ ] Test: 0 fail (baseline mới 566 pass / 0 fail), gồm test architecture và test mới.
- [ ] Không có file nào trong `Features/`, `Repositories/`, `Migrations/`, `Configurations/` bị đổi.
- [ ] Không có model change EF (không đụng mapping).

## Lệnh kiểm chứng

```bash
# trong EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
```

Nếu môi trường không cho chạy lệnh, ghi rõ trong report — không đoán kết quả.

## Ràng buộc

- Không commit, push, merge; không tạo branch.
- Không thêm package; không sửa test hiện có ngoài `ModuleMap.cs`.
- Không in hoặc ghi secret, token, API key, connection string.
- Gặp điều mơ hồ hoặc cần sửa ngoài phạm vi: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt từng yêu cầu>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
