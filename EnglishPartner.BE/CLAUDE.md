@AGENTS.md

# Backend — ghi chú cho Claude Code

- Kiến trúc mục tiêu: @.agents/architecture.md
- Build: `dotnet build EnglishLiveTutor.sln` · Test: `dotnet test EnglishLiveTutor.sln` (chạy trong `EnglishPartner.BE`).
- Test hiện dùng EF InMemory: pass không chứng minh SQL Server, transaction thật, SignalR hay provider Gemini hoạt động.
- Không chạy API, migrate/seed SQL hay gọi Gemini trả phí chỉ để kiểm tra tài liệu.
