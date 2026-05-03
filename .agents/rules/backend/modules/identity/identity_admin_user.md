---
trigger: manual
---

# Identity — Admin: User Management

> **Scope:** List users, Activate/Deactivate, Delete, Reset Password, Assign/Remove Roles  
> **Base URL:** `/api/v1/admin/identity`  
> **Auth:** Bearer (Admin) + permission `identity.users.manage`  
> **@mention khi làm:** Admin user list, activate, deactivate, delete user, reset password, user role assignment

---

## Quy tắc

- **Một user có thể có NHIỀU roles** (UserRole là many-to-many)
- Khi assign/remove role → gọi 1 API call per role
- Không thể xóa user đang online (session còn trong Redis) — server revoke session trước, sau đó xóa

---

## 1. GET /users — Danh sách users

```
GET /api/v1/admin/identity/users
Auth: Bearer + identity.users.manage

Query params (tất cả optional):
  ?search=john          ← tìm theo email hoặc displayName
  &role=Admin           ← filter theo role name
  &isActive=true        ← filter theo trạng thái
  &page=1               ← mặc định 1
  &pageSize=20          ← mặc định 20

Response 200:
{
  "items": [
    {
      "id": "guid",
      "email": "user@example.com",
      "displayName": "Nguyễn Văn A",
      "isActive": true,
      "createdAt": "2025-04-01T08:00:00Z",
      "roles": ["Learner", "Content Creator"]   ← nhiều roles
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

---

## 2. POST /users/{id}/activate

```
POST /api/v1/admin/identity/users/{id:guid}/activate
Auth: Bearer + identity.users.manage
(no body)

Response 204 NoContent
```

**Lỗi:** `Identity.User.NotFound` (404)

---

## 3. POST /users/{id}/deactivate

```
POST /api/v1/admin/identity/users/{id:guid}/deactivate
Auth: Bearer + identity.users.manage
(no body)

Response 204 NoContent
```

**Lỗi:** `Identity.User.NotFound` (404)

---

## 4. DELETE /users/{id}

Xóa vĩnh viễn tài khoản user.

```
DELETE /api/v1/admin/identity/users/{id:guid}
Auth: Bearer + identity.users.manage

Response 204 NoContent
```

> ⚠️ Hành động không thể hoàn tác. FE phải hiện `AlertDialog` xác nhận trước khi gọi.

**Lỗi:** `Identity.User.NotFound` (404)

---

## 5. POST /users/{id}/reset-password

Reset mật khẩu user về mật khẩu ngẫu nhiên.

```
POST /api/v1/admin/identity/users/{id:guid}/reset-password
Auth: Bearer + identity.users.manage
Body: { "sendEmail": true | false }

sendEmail: true  → Server gửi mật khẩu mới qua email cho user
sendEmail: false → Server trả mật khẩu mới trong response (chỉ Admin thấy)

Response 200 (sendEmail: false):
{ "newPassword": "Xk9#mP2vQr" }

Response 200 (sendEmail: true):
{}
```

**Lỗi:** `Identity.User.NotFound` (404)

---

## 6. POST /users/{id}/roles — Assign hoặc Remove role

```
POST /api/v1/admin/identity/users/{id:guid}/roles
Auth: Bearer + identity.roles.manage   ← (permission khác với users.manage)
Body: { "roleId": "guid", "operation": "Assign" | "Revoke" }

Response 204 NoContent
```

**Lỗi:**
- `Identity.User.NotFound` (404)
- `Identity.Role.NotFound` (404)
- `Identity.User.RoleAlreadyAssigned` (400) — Assign khi đã có role này rồi

> **Note:** Operation là `"Assign"` hoặc `"Revoke"` (không phải `"Remove"`).
