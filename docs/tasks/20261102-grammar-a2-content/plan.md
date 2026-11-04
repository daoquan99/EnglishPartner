# Plan: Bộ bài core Grammar A2 (rồi đến B1)

- Mã task: `20261102-grammar-a2-content`
- Ngày: 2026-11-02
- Liên quan: task `20261029-grammar-roadmap` (G6 A1), `docs/tasks/20261029-grammar-roadmap/exercise-prompt.md`, `curriculum.md`
- **Cần người dùng duyệt:** Có. Bạn duyệt bản review nội dung trước khi gộp vào content pack. Không có code mới, không migration, không đổi contract.
- Trạng thái: Đã duyệt 2026-11-02 ("ok bạn làm đi") — gồm các điểm cần chốt theo đề xuất

## Mục tiêu

Soạn 200 bài core cho 20 step A2 (10 bài mỗi step, cùng cấu trúc với A1), gộp vào `grammar-a2.json` để học viên luyện và qua step A2 được. B1 (18 step, 180 bài) làm đợt sau khi A2 đã duyệt. Cách soạn của B1 rút kinh nghiệm từ A2.

## Tiêu chí hoàn thành

- [x] `grammar-a2.json`: 20 step × 10 bài. `key` có dạng `A2-NN-ENN` và không trùng. `isCore=true`, `Deterministic`, `maxPoints=10`.
- [x] `GrammarBundledContentPackTests` pass, có thêm kiểm tra cho A2: 10 bài mỗi step, đủ bộ type theo bảng, importer không báo lỗi và import lần hai không tạo bản trùng.
- [x] Bản review `review/a2-review.md` liệt kê từng bài: câu hỏi, đáp án và ghi chú. Claude đã tự giải toàn bộ, sửa các bài sai và ghi lại số bài đã sửa. **Bạn duyệt** bản review. *(2026-11-02: bạn ủy quyền cho Claude tự review và duyệt.)*
- [x] Không bài nào dùng ngữ pháp của step sau. Từ vựng ở mức A1–A2. Câu tiếng Anh không quá 15 từ.

## Hiện trạng đã xác minh

- `src/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Infrastructure/Grammar/ContentPacks/grammar-a2.json`: 20 step đã có lý thuyết, **0 bài tập**. A1 có 200 bài (G6).
- `GrammarContentPackImporter` thêm bài theo `key` cho từng step, idempotent và bỏ qua bài trùng hash. Trên DB đã có dữ liệu, admin bấm "Nhập nội dung chuẩn" ở `/admin/grammar` là bài mới được thêm.
- Quy trình A1 (prompt `prompt-agy-g6-a1.md`): AGY soạn file JSON riêng, Claude review và sửa 41/200 bài, sau đó người dùng duyệt.

## Phân công

| Bước | Ai | Kết quả |
| --- | --- | --- |
| C1 | AGY | Soạn `docs/tasks/20261102-grammar-a2-content/a2-exercises.json`. Prompt lấy từ G6, đổi các điểm: level A2, phạm vi ngữ pháp là A1 cộng các step A2 trước step đang soạn, câu ≤ 15 từ, step ôn tập `A2-20`. Mỗi step đúng 10 bài theo bảng type của A1. Chỉ tạo file này, không sửa code. |
| C2 | Claude | Kiểm schema bằng importer thật (test tạm), tự giải 200 bài, sửa bài sai và viết `review/a2-review.md`. |
| C3 | Bạn | Duyệt hoặc ghi chú vào bản review. |
| C4 | Claude | Gộp vào `grammar-a2.json`, cập nhật `GrammarBundledContentPackTests`, chạy build/test, commit. |
| Đợt 2 | | Lặp lại C1–C4 cho B1 sau khi A2 xong. |

## Kiểm chứng

```bash
node -e "JSON.parse(require('fs').readFileSync('docs/tasks/20261102-grammar-a2-content/a2-exercises.json','utf8'))"
dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx --filter "FullyQualifiedName~GrammarBundledContentPack"
dotnet test EnglishPartner.BE/EnglishLiveTutor.slnx
```

## Review cần chạy

- [x] Claude review nội dung (đáp án, phạm vi ngữ pháp)
- [x] code-reviewer: ĐẠT CÓ ĐIỀU KIỆN; đã xử lý các điều kiện ở vòng 2 (xem report)

## Rủi ro

- Đáp án sai là lỗi nặng nhất: ở A1, AGY làm sai khoảng 20%. Vì vậy bắt buộc tự giải toàn bộ, không dò mẫu.
- A2 có nhiều cấu trúc cho phép nhiều đáp án (`going to`/`will`, các dạng so sánh). Phải liệt kê đủ `acceptedAnswers`; câu nào còn mơ hồ thì bỏ.

## Đợt 2: B1 (thêm ngày 2026-11-02)

- [x] `grammar-b1.json`: 18 step, mỗi step 10 bài. Bảng type và luật chất lượng giống A2, câu tối đa 18 từ, bổ sung các bẫy riêng của B1. Prompt: `prompt-b1.md`.
- [x] Worker: AGY hết credit (lỗi 429), chuyển sang Codex. Claude tự giải và sửa bài; review ở `review/b1-review.md`.
- [x] Người dùng ủy quyền cho Claude tự review và tự duyệt.
- [x] Test đã đổi: `GrammarBundledContentPackTests` kiểm 580 bài, bảng type E01–E10 đúng thứ tự, `maxPoints` = 10, key không trùng trong level, và import lần 2 không tạo bài trùng.
- [x] Phạm vi mở rộng (có ghi report): xáo vị trí option của A1 MC; sửa hint lộ đáp án và `maxLength` của A2.
