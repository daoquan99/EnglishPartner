# Learning API — Dashboard & Leaderboard

> **Auth:** Bearer token required

---

## 1. Dashboard (Cá nhân)

```http
GET /api/v1/learning/dashboard
```

> `UserId` tự động lấy từ Bearer token — KHÔNG truyền qua params.

**Response `200 OK`:**
```json
{
  "userId": "guid",
  "currentLevel": "A2",
  "totalXp": 1250,
  "currentStreak": 7,
  "longestStreak": 14
}
```

**Error:** `404` — `Learning.Profile.NotFound` (profile chưa được tạo — race condition khi mới đăng ký)

---

## 2. Leaderboard (Xếp hạng)

```http
GET /api/v1/learning/leaderboard?count=50
```

| Query Param | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `count` | int | ❌ | 50 | Số lượng top users (tối đa 100) |

**Response `200 OK`:**
```json
[
  {
    "userId": "guid",
    "displayName": "John Doe",
    "totalXp": 5000,
    "currentLevel": "B2",
    "currentStreak": 30
  }
]
```

> ⚡ Cache 5 phút. Dữ liệu public.
