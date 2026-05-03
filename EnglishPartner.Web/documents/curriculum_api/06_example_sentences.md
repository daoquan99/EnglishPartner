# Curriculum API — Example Sentences

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.admin` permission

---

## 1. Lấy danh sách câu ví dụ

```http
GET /api/v1/curriculum/example-sentences?languageCode=en&page=1&pageSize=20&searchTerm=hello&vocabularyId=guid&grammarId=guid&topicId=guid
```

| Query Param | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `languageCode` | string | ❌ | `en` | |
| `page` | int | ❌ | `1` | |
| `pageSize` | int | ❌ | `20` | |
| `searchTerm` | string | ❌ | `null` | Tìm theo nội dung câu (`Text`) |
| `vocabularyId` | guid | ❌ | `null` | Filter theo từ vựng |
| `grammarId` | guid | ❌ | `null` | Filter theo ngữ pháp |
| `topicId` | guid | ❌ | `null` | Filter theo topic |

> 💡 Có thể lọc kết hợp nhiều điều kiện (AND condition).

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "text": "She goes to school every day.",
      "audioUrl": null,
      "note": "Present Simple with third person",
      "level": "A1",
      "translations": [
        { "languageCode": "vi", "translatedText": "Cô ấy đi học mỗi ngày." }
      ]
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

> ℹ️ Không có endpoint GetById riêng. Câu ví dụ được hiển thị kèm trong Vocabulary/Grammar detail page.

---

## 2. Tạo câu ví dụ (Admin)

```http
POST /api/v1/admin/curriculum/example-sentences
```

**Request Body:**
```json
{
  "text": "She goes to school every day.",
  "languageCode": "en",
  "translations": {
    "vi": "Cô ấy đi học mỗi ngày.",
    "ja": "彼女は毎日学校に行きます。"
  },
  "audioUrl": null,
  "note": "Present Simple with third person",
  "level": "A1",
  "vocabularyId": "guid or null",
  "grammarId": "guid or null",
  "topicId": "guid or null"
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `text` | string | ✅ | Nội dung câu |
| `languageCode` | string | ✅ | Mã ngôn ngữ gốc |
| `translations` | object | ❌ | Dictionary `languageCode → translatedText`, hỗ trợ nhiều ngôn ngữ |
| `audioUrl` | string | ❌ | URL file audio |
| `note` | string | ❌ | Ghi chú |
| `level` | string | ❌ | Trình độ (A1, A2, B1...) |
| `vocabularyId` | guid | ❌ | Liên kết từ vựng |
| `grammarId` | guid | ❌ | Liên kết ngữ pháp |
| `topicId` | guid | ❌ | Liên kết chủ đề |

**Response `201 Created`:** `"guid"` (example sentence id)

---

## 3. Cập nhật câu ví dụ (Admin)

```http
PUT /api/v1/admin/curriculum/example-sentences/{id}
```

**Request Body:** Giống Create + `"id": "guid"`. Không cần `languageCode`.

> ⚠️ `translations` sẽ ghi đè bản dịch của từng ngôn ngữ đã có (upsert theo `languageCode`).

**Response `204 No Content`**  
**Error:** `404` — `ExampleSentence.NotFound`

---

## 4. Xóa câu ví dụ (Admin)

```http
DELETE /api/v1/admin/curriculum/example-sentences/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `ExampleSentence.NotFound`
