# AI API — Admin (Request Logs)

> **Auth:** Bearer + `ai.logs.read` permission  
> **Base URL:** `/api/v1/admin/ai`

---

## 1. Lấy lịch sử AI Request Logs

```http
GET /api/v1/admin/ai/request-logs?page=1&pageSize=20&requestType=Evaluate&isSuccess=true&from=2026-01-01&to=2026-04-23
```

| Query Param | Type | Required | Default | Mô tả |
|---|---|---|---|---|
| `page` | int | ❌ | 1 | Trang hiện tại |
| `pageSize` | int | ❌ | 20 | Số item mỗi trang |
| `requestType` | string | ❌ | null | Filter: `"Evaluate"`, `"Chat"`, `"Generate"`, `"TTS"`, `"STT"` |
| `isSuccess` | bool | ❌ | null | Filter theo success/fail |
| `from` | DateTime | ❌ | null | Từ ngày |
| `to` | DateTime | ❌ | null | Đến ngày |

**Response `200 OK`:**
```json
{
  "items": [
    {
      "id": "guid",
      "requestType": "Evaluate",
      "inputText": "I go to school yesterday",
      "outputText": "{\"qualityScore\":3,...}",
      "modelName": "gemini-2.0-flash",
      "inputTokens": 150,
      "outputTokens": 80,
      "latencyMs": 1200,
      "isSuccess": true,
      "errorMessage": null,
      "userId": "guid",
      "createdAt": "2026-04-23T10:00:00Z"
    }
  ],
  "totalCount": 500,
  "page": 1,
  "pageSize": 20
}
```

> Các bản ghi được hệ thống tạo tự động mỗi khi gọi Gemini API.
