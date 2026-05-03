---
trigger: manual
---

# Curriculum — Exercise

> **Scope:** CRUD Exercise (User read + Admin write), ExerciseOption, ExerciseType enum  
> **User URL:** `GET /api/v1/curriculum/exercises`  
> **Admin URL:** `/api/v1/admin/curriculum/exercises`  
> **Auth:** User = Bearer. Admin Group = `"Admin"`, per-endpoint = `curriculum.exercise.write`  
> **@mention khi làm:** Exercise CRUD, exercise options, exercise types, add option

---

## Entities

```
Exercise : AggregateRoot
├── Title (string)                ← tiêu đề bài tập
├── Instruction (string)          ← hướng dẫn làm bài
├── Type (ExerciseType enum)
├── Content (string?)             ← nội dung bổ sung (đoạn văn, đoạn hội thoại...)
├── CorrectAnswer (string)        ← đáp án đúng
├── Explanation (string?)         ← giải thích đáp án
├── LanguageCode (string)
├── Level (string?)
├── XpReward (int, default 10)
├── TopicId (Guid?, soft FK)
├── VocabularyId (Guid?, soft FK)
├── GrammarId (Guid?, soft FK)
├── IsAiGenerated (bool)
└── Options (List<ExerciseOption>)   ← owned, cascade
    ├── Text (string)
    ├── IsCorrect (bool)
    └── Order (int)
```

## ExerciseType Enum (28 values)

### AI-Generated (text-based only):
`MultipleChoice(0)`, `FillInTheBlank(1)`, `TrueFalse(2)`, `Translation(4)`,
`SentenceCompletion(9)`, `WordForm(10)`, `VerbTense(11)`,
`TrueFalseNotGiven(14)`, `Paraphrasing(18)`, `GapFilling(19)`,
`ReorderingWords(22)`, `RewritingSentences(23)`, `DialogueCompletion(24)`,
`ShortAnswerQuestions(27)`

### NOT supported by AI Generate (requires media/complex input):
`Matching(3)`, `ListeningComprehension(5)`, `Speaking(6)`,
`SentenceTransformation(7)`, `ErrorIdentification(8)`,
`ReadingComprehension(12)`, `Writing(13)`, `YesNoQuestions(15)`,
`WhQuestions(16)`, `TagQuestions(17)`, `ClozeTest(20)`,
`Dictation(21)`, `PictureDescription(25)`, `EssayWriting(26)`

→ Gửi các types này vào AI Generate sẽ trả `400 Exercise.UnsupportedAiType`.

---

## 1. GET /exercises — Danh sách (User)

```
GET /api/v1/curriculum/exercises?languageCode=en&topicId=guid&vocabularyId=guid&grammarId=guid&level=level&page=1&pageSize=20
Auth: Bearer

Response 200: ExercisesResponse (items, totalCount, page, pageSize)
```

> Sử dụng `ICacheableQuery` (Automatic Caching). Cache key: `curriculum:exercises:{LanguageCode}:p{Page}:s{PageSize}:t{TopicId}:v{VocabularyId}:g{GrammarId}:lvl{Level}`

---

## 2. GET /exercises/{id} — Chi tiết (User)

```
GET /api/v1/curriculum/exercises/{id:guid}
Auth: Bearer

Response 200: ExerciseDetailResponse (kèm tất cả options)
```

**Lỗi:** `Exercise.NotFound` (404)

---

## 3. POST /exercises — Tạo mới (Admin)

```
POST /api/v1/admin/curriculum/exercises
Auth: Bearer + curriculum.exercise.write
Body: CreateExerciseCommand

Response 201: { id: "guid" }
```

---

## 4. PUT /exercises/{id} — Cập nhật (Admin)

```
PUT /api/v1/admin/curriculum/exercises/{id:guid}
Auth: Bearer + curriculum.exercise.write
Body: UpdateExerciseCommand { id, ... }

⚠️ Route id PHẢI match body id.

Response 204 NoContent
```

**Lỗi:** `Exercise.NotFound` (404)

---

## 5. DELETE /exercises/{id} — Soft-delete (Admin)

```
DELETE /api/v1/admin/curriculum/exercises/{id:guid}
Auth: Bearer + curriculum.exercise.write

Response 204 NoContent
```

**Lỗi:** `Exercise.NotFound` (404)

---

## 6. POST /exercises/{exerciseId}/options — Thêm Option (Admin)

```
POST /api/v1/admin/curriculum/exercises/{exerciseId:guid}/options
Auth: Bearer + curriculum.exercise.write
Body: { "text": "Option A", "isCorrect": true }

⚠️ ExerciseId lấy từ ROUTE, không từ body (BUG-3 pattern).

Response 204 NoContent
```

**Lỗi:** `Exercise.NotFound` (404)

---

## 7. Commands & Queries

| | Command/Query | Returns |
|---|---|---|
| ✍ | `CreateExerciseCommand` | `Guid` |
| ✍ | `UpdateExerciseCommand` | - |
| 🗑 | `DeleteExerciseCommand` | - |
| ✍ | `AddExerciseOptionCommand(exerciseId, text, isCorrect)` | - |
| 📖 | `GetExercisesQuery(languageCode, page?, pageSize?, topicId?, vocabularyId?, grammarId?, level?)` | `ExercisesResponse` (ICacheableQuery) |
| 📖 | `GetExerciseByIdQuery(id)` | `ExerciseDetailResponse` |
