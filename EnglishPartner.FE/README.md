# EnglishLiveTutor Frontend (React 19 + Vite + TypeScript)

## 1. Kiến trúc Feature-Based + Separation of Concerns + Role/Permission-Based Routing

```text
src/
├── app/                              # App Shell, Providers, Layouts & Lazy Router
│   ├── layouts/
│   │   ├── StudentLayout.tsx         # Layout Cổng Học Viên (Live Tutor + Daily Vocabulary)
│   │   └── AdminLayout.tsx           # Layout Cổng Quản Trị (AdminSidebar + RBAC)
│   ├── providers/
│   │   └── AppProviders.tsx          # TanStack QueryClientProvider + I18nextProvider + AuthBootstrap
│   ├── router.tsx                    # React.lazy + Suspense + RequireAuth + RequirePermission
│   └── App.tsx
├── features/                         # Độc lập theo từng miền nghiệp vụ (chỉ export qua index.ts)
│   ├── auth/                         # Login, Register, RefreshToken, RequireAuth, RequirePermission, CanAccess
│   ├── catalog/                      # DB Catalogs: 19 Modes, 30 Voices, Speech Speeds, Topics, AI Models
│   ├── tutor/                        # Live AI Speaking Tutor (3 SignalR Hubs + Setup + Grammar Correction)
│   ├── vocabulary/                   # Sense Flashcards + SM-2 + AI Vocab Generator + Audio Pronunciation Studio
│   └── admin/                        # AdminSidebar + Dashboard, Topics, Prompts, Voices, Rubrics, Users, Roles
└── shared/                           # Dùng chung (Không chứa business logic)
    ├── api/
    │   ├── httpClient.ts             # Axios + In-Memory AccessToken + HttpOnly Cookie + Single-Flight 401 Refresh
    │   └── openapi.types.ts          # OpenAPI TypeScript Contracts đồng bộ với Backend .NET 10
    ├── components/ui/                # Button, Badge, TopicCreatablePicker
    ├── i18n/                         # i18next (VI / EN)
    └── lib/cn.ts
```

## 2. Hướng dẫn chạy với Backend (.NET 10)

```bash
npm install
npm run dev
```
- Mặc định Vite chạy tại `http://localhost:5173` và proxy `/api` + `/hubs` sang Backend `.NET 10` tại `http://localhost:5099`.
- Tài khoản Seeder có sẵn:
  - **Admin**: `admin` / `Admin@123`
  - **Student (Learner)**: `default-learner` / `Learner@123`
