# Plan: Sửa toàn bộ review 2026-10-28

- Mã task: `20261028-review-fix`
- Ngày: 2026-10-28
- Liên quan: [docs/reviews/2026-10-28-security-performance-architecture.md](../../reviews/2026-10-28-security-performance-architecture.md), ADR-0001…0005
- **Cần người dùng duyệt:** Đã duyệt — người dùng yêu cầu "fix hết" và chọn AGY làm worker. ADR-0005 được coi là Accepted theo yêu cầu này. Migration xóa hội thoại (ADR-0002) chỉ được **tạo**, không chạy trên DB thật.
- Worker: AGY, `--allow-shell` (được chạy `dotnet`/`npm` trong worktree). Model: `gemini-3.8-flash-high` cho B1–B5, B4 fix, B7; `gemini-3.8-flash-medium` cho B6, B8, B9 (người dùng chọn để tăng tốc). AGY chỉ chạy test liên quan; orchestrator chạy toàn bộ suite khi nghiệm thu.
- Worktree: `D:/Projects/EnglishPartner.worktrees/20261028-review-fix-agy`, branch `ai/20261028-review-fix-agy`. Mỗi batch kiểm chứng xong được `git add` (stage, không commit) để batch sau có diff riêng.

## Baseline (đo trước khi sửa)

- BE build pass; `dotnet test`: 185/194 pass, **9 test fail sẵn** (ngoài phạm vi): `IdentityPromptsAndVoicesTests.TeacherVoicesCatalog_*`, 2 × `ManualVocabularyGenerationTests.*`, 6 × `PronunciationSynthesisAudioCacheTests.*` (EF InMemory không hỗ trợ `DataLength`).
- FE `npm run build` pass; Vitest 158/158 pass.
- Tiêu chí chung: không phát sinh test fail mới ngoài 9 test trên.

## Batch

| # | Phạm vi | Mục review | Contract/dữ liệu |
| --- | --- | --- | --- |
| B1 | BE cấu hình, secret, hardening HTTP | S1, S5, S6, S10, A6, P8 | Không đổi API. Cần cấu hình `Jwt:SecretKey`, `AdminUser:Password` |
| B2 | BE auth | S7, S8, S11, S12, P5, cookie Secure, ADMIN bypass, `AllowAllPermissionChecker` | 429 mới; refresh token cũ mất hiệu lực (phải đăng nhập lại) |
| B3 | BE ownership ngoài Conversation | S4, `ListLessonAttemptsQuery.UserId`, fallback `default-learner` | Bỏ tham số `userId` ở 2 endpoint |
| B4 | BE Conversation + realtime | S2, S3, P2, P7, S9 (server) | Migration thêm `UserId` + xóa hội thoại cũ; hub từ chối conversation không thuộc user |
| B5 | BE lỗi | ADR-0005, A4 | Body lỗi thành ProblemDetails + `error`; status code mỗi endpoint giữ nguyên |
| B6 | BE performance | P1, P3, P4, P6 | Request không phân trang bị chặn trần 1000; `pageSize` tối đa 100 |
| B7 | BE kiến trúc | A2, A3, A5 | Redis backplane bật theo cấu hình |
| B8 | FE | S9 (client), RequireAuth, `error` trong ProblemDetails, server paging cho admin vocabularies/users, bỏ `userId` gửi đi, S13 | Theo B3/B5/B6 |
| B9 | FE performance | P9 (tách `DailyVocabularyPage`) | Không đổi hành vi |

Không làm trong đợt này (ghi lý do trong report): gộp 3 hub thành 1 (đổi contract realtime lớn, cần ADR riêng); bắt buộc đổi mật khẩu lần đầu (tính năng mới: cột, endpoint, trang FE); rotate MediatR license và JWT secret thật (thao tác của người dùng); Full-Text Search (cần cấu hình SQL Server).

## Kiểm chứng mỗi batch

```bash
# trong <worktree>/EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
# trong <worktree>/EnglishPartner.FE (B5, B6, B8, B9)
npm run build
npx vitest run
```

Không chạy API, không migrate/seed SQL Server, không gọi Gemini.

## Review

- B1–B4: security-reviewer
- B4, B6, B9: performance-reviewer
- Cuối đợt: code-reviewer trên toàn bộ diff
