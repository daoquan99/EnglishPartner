# Learning API — Spaced Repetition (Reviews)

> **Auth:** Bearer token required

---

## 1. Lấy items cần ôn hôm nay

```http
GET /api/v1/learning/reviews/due
```

> Server tự lấy `utcNow` — FE không cần truyền thời gian.

**Response `200 OK`:**
```json
[
  {
    "itemId": "guid",
    "type": 0,
    "easinessFactor": 2.5,
    "intervalDays": 6,
    "repetitions": 3
  }
]
```

**`type` values (ItemType):**

| Value | Meaning |
|---|---|
| 0 | Vocabulary |
| 1 | Grammar |
| 2 | ExampleSentence |
| 3 | Topic |
| 4 | Conversation |
| 5 | Exercise |

> 💡 Dùng `itemId` + `type` để fetch chi tiết từ Curriculum API tương ứng.

---

## 2. Submit Standalone Review

```http
POST /api/v1/learning/reviews
```

> ⚠️ Dùng khi ôn tập **NGOÀI** study session (ví dụ: flashcard mode).

**Request Body:**
```json
{
  "itemId": "guid",
  "type": 0,
  "qualityRating": 4
}
```

| Field | Type | Required | Mô tả |
|---|---|---|---|
| `itemId` | guid | ✅ | ID curriculum item |
| `type` | int | ✅ | ItemType enum (0-5) |
| `qualityRating` | int | ✅ | SM-2 quality score (0-5) |

**Quality Rating scale:**

| Score | Ý nghĩa |
|---|---|
| 0 | Blackout / Không nhớ |
| 1 | Hoàn toàn sai |
| 2 | Sai một phần |
| 3 | Đúng nhưng có lỗi nhỏ |
| 4 | Đúng, có thể cải thiện |
| 5 | Hoàn hảo |

**Response `204 No Content`**

> Standalone review cho **2 XP** mỗi lần.
