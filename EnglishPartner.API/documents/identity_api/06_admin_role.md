# Identity API — Admin Role & Permissions

> **Quyền yêu cầu:** Người gọi API phải có vai trò Admin và có permission `identity.roles.manage`.
> **Base URL:** `/api/v1/admin/identity`

## 1. Lấy danh sách toàn bộ Roles
`GET /api/v1/admin/identity/roles`
**Response 200 OK:**
```json
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
        "module": "Curriculum"
      }
    ]
  }
]
```

## 2. Tạo Role mới
`POST /api/v1/admin/identity/roles`
**Body:**
```json
{ "name": "Moderator", "description": "..." }
```
**Response 201 Created:**
```json
{ "id": "guid", "name": "Moderator", "description": "...", "permissions": [] }
```

## 3. Sửa thông tin Role
`PUT /api/v1/admin/identity/roles/{id}`
**Body:**
```json
{ "name": "New Name", "description": "..." }
```
**Response 204 NoContent**

## 4. Xóa Role
`DELETE /api/v1/admin/identity/roles/{id}`
**Response 204 NoContent**
**Lỗi:** `Identity.Role.HasUsers` (400) - Không thể xóa nếu Role đang được gán cho ai đó.

## 5. Thêm Permission vào Role
`POST /api/v1/admin/identity/roles/{id}/permissions`
**Body:**
```json
{ "permissionId": "guid" }
```
**Response 204 NoContent**
**Lỗi:** `Identity.Role.PermissionAlreadyAssigned` (400)

## 6. Xóa Permission khỏi Role
`DELETE /api/v1/admin/identity/roles/{id}/permissions/{permissionId}`
**Response 204 NoContent**

## 7. Xem danh sách tất cả Permissions hệ thống (Read-only)
`GET /api/v1/admin/identity/permissions`
**Mô tả:** Lấy danh sách quyền hạn cố định để UI render checkbox/select.
**Response 200 OK:**
```json
[
  { "id": "guid", "code": "curriculum.vocabulary.write", "name": "Write Vocabulary", "module": "Curriculum", "description": "..." },
  { "id": "guid", "code": "identity.users.manage", "name": "Manage Users", "module": "Identity", "description": "..." }
]
```
