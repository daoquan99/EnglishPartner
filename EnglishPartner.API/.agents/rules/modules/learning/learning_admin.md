---
trigger: manual
---

# Learning — Admin

> **Scope:** Can thiệp dữ liệu người dùng  
> **Base URL:** `/api/v1/admin/learning`  
> **Auth:** Bearer + `learning.admin.write` (group-level, chỉ permission string — KHÔNG có "Admin" policy riêng)  
> **@mention khi làm:** learning admin, adjust xp, reset xp, force abandon

---

## 1. XP Management

### Adjust XP (Cộng / Trừ)
```
PUT /api/v1/admin/learning/users/{userId:guid}/xp
Auth: Bearer + learning.admin.write
Body: { "amount": 500, "reason": "Event bonus" }
Response 200
```

### Reset XP (Về 0)
```
POST /api/v1/admin/learning/users/{userId:guid}/xp/reset
Auth: Bearer + learning.admin.write
Response 204
```

---

## 2. Mastery Management

### Xóa Item Mastery cụ thể
```
DELETE /api/v1/admin/learning/users/{userId:guid}/masteries/{itemId:guid}
Auth: Bearer + learning.admin.write
Response 204
```

### Reset toàn bộ quá trình học
```
POST /api/v1/admin/learning/users/{userId:guid}/masteries/reset
Auth: Bearer + learning.admin.write
Response 204
```

---

## 3. Session Management

Ép một Study Session đang In-Progress chuyển sang trạng thái Abandoned (Dùng trong trường hợp hệ thống kẹt, backend worker xử lý).

```
POST /api/v1/admin/learning/sessions/{id:guid}/abandon
Auth: Bearer + learning.admin.write
Response 204
```
