# Identity API — Admin User Management

> **Quyền yêu cầu:** Người gọi API phải có vai trò Admin và có permission `identity.users.manage` (riêng quản lý role cho user cần `identity.roles.manage`).
> **Base URL:** `/api/v1/admin/identity`

## 1. Lấy danh sách Users (Phân trang)
`GET /api/v1/admin/identity/users`
**Query Params:**
- `search` (string): Tìm email hoặc displayName
- `role` (string): Lọc theo tên Role
- `isActive` (boolean): Lọc user đang bị khóa hay không
- `page` (int, default: 1)
- `pageSize` (int, default: 20)
**Response 200 OK:**
```json
{
  "items": [
    {
      "id": "guid",
      "email": "user@example.com",
      "displayName": "Nguyễn Văn A",
      "isActive": true,
      "createdAt": "2025-04-01T08:00:00Z",
      "roles": [
        {
          "id": "guid",
          "name": "Learner"
        },
        {
          "id": "guid",
          "name": "Content Creator"
        }
      ]
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

## 2. Kích hoạt tài khoản
`POST /api/v1/admin/identity/users/{id}/activate`
**Response 204 NoContent**

## 3. Khóa tài khoản
`POST /api/v1/admin/identity/users/{id}/deactivate`
**Response 204 NoContent**

## 4. Xóa vĩnh viễn user
`DELETE /api/v1/admin/identity/users/{id}`
**Response 204 NoContent**
*(Cảnh báo: Hành động này xóa cả session, không thể khôi phục)*

## 5. Reset mật khẩu User
`POST /api/v1/admin/identity/users/{id}/reset-password`
**Body:**
```json
{
  "sendEmail": false 
}
```
*Note: `sendEmail: true` server sẽ tự gửi email cho user. `false` server sẽ trả mật khẩu.*
**Response 200 OK (nếu sendEmail=false):**
```json
{ "newPassword": "Xk9#mP2vQr" }
```

## 6. Gán / Hủy Role của User
`POST /api/v1/admin/identity/users/{id}/roles`
**Mô tả:** Permission yêu cầu `identity.roles.manage`. User có thể có nhiều roles, gọi API cho từng role.
**Body:**
```json
{
  "roleId": "guid",
  "operation": "Assign" // Hoặc "Revoke"
}
```
**Response 204 NoContent**
**Lỗi:** `Identity.User.RoleAlreadyAssigned` (400)
