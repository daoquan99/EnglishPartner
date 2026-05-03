---
trigger: manual
---

# AI — Admin & Logs

> **Scope:** AiRequestLogs (lịch sử gọi AI)  
> **Endpoint:** `/api/v1/admin/ai/request-logs`  
> **Auth:** Bearer + `ai.logs.read` (group-level, chỉ permission string — KHÔNG có "Admin" policy riêng)  
> **@mention khi làm:** AI request logs, lịch sử Gemini

---

## 1. GET /request-logs

Lấy lịch sử tất cả các request được gửi tới Gemini (Chat, Evaluation, Generate, v.v.).

```
GET /api/v1/admin/ai/request-logs
Query Params:
- page (default 1)
- pageSize (default 20)
- requestType (string, e.g. "Evaluate", "Chat")
- isSuccess (bool)
- from (DateTime)
- to (DateTime)

Response 200: Paginated list of logs (Prompt, Response, LatencyMs, Error).
```

> Các bản ghi này được hệ thống tạo tự động trong `ExecuteGeminiRequestAsync()`.
