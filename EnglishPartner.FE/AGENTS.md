# EnglishPartner Frontend — hướng dẫn làm việc

## Phạm vi và hiện trạng

Hướng dẫn này áp dụng cho `EnglishPartner.FE`. Git root là thư mục cha `EnglishPartner`; `EnglishPartner.BE` là dự án cùng cấp. Công việc FE không tự mở rộng thành sửa BE, migration hoặc cấu hình triển khai.

Ứng dụng hiện dùng React 19, TypeScript, Vite, Tailwind CSS 4, React Router, TanStack Query, Zustand và i18next. Tên package/source còn dùng `EnglishLiveTutor`; tên hiển thị hiện tại là **LingoMint AI Tutor**. Không đổi tên trong công việc không liên quan.

Hai portal đang tồn tại:

| Portal | Route và shell | Mục đích |
| --- | --- | --- |
| Student Portal | `/student/*`, `src/app/layouts/StudentLayout.tsx`, `.learnerTheme` | Live Tutor, học từ vựng, luyện và chấm phát âm |
| Admin Portal | `/admin/*`, `src/app/layouts/AdminLayout.tsx`, `.adminTheme` | Quản trị nội dung, catalog AI, người dùng và phân quyền |

Đọc [quy tắc thiết kế hệ thống](.agents/design-system.md) khi làm UI. Các quy tắc là hướng dẫn cho thay đổi tiếp theo, không phải xác nhận toàn bộ source hiện đã tuân thủ.

## Cách chọn skill

Đọc skill liên quan tới công việc, không cần tải toàn bộ:

| Công việc | Skill |
| --- | --- |
| Token, typography, shared UI, theme và tương tác chung | [design-system](.agents/skills/design-system/SKILL.md) |
| Thiết kế/sửa màn hình học, tutor, từ vựng, phát âm và lịch sử học viên | [student-portal-design](.agents/skills/student-portal-design/SKILL.md) |
| Thiết kế/sửa trang quản trị, bảng dữ liệu, form CRUD và quyền thao tác | [admin-portal-design](.agents/skills/admin-portal-design/SKILL.md) |
| Kiểm tra thiết kế, responsive và trạng thái UI của một thay đổi | [portal-design-review](.agents/skills/portal-design-review/SKILL.md) |

Skill hỗ trợ phạm vi người dùng đã yêu cầu; không tự cho phép thiết kế lại toàn bộ ứng dụng. Yêu cầu mới của người dùng quyết định phạm vi công việc.

## Quy tắc thực hiện

- Đọc `git status --short` trước khi sửa. Giữ nguyên thay đổi tracked và file untracked không thuộc công việc; không ghi đè hoặc reset.
- Yêu cầu “check”, phân tích hoặc “cho prompt” chỉ cho phép kiểm tra và tạo tài liệu được yêu cầu. Không tự thực thi các prompt trong [docs/prompts](docs/prompts).
- Khi đã được yêu cầu triển khai, hoàn thành trong phạm vi được giao; không hỏi lại cho các bước sửa/kiểm tra thông thường đã được cho phép.
- Không đọc/in giá trị `.env`, API key, access token, refresh token hoặc connection string để mô tả thiết kế. Dùng tên cấu hình và dữ liệu minh họa không nhạy cảm.
- Không tự cài thêm UI framework, đổi router/store hoặc tạo hệ thống component song song chỉ để hoàn thành một màn hình.
- Báo cáo tiếng Việt; dùng đúng tên file, route, symbol và contract khi giải thích kỹ thuật.

## Cấu trúc và quyền sở hữu

- `src/app`: composition, providers, router và hai shell; không đặt nghiệp vụ riêng của trang tại đây.
- `src/features/<feature>`: page, hook, API adapter, type và component của miền đó. Dùng public `index.ts` cho tích hợp giữa feature khi phù hợp với cấu trúc hiện có.
- `src/shared`: UI và tiện ích không mang nghiệp vụ riêng; không import ngược `app` hoặc `features`.
- Request dùng `src/shared/api/httpClient.ts`; giữ cơ chế credentials, header, token refresh và lỗi hiện có.
- Server state thuộc TanStack Query; client UI state thuộc state cục bộ hoặc store hiện có. Không sao chép lịch sử server vào localStorage như dữ liệu chính thức.
- Khi sửa contract, đối chiếu API adapter, DTO FE và endpoint/DTO BE liên quan. `openapi.types.ts` là nguồn type trong repo, không phải bằng chứng API đang chạy đã đồng bộ.
- Nếu BE thiếu chức năng, ghi rõ gap và contract cần thêm; không bịa endpoint, score, trạng thái thành công hoặc dữ liệu persisted.

## Thiết kế và tương tác cần giữ

- Giữ hai shell và route tách biệt, dùng chung UI nền tảng; không đưa công cụ quản trị vào luồng học thông thường.
- Giữ theme theo portal. CSS variable chỉ có tác dụng khi component thực sự sử dụng; không coi việc khai báo token là đã theme hóa toàn bộ UI.
- Shell hiện khóa body scroll, sidebar và main cuộn độc lập. Không thêm `h-screen` vào page gây cuộn lồng không cần thiết.
- Student Portal hiện có một điểm nhập Google AI Studio API Key tại Left Menu; tái dùng store và cơ chế focus đó khi cần, không tạo thêm ô nhập key rải rác.
- Khi đổi đối tượng luyện tập hoặc rời trang: quản lý microphone, audio, request đang chờ và dữ liệu đúng đối tượng; không để response cũ gắn vào câu/từ mới.
- Menu, route guard và quyền của thao tác phải nhất quán. Việc ẩn nút ở FE không thay thế authorization tại BE; không sửa quyền bằng cách bỏ guard.
- Trạng thái loading, rỗng, lỗi, thiếu quyền và đang lưu phải phân biệt rõ. Không biến lỗi API thành dữ liệu rỗng hoặc thành công.

## Kiểm chứng và bàn giao

- Chỉ tạo tài liệu/skill: kiểm tra link cục bộ, YAML frontmatter, scaffold còn sót và whitespace; không cần chạy app build/test hay khởi động BE.
- Sửa source: chọn test có ý nghĩa trong feature bị tác động và `npm run build` khi phù hợp. `npm run test -- <đường-dẫn-test>` chạy Vitest có giới hạn; không mặc định chạy cả suite.
- Sửa UI: kiểm tra viewport nhỏ/desktop, text Việt/Anh dài, keyboard/focus, scroll và các trạng thái liên quan nếu có công cụ UI. Nếu chưa kiểm tra bằng trình duyệt, nói rõ.
- Build/type-check không xác nhận API, SQL Server, microphone, SignalR hoặc provider AI hoạt động thực tế. Không gọi AI trả phí hay khởi động BE có migration/seed chỉ để kiểm tra tài liệu.
- Handoff nêu kết quả, file đổi, kiểm chứng đã chạy và phần còn thiếu. Không commit/push/publish nếu chưa được yêu cầu.
