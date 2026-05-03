# Identity API — Auth

> **Mục tiêu:** Quản lý quy trình đăng ký, đăng nhập, token và phiên bản của user.

## 1. Dual-Token Architecture

- **Access Token (JWT):** 
  - Lưu ở FE memory (Zustand/Redux). Tuyệt đối không lưu LocalStorage.
  - Sống 60 phút. Dùng để gọi các API bảo mật (`Authorization: Bearer <token>`).
- **Refresh Token:**
  - Nằm trong `__Secure-ep-refresh` cookie (HttpOnly, Secure). FE không thể đọc bằng JS.
  - Sống 7 ngày. Browser tự động gửi khi gọi đến `/api/v1/auth/*`.

## 2. Các Endpoints

### 2.1 Đăng ký
`POST /api/v1/auth/register`
**Headers:**
- `X-Timezone-Offset`: integer (VD: `7` cho UTC+7).
**Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "displayName": "Nguyễn Văn A"
}
```
**Response 201 Created:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "Nguyễn Văn A",
  "accessToken": "eyJ...",
  "expiresAt": "2025-04-21T10:00:00Z",
  "isOnboardingCompleted": false,
  "permissions": null
}
```
*Note: Browser sẽ tự nhận thêm cookie `__Secure-ep-refresh`.*

### 2.2 Đăng nhập
`POST /api/v1/auth/login`
**Headers:**
- `X-Timezone-Offset`: integer (VD: `7`).
**Body:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```
**Response 200 OK:** Dữ liệu giống Register.

### 2.3 Social Login
`POST /api/v1/auth/social-login`
**Body:**
```json
{
  "provider": "Google",
  "token": "<idToken từ Google SDK>"
}
```
**Response 200 OK:** Dữ liệu giống Register.

### 2.4 Silent Refresh Token
`POST /api/v1/auth/refresh`
**Yêu cầu:** FE gọi bằng `axios` với `withCredentials: true` hoặc `fetch` với `credentials: 'include'`.
**Body:** Không có.
**Response 200 OK:**
```json
{
  "accessToken": "eyJ...new_token...",
  "expiresAt": "2025-04-21T11:00:00Z"
}
```
*Note: Cookie refresh token cũ sẽ được thay bằng cookie mới (Token Rotation).*

### 2.5 Đăng xuất
`POST /api/v1/auth/logout`
**Headers:** `Authorization: Bearer <accessToken>`
**Yêu cầu:** `credentials: 'include'` (để server xóa cookie).
**Response 204 NoContent**

### 2.6 Đổi mật khẩu
`POST /api/v1/auth/change-password`
**Headers:** `Authorization: Bearer <accessToken>`
**Body:**
```json
{
  "currentPassword": "old",
  "newPassword": "new"
}
```
**Response 204 NoContent**
*Note: Đổi MK xong server sẽ hủy toàn bộ các phiên đăng nhập khác của user.*

### 2.7 Quên mật khẩu
`POST /api/v1/auth/forgot-password`
**Body:**
```json
{ "email": "user@example.com" }
```
**Response 200 OK:**
```json
{ "message": "If the email exists, a reset code has been sent." }
```
*Xem chi tiết flow tại `07_reset_password.md`.*

### 2.8 Đặt lại mật khẩu
`POST /api/v1/auth/reset-password`
**Body:**
```json
{
  "email": "user@example.com",
  "code": "123456",
  "newPassword": "MyNewStrongPassword!"
}
```
**Response 204 NoContent**
*Xem chi tiết flow tại `07_reset_password.md`.*

## 3. Quản lý lỗi (Error Codes)
- `Identity.User.InvalidCredentials` (401)
- `Identity.User.Deactivated` (401)
- `Identity.User.EmailAlreadyExists` (409)
- `Identity.Token.InvalidRefreshToken` (401)
- `Identity.Session.NotFound` (401)
- `Identity.PasswordReset.InvalidToken` (400)
- `Auth.SocialLogin.InvalidToken` (400)

