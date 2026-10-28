# Việc giao cho AGY: C2 — Pronunciation/Vocabulary AI commands dùng DbContext scoped + một lần commit (backend)

Batch C1 (Identity) đã làm trong worktree này — giữ nguyên. Đọc trước: `AGENTS.md` (Luật 4, 8, 9), `EnglishPartner.BE/AGENTS.md`, plan `D:/Projects/EnglishPartner/docs/tasks/20261029-a1-unit-of-work/plan.md`, skill `EnglishPartner.BE/.agents/skills/infrastructure-persistence/SKILL.md`, `EnglishPartner.BE/.agents/skills/ai-provider-integration/SKILL.md`, `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`.

## Phạm vi

Được sửa: `src/EnglishLiveTutor.Application/Features/Pronunciation/Commands/AssessPronunciation/**`, `src/EnglishLiveTutor.Application/Features/Vocabulary/Commands/GenerateVocabulary/**`, `src/EnglishLiveTutor.Application/Features/Vocabulary/Commands/ImportVocabularyJson/**`, `src/EnglishLiveTutor.Application/Features/Identity/Commands/CreatePermission/**`, `src/EnglishLiveTutor.Application/Features/Identity/Commands/RevokeRefreshToken/**`, `src/EnglishLiveTutor.Application/Features/Vocabulary/VocabularyGenerationWriter.cs`, `src/EnglishLiveTutor.Application/Features/Vocabulary/VocabularyProjectionHelper.cs` (chỉ nếu cần nhận `IAppDbContext`), `src/EnglishLiveTutor.Application/DependencyInjection.cs` (đổi lifetime writer nếu cần), `tests/**`.
Không được sửa: route/DTO/status code/message lỗi trả client, migration, Domain, provider Infrastructure, FE, package.

## Yêu cầu

1. **AssessPronunciationCommandHandler**: bỏ `IAppDbContextFactory`. Đọc model/vocabulary/rubric qua `IAppDbContext` scoped; gọi provider AI **trước** khi ghi; ghi kết quả và commit **một lần** qua `IUnitOfWork.CommitChangesAsync`. Không mở transaction quanh lời gọi provider (Luật 8).
2. **GenerateVocabularyCommandHandler**: bỏ factory; đọc `AiModelCatalog` qua `IAppDbContext` scoped. Bỏ property thừa `dbFactory => _dbFactory`.
3. **VocabularyGenerationWriter.SaveAsync**: dùng `IAppDbContext` scoped + `IUnitOfWork` (một lần commit). Đổi đăng ký DI sang `Scoped` nếu đang `Transient` mà cần chia sẻ context. `ImportVocabularyJson` vẫn chạy đúng.
4. **RecordFailureAsync** (audit khi provider lỗi): phải bảo đảm **không** lưu nhầm entity từ vựng đang dở. Chọn một:
   (a) giữ context riêng từ factory cho riêng audit lỗi — chấp nhận như ngoại lệ có chủ đích, giữ comment giải thích; hoặc
   (b) dùng context scoped nhưng chỉ khi chắc chắn chưa có entity từ vựng nào được track (provider lỗi trước khi `SaveAsync` chạy).
   Ghi lựa chọn và lý do trong report. **Bảo mật:** `audit.MarkFailed(...)` không được lưu `ex.Message` thô (có thể chứa URL kèm API key của provider) — lưu `ex.GetType().Name` + thông điệp đã làm sạch (bỏ query string `key=...`, giới hạn độ dài ~500).
5. **Hai handler Identity còn sót** (`CreatePermission`, `RevokeRefreshToken`): thay `_xxxRepo.SaveChangesAsync` bằng `IUnitOfWork.CommitChangesAsync` (một lần), giữ hành vi; cập nhật test reflection/đếm commit của C1 để bao gồm hai handler này.
6. **Test** (EF InMemory, provider giả, không mạng):
   - Assess: provider được gọi trước khi ghi; commit đúng 1 lần (decorator đếm `IUnitOfWork`); provider lỗi → không có bản ghi assessment.
   - Generate: thành công → từ vựng + audit success, commit 1 lần; provider ném lỗi có message chứa `?key=SECRET` → không có từ vựng nào được lưu, audit failed được lưu và **không chứa** `SECRET`.
   - Import JSON vẫn lưu đủ graph và gắn kế hoạch hôm nay.
   - Không handler/writer trong phạm vi còn phụ thuộc `IAppDbContextFactory` (trừ phương án 4a cho audit lỗi — khi đó test chỉ cho phép factory trong `VocabularyGenerationWriter`).
   - 2 test baseline `ManualVocabularyGenerationTests.*` đang fail: tìm nguyên nhân. Nếu do code trong phạm vi → sửa để pass; nếu do giới hạn EF InMemory/môi trường → ghi rõ trong report, không xóa/skip test.

## Lệnh kiểm chứng

Chỉ chạy `dotnet build` và `dotnet test EnglishLiveTutor.sln --no-build --filter "<lớp test liên quan>"`; toàn bộ suite tối đa một lần ở cuối.

```bash
cd EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Vocabulary|FullyQualifiedName~Pronunciation"
```

Build 0 warning. Không fail mới ngoài baseline.

## Ràng buộc

Không `git add`/`stash`/`reset`/`checkout`, không commit/push, không thêm package, không chạy API/SQL/Gemini, không sửa ngoài phạm vi.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <yêu cầu 1–5; lựa chọn ở mục 4 và lý do; nguyên nhân 2 test baseline>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test, số warning>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
