---
trigger: manual
---

# Curriculum — Vocabulary

> **Scope:** CRUD Vocabulary (User read + Admin write), Pagination, Caching, Search  
> **User URL:** `GET /api/v1/curriculum/vocabularies`  
> **Admin URL:** `/api/v1/admin/curriculum/vocabularies`  
> **Auth:** User = Bearer (đăng nhập). Admin = `curriculum.vocabulary.write` ở group level  
> **@mention khi làm:** Vocabulary CRUD, vocabulary search, synonym, vocabulary caching

---

## Entity

```
Vocabulary : AggregateRoot
├── Word (string, max 200)
├── Definition (string)
├── LanguageCode (string, max 10)
├── Pronunciation (string?)
├── PhoneticTranscription (string?)
├── PartOfSpeech (string?, e.g. "noun", "verb")
├── ImageUrl (string?)
├── AudioUrl (string?)
├── Level (string?, e.g. "A1", "B2")
├── TopicId (Guid?, soft FK — no EF navigation)
├── Synonyms (List<string> → PostgreSQL text[])
├── IsAiGenerated (bool)
└── [inherited] IsDeleted, IsActive, CreatedAt, CreatedBy...
```

---

## 1. GET /vocabularies — Danh sách (User, có phân trang)

```
GET /api/v1/curriculum/vocabularies
Auth: Bearer (đăng nhập)
Query params (bắt buộc languageCode):
  ?languageCode=en
  &page=1                ← bắt buộc
  &pageSize=20           ← bắt buộc
  &searchTerm=hello      ← optional, ILike trên Word + Definition
  &level=A1              ← optional, exact match
  &topicId=guid          ← optional, exact match

Response 200: VocabulariesResponse (paginated)
```

**Caching:**
```csharp
// Cache key format:
$"curriculum:vocabularies:{languageCode}:p{page}:s{pageSize}:lvl{level ?? "all"}:topic{topicId ?? "all"}"

// TTL: 30 phút
// Skip cache nếu có SearchTerm (quá dynamic)
// Invalidate: RemoveByPrefixAsync($"curriculum:vocabularies:{languageCode}:")
//   → gọi trong Create, Update, Delete, GenerateAndSaveVocabulary handlers
```

---

## 2. GET /vocabularies/{id} — Chi tiết (User)

```
GET /api/v1/curriculum/vocabularies/{id:guid}
Auth: Bearer (đăng nhập)

Response 200: VocabularyDetailResponse (kèm example sentences)
```

**Lỗi:** `Vocabulary.NotFound` (404)

---

## 3. POST /vocabularies — Tạo mới (Admin)

```
POST /api/v1/admin/curriculum/vocabularies
Auth: Bearer + curriculum.vocabulary.write

Body: CreateVocabularyCommand (bind trực tiếp từ body)
{
  "word": "hello",
  "definition": "...",
  "languageCode": "en",
  "level": "A1",
  "topicId": "guid | null",
  "synonyms": ["hi", "hey"]
}

Response 201: { id: "guid" }
```

**Lỗi:** `Vocabulary.WordAlreadyExists` (409)

### 5. PUT /vocabularies/{id} (Admin)
```
PUT /api/v1/admin/curriculum/vocabularies/{id:guid}
Auth: Bearer + curriculum.vocabulary.write
Body: UpdateVocabularyCommand { id, ... }
Response 204
```

**Lỗi:** `Vocabulary.NotFound` (404)

### 6. DELETE /vocabularies/{id} (Admin)
```
DELETE /api/v1/admin/curriculum/vocabularies/{id:guid}
Auth: Bearer + curriculum.vocabulary.write
Response 204
```

**Lỗi:** `Vocabulary.NotFound` (404)

---

## 5. Commands & Queries

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateVocabularyCommand` | `Guid` |
| ✍ | `UpdateVocabularyCommand` | - |
| ✍ | `DeleteVocabularyCommand` | - |
| 📖 | `GetVocabulariesQuery(languageCode, ...)` | `VocabulariesResponse` (Paginated, Cacheable nếu k có search) |
| 📖 | `LookupVocabulariesQuery(languageCode, searchTerm, page, pageSize)` | `LookupVocabulariesResponse` |
| 📖 | `GetVocabularyByIdQuery(id)` | `VocabularyDetailResponse` |

> **Synonyms:** Lưu dạng `List<string>` → map PostgreSQL `text[]`. Không dùng join table.
