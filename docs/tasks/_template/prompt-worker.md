# Việc giao cho <Codex|AGY>: <tên phần việc>

## Mục tiêu

<Một câu. Liên quan: mục review / ADR.>

## Bối cảnh cần đọc

- `AGENTS.md` (gốc) và `<EnglishPartner.BE|EnglishPartner.FE>/AGENTS.md`
- Plan: `docs/tasks/<mã-task>/plan.md`
- Skill: `<đường dẫn SKILL.md từ gốc repo>`
- Code liên quan: `<file:dòng>` — <hành vi hiện tại>

## Phạm vi

Chỉ được sửa:
- `<đường dẫn>`

Không được sửa:
- Migration, `appsettings*.json`, contract ngoài danh sách dưới đây, file không nêu ở trên.

Contract được phép đổi: <không có | liệt kê chính xác>

## Yêu cầu

1. <Bước cụ thể>
2. <Bước cụ thể>
3. Viết/cập nhật test: <file test, case cần có>

## Tiêu chí hoàn thành

- [ ] <Kiểm tra được>
- [ ] Build và test dưới đây pass

## Lệnh kiểm chứng

```bash
<lệnh build/test chạy trong thư mục làm việc>
```

Nếu môi trường không cho chạy lệnh (sandbox, thiếu mạng), ghi rõ trong report — không đoán kết quả.

## Ràng buộc

- Không commit, push, merge; không tạo branch.
- Không thêm package/dependency nếu không được nêu ở trên.
- Không in hoặc ghi secret, token, API key, connection string.
- Gặp điều mơ hồ hoặc cần sửa ngoài phạm vi: dừng và ghi vào report, không tự quyết.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt từng yêu cầu>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, hoặc "không chạy được: lý do">
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
