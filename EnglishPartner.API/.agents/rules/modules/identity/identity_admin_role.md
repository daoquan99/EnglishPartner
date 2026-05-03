---
trigger: manual
---

# Identity — Admin: Role & Permission Management

> **Scope:** Role CRUD, Assign/Remove permissions cho role, List all permissions  
> **Base URL:** `/api/v1/admin/identity`  
> **Auth:** Bearer (Admin) + permission `identity.roles.manage`  
> **@mention khi làm:** Admin role management, create role, delete role, assign permission, permission list

---

## Quy tắc

- **Một role có thể có NHIỀU permissions** (RolePermission là many-to-many)
- **Một user có thể có NHIỀU roles** (UserRole là many-to-many)
- Permission là **static constants** seeded từ code — không tạo/xóa qua API
- Không thể xóa role đang được gán cho user (trả `Identity.Role.HasUsers`)

---

## 1. GET /roles — Danh sách roles

```
GET /api/v1/admin/identity/roles
Auth: Bearer + identity.roles.manage

Response 200:
[
  {
    "id": "guid",
    "name": "Content Creator",
    "description": "Can write curriculum content",
    "permissions": [
      {
        "id": "guid",
        "code": "curriculum.vocabulary.write",
        "name": "Write Vocabulary",
        "module": "Curriculum",
        "description": "..."
      }
    ]
  }
]
```

---

## 2. POST /roles — Tạo role mới

```
POST /api/v1/admin/identity/roles
Auth: Bearer + identity.roles.manage
Body: { "name": "Moderator", "description": "..." }

Response 201:
{ "id": "guid", "name": "Moderator", "description": "...", "permissions": [] }
```

**Lỗi:** `Identity.Role.NameAlreadyExists` (409)

---

## 3. PUT /roles/{id} — Cập nhật role

```
PUT /api/v1/admin/identity/roles/{id:guid}
Auth: Bearer + identity.roles.manage
Body: { "name": "New Name", "description": "..." }

Response 204 NoContent
```

**Lỗi:** `Identity.Role.NotFound` (404), `Identity.Role.NameAlreadyExists` (409)

---

## 4. DELETE /roles/{id} — Xóa role

```
DELETE /api/v1/admin/identity/roles/{id:guid}
Auth: Bearer + identity.roles.manage

Response 204 NoContent
```

> ⚠️ Không thể xóa role đang được gán cho user. FE hiện `AlertDialog` xác nhận.

**Lỗi:** `Identity.Role.NotFound` (404), `Identity.Role.HasUsers` (400)

---

## 5. POST /roles/{id}/permissions — Assign permission vào role

```
POST /api/v1/admin/identity/roles/{id:guid}/permissions
Auth: Bearer + identity.roles.manage
Body: { "permissionId": "guid" }

Response 204 NoContent
```

**Lỗi:**
- `Identity.Role.NotFound` (404)
- `Identity.Permission.NotFound` (404)
- `Identity.Role.PermissionAlreadyAssigned` (400) — Permission đã gán vào role rồi

---

## 6. DELETE /roles/{id}/permissions/{permissionId} — Remove permission khỏi role

```
DELETE /api/v1/admin/identity/roles/{id:guid}/permissions/{permissionId:guid}
Auth: Bearer + identity.roles.manage

Response 204 NoContent
```

**Lỗi:**
- `Identity.Role.NotFound` (404)
- `Identity.Role.PermissionNotAssigned` (400) — Permission chưa được gán vào role

---

## 7. GET /permissions — Danh sách tất cả permissions

Chỉ đọc — permissions là static constants, seeded từ `Permissions.cs` trong Domain.

```
GET /api/v1/admin/identity/permissions
Auth: Bearer + identity.roles.manage

Response 200:
[
  { "id": "guid", "code": "curriculum.vocabulary.write", "name": "Write Vocabulary", "module": "Curriculum", "description": "..." },
  { "id": "guid", "code": "identity.users.manage",      "name": "Manage Users",     "module": "Identity",    "description": "..." },
  { "id": "guid", "code": "identity.roles.manage",      "name": "Manage Roles",     "module": "Identity",    "description": "..." }
]
```

---

## 8. Permission Codes (đầy đủ — từ `Permissions.cs`)

Permission code format: `{module}.{resource}.{action}`

| Code | Module | Mô tả |
|---|---|---|
| `identity.users.manage` | Identity | Quản lý Users (CRUD, activate/deactivate) |
| `identity.roles.manage` | Identity | Quản lý Roles & Permissions |
| `curriculum.topics.write` | Curriculum | Tạo/sửa topics |
| `curriculum.vocabulary.write` | Curriculum | Tạo/sửa vocabulary |
| `curriculum.grammar.write` | Curriculum | Tạo/sửa grammar |
| `curriculum.exercise.write` | Curriculum | Tạo/sửa exercises |
| `curriculum.conversation.write` | Curriculum | Tạo/sửa conversations |
| `curriculum.sentence.write` | Curriculum | Tạo/sửa example sentences |
| `curriculum.generate` | Curriculum | Chạy AI generation |
| `learning.admin.view` | Learning | Xem dữ liệu learning của user khác |
| `learning.admin.write` | Learning | Thao tác dữ liệu learning (XP, mastery, session) |
| `ai.logs.read` | AI | Xem lịch sử AI request logs |
| `notifications.admin` | Notifications | Quản lý thông báo (gửi, xóa, read-all) |

> Danh sách đầy đủ xem trong: `EnglishPartner.Modules.Identity.Domain/Roles/Permissions.cs`
> `Permissions.AdminPermissions[]` chứa tất cả permissions trên — được gán mặc định cho role Admin.
