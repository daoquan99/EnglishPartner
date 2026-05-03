---
trigger: manual
---

# Identity — Auth

> **Scope:** Register, Login, Social Login, Refresh Token, Logout, Change Password, JWT & Session Architecture  
> **@mention khi làm:** Login, Register, Token, Session, Change Password, Social Login, JWT

---

## 1. Dual-Token Model

```
ACCESS TOKEN (JWT)
  • Lifetime: configurable (JwtSettings.AccessTokenExpirationMinutes, default 60 phút)
  • Claims: sub (userId), email, role[], permission[], sid (sessionId)
  • Lưu trong: FE memory (Zustand) — KHÔNG localStorage/sessionStorage
  • Gửi qua: Authorization: Bearer {token}

REFRESH TOKEN
  • Lifetime: configurable (JwtSettings.RefreshTokenExpirationDays, default 7 ngày)
  • Lưu trong: DB (identity.refresh_tokens) + HttpOnly cookie
  • Cookie: __Secure-ep-refresh, Path=/api/v1/auth, HttpOnly, Secure, SameSite=Lax
  • SameSite=Lax (KHÔNG PHẢI Strict) — cần thiết cho Google OAuth redirect callback
  • FE KHÔNG ĐỌC được (HttpOnly)
```

---

## 2. Redis Session

```
Key pattern: EnglishPartner:session:{sessionId}
TTL: configurable (= RefreshTokenExpirationDays, default 7 ngày) — reset mỗi lần /refresh (sliding window)
Value: { userId, createdAt }

SessionValidationMiddleware: chạy trước UseAuthorization
  → verify JWT sid claim còn trong Redis → nếu không → 401 immediately
```

> **Session TTL = Refresh Token TTL** (cùng config `RefreshTokenExpirationDays`). Không dùng AccessToken lifetime.

---

## 3. Auth Endpoint Pattern (Ngoại lệ Rule #9)

> ⚠️ Auth endpoints (`/api/v1/auth/*`) là **NGOẠI LỆ** của Rule #9 ("group-level auth").
> Auth group KHÔNG có `.RequireAuthorization()` ở group level vì phải mix Anonymous + Authenticated routes:
> - `/register`, `/login`, `/social-login`, `/refresh` → `.AllowAnonymous()`
> - `/logout`, `/change-password` → `.RequireAuthorization()` (per-endpoint)
>
> Logout và ChangePassword dùng `ClaimsPrincipal` trực tiếp ở endpoint layer (KHÔNG dùng `ICurrentUserService`) và truyền `userId` qua Command record. Đây là **design trade-off có chủ đích** vì cần validate ownership TRƯỚC KHI vào handler.

---

## 4. Header bắt buộc

### `X-Timezone-Offset`
Bắt buộc với `POST /register` và `POST /login`.

```
Kiểu: integer (số giờ lệch UTC, từ -12 đến 14)
Mục đích: Server tính "ngày hôm nay" theo múi giờ user cho streak
Ví dụ: Hà Nội = 7, Tokyo = 9, London = 0
```

---

## 5. POST /api/v1/auth/register

```
Headers: X-Timezone-Offset: 7
Body: { email, password, displayName }

→ Validate email unique
→ User.Create() → RaiseDomainEvent(UserRegisteredDomainEvent)
→ CreateSession (Redis, TTL=7 days)
→ GenerateAccessToken (JWT, 60 min)
→ RefreshToken.Create() → store in DB
→ Set __Secure-ep-refresh cookie (HttpOnly, 7 days)

Response 201:
{
  "userId": "guid",
  "email": "...",
  "displayName": "...",
  "accessToken": "eyJ...",
  "expiresAt": "2025-04-21T10:00:00Z",
  "isOnboardingCompleted": false,
  "permissions": null
}
```

**Lỗi:** `Identity.User.EmailAlreadyExists` (409)

---

## 6. POST /api/v1/auth/login

```
Headers: X-Timezone-Offset: 7
Body: { email, password }

→ Find user by email
→ Check IsActive → 401 Deactivated if false
→ passwordHasher.Verify() → 401 InvalidCredentials if fail
→ GetRolesAndPermissionsAsync(roleIds)
→ CreateSession (Redis, TTL=7 days)
→ GenerateAccessToken (includes: sub, email, role[], permission[], sid)
→ RefreshToken.Create() → store in DB
→ Set __Secure-ep-refresh cookie

Response 200: (giống register)
```

> `permissions` là `null` cho Learner, `string[]` cho Admin.  
> FE detect Admin: `permissions?.length > 0`

**Lỗi:** `Identity.User.InvalidCredentials` (401), `Identity.User.Deactivated` (401)

---

## 7. POST /api/v1/auth/social-login

```
Body: { "provider": "Google", "token": "<idToken from Google OAuth>" }

→ Validate provider token via Google SDK
→ Upsert User (tạo nếu chưa có, skip password)
→ Same session/token flow as Login
→ Return same LoginResponse shape
```

**Lỗi:** `Auth.SocialLogin.InvalidToken` (400)

---

## 8. POST /api/v1/auth/refresh

```
(no body — cookie __Secure-ep-refresh gửi tự động)
credentials: 'include' BẮT BUỘC

→ Read __Secure-ep-refresh cookie
→ Find RefreshToken by value in DB
→ Validate: not revoked, not expired
→ Validate: Redis session (sid claim) còn tồn tại
→ refreshToken.Rotate(newToken, newExpiry)  ← UPDATE in-place, KHÔNG INSERT row mới
→ Update Redis session TTL = 7 days
→ GenerateAccessToken (new, 60 min)
→ Set new __Secure-ep-refresh cookie (old one replaced)

Response 200:
{ "accessToken": "eyJ...", "expiresAt": "2025-04-21T11:00:00Z" }
```

> **Token rotation:** nếu refresh token bị leak → dùng lần 2 sẽ thấy đã rotate → invalid → server revoke toàn bộ session.

**Lỗi:** `Identity.Token.InvalidRefreshToken` (401), `Identity.Session.NotFound` (401), `Identity.User.Deactivated` (401)

---

## 9. POST /api/v1/auth/logout

```
Auth: Bearer <accessToken>
(no body, credentials: 'include')

→ Delete Redis session (sessionId from JWT sid claim)
→ Mark RefreshToken as revoked in DB
→ Clear __Secure-ep-refresh cookie (Max-Age=0)

Response 204 NoContent
```

---

## 10. POST /api/v1/auth/change-password

```
Auth: Bearer <accessToken>
Body: { "currentPassword": "...", "newPassword": "..." }

→ Verify current password
→ Hash new password, update User
→ RevokeAllByUserIdAsync() — revoke ALL refresh tokens for user in DB
→ DeleteUserSessionAsync(sessionId) — xóa Redis session hiện tại
→ Clear __Secure-ep-refresh cookie

Response 204 NoContent
```

> **Security:** Sau khi đổi mật khẩu, TẤT CẢ sessions bị hủy. FE phải redirect /login.

**Lỗi:** `Identity.User.InvalidCredentials` (401)

---

## 11. Silent Refresh Flow (Frontend)

```
T=0:   Login → Access Token (60 min) | Session TTL = 7 days
T=55m: Silent refresh → new Access Token | Session TTL reset = 7 days
T=115m: Silent refresh again → …
```

**App startup** (reload trang):
```
accessToken bị mất (biến JS bị xóa)
→ Gọi POST /api/v1/auth/refresh
  Thành công → khôi phục session
  Thất bại → redirect /login
```

**On 401:**
```
→ Thử gọi /refresh 1 lần → retry request gốc
→ Nếu /refresh cũng fail → logout + redirect /login
```
