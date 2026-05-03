---
trigger: manual
---

# Learning — Dashboard & Leaderboard

> **Scope:** Lấy thống kê cá nhân, ôn tập và xếp hạng  
> **Auth:** Bearer  
> **@mention khi làm:** dashboard, learning progress, leaderboard, due reviews

---

## 1. Dashboard (Cá nhân)

```
GET /api/v1/learning/dashboard
Auth: Bearer

Response 200: { TotalXp, CurrentLevel, CurrentStreak, LongestStreak, MasteredItemsCount... }
```

> `UserId` tự động được lấy từ `ICurrentUserService` trong Query Handler.

---

## 2. Due Reviews (Từ vựng cần ôn hôm nay)

Dựa trên ngày `NextReviewDate` của thuật toán SM-2.
```
GET /api/v1/learning/reviews/due
Auth: Bearer

Response 200: List<DueReviewDto>
```

> Dùng `TimeProvider.GetUtcNow()` để query những record có `NextReviewDate <= utcNow`.

---

## 3. Leaderboard

```
GET /api/v1/learning/leaderboard?count=50
Auth: Bearer

Response 200: List<LeaderboardEntryResponse>
```

> Đây là dữ liệu public/chia sẻ. Sử dụng `ICacheableQuery` với TTL 5 phút. Cache key: `learning:leaderboard:{count}`.
