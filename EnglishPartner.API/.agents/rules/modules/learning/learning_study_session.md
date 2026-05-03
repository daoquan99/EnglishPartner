---
trigger: manual
---

# Learning — Study Session

> **Scope:** Bắt đầu / Làm bài / Nộp bài  
> **Auth:** Bearer  
> **@mention khi làm:** study session, submit answer, evaluate answer

---

## 1. Start Session

```
POST /api/v1/learning/sessions
Auth: Bearer
Body: { "sessionTitle": "Unit 1", "totalItems": 10 }

Response 201: { sessionId }
```

> **Side-effect:** Command handler sẽ lấy các session bị "Stale" (quá 30 phút không hoạt động) của user này và tự động chuyển sang trạng thái `Abandoned`.

---

## 2. Ping Session (Keep-alive)

Cần gọi mỗi 2-5 phút để báo hiệu session vẫn đang hoạt động (không bị stale).
```
POST /api/v1/learning/sessions/{id:guid}/ping
Auth: Bearer
Response 204
```

---

## 3. Submit Text Answer (Tự động chấm điểm)

Khi user làm xong một câu hỏi dạng text trong session. Điểm SM-2 được tự động tính toán bằng AI.

```
POST /api/v1/learning/sessions/{id:guid}/answer
Auth: Bearer
Body: 
{
  "itemId": "guid",
  "itemType": 0,          // Learning.ItemType enum (0=Vocab, 1=Grammar...)
  "userAnswer": "apple",
  "timeSpentSeconds": 15
}

Response 200: SubmitAnswerResponse { qualityScore, isCorrect, nextReviewDate, ... }
```

**Flow ngầm:**
1. Lấy context thông qua `ICurriculumQueryService.GetItemContextAsync(itemId, curriculumType)`.
2. Gọi `IAiEvaluationService.EvaluateTextAnswerAsync(question, correctAnswer, userAnswer)` → Score (0-100).
3. `ScoreMapper.ToSm2QualityScore(score)` → QualityScore (0-5).
4. Gọi `ItemMastery.Review(qualityScore, utcNow)` → Tính `IntervalDays` theo thuật toán SM-2.
5. Lưu `LearningActivityLog` (kèm `RawScore`, `AiFeedback`).

---

## 4. Submit Audio Answer (Luyện phát âm)

Khi user luyện nói Vocab/Grammar/Sentence trong session.

```
POST /api/v1/learning/sessions/{id:guid}/answer/audio
Auth: Bearer
Content-Type: multipart/form-data
Form: { itemId, itemType (0|1|2), timeSpentSeconds, audioFile }

Response 200: SubmitAudioAnswerResponse { qualityScore, transcription, pronunciationFeedback, ... }
```

**Flow ngầm:**
1. Lấy context thông qua `ICurriculumQueryService`.
2. Gọi `IAiEvaluationService.EvaluatePronunciationAsync(targetText, audioStream, languageCode)` → Score (0-100).
3. `ScoreMapper.ToSm2QualityScore(score)` → QualityScore (0-5).
4. `ItemMastery.Review(qualityScore, utcNow)` → SM-2.
5. Lưu `LearningActivityLog` (userAnswer = `"[AUDIO SUBMISSION]"`, `RawScore`, `AiFeedback`).

---

## 5. Evaluate Conversation Line (Role-play — Stateless)

Chấm từng câu thoại trong hội thoại Role-play. **KHÔNG** ảnh hưởng SM-2.

```
POST /api/v1/learning/conversations/evaluate-line
Auth: Bearer
Content-Type: multipart/form-data
Form: { conversationId, lineId, audioFile }

Response 200: { score (0-100), transcription, pronunciationFeedback }
```

> FE lưu điểm từng câu ở local state. Khi kết thúc hội thoại, tính trung bình rồi gửi qua API §3 với `itemType: 4` (Conversation).

> 📄 **Chi tiết đầy đủ:** Xem [learning_practice_workflows.md](../../common/learning_practice_workflows.md)

---

## 6. Submit Standalone Review (Không cần session)

Dùng khi ôn tập thẻ flashcard rời rạc (Ví dụ chức năng Daily Review).

```
POST /api/v1/learning/reviews
Auth: Bearer
Body: { "itemId": "guid", "type": 0, "qualityRating": 4 }

Response 204
```

> Ở endpoint này, client gửi trực tiếp `qualityRating` (ví dụ user tự đánh giá mức độ khó dễ trên màn hình).

---

## 7. Submit Session (Hoàn thành)

```
PUT /api/v1/learning/sessions/{id:guid}/submit
Auth: Bearer
Body: { "score": 85 }

Response 204
```

> Kích hoạt `StudySessionCompletedDomainEvent`, kích hoạt tính toán XP và Streak.
