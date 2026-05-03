# Identity API — Streak

> **Lưu ý kiến trúc:** Logic xử lý Streak thực chất thuộc Module Learning, nhưng API vẫn do Identity expose tạm thời.

## 1. Lấy thông tin Streak hiện tại
`GET /api/v1/streaks`
**Headers:** `Authorization: Bearer <accessToken>`
**Response 200 OK:**
```json
{
  "currentStreak": 5, // Số ngày liên tiếp
  "longestStreak": 12, // Kỷ lục streak
  "lastActivityDate": "2025-04-21", // YYYY-MM-DD
  "freezeTokens": 2 // Lượt bảo vệ chuỗi
}
```

## 2. Check-in (Điểm danh / Ghi nhận học tập)
`POST /api/v1/streaks/check-in`
**Headers:** `Authorization: Bearer <accessToken>`
**Mô tả:** Gọi khi user hoàn thành task học để tăng streak của ngày hôm nay.
**Body:** Không có
**Response 200 OK:**
```json
{
  "currentStreak": 6,
  "longestStreak": 12,
  "isNewRecord": false
}
```

**Lỗi:** 
- `Identity.Streak.AlreadyCheckedIn` (400) - Hôm nay đã check in rồi.
