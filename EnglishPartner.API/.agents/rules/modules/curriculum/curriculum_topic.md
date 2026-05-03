---
trigger: manual
---

# Curriculum — Topic

> **Scope:** CRUD Topic (User read + Admin write), seeded Languages  
> **User URL:** `GET /api/v1/curriculum/topics`  
> **Admin URL:** `/api/v1/admin/curriculum/topics`  
> **Auth:** User = Bearer. Admin Group = `"Admin"`, per-endpoint = `curriculum.topics.write`  
> **@mention khi làm:** Topic CRUD, topic search, language list

---

## Entity

```
Topic : AggregateRoot
├── Title (string)
├── Description (string?)
├── LanguageCode (string)
├── Level (string?, e.g. "A1", "B2")
├── SortOrder (int)
├── ImageUrl (string?)
└── [inherited] IsDeleted, IsActive, CreatedAt...
```

---

## 1. GET /topics — Danh sách (User)

```
GET /api/v1/curriculum/topics?languageCode=en&searchTerm=food
Auth: Bearer

Response 200: List<TopicResponse>
```

> Sử dụng `ICacheableQuery` (Automatic Caching). Cache key: `curriculum:topics:{languageCode}`. TTL: 60 phút.
> SearchTerm là optional — dùng keyword search trên Title/Description.

---

## 2. GET /topics/{id} — Chi tiết (User)

```
GET /api/v1/curriculum/topics/{id:guid}
Auth: Bearer

Response 200: TopicDetailResponse (kèm danh sách vocabulary, conversation thuộc topic)
```

**Lỗi:** `Topic.NotFound` (404)

---

## 3. POST /topics — Tạo mới (Admin)

```
POST /api/v1/admin/curriculum/topics
Auth: Bearer + curriculum.topics.write
Body: CreateTopicCommand

Response 201: { id: "guid" }
```

---

## 4. PUT /topics/{id} — Cập nhật (Admin)

```
PUT /api/v1/admin/curriculum/topics/{id:guid}
Auth: Bearer + curriculum.topics.write
Body: UpdateTopicCommand { id, ... }

⚠️ Route id PHẢI match body id.

Response 204 NoContent
```

**Lỗi:** `Topic.NotFound` (404)

---

## 5. DELETE /topics/{id} — Soft-delete (Admin)

```
DELETE /api/v1/admin/curriculum/topics/{id:guid}
Auth: Bearer + curriculum.topics.write

Response 204 NoContent
```

**Lỗi:** `Topic.NotFound` (404)

---

## 6. Commands & Queries

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateTopicCommand` | `Guid` |
| ✍ | `UpdateTopicCommand` | - |
| 🗑 | `DeleteTopicCommand` | - |
| 📖 | `GetTopicsQuery(languageCode, searchTerm?)` | `List<TopicResponse>` (ICacheableQuery) |
| 📖 | `GetTopicByIdQuery(id)` | `TopicDetailResponse` |
