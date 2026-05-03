---
trigger: manual
---

# Common: Learning Practice Workflows

> **Scope:** Luồng xử lý chi tiết khi User luyện tập qua Learning Module.  
> **Liên quan:** [learning_study_session.md](../modules/learning/learning_study_session.md), [learning_core.md](../modules/learning/learning_core.md), [cross_module.md](./cross_module.md)  
> @mention this file when working on: submit answer, evaluate, pronunciation, conversation, role-play, study session flow

---

## Tổng quan

Hệ thống hỗ trợ **5 luồng luyện tập** chính, chia thành 3 nhóm API:

| Nhóm | API Endpoint | Luồng | ItemType |
|---|---|---|---|
| **Text** | `POST /sessions/{id}/answer` | Luồng 1: Bài tập (Exercise) | 5 |
| **Audio** | `POST /sessions/{id}/answer/audio` | Luồng 2-4: Luyện nói (Vocab/Grammar/Sentence) | 0, 1, 2 |
| **Conversation** | `POST /conversations/evaluate-line` + `POST /sessions/{id}/answer` | Luồng 5: Role-play hội thoại | 4 |

> Tất cả đều nằm trong scope của **Study Session** (`POST /sessions` → làm bài → `PUT /sessions/{id}/submit`).

---

## Score Mapping (Chung cho mọi luồng)

AI trả điểm **0-100**. Hệ thống quy đổi sang SM-2 Quality Score **0-5** qua `ScoreMapper.ToSm2QualityScore()`:

| AI Score (0-100) | SM-2 Quality (0-5) | Ý nghĩa |
|---|---|---|
| 90-100 | 5 | Hoàn hảo |
| 80-89 | 4 | Tốt, sai nhỏ |
| 60-79 | 3 | Đạt, nhớ mang máng |
| 40-59 | 2 | Sai, nhưng dễ nhớ lại |
| 20-39 | 1 | Sai, nhớ mơ hồ |
| 0-19 | 0 | Hoàn toàn quên |

> SM-2 lapse threshold: `Quality < 3` → reset `Repetitions=0, IntervalDays=1`.

---

## Luồng 1: Làm bài tập (Exercise)

**API:** `POST /api/v1/learning/sessions/{id}/answer`  
**ItemType:** `5` (Exercise)  
**Content-Type:** `application/json`

### Sequence Diagram

```
Frontend                      Backend (SubmitAnswerCommandHandler)           AI Module
   │                                │                                          │
   │  POST /sessions/{id}/answer    │                                          │
   │  { itemId, itemType:5,         │                                          │
   │    userAnswer, timeSpent }     │                                          │
   │──────────────────────────────▶│                                          │
   │                                │  1. Validate session (owner, InProgress) │
   │                                │  2. session.Ping(utcNow)                 │
   │                                │                                          │
   │                                │  3. ICurriculumQueryService              │
   │                                │     .GetItemContextAsync(itemId,         │
   │                                │       CurriculumItemType.Exercise)       │
   │                                │  ← { Question, CorrectAnswer, Hint }     │
   │                                │                                          │
   │                                │  4. Phân loại Exercise:                  │
   │                                │     ┌─ Objective (MC/TrueFalse/etc.)?    │
   │                                │     │  → So sánh userAnswer == correct   │
   │                                │     │    Đúng: Score=100 → Quality 5     │
   │                                │     │    Sai:  Score=20  → Quality 1     │
   │                                │     │                                    │
   │                                │     └─ Subjective (TopicResponse/etc.)?  │
   │                                │        → Gọi AI ─────────────────────────▶
   │                                │                                          │
   │                                │  5. IAiEvaluationService                 │
   │                                │     .EvaluateTextAnswerAsync(            │
   │                                │        question, correctAnswer,          │
   │                                │        userAnswer)                       │
   │                                │  ← { Score:85, IsCorrect, Explanation }  │
   │                                │                                          │
   │                                │  6. ScoreMapper.ToSm2QualityScore(85)→4 │
   │                                │  7. ItemMastery.Review(4, utcNow)        │
   │                                │  8. LearningActivityLog.Record(...)      │
   │                                │                                          │
   │  ◀──────────────────────────── │ { qualityScore:4, isCorrect:true,        │
   │    SubmitAnswerResponse         │   explanation, nextReviewDate }          │
```

