# ADR-0004: Scale ngang bằng Redis backplane + sticky session

- Trạng thái: Accepted
- Ngày: 2026-10-28
- Liên quan: review A5, P7

## Bối cảnh

Live Tutor dùng 3 SignalR hub. `GeminiLiveConnectionManager` (Singleton) giữ WebSocket tới Gemini Live và state phiên (`_bridges`, `_turnStates`, `_sessionApiKeys`) trong RAM của một process.

## Quyết định

- Dùng Redis backplane cho SignalR (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) khi chạy từ 2 instance trở lên.
- Load balancer bật sticky session cho `/hubs/*`: WebSocket tới Gemini và state phiên vẫn ở instance đã mở phiên, Redis không chuyển được chúng.
- Thêm dọn state khi disconnect và TTL cho phiên mồ côi.
- Redis cũng có thể làm store cho rate limiter phân tán khi cần.

## Hệ quả

- Thêm hạ tầng Redis (connection string qua cấu hình, không commit).
- Instance chết thì phiên live trên instance đó mất; client phải khởi tạo lại phiên.
- Chạy một instance không cần Redis; backplane bật theo cấu hình.

## Thứ tự

Làm sau các mục bảo mật S1–S4 và S2/S3, vì scale hệ thống còn lỗ hổng phân quyền chỉ làm lỗ hổng lan rộng hơn.

## Việc cần làm

- [ ] `OnDisconnectedAsync` + TTL dọn state (P7).
- [ ] Bật Redis backplane theo cấu hình `Redis:ConnectionString`.
- [ ] Tài liệu triển khai: sticky session, health check Redis.
