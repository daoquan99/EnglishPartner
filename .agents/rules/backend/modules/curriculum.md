---
trigger: manual
---

# Module: Curriculum — Index

> **Scope:** Vocabulary, Grammar, Topic, Exercise, ExampleSentence, Conversation, Language, AI Generate  
> DB Schema: `curriculum`

---

## Tài liệu con

| File | Nội dung |
|---|---|
| [curriculum_vocabulary.md](./curriculum/curriculum_vocabulary.md) | CRUD Vocabulary (User + Admin), caching pattern, search |
| [curriculum_grammar.md](./curriculum/curriculum_grammar.md) | CRUD Grammar (User + Admin), GrammarExample + Translations |
| [curriculum_topic.md](./curriculum/curriculum_topic.md) | CRUD Topic (User + Admin), seeded languages |
| [curriculum_exercise.md](./curriculum/curriculum_exercise.md) | CRUD Exercise (User + Admin), ExerciseOption, ExerciseType enum |
| [curriculum_conversation.md](./curriculum/curriculum_conversation.md) | CRUD Conversation (User + Admin), ConversationLine + Translations |
| [curriculum_example_sentence.md](./curriculum/curriculum_example_sentence.md) | CRUD ExampleSentence (User + Admin), Translations |
| [curriculum_ai_generate.md](./curriculum/curriculum_ai_generate.md) | AI Generate endpoints: Vocab, Grammar, Exercise, Conversation, ExampleSentences |

---

## Key Entities (tổng quan)

| Entity | Type | Key Fields |
|---|---|---|
| `Topic` | AggregateRoot | title, description?, languageCode, level?, sortOrder, imageUrl? |
| `Vocabulary` | AggregateRoot | word, definition, languageCode, pronunciation?, phoneticTranscription?, partOfSpeech?, imageUrl?, audioUrl?, level?, topicId?, synonyms (PG text[]), isAiGenerated |
| `Grammar` | AggregateRoot | title, explanation?, formula?, languageCode, level?, topicId?, isAiGenerated |
| `GrammarExample` | Entity | sentence, note? — child of Grammar |
| `GrammarExampleTranslation` | Entity | languageCode, text — child of GrammarExample |
| `ExampleSentence` | AggregateRoot | text, languageCode, audioUrl?, note?, level?, topicId?, vocabularyId?, grammarId?, isAiGenerated |
| `ExampleSentenceTranslation` | Entity | languageCode, text — child of ExampleSentence |
| `Exercise` | AggregateRoot | title, instruction, type (enum), content?, correctAnswer, explanation?, languageCode, level?, xpReward (default 10), topicId?, vocabularyId?, grammarId?, isAiGenerated |
| `ExerciseOption` | Entity | text, isCorrect, order — owned by Exercise |
| `Conversation` | AggregateRoot | title, scenario?, languageCode, level?, imageUrl?, topicId?, grammarId?, isAiGenerated |
| `ConversationLine` | Entity | speaker, text, order, audioUrl? — child of Conversation |
| `ConversationLineTranslation` | Entity | languageCode, text — child of ConversationLine |
| `Language` | AggregateRoot | code, name, nativeName, sortOrder (seeded data) |

---

## Content Relationships

```
Topic (1) ──── (many) Vocabulary      (via TopicId — soft FK, no EF FK constraint)
Topic (1) ──── (many) Grammar
Topic (1) ──── (many) Conversation
Topic (1) ──── (many) ExampleSentence
Topic (1) ──── (many) Exercise

Vocabulary (1) ── (many) ExampleSentence  (vocabularyId)
Vocabulary (1) ── (many) Exercise         (vocabularyId)
Grammar (1)    ── (many) GrammarExample   (owned, cascades)
Grammar (1)    ── (many) ExampleSentence  (grammarId)
Grammar (1)    ── (many) Exercise         (grammarId)
Grammar (1)    ── (many) Conversation     (grammarId)

Exercise (1) ── (many) ExerciseOption    (owned, cascades)
Conversation (1) ── (many) ConversationLine (owned, ordered by Order)
ConversationLine (1) ── (many) ConversationLineTranslation (owned)
```

### TTS Hook (Background Audio Generation)
Khi Admin thêm mới `ConversationLine` hoặc AI Generate ra Conversation, hệ thống cần tự động gọi `IAiEvaluationService.GenerateTtsAsync` để tạo file âm thanh và update vào trường `AudioUrl` của `ConversationLine`.

### Cross-Module Contracts (for Learning)
Curriculum cần cung cấp các interface query (trong `Curriculum.Contracts`) để module `Learning` có thể gọi khi chấm điểm (để lấy Text gốc hoặc Đáp án). Ví dụ: `GetExerciseAnswerAsync`, `GetConversationLineTextAsync`, v.v.

---

## Multi-Language Support

- Tất cả entities có `LanguageCode` (default `"en"`)
- Tất cả list queries nhận `?languageCode=en` filter
- Translations là child entities owned by parent (cascade delete)
- `Language` entity: seeded qua `CurriculumDataSeeder.Seed(modelBuilder)`

---

## Error Codes (đầy đủ)

| Code | HTTP | When |
|---|---|---|
| `Topic.NotFound` | 404 | Topic id không tồn tại |
| `Vocabulary.NotFound` | 404 | Vocabulary id không tồn tại |
| `Vocabulary.WordAlreadyExists` | 409 | Word trùng |
| `Grammar.NotFound` | 404 | Grammar id không tồn tại |
| `Conversation.NotFound` | 404 | Conversation id không tồn tại |
| `ExampleSentence.NotFound` | 404 | ExampleSentence id không tồn tại |
| `Exercise.NotFound` | 404 | Exercise id không tồn tại |
| `Exercise.UnsupportedAiType` | 400 | Media-only type được gửi tới AI generate |
| `Language.NotFound` | 404 | Language code không tồn tại |
| `Language.AlreadyExists` | 409 | Language code đã tồn tại |

> **Lưu ý:** NotFound errors sử dụng `CommonErrors.NotFound(entity, id)` chung. Chỉ các business errors (`WordAlreadyExists`, `UnsupportedAiType`, `AlreadyExists`) mới define trực tiếp trong `CurriculumErrors`.

---

## Auth Pattern

> ⚠️ Vocabulary admin group dùng **permission string trực tiếp** ở group level (`"curriculum.vocabulary.write"`).
> Tất cả entities khác dùng `"Admin"` policy ở group level + permission string ở **per-endpoint**.
> AI Generate dùng `"Admin"` ở group + `"curriculum.generate"` + `RequireRateLimiting("ai-generate")` per-endpoint.

---

## Domain Events (Pending Consumers)

| Event | Defined in | Status | Restore when |
|---|---|---|---|
| `VocabularyCreatedDomainEvent` | `Vocabularies/Events/` | Commented out in `Vocabulary.Create()` | Implementing auto-add to user review queue |
| `TopicCreatedDomainEvent` | `Topics/Events/` | Commented out in `Topic.Create()` | Implementing notify Learning module of new topic |
| `ExerciseCreatedDomainEvent` | `Exercises/Events/` | Defined but NOT called in `Exercise.Create()` | Implementing exercise recommendation pipeline |

> **Khi restore:** Uncomment `RaiseDomainEvent(...)` trong entity, tạo handler trong `Application/` hoặc `Infrastructure/`, publish integration event qua Outbox.
