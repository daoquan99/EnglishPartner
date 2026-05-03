---
trigger: manual
---

# Curriculum — Conversation

> **Scope:** CRUD Conversation (User read + Admin write), ConversationLine + Translations  
> **User URL:** `GET /api/v1/curriculum/conversations`  
> **Admin URL:** `/api/v1/admin/curriculum/conversations`  
> **Auth:** User = Bearer. Admin Group = `"Admin"`, per-endpoint = `curriculum.conversation.write`  
> **@mention khi làm:** Conversation CRUD, conversation lines, conversation translations

---

## Entities

```
Conversation : AggregateRoot
├── Title (string)
├── Scenario (string?)        ← bối cảnh hội thoại (e.g. "At a restaurant")
├── LanguageCode (string)
├── Level (string?)
├── ImageUrl (string?)
├── TopicId (Guid?, soft FK)
├── GrammarId (Guid?, soft FK)
├── IsAiGenerated (bool)
└── Lines (List<ConversationLine>)   ← owned, ordered by Order
    ├── Speaker (string)
    ├── Text (string)
    ├── Order (int)
    ├── AudioUrl (string?)
    └── Translations (List<ConversationLineTranslation>)  ← owned
        ├── LanguageCode (string)
        └── Text (string)
```

---

## 1. GET /conversations — Danh sách (User)

```
GET /api/v1/curriculum/conversations?languageCode=en&topicId={topicId}&level={level}&page=1&pageSize=20
Auth: Bearer

Response 200: ConversationsResponse (items, totalCount, page, pageSize)
```

> Sử dụng `ICacheableQuery` (Automatic Caching). Cache key: `curriculum:conversations:{LanguageCode}:p{Page}:s{PageSize}:topic{TopicId}:lvl{Level}`.

---

## 2. GET /conversations/{id} — Chi tiết (User)

```
GET /api/v1/curriculum/conversations/{id:guid}
Auth: Bearer

Response 200: ConversationDetailResponse (kèm tất cả lines + translations)
```

**Lỗi:** `Conversation.NotFound` (404)

---

## 3. POST /conversations — Tạo mới (Admin)

```
POST /api/v1/admin/curriculum/conversations
Auth: Bearer + curriculum.conversation.write
Body: CreateConversationCommand

Response 201: { id: "guid" }
```

---

## 4. PUT /conversations/{id} — Cập nhật metadata (Admin)

```
PUT /api/v1/admin/curriculum/conversations/{id:guid}
Auth: Bearer + curriculum.conversation.write
Body: UpdateConversationCommand { id, ... }

⚠️ Route id PHẢI match body id.

Response 204 NoContent
```

**Lỗi:** `Conversation.NotFound` (404)

---

## 5. DELETE /conversations/{id} — Soft-delete (Admin)

```
DELETE /api/v1/admin/curriculum/conversations/{id:guid}
Auth: Bearer + curriculum.conversation.write

Response 204 NoContent
```

**Lỗi:** `Conversation.NotFound` (404)

---

## 6. POST /conversations/{conversationId}/lines — Thêm Line (Admin)

```
POST /api/v1/admin/curriculum/conversations/{conversationId:guid}/lines
Auth: Bearer + curriculum.conversation.write
Body:
{
  "speaker": "Alice",
  "text": "Hello!",
  "translations": { "vi": "Xin chào!", "ja": "こんにちは！" },
  "audioUrl": null
}

⚠️ ConversationId lấy từ ROUTE, không từ body (BUG-3 pattern).
⚠️ translations là Dictionary<string, string> (languageCode → translatedText), hỗ trợ nhiều ngôn ngữ.

Response 204 NoContent
```

**Lỗi:** `Conversation.NotFound` (404)

**Lỗi:** `Conversation.NotFound` (404)

## 7. PUT /conversations/{conversationId}/lines/{lineId} — Sửa Line (Admin)

```
PUT /api/v1/admin/curriculum/conversations/{conversationId:guid}/lines/{lineId:guid}
Auth: Bearer + curriculum.conversation.write
Body:
{
  "speaker": "Alice",
  "text": "Hello again!",
  "translations": { "vi": "Xin chào lần nữa!" },
  "audioUrl": null
}

⚠️ ConversationId và LineId lấy từ ROUTE.
⚠️ translations sẽ ghi đè theo từng languageCode.

Response 204 NoContent
```

**Lỗi:** `Conversation.NotFound` (404), `ConversationLine.NotFound` (404)

## 8. PUT /conversations/{conversationId}/lines/reorder — Đổi thứ tự Lines (Admin)

```
PUT /api/v1/admin/curriculum/conversations/{conversationId:guid}/lines/reorder
Auth: Bearer + curriculum.conversation.write
Body: { "orderedLineIds": ["guid1", "guid2"] }

Response 204 NoContent
```

**Lỗi:** `Conversation.NotFound` (404)

---

## 8. Commands & Queries

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateConversationCommand` | `Guid` |
| ✍ | `UpdateConversationCommand` | - |
| 🗑 | `DeleteConversationCommand` | - |
| ✍ | `AddConversationLineCommand(conversationId, speaker, text, translations?, audioUrl?)` | - |
| ✍ | `UpdateConversationLineCommand(conversationId, lineId, speaker, text, audioUrl?, translations?)` | - |
| ✍ | `ReorderConversationLinesCommand(conversationId, orderedLineIds)` | - |
| 📖 | `GetConversationsQuery(languageCode, page?, pageSize?, topicId?, level?)` | `ConversationsResponse` (ICacheableQuery) |
| 📖 | `GetConversationByIdQuery(id)` | `ConversationDetailResponse` |
