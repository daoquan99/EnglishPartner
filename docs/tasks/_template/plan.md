# Plan: <tên task>

- Mã task: `YYYYMMDD-<slug>`
- Ngày:
- Liên quan: <mục review (S1, P2…), ADR>
- **Cần người dùng duyệt:** Có / Không — <lý do: migration, đổi contract, xóa dữ liệu, đổi luật>
- Trạng thái: Draft / Đã duyệt / Đang làm / Xong

## Mục tiêu

<Một đoạn ngắn: thay đổi gì, vì sao.>

## Tiêu chí hoàn thành

- [ ] <Kiểm tra được bằng test, lệnh hoặc quan sát cụ thể>
- [ ] <Với bảo mật: user B không truy cập được dữ liệu user A — có test>

## Hiện trạng đã xác minh

<File:dòng và hành vi hiện tại đã đọc trong code. Tách rõ phần còn giả định.>

## Phạm vi

| Được sửa | Không được sửa |
| --- | --- |
| `<đường dẫn>` | `<đường dẫn>` (lý do) |

## Contract thay đổi

| Loại | Trước | Sau | Tương thích ngược? |
| --- | --- | --- | --- |
| route / DTO / hub event / paging / lỗi | | | |

## Dữ liệu và migration

<Migration mới, dữ liệu bị đổi/xóa, cách backup, ai xác nhận. Ghi "Không có" nếu không có.>

## Phân công

| Phần việc | Worker | Scope | Skill cần đọc |
| --- | --- | --- | --- |
| 1. | Codex | be | `EnglishPartner.BE/.agents/skills/...` |
| 2. | AGY | fe | `EnglishPartner.FE/.agents/skills/...` |

## Kiểm chứng

```bash
# BE (trong EnglishPartner.BE)
dotnet build EnglishLiveTutor.sln
dotnet test EnglishLiveTutor.sln --filter "<filter>"
# FE (trong EnglishPartner.FE)
npm run build
npm run test -- <đường-dẫn-test>
```

Không kiểm chứng được tĩnh: <SQL Server thật, SignalR, microphone, provider AI…>

## Review cần chạy

- [ ] security-reviewer
- [ ] performance-reviewer
- [ ] code-reviewer
- [ ] Review chéo bằng worker khác (task rủi ro cao)

## Rủi ro và câu hỏi mở

-