### Request Body

```json
{
  "itemId": "exercise-guid",
  "itemType": 5,
  "userAnswer": "A",
  "timeSpentSeconds": 15
}
```

### Response

```json
{
  "qualityScore": 4,
  "isCorrect": true,
  "suggestedCorrection": null,
  "explanation": "Correct! Good understanding of past tense.",
  "newIntervalDays": 6,
  "newEasinessFactor": 2.6,
  "nextReviewDate": "2026-04-29T00:00:00Z"
}
```

### Backend Logic (Chi tiết)

1. **Validate session** — kiểm tra session tồn tại, thuộc về user, trạng thái `InProgress`.
2. **Ping session** — cập nhật `LastActivityTime` để tránh bị đánh dấu Stale.
3. **Lấy context** — gọi `ICurriculumQueryService.GetItemContextAsync(itemId, Exercise)` để lấy câu hỏi gốc và đáp án.
4. **AI Evaluation** — gọi `IAiEvaluationService.EvaluateTextAnswerAsync(question, correctAnswer, userAnswer)`.
   - **Hiện tại:** Mọi Exercise đều gọi AI. AI tự phân biệt objective/subjective dựa vào context.
   - **Tối ưu tương lai:** Backend có thể tự check đáp án cho MC trước khi gọi AI (tiết kiệm API call).
5. **Quy đổi điểm** — `ScoreMapper.ToSm2QualityScore(aiScore)` → Quality 0-5.
6. **SM-2** — `ItemMastery.Review(qualityScore, utcNow)` → tính `IntervalDays`, `NextReviewDate`.
7. **Log** — `LearningActivityLog.Record(...)` với `RawScore` (0-100) và `AiFeedback`.

### Ghi chú cho Frontend

- FE gửi `userAnswer` dạng text (VD: `"A"`, `"True"`, `"She goes to school"`).
- FE **KHÔNG** gửi `qualityScore` — AI tự chấm.
- FE dùng `isCorrect`, `explanation`, `suggestedCorrection` để hiển thị feedback.

---

## Luồng 2, 3, 4: Luyện nói (Vocab / Grammar / Sentence)

**API:** `POST /api/v1/learning/sessions/{id}/answer/audio`  
**ItemType:** `0` (Vocab), `1` (Grammar), `2` (ExampleSentence)  
**Content-Type:** `multipart/form-data`

### Sequence Diagram

```
Frontend                      Backend (SubmitAudioAnswerCommandHandler)      AI Module
   │                                │                                          │
   │  POST /sessions/{id}/answer/   │                                          │
   │  audio (multipart/form-data)   │                                          │
   │  ┌ itemId: guid                │                                          │
   │  ├ itemType: 0|1|2             │                                          │
   │  ├ timeSpentSeconds: 15        │                                          │
   │  └ audioFile: recording.webm   │                                          │
   │──────────────────────────────▶│                                          │
   │                                │  1. Validate session (owner, InProgress) │
   │                                │  2. session.Ping(utcNow)                 │
   │                                │                                          │
   │                                │  3. ICurriculumQueryService              │
   │                                │     .GetItemContextAsync(itemId,         │
   │                                │       Vocabulary|Grammar|ExampleSentence)│
   │                                │  ← { Question (target text), Language }  │
   │                                │                                          │
   │                                │  4. IAiEvaluationService                 │
   │                                │     .EvaluatePronunciationAsync(         │
   │                                │        targetText, audioStream,          │
   │                                │        languageCode)   ──────────────────▶
   │                                │  ← { Score:85, Transcription,            │
   │                                │       PronunciationFeedback }            │
   │                                │                                          │
   │                                │  5. ScoreMapper.ToSm2QualityScore(85)→4 │
   │                                │  6. ItemMastery.Review(4, utcNow)        │
   │                                │  7. LearningActivityLog.Record(          │
   │                                │       userAnswer: "[AUDIO SUBMISSION]",  │
   │                                │       rawScore: 85,                      │
   │                                │       aiFeedback: "...")                 │
   │                                │                                          │
   │  ◀──────────────────────────── │ { qualityScore:4, transcription,         │
   │    SubmitAudioAnswerResponse    │   pronunciationFeedback,                │
   │                                │   newIntervalDays, nextReviewDate }      │
```

