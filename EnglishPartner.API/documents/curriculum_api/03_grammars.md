# Curriculum API — Grammars

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.admin` permission

---

## 1. Lấy danh sách ngữ pháp

```http
GET /api/v1/curriculum/grammars?languageCode=en
```

| Query Param | Type | Required |
|---|---|---|
| `languageCode` | string | ✅ |

**Response `200 OK`:**
```json
[
  {
    "id": "guid",
    "title": "Present Simple",
    "level": "A1",
    "topicId": "guid"
  }
]
```

> ⚡ Cache 12 giờ.

---

## 2. Lấy chi tiết ngữ pháp

```http
GET /api/v1/curriculum/grammars/{id}
```

**Response `200 OK`:**
```json
{
  "id": "guid",
  "title": "Present Simple",
  "languageCode": "en",
  "level": "A1",
  "explanation": "Dùng để diễn tả thói quen, sự thật hiển nhiên...",
  "formula": "S + V(s/es) + O",
  "topicId": "guid",
  "examples": [
    {
      "id": "guid",
      "sentence": "She goes to school every day.",
      "note": "Third person singular adds -es",
      "sortOrder": 1,
      "translations": [
        { "languageCode": "vi", "translatedText": "Cô ấy đi học mỗi ngày." }
      ]
    }
  ]
}
```

**Error:** `404` — `Grammar.NotFound`

---

## 3. Tìm kiếm gợi ý ngữ pháp (Lookup)

```http
GET /api/v1/admin/curriculum/grammars/lookup?languageCode=en&searchTerm=ab&page=1&pageSize=20
```

| Query Param | Type | Required | Default |
|---|---|---|---|
| `languageCode` | string | ✅ | |
| `searchTerm` | string | ❌ | null |
| `page` | int | ❌ | 1 |
| `pageSize` | int | ❌ | 20 |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "label": "Present Simple"
    }
  ],
  "totalCount": 45
}
```

---

## 4. Tạo ngữ pháp (Admin)

```http
POST /api/v1/admin/curriculum/grammars
```

**Request Body:**
```json
{
  "title": "Present Simple",
  "languageCode": "en",
  "level": "A1",
  "explanation": "Dùng để diễn tả thói quen...",
  "formula": "S + V(s/es) + O",
  "topicId": "guid or null"
}
```

| Field | Type | Required |
|---|---|---|
| `title` | string | ✅ |
| `languageCode` | string | ✅ |
| `level` | string | ❌ |
| `explanation` | string | ❌ |
| `formula` | string | ❌ |
| `topicId` | guid | ❌ |

**Response `201 Created`:** `"guid"` (grammar id)

---

## 5. Cập nhật ngữ pháp (Admin)

```http
PUT /api/v1/admin/curriculum/grammars/{id}
```

**Request Body:** Giống Create + `"id": "guid"` (PHẢI match route id).

**Response `204 No Content`**  
**Error:** `404` — `Grammar.NotFound`

---

## 6. Xóa ngữ pháp (Admin)

```http
DELETE /api/v1/admin/curriculum/grammars/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `Grammar.NotFound`
