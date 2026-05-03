# EnglishPartner — Full-Stack Integration Rules

> **Scope:** Rules governing how Backend and Frontend interact.
> BE-only → `backend/rules.md` | FE-only → `frontend/rules.md`

---

## 1. Project Structure

```
EnglishPartner/
├── EnglishPartner.API/          ← Backend (.NET 10, C#)
│   ├── src/
│   │   ├── Apps/EnglishPartner.Api/      ← Host, Endpoints, Hubs
│   │   ├── Modules/                       ← 7 modules (Identity, AI, Curriculum, Learning, ...)
│   │   │   └── {Module}/
│   │   │       ├── {Module}.Domain/       ← Entities, AggregateRoots, DomainEvents
│   │   │       ├── {Module}.Application/  ← CQRS Commands/Queries, DTOs, Interfaces
│   │   │       ├── {Module}.Infrastructure/ ← EF Core, Repositories, External Services
│   │   │       ├── {Module}.Api/          ← Endpoints, ModuleInstaller
│   │   │       └── {Module}.Contracts/    ← Integration Events (cross-module)
│   │   └── Shared/
│   │       ├── SharedKernel/              ← AggregateRoot, Entity, Result<T>, Error
│   │       └── SharedKernel.Application/  ← ICurrentUserService, ICacheService, Settings
│   └── tests/
│
├── EnglishPartner.Web/          ← Frontend (Next.js 15+, TypeScript)
│   └── src/
│       ├── app/                           ← Pages (App Router)
│       ├── features/                      ← Feature modules (api/, components/, types/)
│       ├── shared/                        ← Shared components, hooks, utils
│       ├── lib/                           ← axios, react-query, zustand, signalr
│       └── ui/                            ← shadcn/ui primitives (DO NOT EDIT)
│
└── .agents/                     ← THIS — Unified agent rules
```

---

## 2. Dependency Inventory

> **Source of truth:** BE → `Directory.Packages.props` (Central Package Management) | FE → `package.json`
> These are pinned versions. Do NOT upgrade without user approval.

### 2.1 Backend — .NET 10 (C#)

**Build config** (`Directory.Build.props`):
- `TargetFramework`: `net10.0`
- `LangVersion`: `latest`
- `ImplicitUsings`: enabled
- `Nullable`: enabled
- `TreatWarningsAsErrors`: `true`

**NuGet packages** (`Directory.Packages.props`):

| Category | Package | Version |
|---|---|---|
| **CQRS/DDD** | MediatR | 14.1.0 |
| **Validation** | FluentValidation | 12.1.1 |
| | FluentValidation.DependencyInjectionExtensions | 12.1.1 |
| **Data** | Microsoft.EntityFrameworkCore | 10.0.4 |
| | Microsoft.EntityFrameworkCore.Design | 10.0.4 |
| | Microsoft.EntityFrameworkCore.Relational | 10.0.4 |
| | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |
| **Cache** | Microsoft.Extensions.Caching.StackExchangeRedis | 10.0.0 |
| **Auth** | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0 |
| | Microsoft.IdentityModel.JsonWebTokens | 8.17.0 |
| | Microsoft.IdentityModel.Tokens | 8.17.0 |
| | System.IdentityModel.Tokens.Jwt | 8.17.0 |
| | Google.Apis.Auth | 1.69.0 |
| | BCrypt.Net-Next | 4.0.3 |
| **AI** | Google.GenAI | 1.6.1 |
| | Google.Cloud.AIPlatform.V1 | 3.70.0 |
| | Google.Cloud.TextToSpeech.V1 | 3.18.0 |
| | Google.Cloud.Speech.V1 | 3.9.0 |
| | Mscc.GenerativeAI | 3.1.0 |
| | Microsoft.Extensions.Http.Resilience | 9.6.0 |
| **Audio** | NAudio | 2.3.0 |
| **API** | Asp.Versioning.Http | 8.1.1 |
| | Scalar.AspNetCore | 2.13.22 |
| | Microsoft.AspNetCore.OpenApi | 10.0.0 |
| **Jobs** | Quartz | 3.18.0 |
| | Quartz.Extensions.Hosting | 3.18.0 |
| | Hangfire.AspNetCore | 1.8.23 |
| | Hangfire.PostgreSql | 1.21.1 |
| **DI** | Scrutor | 7.0.0 |
| **Email** | MailKit | 4.13.0 |
| | Resend | 0.5.0 |
| **Logging** | Serilog.AspNetCore | 10.0.0 |
| | Serilog.Enrichers.Environment | 3.0.1 |
| | Serilog.Enrichers.Thread | 4.0.0 |
| | Serilog.Sinks.Console | 6.1.1 |
| **Health** | AspNetCore.HealthChecks.NpgSql | 9.0.0 |
| | AspNetCore.HealthChecks.Redis | 9.0.0 |
| **Telemetry** | OpenTelemetry.Extensions.Hosting | 1.15.2 |
| | OpenTelemetry.Instrumentation.AspNetCore | 1.15.2 |
| **JSON** | Newtonsoft.Json | 13.0.4 |
| **Testing** | xunit | 2.9.3 |
| | Microsoft.NET.Test.Sdk | 17.14.0 |
| | NSubstitute | 5.3.0 |
| | FluentAssertions | 7.2.0 |
| | Testcontainers.PostgreSql | 4.14.2 |
| | coverlet.collector | 6.0.4 |

