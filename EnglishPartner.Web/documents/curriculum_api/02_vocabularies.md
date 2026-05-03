# Curriculum API — Vocabularies

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.vocabulary.write` permission

---

## 1. Lấy danh sách từ vựng (Paged)

```http
GET /api/v1/curriculum/vocabularies?languageCode=en&page=1&pageSize=20&searchTerm=apple&level=A1&topicId=guid
```

| Query Param | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `languageCode` | string | ❌ | `en` | Mã ngôn ngữ |
| `page` | int | ❌ | 1 | Trang hiện tại |
| `pageSize` | int | ❌ | 20 | Số item mỗi trang |
| `searchTerm` | string | ❌ | null | Tìm theo Word/Definition |
| `level` | string | ❌ | null | Filter theo CEFR level |
| `topicId` | guid | ❌ | null | Filter theo topic |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "word": "apple",
      "definition": "A round fruit with red or green skin",
      "partOfSpeech": "noun",
      "level": "A1"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```

> ⚠️ **Chỉ Vocabulary dùng paged response.** Các entity khác (Topic, Grammar, Exercise...) trả plain list `[]`.

---

## 2. Lấy chi tiết từ vựng

```http
GET /api/v1/curriculum/vocabularies/{id}
```

**Response `200 OK`:**
```json
{
  "id": "guid",
  "word": "apple",
  "definition": "A round fruit with red or green skin",
  "languageCode": "en",
  "pronunciation": "/ˈæp.əl/",
  "phoneticTranscription": "ap-uhl",
  "partOfSpeech": "noun",
  "imageUrl": "https://...",
  "audioUrl": "https://...",
  "level": "A1",
  "topicId": "guid",
  "synonyms": ["fruit", "pome"]
}
```

**Error:** `404` — `Vocabulary.NotFound`

---

## 3. Tạo từ vựng (Admin)

```http
POST /api/v1/admin/curriculum/vocabularies
```

**Request Body:**
```json
{
  "word": "apple",
  "definition": "A round fruit with red or green skin",
  "languageCode": "en",
  "pronunciation": "/ˈæp.əl/",
  "phoneticTranscription": "ap-uhl",
  "partOfSpeech": "noun",
  "level": "A1",
  "topicId": "guid or null",
  "synonyms": ["fruit", "pome"]
}
```

| Field | Type | Required |
|---|---|---|
| `word` | string | ✅ |
| `definition` | string | ✅ |
| `languageCode` | string | ✅ |
| `pronunciation` | string | ❌ |
| `phoneticTranscription` | string | ❌ |
| `partOfSpeech` | string | ❌ |
| `level` | string | ❌ |
| `topicId` | guid | ❌ |
| `synonyms` | string[] | ❌ |

**Response `201 Created`:** `"guid"` (vocabulary id)  
**Error:** `409` — `Vocabulary.WordAlreadyExists`

---

## 4. Cập nhật từ vựng (Admin)

```http
PUT /api/v1/admin/curriculum/vocabularies/{id}
```

**Request Body:** Giống Create + `"id": "guid"` (PHẢI match route id).

**Response `204 No Content`**  
**Error:** `404` — `Vocabulary.NotFound`

---

## 5. Xóa từ vựng (Admin)

```http
DELETE /api/v1/admin/curriculum/vocabularies/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `Vocabulary.NotFound`
