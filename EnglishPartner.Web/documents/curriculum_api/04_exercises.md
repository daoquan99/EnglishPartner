# Curriculum API — Exercises

> **Auth:** Bearer token required  
> **Admin endpoints** yêu cầu Admin role + `curriculum.admin` permission

---

## 1. Lấy danh sách bài tập

```http
GET /api/v1/curriculum/exercises?languageCode=en&topicId=guid&vocabularyId=guid&grammarId=guid&level=A1&page=1&pageSize=20
```

| Query Param | Type | Required | Mô tả |
|---|---|---|---|
| `languageCode` | string | ✅ | Mã ngôn ngữ |
| `page` | int | ❌ | Trang hiện tại (default: 1) |
| `pageSize` | int | ❌ | Kích thước trang (default: 20) |
| `topicId` | guid | ❌ | Filter theo topic |
| `vocabularyId` | guid | ❌ | Filter theo từ vựng |
| `grammarId` | guid | ❌ | Filter theo ngữ pháp |
| `level` | string | ❌ | Filter theo level |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "title": "Choose the correct verb form",
      "type": "MultipleChoice",
      "level": "A1",
      "xpReward": 10
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

> ⚡ Cache 6 giờ.

---

## 2. Lấy chi tiết bài tập

```http
GET /api/v1/curriculum/exercises/{id}
```

**Response `200 OK`:**
```json
{
  "id": "guid",
  "title": "Choose the correct verb form",
  "instruction": "Select the best answer for each question.",
  "type": "MultipleChoice",
  "correctAnswer": "goes",
  "languageCode": "en",
  "level": "A1",
  "topicId": "guid",
  "vocabularyId": null,
  "grammarId": "guid",
  "content": "She ___ to school every day.",
  "explanation": "'Goes' is correct because the subject is third person singular.",
  "xpReward": 10,
  "options": [
    { "id": "guid", "text": "go", "isCorrect": false, "sortOrder": 1 },
    { "id": "guid", "text": "goes", "isCorrect": true, "sortOrder": 2 },
    { "id": "guid", "text": "going", "isCorrect": false, "sortOrder": 3 }
  ]
}
```

> ⚠️ **FE nên ẩn `correctAnswer` và `isCorrect`** khi hiển thị bài tập cho user làm. Chỉ show sau khi submit.

**Error:** `404` — `Exercise.NotFound`

---

## 3. Tạo bài tập (Admin)

```http
POST /api/v1/admin/curriculum/exercises
```

**Request Body:**
```json
{
  "title": "Choose the correct verb form",
  "instruction": "Select the best answer for each question.",
  "type": 0,
  "correctAnswer": "goes",
  "languageCode": "en",
  "level": "A1",
  "topicId": "guid or null",
  "vocabularyId": null,
  "grammarId": "guid or null",
  "content": "She ___ to school every day.",
  "explanation": "'Goes' is correct...",
  "xpReward": 10
}
```

| Field | Type | Required |
|---|---|---|
| `title` | string | ✅ |
| `instruction` | string | ✅ |
| `type` | int (enum) | ✅ |
| `correctAnswer` | string | ✅ |
| `languageCode` | string | ✅ |
| `level` | string | ❌ |
| `topicId` | guid | ❌ |
| `vocabularyId` | guid | ❌ |
| `grammarId` | guid | ❌ |
| `content` | string | ❌ |
| `explanation` | string | ❌ |
| `xpReward` | int | ✅ |

> ⚠️ **`type` gửi dạng int, nhận về dạng string.**

**ExerciseType values:**

| Value | Name | Value | Name |
|---|---|---|---|
| 0 | MultipleChoice | 14 | TrueFalseNotGiven |
| 1 | FillInTheBlank | 15 | MatchingHeadings |
| 2 | TrueFalse | 16 | SummaryCompletion |
| 3 | Matching | 17 | ShortAnswerFromPassage |
| 4 | Translation | 18 | Paraphrasing |
| 5 | ListeningComprehension | 19 | GapFilling |
| 6 | Speaking | 20 | ErrorCorrection |
| 7 | DictationWriting | 21 | ParagraphWriting |
| 8 | ReadingComprehension | 22 | ReorderingWords |
| 9 | SentenceCompletion | 23 | RewritingSentences |
| 10 | WordForm | 24 | DialogueCompletion |
| 11 | VerbTense | 25 | EssayWriting |
| 12 | PronunciationPractice | 26 | SituationalResponse |
| 13 | ConversationRole | 27 | ShortAnswerQuestions |

**Response `201 Created`:** `"guid"` (exercise id)

---

## 4. Thêm Option cho bài tập (Admin)

```http
POST /api/v1/admin/curriculum/exercises/{exerciseId}/options
```

**Request Body:**
```json
{
  "text": "goes",
  "isCorrect": true
}
```

> `exerciseId` lấy từ URL, KHÔNG gửi trong body.

**Response `204 No Content`**  
**Error:** `404` — `Exercise.NotFound`

---

## 5. Cập nhật bài tập (Admin)

```http
PUT /api/v1/admin/curriculum/exercises/{id}
```

**Request Body:** Giống Create + `"id": "guid"` (PHẢI match route id).

**Response `204 No Content`**  
**Error:** `404` — `Exercise.NotFound`

---

## 6. Xóa bài tập (Admin)

```http
DELETE /api/v1/admin/curriculum/exercises/{id}
```

**Response `204 No Content`**  
**Error:** `404` — `Exercise.NotFound`