### 2.2 Frontend — Next.js 15+ (TypeScript)

**Package manager:** pnpm

**Dependencies:**

| Category | Package | Version |
|---|---|---|
| **Framework** | next | ^15.3.1 |
| | react | ^19.1.0 |
| | react-dom | ^19.1.0 |
| **Server State** | @tanstack/react-query | ^5.99.0 |
| | @tanstack/react-query-devtools | ^5.99.0 |
| **Client State** | zustand | ^5.0.4 |
| **HTTP** | axios | ^1.9.0 |
| **Forms** | react-hook-form | ^7.56.3 |
| | @hookform/resolvers | ^5.2.2 |
| | zod | ^3.25.28 |
| **UI** | radix-ui | ^1.4.3 |
| | lucide-react | ^0.511.0 |
| | class-variance-authority | ^0.7.1 |
| | clsx | ^2.1.1 |
| | tailwind-merge | ^3.3.0 |
| | sonner | ^2.0.3 |
| | recharts | ^2.15.3 |
| **Animation** | motion | ^12.9.4 |
| **Realtime** | @microsoft/signalr | ^8.0.7 |
| **Auth** | @react-oauth/google | ^0.13.5 |
| **URL State** | nuqs | ^2.4.3 |
| **Date** | date-fns | ^4.1.0 |

**Dev Dependencies:**

| Package | Version |
|---|---|
| tailwindcss | ^4.1.6 |
| @tailwindcss/postcss | ^4.1.6 |
| typescript | ^5.8.3 |
| @types/node | ^22.15.17 |
| @types/react | ^19.1.4 |
| @types/react-dom | ^19.1.3 |
| eslint | ^9.26.0 |
| eslint-config-next | ^15.3.1 |
| prettier | ^3.5.3 |
| prettier-plugin-tailwindcss | ^0.6.12 |

---

## 3. API Contract

### 3.1 URL Convention

```
User routes:    /api/v1/{module}/{resource}          → RequireAuthorization
Admin routes:   /api/v1/admin/{module}/{resource}    → RequireAuthorization("{permission}")
```

### 3.2 Request/Response Flow

```
[FE Component]
    → useXxxQuery/Mutation (React Query)
        → xxxApi function (apiClient.get/post/...)
            → [HTTP] → /api/v1/...
                → [BE Minimal API Endpoint]
                    → ISender.Send(Command/Query)
                        → Handler → Result<T>
                            → 200/201/400/404/500
```

### 3.3 Response Patterns

| Backend Result | HTTP Status | FE Handling |
|---|---|---|
| `Result.Success()` | 200 OK | `onSuccess` callback |
| `Result.Success(value)` | 200/201 with JSON body | Parse response type |
| `Result.Failure(Error.NotFound)` | 404 ProblemDetails | toast.error |
| `Result.Failure(Error.Validation)` | 400 ProblemDetails | Form field errors |
| `Result.Failure(Error.Conflict)` | 409 ProblemDetails | toast.error |
| Unhandled exception | 500 | Global error handler |

### 3.4 Type Sync Checklist

