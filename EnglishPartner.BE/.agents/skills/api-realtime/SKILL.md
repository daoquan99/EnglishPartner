---
name: api-realtime
description: Review or change Minimal API endpoints and SignalR hubs in EnglishLiveTutor.Api, preserving transport contracts and moving workflows behind Application ports.
---

# API and realtime adapters

Read [AGENTS.md](../../../AGENTS.md), [API decisions](../../architecture.md#5-api), and R1/R5/R6 in [the plan](../../../docs/backend-refactor-plan.md).

HTTP endpoints bind requests, enforce transport authentication/authorization, invoke an Application use case and map a response. Do not copy direct EF deletion from VoiceEndpoints. Composition-root Infrastructure references are expected; concrete provider-manager dependencies in hubs are migration debt.

Use authenticated subject for personal operations and check resource ownership in the use case. Neither X-User-Id, a query userId, a conversation ID nor audit CreatedBy establishes authorization. Keep public catalogs public when intended; handle anonymous demo as an explicit product decision.

For SignalR, authorize the resource before joining a group or starting/streaming/stopping a session. Use an Application session port; keep websocket protocol in Infrastructure. Do not create CQRS commands per audio packet. Define connection/session ownership so disconnect does not stop someone else's valid session; cancel/join/dispose owned work and remove retained keys/state on final cleanup.

Before changes, inventory `/hubs/tutor`, `/hubs/transcribe`, `/hubs/translate`, method names, event names, payloads and argument order. Preserve those through structural work, alongside HTTP routes, JSON fields, `{ error }`, cookie names and X-* pagination headers. Stricter parsing, ProblemDetails and default paging are explicit compatibility changes.

When implementation is authorized, verify real transport binding/middleware with HTTP/SignalR checks appropriate to the slice. Existing EndpointCqrsTests call handlers and do not establish route/auth/stream correctness. Live Google calls are a separate gate, not an automatic part of endpoint verification.
