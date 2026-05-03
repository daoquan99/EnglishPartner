# Identity API — Password Reset

> **Mục tiêu:** Cho phép user tự khôi phục mật khẩu qua email, và admin reset mật khẩu cho user.

---

## 1. User: Quên mật khẩu (Forgot Password)

`POST /api/v1/auth/forgot-password`
**Body:**
```json
{
  "email": "user@example.com"
}
```
**Response 200 OK:**
```json
{
  "message": "If the email exists, a reset code has been sent."
}
```
> ⚠️ **Luôn trả 200** dù email có tồn tại hay không — để chống email enumeration.

**Flow phía server:**
1. Tìm user theo email
2. Tạo mã OTP 6 số (`RandomNumberGenerator.GetInt32`)
3. Lưu vào Redis: key `password-reset:{userId}`, TTL 30 phút
4. Gửi email chứa mã OTP cho user

---

## 2. User: Đặt lại mật khẩu (Reset Password)

`POST /api/v1/auth/reset-password`
**Body:**
```json
{
  "email": "user@example.com",
  "code": "123456",
  "newPassword": "MyNewStrongPassword!"
}
```
**Response 204 NoContent** — thành công, password đã được đổi.

**Lỗi:**
- `Identity.PasswordReset.InvalidToken` (400) — mã OTP sai hoặc đã hết hạn.
- `Identity.User.NotFound` (404) — email không tồn tại.

**Flow phía server:**
1. Tìm user theo email
2. Lấy OTP từ Redis (`password-reset:{userId}`)
3. So sánh OTP
4. Hash password mới → `user.ChangePassword(hash)`
5. Xóa OTP khỏi Redis (one-time use)

---

## 3. Admin: Reset mật khẩu cho User

`POST /api/v1/admin/identity/users/{id}/reset-password`
**Quyền:** `identity.users.manage`
**Body:**
```json
{
  "sendEmail": true
}
```

### Khi `sendEmail = true`:
Server tạo mật khẩu tạm 12 ký tự → gửi email cho user.
**Response 200 OK:**
```json
{
  "temporaryPassword": null,
  "emailSent": true
}
```

### Khi `sendEmail = false`:
Server tạo mật khẩu tạm → trả về cho admin tự gửi.
**Response 200 OK:**
```json
{
  "temporaryPassword": "Xk9#mP2vQr4h",
  "emailSent": false
}
```

> Admin reset **không cần OTP** — admin có quyền trực tiếp. User sẽ nhận email thông báo khi `sendEmail = true`.

---

## 4. Nội dung Email

### Email OTP (User forgot-password):
```
Subject: EnglishPartner — Password Reset Code

Hi [DisplayName],
Your password reset code is: [CODE]
This code expires in 30 minutes.
If you didn't request this, please ignore this email.
```

### Email Admin Reset:
```
Subject: EnglishPartner — Your Password Has Been Reset

Hi [DisplayName],
Your password has been reset by an administrator.
Your temporary password is: [TEMP_PASSWORD]
Please login and change your password immediately.
```

---

## 5. Tổng hợp Error Codes

| Code | HTTP | Mô tả |
|---|---|---|
| `Identity.PasswordReset.InvalidToken` | 400 | Mã OTP sai hoặc hết hạn |
| `Identity.User.NotFound` | 404 | Email/UserId không tồn tại |
