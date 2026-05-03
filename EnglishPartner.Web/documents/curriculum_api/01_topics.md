# Curriculum API — Topics

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.admin` permission

---

## 1. Lấy danh sách topics

```http
GET /api/v1/curriculum/topics?languageCode=en&searchTerm=food
```

| Query Param | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `languageCode` | string | ✅ | — | Mã ngôn ngữ (e.g. `en`, `vi`) |
| `searchTerm` | string | ❌ | null | Tìm kiếm theo Title/Description |

**Response `200 OK`:**
```json
[
  {
    "id": "guid",
    "title": "Daily Routines",
    "description": "Learn about everyday activities",
    "level": "A1",
    "imageUrl": "https://...",
    "sortOrder": 1
  }
]
```

> ⚡ Response được cache 24 giờ. Data tự invalidate khi admin tạo/sửa/xóa.

---

## 2. Lấy chi tiết topic

```http
GET /api/v1/curriculum/topics/{id}
```

**Response `200 OK`:** Giống item trong danh sách (TopicResponse).

**Error:** `404` — `Topic.NotFound`

---

## 3. Tạo topic (Admin)

```http
POST /api/v1/admin/curriculum/topics
```

**Request Body:**
```json
{
  "title": "Daily Routines",
  "languageCode": "en",
  "description": "Learn about everyday activities",
  "level": "A1",
  "imageUrl": null,
  "sortOrder": 1
}
```

| Field | Type | Required |
|---|---|---|
| `title` | string | ✅ |
| `languageCode` | string | ✅ |
| `description` | string | ❌ |
| `level` | string | ❌ |
| `imageUrl` | string | ❌ |
| `sortOrder` | int | ✅ |

**Response `201 Created`:** `"guid"` (topic id)

---

## 4. Cập nhật topic (Admin)

```http
PUT /api/v1/admin/curriculum/topics/{id}
```

**Request Body:** Giống Create + thêm `"id": "guid"` (PHẢI match route id).

**Response `204 No Content`**  
**Error:** `404` — `Topic.NotFound`

---

## 5. Xóa topic (Admin)

```http
DELETE /api/v1/admin/curriculum/topics/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `Topic.NotFound`
