---
trigger: manual
---

# Identity — Password Reset

> **Scope:** Forgot Password, Reset Password (User self-service), Admin Reset Password
> **@mention khi làm:** forgot password, reset password, OTP, admin reset, email password

---

## 1. User Self-Service Flow (Forgot → Reset)

```
User quên mật khẩu
  ↓
POST /api/v1/auth/forgot-password { email }
  ↓
Server tìm user → tạo OTP 6 số → lưu Redis (key: password-reset:{userId}, TTL: 30 phút)
  ↓
Gửi email chứa mã OTP qua IEmailSender
  ↓
User nhập mã OTP + mật khẩu mới
  ↓
POST /api/v1/auth/reset-password { email, code, newPassword }
  ↓
Server verify OTP từ Redis → hash password mới → xóa OTP khỏi Redis
```

### Quy tắc bảo mật:
- **forgot-password luôn trả 200 OK** — dù email có tồn tại hay không → chống email enumeration.
- **OTP là one-time use** — sau khi verify thành công, xóa ngay khỏi Redis.
- **OTP TTL = 30 phút** — hết hạn thì user phải request lại.

---

## 2. Admin Reset Password Flow

```
Admin chọn user cần reset
  ↓
POST /api/v1/admin/identity/users/{id}/reset-password { sendEmail: true/false }
  ↓
Server tạo temp password (12 ký tự random) → hash → lưu vào User
  ↓
Nếu sendEmail = true:
  → Gửi email chứa temp password qua IEmailSender
  → Response: { emailSent: true }
Nếu sendEmail = false:
  → Response: { temporaryPassword: "xxx", emailSent: false }
```

### Khác biệt với User self-service:
- Admin không cần OTP — admin có quyền trực tiếp.
- Admin tạo **temp password** (12 ký tự) → user phải đổi sau khi login.
- User self-service dùng **OTP 6 số** → user tự chọn password mới.

---

## 3. Files liên quan

| Layer | File | Vai trò |
|---|---|---|
| **Command** | `Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs` | Gửi OTP email |
| **Handler** | `Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs` | Tạo OTP → Redis → email |
| **Command** | `Auth/Commands/ResetPassword/ResetPasswordCommand.cs` | Verify OTP + đổi password |
| **Handler** | `Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs` | Check Redis → hash → update |
| **Command** | `Admin/Users/Commands/ResetUserPassword/ResetUserPasswordCommand.cs` | Admin reset |
| **Handler** | `Admin/Users/Commands/ResetUserPassword/ResetUserPasswordCommandHandler.cs` | Tạo temp pass → email nếu sendEmail |
| **Endpoint** | `Endpoints/AuthEndpoints.cs` | `/forgot-password`, `/reset-password` |
| **Endpoint** | `Endpoints/AdminUserEndpoints.cs` | `/admin/.../reset-password` |
| **Error** | `IdentityErrors.PasswordReset.InvalidToken` | OTP sai hoặc hết hạn |

---

## 4. Dependencies

- `IEmailSender` (SharedKernel.Application) — gửi email
- `IDistributedCache` (Redis) — lưu OTP cho user self-service
- `IPasswordHasher` — hash password mới
- `IUserRepository` — query/update user

---

## 5. Coding Rules

1. **Forgot-password LUÔN trả success** — không bao giờ leak thông tin email tồn tại hay không.
2. **OTP chỉ dùng 1 lần** — sau verify phải `cache.RemoveAsync()`.
3. **Email nội dung dùng plain string interpolation** — theo quy tắc chung (xem `.agents/rules/common/email.md`).
4. **Admin reset không cần OTP** — temp password tạo bằng `RandomNumberGenerator.GetString()`.
5. **Không gửi email từ endpoint** — luôn gửi từ CommandHandler.
