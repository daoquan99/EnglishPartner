@AGENTS.md

# Frontend — ghi chú cho Claude Code

- Design system: @.agents/design-system.md
- Build: `npm run build` · Test có giới hạn: `npm run test -- <đường-dẫn-test>` (chạy trong `EnglishPartner.FE`).
- Request luôn đi qua `src/shared/api/httpClient.ts`; type contract ở `src/shared/api/openapi.types.ts`.
- Build/type-check không chứng minh API, SignalR, microphone hay provider AI hoạt động thật.
