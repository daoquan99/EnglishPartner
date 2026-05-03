# Technical Decisions

## Identity Module
- **Onboarding Wizard:** Fully dynamic — fetches questions from `GET /onboarding/questions`, renders 1 question per step with RadioGroup. No hardcoded options.
- **Profile dropdowns:** Options for languages, proficiency, goal, daily minutes, reminders all fetched from onboarding questions API (same source of truth as wizard).
- **utcOffsetHours:** Auto-detected from browser on profile save via `-(new Date().getTimezoneOffset() / 60)`.
- **ProficiencyLevel/LearningGoal:** Plain strings (not enums), values come from backend questions API.
- **Role assignment UI:** Multi-checkbox with optimistic updates per checkbox, revert on API failure. No "Save" button — each toggle calls API immediately.
- **Permission toggle UI:** Same pattern as role assignment — optimistic per checkbox, revert on failure.
- **AdminUserResponse.roles:** Array of `{ id, name }` objects (not plain strings).
- **Admin Panel link:** Conditionally rendered in LearnerNavbar based on `isAdmin` from auth store.
- **Reset Password flow:** Uses `mutateAsync` to capture returned `newPassword`, displayed in a copyable dialog.
- **Tab active state:** Explicit `data-[state=active]:bg-[var(--color-primary)]` on TabsTrigger because shadcn defaults don't work with CSS variable theming.
- **SelectContent styling:** Must always add `bg-[var(--color-surface)] border-[var(--color-border)]` to prevent dropdown blending into background.

## Learning Module
- **Client-side pagination for reviews:** API returns flat array, FE handles pagination/iteration locally.
- **Session ping:** 3-minute interval via `useSessionPing` hook, auto-cleared on unmount.
- **Score calculation:** FE computes `(correctCount / totalItems) * 100`, sends to BE on session complete.
- **Profile.NotFound handling:** Dashboard shows onboarding CTA instead of error page when 404.
- **Cross-module item fetching:** FlashcardReview shows item metadata (id, type, interval). Full curriculum content fetching deferred to future iteration.
- **Admin Learning:** Manual userId input (no user search integration yet). All destructive actions gated by `ConfirmActionDialog`.

## Learner Curriculum
- **Directory separation:** Learner components in `curriculum/learner/`, admin in `curriculum/components/`.
- **Zero new API code:** All learner components reuse existing `useCurriculum.ts` hooks (read-only endpoints).
- **Topic as hub:** TopicDetailView is the central navigation point — fetches linked vocab/grammar/conversations/exercises via `topicId` filter.
- **Server-side filtering:** Conversation and vocabulary filters pass params to API (not client-side), matching the admin fix.
- **Exercise types:** Start with MultipleChoice + FillInTheBlank renderers. ExerciseSolver routes by `type` string. Other types fall back to FillInBlank.
- **Conversation UI:** Turn-based role-play. `lines[0].speaker` = AI, others = User. AI lines auto-play audio, User lines show AudioRecorder + inline evaluation feedback. Session lifecycle: start → evaluate lines → avgScore → chốt answer (itemType:4) → submit.
- **Vocabulary pagination:** Uses server-side pagination (`page`/`pageSize` params) — only entity with paged response.

## AI Module
- **Separate module:** `features/ai/` is a cross-cutting module for TTS. Used by Conversation role-play and potentially Vocabulary/Grammar audio later.
- **TTS fallback:** If `audioUrl` is null on a conversation line, call `/ai/tts` on-the-fly. Blob URL created in-component (not cached globally).
- **FormData Content-Type:** Never set `Content-Type: multipart/form-data` manually — browser auto-adds boundary. Manually setting it breaks the request.
- **Admin audio generate:** Buttons in ConversationLinesManager per-line + bulk "Generate All Audio". Invalidates conversation query on success.

