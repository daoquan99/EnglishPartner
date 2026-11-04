# Plan: Refactor backend sang Modular Monolith (lộ trình P1–P4)

- Mã task: `20261101-modular-monolith`
- Ngày: 2026-11-01
- Liên quan: [ADR-0006](../../adr/0006-modular-monolith.md), review A1
- **Cần người dùng duyệt:** P1–P3 không (không đổi DB/contract, người dùng đã đồng ý hướng đi 2026-11-01). **P4 có** — migration chuyển schema trên DB thật.
- Trạng thái: Đang làm
- Backup DB: người dùng đã chạy `backup-lingomint.sql` (thư mục `C:\Backups`) ngày 2026-11-01.

## Mục tiêu

Chuyển `EnglishPartner.BE` sang 7 module (Identity, Catalog, Speech, Lessons, Grammar, Vocabulary, LiveTutor) + Shared Kernel theo ADR-0006, mỗi phase giữ nguyên hành vi, route, JSON, hub và DB cho tới P4.

## Baseline (main `8884da1` + tài liệu Task 0, 2026-11-01)

- `dotnet build EnglishLiveTutor.sln`: 0 lỗi, 8 warning.
- `dotnet test EnglishLiveTutor.sln`: ban đầu 535 pass / 6 fail / 541; sau P1a + sửa DataLength: **566 pass / 0 fail**.
- `IAppDbContext`/`IAppDbContextFactory` dùng trong ~93 file; Features theo số file: Grammar 30, Lessons 18, Identity 17, Learning 7, Vocabulary 7, Pronunciation 6, VocabularyGames 4, Prompts 3, AiModels 1.

## Cách làm

- Một worktree chung: `../EnglishPartner.worktrees/20261101-modular-monolith`, branch `ai/20261101-modular-monolith`, chạy `run-worker.sh … --worktree 20261101-modular-monolith`.
- Worker: Codex `deepseek-flash`; hết quota/lỗi CLI → AGY trên cùng worktree.
- Mốc giữa các gói việc: sau khi orchestrator kiểm chứng + review đạt, `git -C <worktree> add -A` (không commit). Diff của gói đang làm = `git diff` (chưa stage); diff tích lũy = `git diff --cached HEAD`.
- Mỗi gói: prompt riêng `prompt-codex-<gói>.md`; orchestrator tự chạy build/test; `code-reviewer` mỗi gói; `performance-reviewer`/`security-reviewer` khi gói đụng query/DI scope/auth.

## Gói việc

| Gói | Nội dung | Phụ thuộc |
| --- | --- | --- |
| P1a | Architecture test quét source: `ModuleMap`, luật DAG, Contracts sạch, cấm `IAppDbContext(Factory)` trong Features (allowlist), allowlist sinh từ vi phạm hiện có. Chỉ thêm file test | — |
| P1b | 7 interface `I<Module>DbContext`; `AppDbContext` implement; DI scoped cùng instance; `IAppDbContext` kế thừa cả 7 (tương thích) | P1a |
| P1c… | Chuyển handler/service từng module từ `IAppDbContext` sang `I<Module>DbContext`; xóa entry allowlist tương ứng. Thứ tự: Catalog+Identity, Speech, Vocabulary, Lessons, Grammar, LiveTutor | P1b |
| P2… | Contracts cho từng cạnh phụ thuộc; chuyển file module-specific khỏi `Application/Common`; allowlist = 0 | P1 |
| P3… | Tách project `EnglishLiveTutor.Modules.<X>` + `.Contracts`, giữ namespace | P2; plan chi tiết viết khi tới |
| P4… | DbContext thật + schema + baseline migration + outbox | P3; **dừng xin xác nhận**, kiểm trên DB restore từ backup trước |

## Tiêu chí hoàn thành mỗi gói

- [ ] Build 0 lỗi; test ≥ 535 pass + test mới, chỉ 6 fail có sẵn.
- [ ] Không đổi route, JSON, hub, tên bảng/cột, migration (P1–P3); không có model change EF.
- [ ] Allowlist không tăng.
- [ ] `code-reviewer` ĐẠT.

## Contract thay đổi

Không có trong P1–P3. P4: chỉ đổi schema SQL (không đổi API).

## Dữ liệu và migration

P1–P3: không có. P4: migration riêng theo ADR-0006, kiểm trên bản restore `LingoMint_Restore`, chạy DB thật khi người dùng xác nhận.

## Kiểm chứng

```bash
# trong <worktree>/EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln --filter "FullyQualifiedName~Architecture"
```

Không kiểm chứng tĩnh được: SQL Server thật, SignalR, provider Gemini.

## Rủi ro

- Model flash mở rộng phạm vi/đổi hành vi → gói nhỏ, kiểm `git diff -M`, review mỗi gói.
- Một số handler đọc `DbSet` của module khác (phát hiện khi chuyển interface) → thêm Contracts trong P2, tạm thời giữ `IAppDbContext` + allowlist cho file đó.
- `ConversationRepository`/`CefrPromptBuilder` là Singleton dùng factory (realtime) — giữ nguyên tới P2/P3, không đổi lifetime trong gói cơ học.

## Nhật ký quyết định

- 2026-11-01: một session Codex cho mỗi phase (`run-worker.sh --resume last`); khi `runs/codex-tokens.txt` > 250k thì resume kèm `--compact-limit 200000`.
- 2026-11-01: người dùng yêu cầu bỏ 6 test fail có sẵn → sửa nguyên nhân thay vì xóa: `GeneratedPronunciationAudioRepository.cs:39` dùng `EF.Functions.DataLength` (InMemory không hỗ trợ) → `a.AudioData.Length > 0` (SQL Server vẫn dịch thành `DATALENGTH`). Baseline mới mong đợi 0 fail.
- 2026-11-01: migration P4 chỉ viết, không apply; người dùng tự apply khi chạy project.
