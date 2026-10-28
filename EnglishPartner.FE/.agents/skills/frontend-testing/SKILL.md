---
name: frontend-testing
description: Viết, mở rộng hoặc chạy test Vitest cho EnglishPartner FE — route guard, auth store, httpClient (refresh token, lỗi), API adapter, hook TanStack Query và player bài học. Dùng khi một thay đổi FE cần bằng chứng test; không dùng thay cho kiểm tra UI bằng trình duyệt.
---

# Frontend testing

Đọc [AGENTS.md](../../../AGENTS.md). Test chạy bằng Vitest (`npm run test`), đặt trong `src/features/<feature>/__tests__/` hoặc `src/shared/**/__tests__/`. Ví dụ hiện có: `features/auth/__tests__/authGuards.test.tsx`, `shared/api/__tests__/httpClientErrors.test.ts`, `features/lessons/__tests__/lessonAttemptResumeAndNavigation.test.tsx`.

## Cần test gì

- **Auth/guard:** `RequireAuth`/`RequirePermission` theo trạng thái khởi tạo, đã/không đăng nhập, thiếu quyền; logout xóa cache và state.
- **httpClient:** gắn Bearer, refresh single-flight khi nhiều request 401, không retry endpoint auth, map ProblemDetails sang thông báo.
- **API adapter:** URL, method, body, đọc header phân trang và map DTO đúng contract ([api-contract-sync](../../../../.agents/skills/api-contract-sync/SKILL.md)).
- **Hook/component:** loading, rỗng, lỗi, thiếu quyền và thành công là các trạng thái khác nhau; response cũ không gắn vào đối tượng mới khi người dùng chuyển câu/từ.
- **Bảo mật:** token/API key không vào `localStorage` ngoài chỗ ADR cho phép; không render HTML thô từ nội dung AI.

## Quy ước

- Mock ở biên mạng (axios adapter hoặc module API), không mock nội bộ component.
- Mỗi test tự tạo `QueryClient` mới (`retry: false`); reset store Zustand và `sessionStorage`/`localStorage` giữa các test.
- Không gọi BE thật, SignalR thật, microphone hay provider AI.

## Chạy

```bash
npm run test -- src/features/auth/__tests__/authGuards.test.tsx   # trong EnglishPartner.FE
npm run build                                                      # type-check + build
```

Không mặc định chạy toàn bộ suite khi chỉ sửa một feature. Test pass không thay cho kiểm tra responsive, keyboard/focus và audio trên trình duyệt — ghi rõ trong report nếu chưa kiểm tra.