### Request Form-Data

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `itemId` | guid | ✅ | ID của Vocabulary / Grammar / ExampleSentence |
| `itemType` | int | ✅ | `0` = Vocab, `1` = Grammar, `2` = Sentence |
| `timeSpentSeconds` | int | ✅ | Thời gian user bỏ ra (giây) |
| `audioFile` | file | ✅ | File ghi âm phát âm (.webm, .wav, .mp3, .m4a) |

### Response

```json
{
  "qualityScore": 4,
  "transcription": "apple",
  "pronunciationFeedback": "Good pronunciation! The vowel sound 'æ' was slightly off.",
  "newIntervalDays": 6,
  "newEasinessFactor": 2.6,
  "nextReviewDate": "2026-04-29T00:00:00Z"
}
```

### Backend Logic (Chi tiết)

1. **Validate session** — giống Luồng 1.
2. **Lấy context** — `ICurriculumQueryService.GetItemContextAsync(itemId, Vocabulary|Grammar|ExampleSentence)`.
   - `targetText` = `context.Question` (text mục tiêu mà user cần đọc).
3. **AI Pronunciation** — `IAiEvaluationService.EvaluatePronunciationAsync(targetText, audioStream, languageCode)`.
   - AI speech-to-text → so sánh với targetText → chấm điểm phát âm 0-100.
4. **SM-2 + Log** — giống Luồng 1.

### Target Text Mapping

Mỗi ItemType trả về `targetText` khác nhau từ `CurriculumQueryService`:

| ItemType | Context.Question (targetText) | Ví dụ |
|---|---|---|
| Vocabulary (0) | `Define the word "apple" (noun)` | User đọc từ "apple" |
| Grammar (1) | `Explain the grammar rule: "Present Perfect"...` | User đọc quy tắc |
| ExampleSentence (2) | `Translate: "She goes to school every day."` | User đọc câu mẫu |

### Ghi chú cho Frontend

- Endpoint này nhận `multipart/form-data` (KHÔNG phải JSON).
- FE cần record audio bằng MediaRecorder API → gửi blob file.
- `.DisableAntiforgery()` đã được set ở server — FE không cần gửi antiforgery token.

---

## Luồng 5: Conversation Role-play (Hội thoại)

Luồng phức tạp nhất — gồm **3 giai đoạn**: chuẩn bị TTS, chấm từng câu, chốt session.

### Tổng quan Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ Phase A: Chuẩn bị (Admin / Background Job)                        │
│                                                                     │
│ Admin tạo Conversation → Background Job chạy TTS → Lưu AudioUrl   │
│ vào mỗi ConversationLine                                           │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│ Phase B: Luyện từng câu (Stateless — Không ảnh hưởng SM-2)        │
│                                                                     │
│ FE play AudioUrl của AI → User ghi âm đọc theo                     │
│ → POST /conversations/evaluate-line                                │
│ → AI chấm điểm 0-100 cho từng câu → Trả feedback ngay             │
│                                                                     │
│ FE lưu điểm các câu vào local state (array scores)                 │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│ Phase C: Chốt Mastery (SM-2 — Ghi nhận cho toàn bộ Conversation)  │
│                                                                     │
│ FE tính điểm trung bình (VD: [80,90,70] → avg=80)                 │
│ → POST /sessions/{id}/answer                                       │
│   { itemId: conversationId, itemType: 4, userAnswer: "80" }       │
│ → Backend: ScoreMapper(80) → Quality 4 → SM-2 Review              │
└─────────────────────────────────────────────────────────────────────┘
```

---

### Phase A: Chuẩn bị Audio TTS (Admin-side)

Khi Admin generate hoặc tạo Conversation bên Curriculum module, hệ thống cần tạo sẵn audio cho mỗi `ConversationLine`:

```
Admin creates Conversation
    │
    ▼
ConversationLine.AudioUrl = null (ban đầu)
    │
    ▼
Background Job / Event Handler:
    for each line where AudioUrl is null:
        audioStream = IAiEvaluationService.GenerateTtsAsync(line.Text, languageCode)
        Upload audioStream → FileStorage → get URL
        line.Update(speaker, text, audioUrl)