When adding/modifying a BE endpoint:
1. ✅ Update BE DTO (Query response record)
2. ✅ Update FE type in `features/{module}/types/{module}.types.ts`
3. ✅ Update FE API function in `features/{module}/api/{module}.api.ts`
4. ✅ Update/create FE React Query hook in `features/{module}/api/use{Module}.ts`
5. ✅ Verify `dotnet build` passes
6. ✅ Verify `npx tsc --noEmit` passes

---

## 4. Authentication Flow

```
[Login]
FE → POST /api/v1/auth/login { email, password }
BE → Sets HttpOnly cookie (__Secure-ep-refresh)
BE → Returns { accessToken, user }
FE → Stores accessToken in Zustand (in-memory only)
FE → Stores user in Zustand

[Authenticated Request]
FE → Axios interceptor adds: Authorization: Bearer <token>
FE → Axios interceptor adds: X-Timezone-Offset: <offset>

[Token Refresh]
FE → 401 response intercepted
FE → POST /api/v1/auth/refresh (cookie sent automatically)
BE → Validates refresh token from cookie
BE → Returns new { accessToken }
FE → Updates Zustand → retries original request

[Logout]
FE → POST /api/v1/auth/logout
BE → Clears refresh cookie
FE → Clears Zustand state → redirect to /login
```

### Key Rules
- Access token: **Zustand in-memory only** — NEVER localStorage/sessionStorage
- Refresh token: **HttpOnly cookie** — FE cannot read/write
- `ICurrentUserService` on BE — NEVER pass UserId via Command/Query
- Middleware (`middleware.ts`) protects FE routes via `ep-role` cookie

---

## 5. Realtime (SignalR)

### 5.1 Hub Endpoints

| Hub | URL | Purpose |
|---|---|---|
| `NotificationHub` | `/hubs/notifications` | Push notifications |
| `AiLiveAvatarHub` | `/hubs/ai-live-avatar` | Real-time voice chat with Gemini Live |

### 5.2 Connection Pattern

**Backend:**
```csharp
// Hub registered in Api layer
app.MapHub<NotificationHub>("/hubs/notifications");

// Use IHubContext for callbacks outside Hub lifetime
var client = _hubContext.Clients.Client(connectionId);
await client.SendAsync("EventName", payload);
```

**Frontend:**
```typescript
// Connection via @microsoft/signalr
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${apiUrl}/hubs/xxx?access_token=${token}`)
  .withAutomaticReconnect()
  .build()

// Event handlers
connection.on('EventName', (data) => { ... })

// ALWAYS cleanup
return () => { connection.stop() }
```

### 5.3 AiLiveAvatarHub Events

| Direction | Event | Payload |
|---|---|---|
| BE → FE | `SessionReady` | (none) |
| BE → FE | `AudioChunkReceived` | `string` (base64 PCM) |
| BE → FE | `TextChunkReceived` | `string` |
| BE → FE | `InputTranscriptionReceived` | `string` |
| BE → FE | `OutputTranscriptionReceived` | `string` |
| BE → FE | `TurnComplete` | (none) |
| BE → FE | `SessionError` | `string` (error message) |
| FE → BE | `SendAudioChunk` | `string` (base64 PCM) |
| FE → BE | `SendTextMessage` | `string` |

### 5.4 Connection Query Parameters

| Param | Required | Description |
|---|---|---|
| `access_token` | Yes | JWT for authentication |
| `targetLanguage` | No | Language for system prompt (default: English) |
| `voiceName` | No | Gemini Live voice ID |
| `sessionId` | No | Existing ChatSession ID to continue (loads history) |
| `systemPrompt` | No | Custom system prompt override |

---

## 6. Session Types

The `ChatSession` entity supports two modes:

| SessionType | Created By | History |
|---|---|---|
| `Chat` | FE via `POST /api/v1/ai/chat/sessions` | Text messages |
| `LiveAudio` | Hub on SignalR connect | Transcription messages |

Both types share the same progressive summarization pipeline for managing long conversations.

---

## 7. Cross-Stack Validation

Before considering any cross-stack task complete:

```bash
# Backend
cd EnglishPartner.API
dotnet build src/Apps/EnglishPartner.Api --no-restore

# Frontend
cd EnglishPartner.Web
npx tsc --noEmit
```

Both must pass with 0 errors.
