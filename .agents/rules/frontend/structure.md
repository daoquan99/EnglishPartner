# EnglishPartner Web — Project Structure

> Cập nhật: 2026-04-24. Agent phải update file này khi tạo/xóa file.

```
src/
├── app/
│   ├── globals.css                          # Tailwind v4 + CSS variables + theme
│   ├── layout.tsx                           # Root layout (fonts, metadata, providers)
│   ├── page.tsx                             # Landing redirect
│   ├── providers.tsx                        # React Query + Google OAuth providers
│   ├── (auth)/
│   │   ├── layout.tsx                       # Auth layout (centered card)
│   │   ├── login/page.tsx                   # Login page
│   │   └── register/page.tsx                # Register page
│   ├── (admin)/
│   │   ├── layout.tsx                       # Admin layout (sidebar)
│   │   └── admin/
│   │       ├── page.tsx                     # Admin dashboard
│   │       ├── users/page.tsx               # User management page
│   │       ├── roles/page.tsx               # Role management page
│   │       └── learning/page.tsx            # Learning admin management page
│   ├── (dashboard)/
│   │   ├── layout.tsx                       # Learner layout (navbar)
│   │   ├── dashboard/page.tsx               # Learner dashboard
│   │   ├── dashboard/curriculum/page.tsx    # Topic explorer (Learn page)
│   │   ├── dashboard/curriculum/topics/[id]/page.tsx  # Topic detail
│   │   ├── dashboard/vocabulary/page.tsx    # Vocabulary browser
│   │   ├── dashboard/vocabulary/[id]/page.tsx  # Vocabulary detail
│   │   ├── dashboard/grammar/page.tsx       # Grammar browser
│   │   ├── dashboard/grammar/[id]/page.tsx  # Grammar lesson
│   │   ├── dashboard/conversations/page.tsx # Conversation browser
│   │   ├── dashboard/conversations/[id]/page.tsx  # Conversation reader
│   │   ├── dashboard/exercises/[id]/page.tsx  # Exercise solver
│   │   ├── dashboard/review/page.tsx        # Flashcard review page
│   │   ├── dashboard/study/page.tsx         # Study session page
│   │   ├── onboarding/page.tsx              # Onboarding wizard
│   │   └── settings/page.tsx                # User settings
├── features/
│   ├── auth/
│   │   ├── api/
│   │   │   ├── auth.api.ts                  # Login, register, refresh, logout, changePassword API calls
│   │   │   └── useAuth.ts                   # React Query hooks (useLogin, useRegister, useLogout, useSocialLogin, useChangePassword)
│   │   ├── components/
│   │   │   ├── GoogleLoginButton.tsx        # Google OAuth button
│   │   │   ├── LoginForm.tsx                # Email/password login form
│   │   │   └── RegisterForm.tsx             # Registration form
│   │   ├── hooks/                           # (empty)
│   │   └── types/
│   │       ├── auth.types.ts                # LoginRequest, RegisterRequest, AuthResponse, ChangePasswordRequest, etc.
│   │       └── auth.schemas.ts              # Zod schemas for login/register forms
│   ├── identity/
│   │   ├── api/
│   │   │   ├── admin.api.ts                 # Admin user CRUD + Role CRUD + Permissions APIs
│   │   │   ├── identity.api.ts              # Profile, external-logins, streak APIs
│   │   │   ├── useAdmin.ts                  # React Query hooks for admin users + roles + permissions
│   │   │   └── useIdentity.ts               # React Query hooks for profile, external-logins, streak
│   │   ├── components/
│   │   │   ├── ManageRoleDialog.tsx          # Dialog to assign roles to user
│   │   │   ├── OnboardingWizard.tsx          # Multi-step onboarding wizard
│   │   │   ├── ProfileSettings.tsx           # Profile settings orchestrator
│   │   │   ├── ProfileFormTab.tsx            # Profile edit form tab
│   │   │   ├── SecurityTab.tsx               # Security settings tab
│   │   │   ├── ExternalLoginsTab.tsx         # Linked accounts tab
│   │   │   ├── StreakTab.tsx                 # Streak stats tab
│   │   │   ├── RoleFormDialog.tsx            # Dialog for create/edit role (name + description)
│   │   │   ├── RoleManagement.tsx            # Role list + actions orchestrator
│   │   │   ├── RolePermissionsDialog.tsx     # Dialog to toggle permissions for a role
│   │   │   ├── RoleTableRow.tsx              # Single role row with actions
│   │   │   ├── UserFormDialog.tsx            # Dialog for create/edit user
│   │   │   ├── UserManagement.tsx            # User list + actions orchestrator
│   │   │   └── UserTableRow.tsx              # Single role row with dropdown actions
│   │   └── types/
│   │       ├── admin.types.ts               # AdminUserResponse, GetUsersParams, RoleResponse, PermissionResponse, etc.
│   │       ├── identity.types.ts            # OnboardingQuestion, UserProfileResponse, UpdateProfileRequest, StreakResponse, ExternalLoginResponse
│   │       └── identity.schemas.ts          # Zod schemas for profile forms (legacy onboarding schema)
│   ├── curriculum/                          # Curriculum content management
│   │   ├── api/
│   │   │   ├── curriculum.api.ts            # Axios calls for topics, vocabularies, grammars, exercises, conversations, examples, AI
│   │   │   └── useCurriculum.ts             # React Query hooks for Curriculum endpoints
│   │   ├── components/                      # Curriculum UI components
│   │   │   ├── TopicManagement.tsx
│   │   │   ├── TopicFormDialog.tsx
│   │   │   ├── VocabularyManagement.tsx
│   │   │   ├── VocabularyFormDialog.tsx
│   │   │   ├── AIGenerateVocabularyDialog.tsx
│   │   │   ├── GrammarManagement.tsx
│   │   │   ├── GrammarFormDialog.tsx
│   │   │   ├── AIGenerateGrammarDialog.tsx
│   │   │   ├── ConversationManagement.tsx
│   │   │   ├── ConversationFormDialog.tsx
│   │   │   ├── ConversationLinesManager.tsx
│   │   │   ├── AIGenerateConversationDialog.tsx
│   │   │   ├── ExampleSentenceManagement.tsx
│   │   │   ├── ExampleSentenceFormDialog.tsx
│   │   │   ├── AIGenerateExampleSentenceDialog.tsx
│   │   │   ├── ExerciseManagement.tsx
│   │   │   ├── ExerciseFormDialog.tsx
│   │   │   ├── ExerciseOptionsManager.tsx
│   │   │   ├── AIGenerateExerciseDialog.tsx
│   │   │   ├── LanguageManagement.tsx
│   │   │   └── LanguageFormDialog.tsx
│   │   ├── learner/                          # Learner-facing curriculum UI
│   │   │   ├── TopicExplorer.tsx             # Topic grid with search + level filter
│   │   │   ├── TopicCard.tsx                 # Single topic card
│   │   │   ├── TopicDetailView.tsx           # Topic detail with linked content tabs
│   │   │   ├── VocabularyBrowser.tsx         # Paginated vocabulary list with filters
│   │   │   ├── VocabularyWordCard.tsx        # Single word card
│   │   │   ├── VocabularyDetailView.tsx      # Word detail with audio/synonyms/examples
│   │   │   ├── GrammarBrowser.tsx            # Grammar list with level filter
│   │   │   ├── GrammarLessonView.tsx         # Grammar lesson with formula/examples
│   │   │   ├── ConversationBrowser.tsx       # Conversation grid with filters
│   │   │   ├── ConversationReader.tsx        # Turn-based role-play with session lifecycle
│   │   │   ├── ConversationLineCard.tsx      # Single conversation line (AI/User turn)
│   │   │   ├── ConversationComplete.tsx      # Conversation role-play completion summary
│   │   │   ├── ExerciseSolver.tsx            # Exercise renderer (routes to type components)
│   │   │   ├── MultipleChoiceExercise.tsx    # Multiple choice option buttons
│   │   │   ├── FillInBlankExercise.tsx       # Fill-in-the-blank input
│   │   │   └── ExerciseFeedback.tsx          # Exercise result feedback
│   │   ├── constants/
│   │   │   └── exercise-types.ts
│   │   └── types/                   # Curriculum types and schemas, VocabularyResponse, Generate*Request, etc.
│   ├── gamification/                        # (scaffolded, empty)
│   │   ├── api/
│   │   ├── components/
│   │   └── types/
│   ├── ai/                                  # AI services (TTS, etc.)
│   │   ├── api/
│   │   │   ├── ai.api.ts                    # TTS, Gemini TTS, voices API calls
│   │   │   └── useAi.ts                     # React Query hooks for AI endpoints
│   │   └── types/
│   │       └── ai.types.ts                  # TtsRequest, GeminiTtsRequest, TtsVoice, GenerateAudioResponse
│   ├── learning/                            # Learning module (dashboard, review, study, admin)
│   │   ├── api/
│   │   │   ├── learning.api.ts              # Axios calls for dashboard, reviews, sessions, admin
│   │   │   └── useLearning.ts               # React Query hooks for Learning endpoints
│   │   ├── components/
│   │   │   ├── LearnerDashboard.tsx          # Dashboard orchestrator (stats + leaderboard + CTAs)
│   │   │   ├── DashboardStatsCards.tsx       # XP, Level, Streak, Items Due stat cards
│   │   │   ├── LeaderboardCard.tsx           # Top learners leaderboard
│   │   │   ├── FlashcardReview.tsx           # Spaced repetition flashcard review
│   │   │   ├── ReviewEmptyState.tsx          # "All caught up" celebration state
│   │   │   ├── StudySessionFlow.tsx          # Guided study session orchestrator
│   │   │   ├── AnswerFeedbackCard.tsx        # AI evaluation feedback card
│   │   │   ├── SessionCompleteCard.tsx       # Session summary card
│   │   │   ├── LearningAdminPanel.tsx        # Admin XP/mastery/session management
│   │   │   └── AdjustXpDialog.tsx            # Dialog for adjusting user XP
│   │   ├── hooks/
│   │   │   └── useSessionPing.ts             # Session keep-alive ping hook (3 min interval)
│   │   └── types/
│   │       └── learning.types.ts             # DashboardResponse, ReviewDueItem, SessionTypes, etc.
│   └── notifications/                       # (scaffolded, empty)
│       ├── api/
│       ├── components/
│       └── hooks/
├── shared/
│   ├── components/
│   │   ├── AdminSidebar.tsx                 # Admin sidebar navigation
│   │   ├── AudioRecorder.tsx                # Microphone recorder with pulse animation + timer
│   │   ├── ConfirmActionDialog.tsx          # Reusable confirmation dialog
│   │   ├── LearnerNavbar.tsx                # Learner top navigation bar
│   │   ├── StarRating.tsx                   # Star rating display (0-5 stars)
│   │   └── ThemeToggle.tsx                  # Dark/light mode toggle
│   ├── hooks/
│   │   └── useDebounce.ts                   # Debounce hook for search inputs
│   └── utils/
│       ├── cn.ts                            # clsx + tailwind-merge helper
│       └── index.ts                         # Barrel export
├── lib/
│   ├── axios.ts                             # Axios instance + interceptors (auth, timezone, refresh)
│   ├── query-client.ts                      # React Query client config
│   ├── signalr.ts                           # SignalR hub connection setup
│   └── zustand/
│       ├── auth.store.ts                    # Auth state (accessToken, user, isAuthenticated)
│       ├── notification.store.ts            # Notification state (unread count, list)
│       └── theme.store.ts                   # Theme state (dark/light mode)
├── ui/                                      # shadcn/ui components — DO NOT EDIT
│   ├── alert-dialog.tsx
│   ├── badge.tsx
│   ├── button.tsx
│   ├── card.tsx
│   ├── checkbox.tsx
│   ├── dialog.tsx
│   ├── dropdown-menu.tsx
│   ├── form.tsx
│   ├── input.tsx
│   ├── label.tsx
│   ├── progress.tsx
│   ├── radio-group.tsx
│   ├── select.tsx
│   ├── slider.tsx
│   ├── table.tsx
│   ├── textarea.tsx
│   └── tabs.tsx
└── middleware.ts                             # Route protection (auth redirect, role checks)
```
