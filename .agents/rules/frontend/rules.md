# EnglishPartner Web — Project Rules

> **Đây là luật, không phải gợi ý.** Mọi vi phạm đều tạo ra technical debt và phải sửa lại.

---

## 1. Stack & Công cụ

| Mục | Công cụ |
|---|---|
| Framework | Next.js 15+ (App Router) |
| Language | TypeScript strict mode |
| Styling | TailwindCSS v4 + shadcn/ui |
| Server state | React Query v5 |
| Client state | Zustand v5 |
| HTTP | Axios (chỉ dùng instance từ `@/lib/axios`) |
| Forms | React Hook Form + Zod |
| Realtime | @microsoft/signalr |
| Package Manager | pnpm |

---

## 2. Component Rules — QUAN TRỌNG NHẤT

### 2.1 Giới hạn kích thước file — CỨNG

| Loại file | Giới hạn tối đa |
|---|---|
| Page component (`app/**/page.tsx`) | 80 dòng |
| Feature component | 150 dòng |
| Dialog component | 120 dòng |
| Form component | 150 dòng |

**Khi vượt giới hạn → phải tách ngay, không có ngoại lệ.**

### 2.2 Quy tắc tách component

Tách ra file riêng khi:
- Có `Dialog` / `AlertDialog` → `<Name>Dialog.tsx`
- Có `Form` với nhiều field → `<Name>Form.tsx`
- Có `Table` → mỗi row phức tạp tách thành `<Name>TableRow.tsx`
- Có tab với nhiều content → mỗi tab content tách riêng

### 2.3 One Component Per File

- 1 file = 1 exported component (trừ helper components nhỏ < 20 dòng dùng nội bộ)
- Tên file = tên component: `UserFormDialog.tsx` export `UserFormDialog`

### 2.4 Props Interface

Mọi component có props đều phải định nghĩa interface rõ ràng. Không dùng `any`.

---

## 3. Anti-Patterns — CẤM TUYỆT ĐỐI

- **CẤM** dùng `confirm()`, `alert()`, `prompt()` → dùng AlertDialog / Dialog / toast
- **CẤM** dùng `any` không có lý do → phải tạo type trước khi code
- **CẤM** gọi axios trực tiếp trong component → luôn qua React Query hook
- **CẤM** import axios không từ `@/lib/axios` → chỉ dùng `apiClient`
- **CẤM** TODO không rõ ràng → phải có context: `TODO(#ticket): mô tả cụ thể`
- **CẤM** lưu access token ra localStorage/sessionStorage/cookie → chỉ Zustand in-memory

---

## 4. Workflow Bắt Buộc Khi Code

### 4.1 Trước khi tạo component mới

1. Kiểm tra shadcn/ui đã có component tương tự chưa → dùng trước
2. Nếu cần shadcn component mới → chạy: `pnpm dlx shadcn@latest add <component>`
3. Xác định props interface
4. Xác định file nào sẽ chứa (không nhét vào file đã > 150 dòng)

### 4.2 Trước khi import một module

1. Verify file đó tồn tại trong project
2. Verify named export khớp chính xác
3. Nếu là shadcn component → check `src/ui/<component>.tsx` tồn tại, nếu chưa → cài trước

### 4.3 Sau khi tạo/sửa file

1. Đọc lại file — kiểm tra import paths, types, anti-patterns, giới hạn dòng
2. Verify import paths ở nơi khác nếu file mới
3. Verify package.json nếu thêm dependency mới

---

## 5. API & Data Fetching

### 5.1 Cấu trúc layer

```
Component → useXyzQuery/Mutation (React Query) → xyzApi (axios fn) → apiClient → Backend
```

> Chi tiết patterns xem `.agents/rules/skills.md`

---

## 6. Forms

- **Luôn** dùng `react-hook-form` + `zod` — không dùng `useState` để quản lý form values
- Zod schema trong `features/<name>/types/<name>.schemas.ts`
- Type derive từ schema: `type FormData = z.infer<typeof schema>`
- Dùng `<FormField>` / `<FormItem>` / `<FormMessage>` của shadcn

---

## 7. TypeScript

- `strict: true` — không được tắt
- Không dùng `any` — nếu bắt buộc phải có comment giải thích + ticket
- Không dùng `as` type assertion trừ khi thực sự cần (kèm comment)
- API response types phải explicit — không infer từ `axios.get()` raw

---

## 8. Naming Conventions

| Item | Convention | Ví dụ |
|---|---|---|
| Component files | PascalCase | `UserFormDialog.tsx` |
| Hook files | camelCase | `useAdminUsers.ts` |
| Type files | camelCase | `admin.types.ts` |
| Schema files | camelCase | `admin.schemas.ts` |
| API files | camelCase | `admin.api.ts` |
| Folders | kebab-case | `user-management/` |
| React Query hooks | `use` + PascalCase | `useAdminUsers` |
| Mutation hooks | `use` + Verb + Noun | `useDeleteUser` |
| Query keys | lowercase array | `['admin', 'users']` |

---

## 9. Styling

- **Tailwind-first** — không inline styles, không CSS modules
- `cn()` cho conditional classes
- Design token qua CSS variables: `text-[var(--color-text-primary)]`
- Dark mode là default — design dark trước, light sau
- shadcn components trong `src/ui/` — **KHÔNG SỬA TAY** — override qua `className` prop

---

## 10. Auth & Security

- Access token: chỉ trong Zustand (in-memory)
- Refresh token: HttpOnly cookie — FE không đọc, không ghi
- Axios interceptor tự inject `Authorization: Bearer` và `X-Timezone-Offset`
- 401 → interceptor tự gọi `/auth/refresh` → retry
- Middleware `middleware.ts` protect routes dựa trên cookie `ep-role`

---

## 11. Server vs Client Components

```
Default: Server Component
'use client' chỉ khi:
  - Có event handlers (onClick, onChange...)
  - Dùng browser APIs (window, localStorage...)
  - Dùng React hooks (useState, useEffect...)
  - Dùng Zustand store
```

**Page components trong `app/` nên là Server Component** — truyền data xuống Client Components con.

---

## 12. Realtime (SignalR)

- Hub connection khởi tạo 1 lần trong `lib/signalr.ts`
- Subscribe qua custom hooks trong `features/notifications/hooks/`
- Không connect SignalR trực tiếp trong component
- Luôn cleanup listener khi unmount

---

## 13. Commit & Code Quality

- Không `console.log` trong committed code
- ESLint + Prettier trước khi commit
- TODO phải có context (xem section 3)
- Không push code chưa build thành công
