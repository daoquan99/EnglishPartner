# Learning API — Admin

> **Auth:** Bearer + `learning.admin.write` permission  
> **Base URL:** `/api/v1/admin/learning`

---

## 1. Adjust XP (Cộng / Trừ)

```http
PUT /api/v1/admin/learning/users/{userId}/xp
```

```json
{
  "amount": 500,
  "reason": "Event bonus"
}
```

**Response `200 OK`**

---

## 2. Reset XP (Về 0)

```http
POST /api/v1/admin/learning/users/{userId}/xp/reset
```

**Response `204 No Content`**

---

## 3. Xóa Item Mastery cụ thể

```http
DELETE /api/v1/admin/learning/users/{userId}/masteries/{itemId}
```

**Response `204 No Content`**

---

## 4. Reset toàn bộ quá trình học

```http
POST /api/v1/admin/learning/users/{userId}/masteries/reset
```

**Response `204 No Content`**

---

## 5. Force Abandon Session

```http
POST /api/v1/admin/learning/sessions/{id}/abandon
```

> Ép session đang InProgress chuyển sang Abandoned (dùng khi hệ thống kẹt).

**Response `204 No Content`**
