# AI API — Evaluation (Chấm điểm)

> **Auth:** Bearer token required  
> **Rate Limit:** `ai-scoring` — 50 requests/giờ

---

## 1. Evaluate Text

```http
POST /api/v1/ai/evaluate
```

**Request Body:**
```json
{
  "userInput": "I go to school yesterday",
  "itemId": "guid or null",
  "itemType": 1,
  "itemContext": "string or null"
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `userInput` | string | ✅ | Câu trả lời của user |
| `itemId` | guid | ❌ | ID curriculum item (ưu tiên) |
| `itemType` | int | ❌ | CurriculumItemType (1-6) |
| `itemContext` | string | ❌ | Context text trực tiếp (fallback) |

> 💡 **Context Resolution:**  
> - **Cách 1 (Ưu tiên):** Truyền `itemId` + `itemType` → server tự fetch context từ Curriculum  
> - **Cách 2:** Truyền `itemContext` trực tiếp  
> - Phải có ít nhất 1 trong 2. Không có gì → `400 AI.Evaluate.MissingContext`

**Response `200 OK`:**
```json
{
  "qualityScore": 3,
  "isCorrect": false,
  "suggestedCorrection": "I went to school yesterday",
  "explanation": "'Go' should be 'went' (past tense) because 'yesterday' indicates past time."
}
```

---

## 2. Evaluate Audio

```http
POST /api/v1/ai/evaluate-audio
Content-Type: multipart/form-data
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `audioFile` | IFormFile | ✅ | Audio file (max 10MB) |
| `itemId` | guid | ❌ | ID curriculum item |
| `itemType` | int | ❌ | CurriculumItemType |
| `itemContext` | string | ❌ | Context text |

**Response `200 OK`:**
```json
{
  "transcription": "I go to school yesterday",
  "qualityScore": 3,
  "isCorrect": false,
  "suggestedCorrection": "I went to school yesterday",
  "explanation": "Pronunciation was clear but grammar error detected."
}
```

---

## Errors

| Code | HTTP | Khi nào |
|---|---|---|
| `AI.Evaluate.ItemNotFound` | 404 | itemId không tồn tại |
| `AI.Evaluate.MissingContext` | 400 | Không có itemId/itemType VÀ không có itemContext |
| `AI.Evaluation.ParseFailed` | 502 | AI trả JSON lỗi |
| `AI.Evaluation.ServiceUnavailable` | 503 | AI timeout |
| `AI.EvaluateAudio.ReadFailed` | 400 | Không đọc được audio |
| `AI.EvaluateAudio.UploadFailed` | 500 | Không save được audio |
