# Report: 20261102-grammar-a2-content

## A2 (C1–C4): xong

- AGY soạn 200 bài (`a2-exercises.json`). Claude tự giải từng bài và sửa 33 bài. Đáp án đúng của câu MC được rải lại thành 15/15/15/15 trên 4 vị trí (xem `review/a2-review.md`).
- Bạn ủy quyền cho Claude tự duyệt (2026-11-02).
- Gộp vào `grammar-a2.json` bằng cách chèn trực tiếp vào text, không reformat phần lý thuyết. Script so sánh và xác nhận mọi nội dung ngoài `exercises` giữ nguyên.
- `GrammarBundledContentPackTests`:
  - Import tạo đủ 400 bài, không có lỗi.
  - A1 và A2: mỗi level có 20 step, mỗi step 10 bài core, tất cả `Deterministic`, step nào cũng có bài difficulty 3.
  - Import lần hai không tạo thêm bài nào.
- DB đang chạy: admin bấm "Nhập nội dung chuẩn" ở `/admin/grammar` để thêm 200 bài A2. Importer chỉ thêm bài theo key mới, không sửa bài đã có.

## Lệnh đã chạy

| Lệnh | Kết quả |
| --- | --- |
| `node insert-exercises.js grammar-a2.json a2-exercises.json` | Chèn 200 bài vào 20 step, nội dung khác giữ nguyên |
| `dotnet test tests/Modules/Grammar/EnglishLiveTutor.Modules.Grammar.Tests` | 141/141 pass |

## A1: rải lại vị trí đáp án MC (làm trong task này)

- Trước đây 56/60 câu MC của A1 có đáp án đúng ở lựa chọn đầu tiên. Đã xáo `content.options` thành 15/15/15/15. Id, text và `grading` của từng lựa chọn giữ nguyên, nên cách chấm không đổi. Không có `explanation` nào nhắc tới vị trí lựa chọn.
- A1-16-E08/E09: không sửa. Grader so theo id token, và "The"/"the" là hai token khác nhau, nên đảo hai token này sẽ tạo câu viết hoa sai, việc chấm sai là hợp lý.
- **Chỉ có tác dụng với DB mới.** Importer bỏ qua các bài đã có `key`, nên DB đang chạy vẫn giữ thứ tự cũ. Có hai cách cập nhật DB đang chạy, cả hai đều **chưa làm** và cần bạn xác nhận theo luật 12:
  - sửa từng bài trong trang admin;
  - viết data migration riêng có backup.

## B1 (đợt 2): xong

- **Soạn bài:** AGY báo hết credit (429 `RESOURCE_EXHAUSTED`), nên chuyển sang Codex. Codex soạn 180 bài (`b1-exercises.json`), prompt ở `prompt-agy-b1.md`.
- **Review:** Claude tự giải từng bài và sửa 23 chỗ trên 22 bài. Chi tiết trong `review/b1-review.md`. Bạn đã ủy quyền cho Claude tự duyệt.
- **Gộp vào pack:** chèn vào `grammar-b1.json` theo cách chèn text như A2.
- **Test:** `GrammarBundledContentPackTests` kiểm 580 bài. Mỗi level A1, A2, B1 có đủ step × 10 bài core, tất cả `Deterministic`, và step nào cũng có bài difficulty 3.
- **Lệnh đã chạy:** `dotnet test EnglishLiveTutor.slnx`, kết quả 777/777 pass.
- **DB đang chạy:** admin bấm "Nhập nội dung chuẩn" ở `/admin/grammar` để thêm 200 bài A2 và 180 bài B1.

## Vòng 2 sau code review (A2 và B1)

Code-reviewer kết luận ĐẠT CÓ ĐIỀU KIỆN. Đã xử lý:

- **Hint lộ đáp án.** Hint hiện dưới dạng placeholder trong ô nhập (`FillInTheBlanksPlayer.tsx`), nên học viên nhìn thấy được.
  - Đã đổi 28 hint ở A2 và 21 hint ở B1 sang dạng lựa chọn, ví dụ "(so / such)" hay "(just / already / yet)". Riêng phrasal verb B1-17 thì ghi nghĩa tiếng Việt.
  - Giữ các hint là động từ hoặc tính từ gốc, vì bài vẫn kiểm việc chọn đúng thì hoặc dạng từ.
- **B1-14-E07:** câu cũ "We stopped ___ photos" chấp nhận được cả `taking`. Đã đổi thành "We stopped the car ___ photos".
- **Biến thể đáp án:** B1-03-E07 thêm `got used to` và `had been living`.
- **Câu gián tiếp:** B1-11-E08 đổi "last year" thành "the previous year".
- **Câu gần trùng:** đã viết lại B1-18-E02.
- **`maxLength`:** sửa 5 chỗ trống ở A2 có độ dài nhỏ hơn đáp án.
- **Test kiểm chặt hơn:** bảng type và difficulty E01–E10 đúng thứ tự, `maxPoints` = 10, key không trùng trong level.
- **Hồ sơ:**
  - Các script nằm ở `scripts/`, chạy lại được.
  - Prompt B1 đổi tên thành `prompt-b1.md`, vì Codex chạy prompt này.
  - Pack được dựng lại từ bản gốc (A2 từ `ca566de^`, B1 từ bản trước khi chèn) bằng `scripts/insert-exercises.js`. Phần lý thuyết giữ nguyên.

## Chưa kiểm chứng được

- Chưa import trên SQL Server thật. Test hiện chạy importer trên EF InMemory.
- Chưa xem bài A2/B1 trên giao diện FE bằng trình duyệt.
- DB đang chạy giữ nguyên thứ tự option A1 cũ, và các bài A2 đã import sẽ không nhận hint mới. Lý do: importer bỏ qua key đã tồn tại. A2 mới commit hôm nay; nếu DB chưa import A2 thì lần import tới sẽ lấy bản đã sửa. Tôi chưa kiểm DB thật đã import A2 hay chưa.
