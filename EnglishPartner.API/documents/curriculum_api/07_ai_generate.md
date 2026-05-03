# Curriculum API — AI Generate (Admin Only)

> **Base URL:** `/api/v1/admin/curriculum/generate`  
> **Auth:** Admin role + `curriculum.generate` permission  
> **Rate Limit:** `ai-generate` policy  
> Kết quả được AI tạo ra sẽ **lưu thẳng vào DB** với `isAiGenerated = true`.

---

## 1. Generate Vocabulary

```http
POST /api/v1/admin/curriculum/generate/vocabulary
```

**Request Body:**
```json
{
  "targetLanguage": "en",
  "wordHint": "food",
  "count": 5,
  "level": "A1",
  "topicId": "guid or null",
  "autoGenerateExamples": true,
  "exampleCount": 3,
  "exampleTargetLanguageCodes": ["vi", "ja"]
}
```

| Field | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `targetLanguage` | string | ✅ | — | Ngôn ngữ nội dung sinh ra (vd: "en", "ja") |
| `wordHint` | string | ✅ | — | Từ khóa / chủ đề. AI chọn N từ liên quan |
| `count` | int | ❌ | 1 | Số lượng từ vựng cần tạo |
| `level` | string | ❌ | null | CEFR level (A1-C2) |
| `topicId` | guid | ❌ | null | Gán topic cho từ vựng |
| `autoGenerateExamples` | bool | ❌ | false | Tự động tạo câu ví dụ kèm theo |
| `exampleCount` | int | ❌ | 3 | Số câu ví dụ mỗi từ |
| `exampleTargetLanguageCodes` | string[] | ❌ | [] | Ngôn ngữ dịch cho câu ví dụ |

**Response `200 OK`:** Danh sách vocabulary vừa tạo.

---

## 2. Generate Grammar

```http
POST /api/v1/admin/curriculum/generate/grammar
```

```json
{
  "targetLanguage": "en",
  "title": "Present Simple",
  "count": 3,
  "level": "A1",
  "topicId": null,
  "exampleCount": 3,
  "targetLanguageCodes": ["vi", "ja"]
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `targetLanguage` | string | ✅ | Ngôn ngữ sinh nội dung (vd: "en") |
| `title` | string | ✅ | Tiêu đề ngữ pháp cần tạo |
| `count` | int | ❌ | Số lượng grammar rules |
| `level` | string | ❌ | CEFR level |
| `topicId` | guid | ❌ | Gán topic |
| `exampleCount` | int | ❌ | Số câu ví dụ mỗi rule |
| `targetLanguageCodes` | string[] | ❌ | Ngôn ngữ dịch |

**Response `200 OK`:** Danh sách grammar vừa tạo.

---

## 3. Generate Conversation

```http
POST /api/v1/admin/curriculum/generate/conversation
```

```json
{
  "targetLanguage": "en",
  "topicName": "Ordering Food",
  "count": 2,
  "level": "A2",
  "scenario": "At a restaurant",
  "lineCount": 6,
  "targetLanguageCodes": ["vi"],
  "topicId": null,
  "grammarId": null
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `targetLanguage` | string | ✅ | Ngôn ngữ sinh nội dung (vd: "en") |
| `topicName` | string | ✅ | Chủ đề hội thoại |
| `count` | int | ❌ | Số hội thoại cần tạo |
| `level` | string | ❌ | CEFR level |
| `scenario` | string | ❌ | Bối cảnh cụ thể |
| `lineCount` | int | ❌ | Số dòng thoại mỗi hội thoại |
| `targetLanguageCodes` | string[] | ❌ | Ngôn ngữ dịch |
| `topicId` | guid | ❌ | Gán topic |
| `grammarId` | guid | ❌ | Gán grammar |

**Response `200 OK`:** Danh sách conversation vừa tạo.

---

## 4. Generate Exercise

```http
POST /api/v1/admin/curriculum/generate/exercise
```

```json
{
  "targetLanguage": "en",
  "context": "Daily routines",
  "exerciseType": 0,
  "count": 5,
  "level": "A1",
  "topicId": null,
  "vocabularyId": null,
  "grammarId": null,
  "xpReward": 10
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `targetLanguage` | string | ✅ | Ngôn ngữ sinh nội dung (vd: "en") |
| `context` | string | ✅ | Nội dung / chủ đề bài tập |
| `exerciseType` | int | ✅ | Loại bài tập (enum value) |
| `count` | int | ❌ | Số bài tập cần tạo |
| `level` | string | ❌ | CEFR level |
| `topicId` | guid | ❌ | Gán topic |
| `vocabularyId` | guid | ❌ | Gán vocabulary |
| `grammarId` | guid | ❌ | Gán grammar |
| `xpReward` | int | ❌ | XP thưởng mỗi bài |

> ⚠️ **Chỉ hỗ trợ text-based types:**  
> MultipleChoice(0), FillInTheBlank(1), TrueFalse(2), Matching(3), Translation(4), SentenceCompletion(5), WordForm(6), VerbTense(7), TrueFalseNotGiven(8), Paraphrasing(9), ReorderingWords(10), RewritingSentences(11), DialogueCompletion(12), ShortAnswerQuestions(13), QuestionFormation(14), SynonymAntonym(15), Collocation(16), OddOneOut(17), SpellingCorrection(18), WordStress(19), MultipleResponse(20), SentenceCorrection(21), Categorization(22), TopicResponse(30).
>
> Gửi media-only type (Listening, Speaking...) → `400 Exercise.UnsupportedAiType`.

**Response `200 OK`:** Danh sách exercise vừa tạo.

---

## 5. Generate Example Sentences

```http
POST /api/v1/admin/curriculum/generate/example-sentences
```

```json
{
  "targetLanguage": "en",
  "context": "Greetings",
  "count": 3,
  "level": "A1",
  "targetLanguageCodes": ["vi", "ja"],
  "topicId": null,
  "vocabularyId": null,
  "grammarId": null
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `targetLanguage` | string | ✅ | Ngôn ngữ sinh nội dung (vd: "en") |
| `context` | string | ✅ | Nội dung / chủ đề câu ví dụ |
| `count` | int | ✅ | Số câu cần tạo |
| `level` | string | ❌ | CEFR level |
| `targetLanguageCodes` | string[] | ❌ | Ngôn ngữ dịch |
| `topicId` | guid | ❌ | Gán topic |
| `vocabularyId` | guid | ❌ | Gán vocabulary |
| `grammarId` | guid | ❌ | Gán grammar |

**Response `200 OK`:** Danh sách example sentences vừa tạo.
