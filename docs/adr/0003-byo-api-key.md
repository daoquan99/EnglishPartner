# ADR-0003: Giữ BYO API key, siết cách lưu và xử lý

- Trạng thái: Accepted
- Ngày: 2026-10-28
- Liên quan: review S9, S7

## Bối cảnh

BYO-key (Bring Your Own Key): mỗi học viên nhập Google AI Studio API key của mình; app dùng key đó gọi Gemini, chi phí tính vào tài khoản học viên. Hiện FE lưu key trong `localStorage` (`features/tutor/store/tutorSessionStore.ts`), gửi theo request/hub; server giữ key trong `_sessionApiKeys` của `GeminiLiveConnectionManager` và không dọn.

## Quyết định

- Giữ mô hình BYO-key.
- FE lưu key trong `sessionStorage` (mất khi đóng tab). Có thể thêm tùy chọn "ghi nhớ trên thiết bị này" dùng `localStorage` kèm cảnh báo rõ, mặc định tắt.
- Server chỉ dùng key trong phạm vi request hoặc phiên live: không log, không lưu DB, không đưa vào URL; xóa khi phiên kết thúc hoặc client ngắt kết nối.
- Rate limit theo user vẫn áp dụng cho endpoint AI (tài nguyên server không do key học viên chi trả).
- Key môi trường của server chỉ dùng cho tính năng quản trị khi plan ghi rõ.

## Hệ quả

- Học viên phải nhập lại key mỗi phiên trình duyệt (trừ khi bật ghi nhớ).
- Giảm rủi ro mất key qua XSS hoặc qua bộ nhớ server.

## Việc cần làm

- [x] FE: chuyển storage key, thêm tùy chọn ghi nhớ. — `tutorSessionStore.ts` (`sessionStorage` mặc định, cờ `lingomint_api_key_remember`).
- [x] BE: dọn `_sessionApiKeys` khi dừng phiên/disconnect; rà log không chứa key. — `TryRemove` khi dừng phiên/idle; không lệnh log nào chứa `apiKey`.
- [x] Rate limit cho endpoint AI (S7). — policy `ai` (theo user), `auth`, `auth-refresh`. (xác nhận 2026-11-02)
