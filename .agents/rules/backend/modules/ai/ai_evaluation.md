---
trigger: manual
---

# AI — Evaluation

> **Scope:** Chấm điểm phát âm và ngữ pháp (Text & Audio)  
> **Endpoints:** `/api/v1/ai/evaluate*`  
> **@mention khi làm:** chấm điểm câu trả lời, SM-2 quality score, chấm điểm audio

---

## 1. SM-2 Quality Score

AI trả về cấu trúc JSON chứa `QualityScore` (0-5). Score này được Learning module dùng cho thuật toán Spaced Repetition:

| Score | Ý nghĩa |
|---|---|
| 0 | Blackout / Không trả lời / Trống |
| 1 | Hoàn toàn sai |
| 2 | Sai một phần |
| 3 | Đúng nhưng có lỗi (sai chính tả, sai ngữ pháp nhỏ) |
| 4 | Đúng, có thể cải thiện chút ít |
| 5 | Hoàn hảo |

---

## 2. Evaluate Text

```
POST /api/v1/ai/evaluate
Auth: Bearer + RateLimiting("ai-scoring")
Body: {
  "userInput": "I go to school yesterday",
  "itemId": "guid | null",
  "itemType": 1 | null,
  "itemContext": "string | null"
}

Response 200: { qualityScore, isCorrect, suggestedCorrection, explanation }
```

**Context Resolution:**
- **Cách 1 (Ưu tiên):** Truyền `itemId` + `itemType`. Handler sẽ gọi `ICurriculumQueryService.GetItemContextAsync` để lấy dữ liệu làm bối cảnh chấm.
- **Cách 2:** Truyền trực tiếp chuỗi `itemContext`.

---

## 3. Evaluate Audio

```
POST /api/v1/ai/evaluate-audio (multipart/form-data)
Auth: Bearer + RateLimiting("ai-scoring")
Fields:
- audioFile (IFormFile, max 10MB)
- itemId, itemType, itemContext (như Text)

Response 200: { transcription, qualityScore, isCorrect, suggestedCorrection, explanation }
```

---

## 4. ItemType Mapping (Quan trọng)

Khi module Learning gọi đánh giá thông qua `IAiEvaluationService`, `itemType` của Learning khác với của Curriculum. Module AI có trách nhiệm map:

| Learning.ItemType (int) | Enum Name | CurriculumItemType (int) |
|---|---|---|
| 0 | Vocabulary | 1 |
| 1 | Grammar | 2 |
| 2 | ExampleSentence | 4 |
| 3 | Topic | 6 |
| 4 | Conversation | 5 |
| 5 | Exercise | 3 |

> ⚠️ Không được ép kiểu `(CurriculumItemType)itemType` trực tiếp. Phải dùng switch expression.
