---
name: domain-aggregate-design
description: Design or review DDD aggregates, owned entities and invariants in EnglishLiveTutor.Domain. Use when changing business state or the aggregate-centric domain layout.
---

# Domain aggregate design

Read [AGENTS.md](../../../AGENTS.md) and [Domain decisions](../../architecture.md#2-domain-structure). The current source has 19 AggregateRoot subclasses; inheritance alone does not settle their business lifecycle.

Identify the operation's consistency boundary, root, owned children, external aggregate IDs and invariant before choosing folders. Put multiple roots in separate aggregate folders within an area; keep small single-root areas simple. Keep existing Entity suffixes during initial structural moves.

Use root behavior for rules spanning children. Preserve VocabularySense IDs referenced by plans/progress/assessments. DailyVocabularyItem belongs to its plan; progress is independent per user/sense. User owns role assignments/tokens and Role owns permission assignments in the baseline model.

Validate proposed state before replacing existing collections. Check rubric unique codes, ranges and weights, assessment score consistency, legal review ratings and plan completion transitions. Receive now/learning date explicitly for business transitions. Domain must not resolve HttpContext, EF queries or provider payloads.

Add value objects where they remove divergent business rules; preserve wire/storage values through mappings. Separate deletion capability and history/cache lifecycle when warranted rather than assuming every aggregate can be deleted/restored. Domain events are optional.

For realtime conversations, consult the streaming exception in the architecture decisions before forcing full-history loading or promoting messages to roots. Record the transaction/lifecycle rationale.

If implementation is authorized, use focused invariant tests, then verify EF mappings/callers affected by visibility or namespace changes. For review/design-only requests, produce the model and acceptance scenarios without editing source.
