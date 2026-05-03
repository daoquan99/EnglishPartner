---
trigger: manual
---

# Curriculum — Example Sentence

> **Scope:** CRUD ExampleSentence (User read + Admin write), Translations  
> **User URL:** `GET /api/v1/curriculum/example-sentences`  
> **Admin URL:** `/api/v1/admin/curriculum/example-sentences`  
> **Auth:** User = Bearer. Admin Group = `"Admin"`, per-endpoint = `curriculum.sentence.write`  
> **@mention khi làm:** Example sentence CRUD, sentence translations

---

## Entity

```
ExampleSentence : AggregateRoot
├── Text (string)
├── LanguageCode (string)
├── AudioUrl (string?)
├── Note (string?)
├── Level (string?)
├── TopicId (Guid?, soft FK)
├── VocabularyId (Guid?, soft FK)
├── GrammarId (Guid?, soft FK)
├── IsAiGenerated (bool)
└── Translations (List<ExampleSentenceTranslation>) ← owned
    ├── LanguageCode (string)
    └── Text (string)
```

---

## Endpoints

### 1. GET /example-sentences (User/Admin)
```
GET /api/v1/curriculum/example-sentences?languageCode=en&page=1&pageSize=20&searchTerm=text&vocabularyId=guid&grammarId=guid&topicId=guid
Auth: Bearer
Response 200: ExampleSentencesResponse { items, totalCount, page, pageSize }
```

### 2. POST /example-sentences (Admin)
```
POST /api/v1/admin/curriculum/example-sentences
Auth: Bearer + curriculum.sentence.write
Body: CreateExampleSentenceCommand
```

> ⚠️ `translations` là `Dictionary<string, string>` (languageCode → translatedText), hỗ trợ nhiều ngôn ngữ.

### 3. PUT /example-sentences/{id} (Admin)
```
PUT /api/v1/admin/curriculum/example-sentences/{id:guid}
Auth: Bearer + curriculum.sentence.write
Body: UpdateExampleSentenceCommand { id, ... }
Response 204
```

### 4. DELETE /example-sentences/{id} (Admin)
```
DELETE /api/v1/admin/curriculum/example-sentences/{id:guid}
Auth: Bearer + curriculum.sentence.write
Response 204
```

**Lỗi:** `ExampleSentence.NotFound` (404)

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateExampleSentenceCommand` | `Guid` |
| ✍ | `UpdateExampleSentenceCommand` | - |
| 🗑 | `DeleteExampleSentenceCommand` | - |
| 📖 | `GetExampleSentencesQuery(...)` | `ExampleSentencesResponse` (Paginated) |

> **Lưu ý:** Không có endpoint GetById riêng. Chi tiết kèm theo trong Vocabulary/Grammar response hoặc có thể lấy qua List/Paginated api.
