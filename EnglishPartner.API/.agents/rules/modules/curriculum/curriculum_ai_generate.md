---
trigger: manual
---

# Curriculum — AI Generate

> **Scope:** Admin-only AI content generation endpoints  
> **Base URL:** `/api/v1/admin/curriculum/generate`  
> **Auth:** Bearer (Admin) + `curriculum.generate` + `RequireRateLimiting("ai-generate")`  
> **@mention khi làm:** AI generate vocabulary, grammar, exercise, conversation, example sentences

---

## Quy tắc

- Tất cả endpoints **BẮT BUỘC** gắn `.RequireRateLimiting("ai-generate")` để bảo vệ quota Gemini
- Kết quả được lưu thẳng vào DB với `IsAiGenerated = true` (admin review sau)
- Mỗi endpoint hỗ trợ param `Count` để generate N items cùng lúc
- Kết quả trả về danh sách các items vừa tạo

---

## 1. POST /generate/vocabulary

```
POST /api/v1/admin/curriculum/generate/vocabulary
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "targetLanguage": "en",
  "wordHint": "food",
  "count": 5,
  "level": "A1",
  "topicId": "guid | null",
  "autoGenerateExamples": true,
  "exampleCount": 3,
  "exampleTargetLanguageCodes": ["vi", "ja"]
}

Response 200: GenerateAndSaveVocabularyResponse
```

---

## 2. POST /generate/grammar

```
POST /api/v1/admin/curriculum/generate/grammar
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "targetLanguage": "en",
  "title": "Present Simple",
  "count": 3,
  "level": "A1",
  "topicId": "guid | null",
  "exampleCount": 3,
  "targetLanguageCodes": ["vi", "ja"]
}

Response 200: list of generated grammars
```

---

## 3. POST /generate/conversation

```
POST /api/v1/admin/curriculum/generate/conversation
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "targetLanguage": "en",
  "topicName": "Ordering Food",
  "count": 2,
  "level": "A2",
  "scenario": "At a restaurant",
  "lineCount": 6,
  "targetLanguageCodes": ["vi"],
  "topicId": "guid | null",
  "grammarId": "guid | null"
}

Response 200: list of generated conversations
```

---

## 4. POST /generate/exercise

```
POST /api/v1/admin/curriculum/generate/exercise
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "targetLanguage": "en",
  "context": "Daily routines",
  "exerciseType": "MultipleChoice",
  "count": 5,
  "level": "A1",
  "topicId": "guid | null",
  "vocabularyId": "guid | null",
  "grammarId": "guid | null",
  "xpReward": 10
}

Response 200: list of generated exercises
```

> ⚠️ Nếu `exerciseType` là `Listening`, `Speaking`, `Reading`, `Essay` → trả `400 Exercise.UnsupportedAiType`
> ⚠️ Nếu exercise type cần options mà AI trả về thiếu → handler tự động gọi thêm `GenerateExerciseOptionsAsync` (dùng `ExerciseType.GetDefaultOptionCount()`).

### Option Count mặc định theo ExerciseType

| ExerciseType | Options | Ghi chú |
|---|---|---|
| MultipleChoice | 4 | 1 đúng + 3 nhiễu |
| FillInTheBlank | 4 | Gợi ý đáp án |
| TrueFalse | 2 | Cố định: True / False |
| SentenceCompletion | 4 | Chọn cụm từ |
| WordForm | 4 | Chọn dạng từ |
| VerbTense | 4 | Chọn thì |
| TrueFalseNotGiven | 3 | Cố định: True / False / Not Given |
| DialogueCompletion | 4 | Chọn câu hoàn thành |
| SynonymAntonym | 4 | Chọn synonym/antonym |
| Collocation | 4 | Chọn từ đi kèm |
| OddOneOut | 4 | Chọn từ không thuộc nhóm |
| MultipleResponse | 5 | ≥2 đáp án đúng |
| WordStress | 4 | Chọn âm tiết trọng âm |
| QuestionFormation | 4 | Chọn dạng câu hỏi đúng |
| Translation, Paraphrasing, ReorderingWords, RewritingSentences, ShortAnswerQuestions, SpellingCorrection, SentenceCorrection, TopicResponse, ListeningGapFill, Categorization | 0 | Không cần options |

### Types đã gộp (Obsolete — giữ giá trị enum cho DB backward compat)

| Cũ | Gộp vào |
|---|---|
| GapFilling (19), ClozeTest (20) | FillInTheBlank (1) |
| YesNoQuestions (15), WhQuestions (16), TagQuestions (17) | QuestionFormation (38) |
| EssayWriting (26) | Writing (13) |

> **Source of truth**: `ExerciseTypeExtensions.GetDefaultOptionCount()` trong `ExerciseType.cs`

---

## 5. POST /generate/exercise-options/{exerciseId}

```
POST /api/v1/admin/curriculum/generate/exercise-options/{exerciseId}
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "optionCount": 4
}

Response 200: { exerciseId, options: [{ text, isCorrect }] }
```

> Xoá toàn bộ options cũ rồi thay bằng options mới do AI sinh ra.

---

## 5b. POST /generate/exercise-options/bulk

```
POST /api/v1/admin/curriculum/generate/exercise-options/bulk
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Response 200:
{
  "totalScanned": 50,
  "optionsGenerated": 12,
  "items": [{ "exerciseId": "guid", "title": "...", "optionCount": 4 }]
}
```

> Quét tất cả exercises trong DB, tìm những exercise cần options (theo `RequiresOptions()`) nhưng chưa có → gọi AI sinh options cho từng exercise.

---

## 6. POST /generate/example-sentences

```
POST /api/v1/admin/curriculum/generate/example-sentences
Auth: Bearer + curriculum.generate + RateLimit(ai-generate)

Body:
{
  "targetLanguage": "en",
  "context": "Greetings",
  "count": 3,
  "level": "A1",
  "targetLanguageCodes": ["vi", "ja"],
  "topicId": "guid | null",
  "vocabularyId": "guid | null",
  "grammarId": "guid | null"
}

Response 200: list of generated sentences with translations
```

---

## Commands

| Command | Resource |
|---|---|
| `GenerateAndSaveVocabularyCommand` | Vocabulary + optional ExampleSentences |
| `GenerateAndSaveGrammarCommand` | Grammar + GrammarExamples + Translations |
| `GenerateAndSaveConversationCommand` | Conversation + Lines + Translations |
| `GenerateAndSaveExerciseCommand` | Exercise + Options (auto-fill if missing) |
| `GenerateExerciseOptionsCommand(exerciseId, optionCount?)` | ExerciseOptions (replace existing) |
| `BulkGenerateExerciseOptionsCommand` | Scan DB, generate missing options |
| `GenerateAndSaveExampleSentencesCommand` | ExampleSentence + Translations |

