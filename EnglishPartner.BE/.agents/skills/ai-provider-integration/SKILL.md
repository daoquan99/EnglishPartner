---
name: ai-provider-integration
description: Add, change or review calls to Gemini (HTTP and Live) and the Antigravity CLI runner — BYO API key handling, timeouts, retries, rate limits, prompt building, output validation and cost control. Use when touching Infrastructure/Ai, lesson grading/generation, pronunciation assessment or vocabulary generation.
---

# AI provider integration

Read [AGENTS.md](../../../AGENTS.md), [ADR-0003 BYO key](../../../../docs/adr/0003-byo-api-key.md) and items S7/S9 in the [2026-10-28 review](../../../../docs/reviews/2026-10-28-security-performance-architecture.md).

## Map

- Ports: `Application/Common/Interfaces/IAiServices.cs`, `ILessonAiGenerator.cs`, `IRolePlayReplyProvider.cs`, `IAgyCliRunner.cs`.
- Adapters: `Infrastructure/Ai/*` (Gemini HTTP generators/assessors, `GeminiHttpHelper` retry, `GeminiApiKeyResolver`, `AgyCliRunner`), `Infrastructure/Lessons/LessonAiGrader.cs`, `DictationAiReviewer.cs`.
- Options: `Ai:*` sections bound in `Infrastructure/DependencyInjection.cs`; typed `HttpClient` per adapter with timeout.

## Rules

- **Key (BYO):** the learner's key arrives with the request or live-session start; resolve via `GeminiApiKeyResolver`. Use it for that call/session only: never log it (including in exception messages or request dumps), never persist it, clear it when the session ends. Send it as the `x-goog-api-key` header, never in a URL. Server environment keys are a fallback for admin/back-office features only if the plan says so.
- **Limits:** every AI endpoint has a per-user rate-limit policy and a bounded timeout; retries go through `GeminiHttpHelper` (bounded attempts, honours `Retry-After`). Do not add unbounded loops or nested retries.
- **Transactions:** call the provider first, validate, then open the SQL transaction to persist (root rule 8).
- **Output validation:** parse provider JSON defensively; validate against the expected schema (`ILessonCandidateValidator` and similar) before mutating domain state; on invalid output return a typed failure, not partial data.
- **Prompt input:** user text is data — delimit it clearly in prompts and keep system instructions server-side; do not let request fields override the system prompt unless the feature is an explicit admin setting.
- **Model codes:** come from configuration or the `AiModelCatalog`, validated against an allow-list (see `AntigravityCli:AllowedModels`); never pass an arbitrary client string straight to a CLI or URL.
- **CLI runner:** arguments passed as an argument list, never a concatenated shell string; working directory and timeout fixed by options.

## Verification

Unit-test adapters with a fake `HttpMessageHandler` (success, 429 with `Retry-After`, 5xx, malformed JSON, timeout). Do not call the paid provider in tests or verification; state in the report that real-provider behaviour is unverified.
