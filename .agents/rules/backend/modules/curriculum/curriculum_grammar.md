---
trigger: manual
---

# Curriculum — Grammar

> **Scope:** CRUD Grammar (User read + Admin write), GrammarExample + Translations  
> **User URL:** `GET /api/v1/curriculum/grammars`  
> **Admin URL:** `/api/v1/admin/curriculum/grammars`  
> **Auth:** User = Bearer. Admin Group = `"Admin"`, per-endpoint = `curriculum.grammar.write`  
> **@mention khi làm:** Grammar CRUD, grammar examples, grammar translations

---

## Entity

```
Grammar : AggregateRoot
├── Title (string)
├── Explanation (string?)    ← giải thích ngữ pháp chi tiết
├── Formula (string?)        ← công thức ngữ pháp (e.g. "S + V(s/es) + O")
├── LanguageCode (string)
├── Level (string?)
├── TopicId (Guid?, soft FK)
├── IsAiGenerated (bool)
└── Examples (List<GrammarExample>)   ← owned, cascade
    ├── Sentence (string)
    ├── Note (string?)
    ├── Order (int)
    └── Translations (List<GrammarExampleTranslation>)  ← owned, cascade
        ├── LanguageCode (string)
        └── Text (string)
```

---

## 1. GET /grammars — Danh sách (User)

```
GET /api/v1/curriculum/grammars?languageCode=en
Auth: Bearer

Response 200: List<GrammarListItemResponse>
```

> Sử dụng `ICacheableQuery` (Automatic Caching). Cache key: `curriculum:grammars:{languageCode}`. TTL: 30 phút.

---

## 2. GET /grammars/{id} — Chi tiết (User)

```
GET /api/v1/curriculum/grammars/{id:guid}
Auth: Bearer

Response 200: GrammarDetailResponse (kèm examples + translations)
```

**Lỗi:** `Grammar.NotFound` (404)

---

### 3. GET /grammars/lookup (Admin)

```
GET /api/v1/admin/curriculum/grammars/lookup?languageCode=en&searchTerm=ab&page=1&pageSize=20
Auth: Bearer + curriculum.grammar.write
Response 200: LookupGrammarsResponse { items: [{ id, label }], totalCount }
```

### 4. POST /grammars — Tạo mới (Admin)

```
POST /api/v1/admin/curriculum/grammars
Auth: Bearer + curriculum.grammar.write
Body: CreateGrammarCommand

Response 201: { id: "guid" }
```

### 5. PUT /grammars/{id} — Cập nhật (Admin)

```
PUT /api/v1/admin/curriculum/grammars/{id:guid}
Auth: Bearer + curriculum.grammar.write
Body: UpdateGrammarCommand { id, ... }

⚠️ Route id PHẢI match body id.

Response 204 NoContent
```

**Lỗi:** `Grammar.NotFound` (404)

### 6. DELETE /grammars/{id} — Soft-delete (Admin)

```
DELETE /api/v1/admin/curriculum/grammars/{id:guid}
Auth: Bearer + curriculum.grammar.write

Response 204 NoContent
```

**Lỗi:** `Grammar.NotFound` (404)

---

## 6. Commands & Queries

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateGrammarCommand` | `Guid` |
| ✍ | `UpdateGrammarCommand` | - |
| 🗑 | `DeleteGrammarCommand` | - |
| 📖 | `GetGrammarsQuery(languageCode)` | `List<GrammarListItemResponse>` (ICacheableQuery) |
| 📖 | `LookupGrammarsQuery(languageCode, searchTerm, page, pageSize)` | `LookupGrammarsResponse` |
| 📖 | `GetGrammarByIdQuery(id)` | `GrammarDetailResponse` |
