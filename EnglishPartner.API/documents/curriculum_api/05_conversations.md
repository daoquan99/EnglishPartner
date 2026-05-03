# Curriculum API — Conversations

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.admin` permission

---

## 1. Lấy danh sách hội thoại

```http
GET /api/v1/curriculum/conversations?languageCode=en&topicId={topicId}&level={level}&page=1&pageSize=20
```

| Query Param | Type | Required | Mô tả |
|---|---|---|---|
| `languageCode` | string | ✅ | Mã ngôn ngữ |
| `page` | int | ❌ | Trang hiện tại (default: 1) |
| `pageSize` | int | ❌ | Kích thước trang (default: 20) |
| `topicId` | guid | ❌ | Lọc theo ID chủ đề |
| `level` | string | ❌ | Lọc theo cấp độ CEFR |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "title": "At the Restaurant",
      "scenario": "Ordering food at a restaurant",
      "level": "A2",
      "imageUrl": "https://..."
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

> ⚡ Cache 24 giờ.

---

## 2. Lấy chi tiết hội thoại (kèm Lines)

```http
GET /api/v1/curriculum/conversations/{id}
```

**Response `200 OK`:**
```json
{
  "id": "guid",
  "title": "At the Restaurant",
  "languageCode": "en",
  "scenario": "Ordering food at a restaurant",
  "level": "A2",
  "imageUrl": "https://...",
  "topicId": "guid",
  "grammarId": null,
  "lines": [
    {
      "id": "guid",
      "speaker": "Waiter",
      "text": "Good evening! Table for two?",
      "audioUrl": null,
      "sortOrder": 1,
      "translations": [
        { "languageCode": "vi", "translatedText": "Chào buổi tối! Bàn cho hai người ạ?" }
      ]
    },
    {
      "id": "guid",
      "speaker": "Customer",
      "text": "Yes, please.",
      "audioUrl": null,
      "sortOrder": 2,
      "translations": [
        { "languageCode": "vi", "translatedText": "Vâng, làm ơn." }
      ]
    }
  ]
}
```

> 💡 `lines` được sort theo `sortOrder` ASC. FE nên render tuần tự.  
> 💡 `audioUrl` có thể null — FE dùng TTS API (`POST /api/v1/ai/tts`) để generate on-the-fly nếu cần.

**Error:** `404` — `Conversation.NotFound`

---

## 3. Tạo hội thoại (Admin)

```http
POST /api/v1/admin/curriculum/conversations
```

**Request Body:**
```json
{
  "title": "At the Restaurant",
  "languageCode": "en",
  "scenario": "Ordering food at a restaurant",
  "level": "A2",
  "imageUrl": null,
  "topicId": "guid or null",
  "grammarId": null
}
```

| Field | Type | Required |
|---|---|---|
| `title` | string | ✅ |
| `languageCode` | string | ✅ |
| `scenario` | string | ❌ |
| `level` | string | ❌ |
| `imageUrl` | string | ❌ |
| `topicId` | guid | ❌ |
| `grammarId` | guid | ❌ |

**Response `201 Created`:** `"guid"` (conversation id)

---

## 4. Thêm Line cho hội thoại (Admin)

```http
POST /api/v1/admin/curriculum/conversations/{conversationId}/lines
```

**Request Body:**
```json
{
  "speaker": "Waiter",
  "text": "Good evening!",
  "translations": {
    "vi": "Chào buổi tối!",
    "ja": "こんばんは！"
  },
  "audioUrl": null
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `speaker` | string | ✅ | Tên người nói |
| `text` | string | ✅ | Nội dung câu nói |
| `translations` | object | ❌ | Dictionary `languageCode → translatedText`, hỗ trợ nhiều ngôn ngữ |
| `audioUrl` | string | ❌ | URL file audio |

> `conversationId` lấy từ URL, KHÔNG gửi trong body.  
> `sortOrder` tự động tăng theo thứ tự thêm.

**Response `204 No Content`**  
**Error:** `404` — `Conversation.NotFound`

---

## 5. Cập nhật hội thoại (Admin)

```http
PUT /api/v1/admin/curriculum/conversations/{id}
```

**Request Body:** Giống Create + `"id": "guid"`.

**Response `204 No Content`**  
**Error:** `404` — `Conversation.NotFound`

---

## 6. Xóa hội thoại (Admin)

```http
DELETE /api/v1/admin/curriculum/conversations/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `Conversation.NotFound`

---

## 7. Cập nhật Line trong hội thoại (Admin)

```http
PUT /api/v1/admin/curriculum/conversations/{conversationId}/lines/{lineId}
```

**Request Body:**
```json
{
  "speaker": "Waiter",
  "text": "Good evening!",
  "translations": {
    "vi": "Chào buổi tối!"
  },
  "audioUrl": null
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `speaker` | string | ✅ | Tên người nói |
| `text` | string | ✅ | Nội dung câu nói |
| `translations` | object | ❌ | Dictionary `languageCode → translatedText`, hỗ trợ nhiều ngôn ngữ |
| `audioUrl` | string | ❌ | URL file audio |

**Response `204 No Content`**  
**Error:** `404` — `Conversation.NotFound`, `ConversationLine.NotFound`

---

## 8. Đổi thứ tự Conversation Lines (Admin)

```http
PUT /api/v1/admin/curriculum/conversations/{conversationId}/lines/reorder
```

**Request Body:**
```json
{
  "orderedLineIds": [
    "c8a00350-f827-4c7b-b5d1-67852c4cf2c0",
    "9c75a40b-74b6-4b2a-89a5-5e6df3d1e18d"
  ]
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `orderedLineIds` | array of guid | ✅ | Mảng danh sách các ID của line theo thứ tự mong muốn |

**Response `204 No Content`**  
**Error:** `404` — `Conversation.NotFound`