```

> **Lý do tạo sẵn:** Tránh gọi TTS API mỗi lần user học → tiết kiệm chi phí + giảm latency.  
> **Status:** `GenerateTtsAsync` hiện là stub (`NotImplementedException`). Cần integrate TTS provider (Google Cloud TTS / ElevenLabs / Azure).

---

### Phase B: Chấm từng câu thoại

**API:** `POST /api/v1/learning/conversations/evaluate-line`  
**Content-Type:** `multipart/form-data`

#### Sequence Diagram

```
Frontend                      Backend (EvaluateConversationLineHandler)     AI Module
   │                                │                                          │
   │  1. Play line.AudioUrl (TTS)   │                                          │
   │  2. User records their voice   │                                          │
   │                                │                                          │
   │  POST /conversations/          │                                          │
   │    evaluate-line               │                                          │
   │  ┌ conversationId: guid        │                                          │
   │  ├ lineId: guid                │                                          │
   │  └ audioFile: recording.webm   │                                          │
   │──────────────────────────────▶│                                          │
   │                                │  1. ICurriculumQueryService              │
   │                                │     .GetItemContextAsync(lineId,         │
   │                                │       ConversationLine)                  │
   │                                │  ← { CorrectAnswer: "Hello, how are     │
   │                                │       you today?", Language: "en" }      │
   │                                │                                          │
   │                                │  2. IAiEvaluationService                 │
   │                                │     .EvaluatePronunciationAsync(         │
   │                                │        targetText, audioStream,          │
   │                                │        languageCode)   ──────────────────▶
   │                                │  ← { Score:85, Transcription,            │
   │                                │       PronunciationFeedback }            │
   │                                │                                          │
   │  ◀──────────────────────────── │ { score:85, transcription,               │
   │   EvaluateConversationLine      │   pronunciationFeedback }               │
   │   Response                      │                                         │
   │                                │                                          │
   │  3. FE lưu score=85 vào        │  ⚠️ KHÔNG gọi SM-2                      │
   │     local lineScores[]          │  ⚠️ KHÔNG ghi LearningActivityLog       │
```

#### Request Form-Data

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `conversationId` | guid | ✅ | ID của Conversation |
| `lineId` | guid | ✅ | ID của ConversationLine cần chấm |
| `audioFile` | file | ✅ | File ghi âm user đọc câu thoại |

#### Response

```json
{
  "score": 85,
  "transcription": "Hello, how are you today?",
  "pronunciationFeedback": "Good intonation! Raise pitch slightly at the end of questions."
}
```

> ⚠️ **Quan trọng:** API này chỉ trả điểm tức thì. **KHÔNG** tạo `ItemMastery`, **KHÔNG** ghi `LearningActivityLog`. FE phải lưu điểm ở local state để tính trung bình ở Phase C.

---

### Phase C: Chốt Session — Ghi nhận Mastery cho Conversation

Khi hội thoại kết thúc (user đã luyện tất cả các câu), FE tính điểm trung bình và gọi API submit answer chuẩn.

**API:** `POST /api/v1/learning/sessions/{id}/answer` (cùng API với Luồng 1)  
**Content-Type:** `application/json`

#### Frontend Logic

```javascript
// FE lưu điểm từng câu
const lineScores = [80, 90, 70, 85]; // từ Phase B

// Tính trung bình
const avgScore = Math.round(
    lineScores.reduce((a, b) => a + b, 0) / lineScores.length
); // = 81

// Gửi lên Backend qua API chuẩn
POST /api/v1/learning/sessions/{sessionId}/answer
{
    "itemId": "conversation-guid",
    "itemType": 4,        // Conversation
    "userAnswer": "81",   // Điểm trung bình (dạng string)
    "timeSpentSeconds": 120
}
```

#### Backend Logic

Khi `SubmitAnswerCommandHandler` nhận request với `itemType: 4` (Conversation):

1. Lấy context qua `ICurriculumQueryService.GetItemContextAsync(conversationId, Conversation)`.
2. Gọi `IAiEvaluationService.EvaluateTextAnswerAsync(question, correctAnswer, "81")`.
   - AI nhận `userAnswer = "81"` → hiểu đây là điểm trung bình → trả `Score` tương ứng.
   - **Tối ưu tương lai:** Có thể skip AI call cho Conversation → parse trực tiếp `int.Parse(userAnswer)` làm `rawScore`.
3. `ScoreMapper.ToSm2QualityScore(81)` → Quality `4`.
4. `ItemMastery.Review(4, utcNow)` → SM-2 tính `IntervalDays`, `NextReviewDate` cho **toàn bộ Conversation**.
5. `LearningActivityLog.Record(...)`.

---

## Cross-Module Data Flow (Tổng hợp)

```
┌────────────────────┐     Module Interface (sync)     ┌──────────────────┐
│    Curriculum       │◀─────────────────────────────────│    Learning       │
│                     │  ICurriculumQueryService          │                  │
│  GetItemContext()   │  .GetItemContextAsync()          │  SubmitAnswer    │
│  → Question         │                                  │  SubmitAudioAns  │
│  → CorrectAnswer    │                                  │  EvaluateConvLine│
│  → Hint             │                                  │                  │
│  → LanguageCode     │                                  │                  │
└────────────────────┘                                  └────────┬─────────┘
                                                                  │
                                                                  │ Module Interface
                                                                  │ (sync)
                                                                  ▼
                                                        ┌──────────────────┐
                                                        │       AI         │
                                                        │                  │
                                                        │ EvaluateText()   │
                                                        │ EvaluatePronun() │
                                                        │ GenerateTts()    │
                                                        └──────────────────┘
```

### ItemType Mapping (Learning ↔ Curriculum)

> ⚠️ **Chú ý:** Hai enum khác nhau! Learning bắt đầu từ 0, Curriculum từ 1.

| Learning.ItemType | Value | Curriculum.CurriculumItemType | Value |
|---|---|---|---|
| Vocabulary | 0 | Vocabulary | 1 |
| Grammar | 1 | Grammar | 2 |
| ExampleSentence | 2 | ExampleSentence | 4 |
| Topic | 3 | Topic | 6 |
| Conversation | 4 | Conversation | 5 |
| Exercise | 5 | Exercise | 3 |
| — | — | ConversationLine | 7 |

> Mapping được xử lý trong handler bằng switch expression. Xem `SubmitAnswerCommandHandler` và `SubmitAudioAnswerCommandHandler`.

---

## XP Policy (Theo loại luyện tập)

| Hoạt động | XP | Nguồn |
|---|---|---|
| Session item (text answer) | 5 XP/item | `XpPolicy.XpPerSessionItem` |
| Session bonus (≥80% accuracy) | +10 XP | `XpPolicy.HighScoreBonusXp` |
| Standalone review (flashcard) | 2 XP | `XpPolicy.XpPerStandaloneReview` |
| Audio review (pronunciation) | 5 XP | `XpPolicy.XpPerAudioReview` |
| Conversation line (role-play) | 3 XP | `XpPolicy.XpPerConversationLine` |

---

## Error Codes

| Code | HTTP | Khi nào |
|---|---|---|
| `Learning.Session.NotFound` | 404 | Session không tồn tại hoặc không thuộc user |
| `Learning.Session.NotInProgress` | 400 | Session đã Complete hoặc Abandoned |
| `Common.NotFound` | 404 | `CurriculumItem` không tìm thấy (itemId sai hoặc đã bị xóa) |
| `AI.EvaluationFailed` | 400 | AI không thể phân tích được audio (ồn, trống, sai định dạng) |

---

## Ghi chú Implementation

1. **`LearningActivityLog`** có 2 trường mới: `RawScore` (int?, 0-100) và `AiFeedback` (string?). Đã có migration `AddAiFieldsToActivityLog`.
2. **Audio endpoints** dùng `.DisableAntiforgery()` vì FE gửi `multipart/form-data` không kèm antiforgery token.
3. **Fallback:** Nếu AI service unavailable, handler trả `qualityScore = 0` (worst case SM-2). FE vẫn nhận response bình thường.
4. **TTS stub:** `IAiEvaluationService.GenerateTtsAsync()` hiện throw `NotImplementedException`. Cần integrate TTS provider trước khi Phase A (Conversation TTS) hoạt động.
