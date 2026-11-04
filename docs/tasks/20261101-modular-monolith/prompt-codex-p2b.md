# P2b — Identity directory + Speech score reader (D5, D6) + sửa factory

Theo `docs/tasks/20261101-modular-monolith/plan-p2.md` mục D5, D6. Cùng quy tắc như P2a: không đổi hành vi, query giữ nguyên, allowlist chỉ bớt, test cũ chỉ sửa wiring/`using`.

## Yêu cầu

1. **D5 — `src/EnglishLiveTutor.Application/Features/Identity/Contracts/`**: `IUserDirectory`, `UserLearningProfile(string? TimeZoneId, string? PreferredCefrLevel)`, `UserNameInfo(Guid Id, string Username, string? FullName)`. Impl `Features/Identity/Services/UserDirectory.cs` dùng `IIdentityDbContext` (scoped), đăng ký scoped trong `Application/DependencyInjection.cs`. Method — predicate **chép đúng** từ caller hiện tại:
   - `GetLearningProfileAsync(string userId, ct)`: `AsNoTracking().Where(u => u.Id.ToString() == userId || u.Username == userId)` → `FirstOrDefaultAsync`, trả null nếu không có.
   - `ListUsersByIdOrUsernameAsync(IReadOnlyCollection<string> idsOrUsernames, ct)`: predicate như `ListGrammarModerationQueueQueryHandler.cs` (~dòng 86).
   - `ListUsersByIdsAsync(IReadOnlyCollection<Guid> ids, ct)` và `ListUsersByUsernamesAsync(IReadOnlyCollection<string> usernames, ct)`: như `ListGrammarExercisesQuery.cs` (~dòng 132, 149).
   Caller: `Features/Learning/Commands/UpdateLearningSettings/UpdateLearningSettingsCommandHandler.cs`, `Features/Learning/Queries/GetLearningSettings/GetLearningSettingsQueryHandler.cs`, `Features/Learning/Services/DailyVocabularyPlanInitializer.cs`, `Features/Grammar/Queries/Admin/ListGrammarModerationQueue/ListGrammarModerationQueueQueryHandler.cs`, `Features/Grammar/Queries/ListGrammarExercisesQuery.cs`. Caller tự ghép tên (`FullName ?? Username`…) đúng như cũ.
2. **D6 — `Features/Pronunciation/Contracts/IPronunciationScoreReader`** (impl `Features/Pronunciation/Services/PronunciationScoreReader.cs`, `ISpeechDbContext`, scoped): các method đúng với query hiện có trong `Features/Vocabulary/VocabularyProjectionHelper.cs` (~dòng 40, ~81: điểm mới nhất theo user+sense; theo user+danh sách sense, group theo `SenseId`) và `Features/Learning/Queries/GetLearningProgress/GetLearningProgressQueryHandler.cs` (~dòng 32-35: count + average). Giữ nguyên `OrderByDescending`, điều kiện `OverallScore != null`, kiểu trả (`decimal?`…).
   - Lưu ý `VocabularyProjectionHelper` nhận `db` làm tham số (context từ factory của caller): giữ nguyên chữ ký cho phần Vocabulary, chỉ thay phần đọc `PronunciationAssessments` bằng reader (inject reader hoặc nhận thêm tham số — chọn cách ít thay đổi caller nhất, ghi lại trong report).
3. Sau 1–2: file nào chỉ còn dùng `DbSet` module của mình → chuyển sang `I<Module>DbContext`/`IModuleDbContextFactory<I<Module>DbContext>` theo quy tắc P1c. File còn dùng module khác → giữ, ghi report.
4. **Sửa factory** (`src/EnglishLiveTutor.Infrastructure/Persistence/AppDbContext.cs`, class `ModuleDbContextFactory<TDb>`): thay `return (TDb)(object)context;` bằng `if (context is TDb typed) return typed;` ngược lại `await context.DisposeAsync();` rồi `throw new InvalidOperationException($"AppDbContext does not implement {typeof(TDb).Name}.");`. Thêm test.
5. Test: `UserDirectory` (tìm theo id string, theo username, không thấy; ba method list), `PronunciationScoreReader` (mới nhất, bỏ `OverallScore` null, count/avg) bằng EF InMemory.

## Kiểm chứng

Như P2a (build `--no-restore`, `MODULE_ALLOWLIST_WRITE=1` shrink, toàn bộ test, `has-pending-model-changes`). Baseline: số pass sau P2a, 0 fail.

Worker report theo mẫu P2a (thêm mục "Chữ ký VocabularyProjectionHelper thay đổi thế nào").
