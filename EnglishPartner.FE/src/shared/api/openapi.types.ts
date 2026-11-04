/**
 * OpenAPI-aligned TypeScript Contracts for EnglishLiveTutor Backend (.NET 10).
 * Rule: API types are centralized from OpenAPI schema to prevent FE/BE drift.
 */

export type CefrLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2';

export type ConversationModeCode =
  | 'practice_interview'
  | 'real_interview'
  | 'hr_interview'
  | 'technical_interview'
  | 'system_design'
  | 'behavioral_star'
  | 'stress_interview'
  | 'rapid_fire'
  | 'deep_dive'
  | 'english_coach'
  | 'pronunciation_coach'
  | 'resume_deep_dive'
  | 'daily_conversation'
  | 'daily_standup'
  | 'technical_discussion'
  | 'problem_solving'
  | 'client_meeting'
  | 'debate_mode'
  | 'roleplay_mode'
  | string;

export type TeacherVoiceName =
  | 'Aoede'
  | 'Puck'
  | 'Kore'
  | 'Charon'
  | 'Fenrir'
  | 'Leda'
  | 'Zephyr'
  | 'Orus'
  | 'Callirrhoe'
  | 'Autonoe'
  | 'Enceladus'
  | 'Iapetus'
  | 'Umbriel'
  | 'Algieba'
  | 'Despina'
  | 'Erinome'
  | 'Algenib'
  | 'Rasalgethi'
  | 'Laomedeia'
  | 'Achernar'
  | 'Alnilam'
  | 'Schedar'
  | 'Gacrux'
  | 'Pulcherrima'
  | 'Achird'
  | 'Zubenelgenubi'
  | 'Vindemiatrix'
  | 'Sadachbia'
  | 'Sadaltager'
  | 'Sulafat'
  | string;

// --- Identity & RBAC ---
export interface UserProfileDto {
  id: string;
  username: string;
  email: string;
  fullName: string;
  avatarUrl?: string | null;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  timeZoneId: string;
  isActive: boolean;
  lastLoginAtUtc?: string | null;
  roles: string[];
  permissions: string[];
}

export interface AuthResponseDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserProfileDto;
}

export interface LoginRequestDto {
  usernameOrEmail: string;
  password: string;
  deviceInfo?: string;
}

export interface RegisterUserRequestDto {
  username: string;
  email: string;
  password: string;
  fullName: string;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  timeZoneId?: string;
}

export interface RefreshTokenRequestDto {
  refreshToken?: string;
  deviceInfo?: string;
}

export interface RoleDto {
  id: string;
  code: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  isActive: boolean;
  permissions: string[];
}

export interface PermissionDto {
  id: string;
  code: string;
  name: string;
  module: string;
  description: string;
}

// --- Database-Driven Prompts, Modes, Voices, Speech Speeds & Topics ---
export interface CefrLevelPromptDto {
  id: string;
  cefrLevel: CefrLevel;
  displayName: string;
  levelGuidance: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ConversationModePromptDto {
  id: string;
  modeCode: ConversationModeCode;
  category: string;
  displayName: string;
  directiveTemplate: string;
  displayOrder: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface TeacherVoiceDto {
  id: string;
  name: string;
  displayName: string;
  label: string;
  type: string;
  gender: 'Female' | 'Male' | string;
  persona: string;
  style: string;
  description: string;
  isFeatured: boolean;
  isEnabled: boolean;
  displayOrder: number;
}

export interface SpeechSpeedOptionDto {
  id: string;
  code: string;
  value: number;
  label: string;
  descriptionVi: string;
  recommendedCefrRange: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface UpsertSpeechSpeedRequestDto {
  code: string;
  value: number;
  label: string;
  descriptionVi: string;
  recommendedCefrRange: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface SynthesizePronunciationAudioRequestDto {
  apiKey: string;
  text?: string;
  targetType?: 'word' | 'sentence';
  accent: 'US' | 'UK';
  voiceName: TeacherVoiceName;
  speechSpeed: number;
  modelCode: 'gemini-3.8-live' | 'gemini-3.8-live-extended-thinking' | string;
  targetId?: string;
  vocabularyId?: string;
  senseId?: string;
}

export interface SynthesizePronunciationAudioResponseDto {
  text: string;
  targetType: string;
  accent: string;
  voiceName: string;
  speechSpeed: number;
  modelCodeUsed: string;
  systemPromptUsed: string;
  mimeType: string;
  sampleRate: number;
  durationMs: number;
  audioBase64: string;
}

export interface UpsertAiModelConfigRequestDto {
  displayName: string;
  modelCode: string;
  routedModelCode?: string | null;
  supportsTextGeneration: boolean;
  supportsStructuredOutput: boolean;
  supportsAudioInput: boolean;
  supportsPronunciationAssessment: boolean;
  supportsLiveAudio: boolean;
  supportsTranscription?: boolean;
  supportsTranslation?: boolean;
  supportsStreaming?: boolean;
  isAvailable: boolean;
  isDeprecated: boolean;
  isEnabled: boolean;
  capabilityNote: string;
  displayOrder: number;
}

export interface AdminCreateUserRequestDto {
  username: string;
  email: string;
  password: string;
  fullName: string;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  isActive: boolean;
  roleCodes: string[];
}

export interface AdminUpdateUserRequestDto {
  email: string;
  fullName: string;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: TeacherVoiceName;
  isActive: boolean;
  newPassword?: string;
  roleCodes: string[];
}

export interface TopicDto {
  id: string;
  code: string;
  name: string;
  displayNameVi: string;
  category: string;
  modeCode?: string | null;
  suggestedCefrLevel: CefrLevel;
  supportsConversation: boolean;
  supportsVocabulary: boolean;
  description: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface UpsertTopicRequestDto {
  code: string;
  name: string;
  displayNameVi: string;
  category: string;
  modeCode?: string | null;
  suggestedCefrLevel: CefrLevel;
  supportsConversation: boolean;
  supportsVocabulary: boolean;
  description: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
}

// --- AI Model Catalog ---
export interface AiModelOptionDto {
  id: string;
  displayName: string;
  modelCode: string;
  routedModelCode?: string | null;
  supportsTextGeneration: boolean;
  supportsStructuredOutput: boolean;
  supportsAudioInput: boolean;
  supportsPronunciationAssessment: boolean;
  supportsLiveAudio?: boolean;
  isAvailable: boolean;
  isDeprecated: boolean;
  isEnabled: boolean;
  capabilityNote: string;
  displayOrder: number;
}

// --- Vocabulary & Pronunciation Rubrics ---
export interface ScoringRubricCriterionDto {
  id: string;
  code: string;
  name: string;
  description: string;
  weight: number;
  displayOrder: number;
  minimumScore: number;
  maximumScore: number;
  isEnabled: boolean;
}

export interface ScoringRubricDto {
  id: string;
  code: string;
  name: string;
  description: string;
  version: number;
  assessmentType: string;
  isActive: boolean;
  criteria: ScoringRubricCriterionDto[];
}


// --- Generic Pagination Contracts ---
export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  paged?: boolean;
}

// --- Vocabulary Generation Contracts ---
export interface GenerateVocabularyRequestDto {
  apiKey?: string;
  cefrLevel: CefrLevel;
  topic?: string | null;
  requestedCount: number;
  modelCode: string;
  provider?: string;
  assignToTodayPlan?: boolean;
  examplesPerSense?: number;
  targetTerms?: string[];
  sourceSentence?: string;
}

export interface BuildVocabularyGenerationPromptRequestDto {
  requestedCount: number;
  cefrLevel: CefrLevel;
  topic?: string | null;
  excludedWords?: string[];
  examplesPerSense?: number;
  targetTerms?: string[];
  sourceSentence?: string;
}

export interface ImportVocabularyJsonRequestDto {
  jsonText: string;
  modelCode?: string;
  cefrLevel: CefrLevel;
  topic?: string | null;
  assignToTodayPlan?: boolean;
  examplesPerSense?: number;
}


// --- Grammar Roadmap & Exercise Contracts ---
export type GrammarAttemptKind = 'StepCore' | 'Practice' | 'LevelTest';
export type GrammarExerciseSource = 'Official' | 'UserAi' | 'UserImport';
export type GrammarExerciseVisibility = 'Private' | 'Public';
export type GrammarModerationStatus = 'None' | 'Pending' | 'Approved' | 'Rejected';
export type GrammarStepProgressStatus = 'Locked' | 'Unlocked' | 'Passed';
export type GrammarStepPassedBy = 'Steps' | 'LevelTest';

export interface GrammarRoadmapDto {
  levels: GrammarLevelDto[];
}

export interface GrammarLevelDto {
  level: string;
  isUnlocked: boolean;
  isPassed: boolean;
  canTakeLevelTest: boolean;
  steps: GrammarStepRoadmapItemDto[];
}

export interface GrammarStepRoadmapItemDto {
  code: string;
  order: number;
  titleVi: string;
  titleEn: string;
  summaryVi: string;
  isLevelReview: boolean;
  status: string;
  bestScore100?: number | null;
  passedBy?: string | null;
}

export interface GrammarTheoryFormulaDto {
  label?: string;
  pattern: string;
}

export interface GrammarTheoryExampleDto {
  en: string;
  vi: string;
  highlight?: string;
}

export interface GrammarTheorySectionDto {
  heading: string;
  explanationVi: string;
  formulas?: (string | GrammarTheoryFormulaDto)[];
  examples?: GrammarTheoryExampleDto[];
  notes?: string[];
}

export interface GrammarTheoryMistakeDto {
  wrong: string;
  right: string;
  explanationVi: string;
}

export interface GrammarTheoryDto {
  objectives: string[];
  sections: GrammarTheorySectionDto[];
  commonMistakes?: GrammarTheoryMistakeDto[];
  tips?: string[];
  summary: string[];
}

export interface GrammarStepDetailDto {
  code: string;
  cefrLevel: string;
  order: number;
  titleVi: string;
  titleEn: string;
  summaryVi: string;
  isLevelReview: boolean;
  theory: GrammarTheoryDto;
  status: string;
  bestScore100?: number | null;
  coreExerciseCount: number;
  communityExerciseCount: number;
  myExerciseCount: number;
  prevCode?: string | null;
  nextCode?: string | null;
}

export interface GrammarExerciseLearnerDto {
  id: string;
  key?: string | null;
  type: string;
  title?: string | null;
  instructions?: string | null;
  order: number;
  maxPoints: number;
  gradingMode: string;
  content?: Record<string, unknown> | null;
  difficulty?: number;
  focus?: string | null;
  source: GrammarExerciseSource;
  visibility: GrammarExerciseVisibility;
  moderation: GrammarModerationStatus;
  authorName?: string | null;
  unavailable?: boolean;
}

export interface StartGrammarAttemptRequestDto {
  kind: GrammarAttemptKind;
  stepCode?: string | null;
  cefrLevel?: string | null;
  /** Start a fresh attempt instead of resuming the unfinished one; the old attempt keeps its answers. */
  forceNew?: boolean;
}

export interface LessonAiExecutionConfigDto {
  apiKey?: string | null;
  assessmentModelCode?: string | null;
  provider?: string | null;
}

export interface GrammarAiExecutionConfigDto {
  provider?: string | null;
  modelCode?: string | null;
  apiKey?: string | null;
}

export interface SubmitGrammarAnswerRequestDto {
  exerciseId: string;
  answerPayload: Record<string, unknown>;
  aiConfig?: LessonAiExecutionConfigDto | null;
}

export interface GrammarAttemptAnswerDto {
  exerciseId: string;
  status: string;
  score100?: number | null;
  pointsEarned?: number | null;
  maxPoints: number;
  feedback?: Record<string, unknown> | null;
  answerPayload?: Record<string, unknown> | null;
  suggestedAnswer?: string | null;
}

export interface GrammarAttemptDto {
  id: string;
  kind: GrammarAttemptKind;
  stepCode?: string | null;
  cefrLevel: string;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  score100?: number | null;
  exercises: GrammarExerciseLearnerDto[];
  answers: GrammarAttemptAnswerDto[];
}

export interface GrammarAnswerResultDto {
  attemptId: string;
  exerciseId: string;
  status: string;
  score100?: number | null;
  pointsEarned?: number | null;
  maxPoints: number;
  feedback?: Record<string, unknown> | null;
  answerPayload?: Record<string, unknown> | null;
  suggestedAnswer?: string | null;
}

export interface GrammarAttemptResultDto {
  score100: number;
  passed: boolean;
  unlockedStepCodes: string[];
  levelPassed?: boolean | null;
}

export interface GrammarExercisePromptRequestedItemDto {
  type: string;
  count: number;
  difficulty: number;
}

export interface GenerateGrammarExercisesRequestDto {
  items: GrammarExercisePromptRequestedItemDto[];
  learnerNote?: string | null;
  visibility?: GrammarExerciseVisibility;
  aiConfig?: GrammarAiExecutionConfigDto | null;
}

export interface BuildGrammarExercisePromptRequestDto {
  items: GrammarExercisePromptRequestedItemDto[];
  learnerNote?: string | null;
}

export interface BuildGrammarExercisePromptResponseDto {
  prompt: string;
}

export interface ImportGrammarExercisesJsonRequestDto {
  jsonText: string;
  visibility?: GrammarExerciseVisibility;
  expectedItems?: GrammarExercisePromptRequestedItemDto[] | null;
}

export interface GrammarExerciseRejectionDto {
  index: number;
  issues: string[];
}

export interface GrammarMyExerciseDto {
  id: string;
  stepId: string;
  stepCode: string;
  key?: string | null;
  type: string;
  difficulty: number;
  title: string;
  instructions: string;
  focus?: string | null;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  maxPoints: number;
  gradingMode: string;
  source: GrammarExerciseSource;
  ownerUserId?: string | null;
  authorUserId?: string | null;
  visibility: GrammarExerciseVisibility;
  moderation: GrammarModerationStatus;
  rejectReason?: string | null;
  reviewedBy?: string | null;
  reviewedAtUtc?: string | null;
  isCore: boolean;
  contentHash: string;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface GenerateGrammarExercisesResponseDto {
  created: GrammarMyExerciseDto[];
  rejected: GrammarExerciseRejectionDto[];
}

export interface UpdateMyGrammarExerciseRequestDto {
  title: string;
  instructions: string;
  difficulty: number;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  focus?: string | null;
}

export interface ChangeMyGrammarExerciseVisibilityRequestDto {
  visibility: GrammarExerciseVisibility;
}

// --- Admin Grammar Contracts ---

export interface GrammarStepAdminDto {
  id: string;
  code: string;
  cefrLevel: string;
  order: number;
  titleVi: string;
  titleEn: string;
  summaryVi: string;
  isLevelReview: boolean;
  isPublished: boolean;
  theoryEditedByAdmin: boolean;
  theoryVersion: number;
  coreExerciseCount: number;
  officialExerciseCount: number;
  pendingExerciseCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface GrammarExerciseAdminDto {
  id: string;
  stepId: string;
  key?: string | null;
  type: string;
  difficulty: number;
  title: string;
  instructions: string;
  focus?: string | null;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  maxPoints: number;
  gradingMode: string;
  source: GrammarExerciseSource;
  ownerUserId?: string | null;
  authorUserId?: string | null;
  visibility: GrammarExerciseVisibility;
  moderation: GrammarModerationStatus;
  rejectReason?: string | null;
  reviewedBy?: string | null;
  reviewedAtUtc?: string | null;
  isCore: boolean;
  contentHash: string;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface GrammarStepAdminDetailDto {
  id: string;
  code: string;
  cefrLevel: string;
  order: number;
  titleVi: string;
  titleEn: string;
  summaryVi: string;
  isLevelReview: boolean;
  isPublished: boolean;
  theoryEditedByAdmin: boolean;
  theoryVersion: number;
  theory: GrammarTheoryDto;
  officialExercises: GrammarExerciseAdminDto[];
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface UpdateGrammarStepRequestDto {
  titleVi: string;
  titleEn: string;
  summaryVi: string;
  isPublished: boolean;
}

export interface UpdateGrammarStepTheoryRequestDto {
  theory: GrammarTheoryDto | Record<string, unknown>;
}

export interface CreateOfficialGrammarExerciseRequestDto {
  key?: string | null;
  type: string;
  difficulty: number;
  title: string;
  instructions: string;
  maxPoints: number;
  gradingMode: string;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  isCore: boolean;
  focus?: string | null;
}

export interface UpdateOfficialGrammarExerciseRequestDto {
  type: string;
  difficulty: number;
  title: string;
  instructions: string;
  maxPoints: number;
  gradingMode: string;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  isCore: boolean;
  focus?: string | null;
}

export interface ImportGrammarContentPackRequestDto {
  overwriteExistingTheory?: boolean;
}

export interface GrammarImportIssueDto {
  code?: string | null;
  key?: string | null;
  message: string;
}

export interface GrammarContentPackImportResultDto {
  stepsCreated: number;
  stepsUpdated: number;
  stepsSkipped: number;
  exercisesCreated: number;
  exercisesSkipped: number;
  errors: GrammarImportIssueDto[];
}

export interface GrammarModerationItemDto {
  id: string;
  stepId: string;
  stepCode: string;
  cefrLevel: string;
  authorUserId?: string | null;
  authorName?: string | null;
  type: string;
  difficulty: number;
  title: string;
  instructions: string;
  focus?: string | null;
  content: Record<string, unknown>;
  grading?: Record<string, unknown> | null;
  maxPoints: number;
  gradingMode: string;
  visibility: GrammarExerciseVisibility;
  moderation: GrammarModerationStatus;
  rejectReason?: string | null;
  createdAtUtc: string;
}

export interface RejectGrammarExerciseRequestDto {
  reason: string;
}

export interface PromoteGrammarExerciseRequestDto {
  isCore: boolean;
}

export interface GenerateOfficialGrammarExercisesRequestDto {
  items: GrammarExercisePromptRequestedItemDto[];
  isCore?: boolean;
  aiConfig?: GrammarAiExecutionConfigDto | null;
}

export interface ImportOfficialGrammarExercisesJsonRequestDto {
  jsonText: string;
  isCore?: boolean;
  expectedItems?: GrammarExercisePromptRequestedItemDto[] | null;
}

export interface DraftGrammarTheoryWithAiRequestDto {
  aiConfig?: GrammarAiExecutionConfigDto | null;
}

export interface ValidationIssueDto {
  path: string;
  severity: string;
  message: string;
}

export interface DraftGrammarTheoryWithAiResponseDto {
  theory: GrammarTheoryDto;
  issues: ValidationIssueDto[];
}

export interface GenerateOfficialGrammarExercisesResponseDto {
  created: GrammarExerciseAdminDto[];
  rejected: GrammarExerciseRejectionDto[];
}

export type GrammarExerciseType =
  | 'MultipleChoice'
  | 'TrueFalse'
  | 'FillInTheBlanks'
  | 'Matching'
  | 'SentenceUnscramble'
  | 'ErrorCorrection'
  | 'Translation'
  | 'WritingPractice'
  | 'ReadingComprehension';

export type GrammarGradingMode =
  | 'Deterministic'
  | 'SelfAssessment'
  | 'AiRubric'
  | 'Composite';




// --- Learning Progress & Daily Activity ---
export interface ProgressDailyActivityDto {
  date: string; // "yyyy-MM-dd", ngày địa phương của user
  vocabularyReviews: number;
  lessonAttempts: number;
  grammarAttempts: number;
  pronunciationAssessments: number;
  tutorSessions: number;
}

export interface ProgressVocabularySummaryDto {
  studied: number;
  mastered: number;
  dueForReviewNow: number;
  averageMastery: number;
}

export interface ProgressLessonSummaryDto {
  completedAttempts: number;
  averageScore100: number | null;
}

export interface ProgressGrammarLevelDto {
  level: string;
  passedSteps: number;
  totalSteps: number;
}

export interface ProgressGrammarSummaryDto {
  levels: ProgressGrammarLevelDto[];
  currentStepCode: string | null;
}

export interface ProgressSpeechSummaryDto {
  assessments: number;
  averageScore: number | null;
  averageScoreInRange: number | null;
}

export interface ProgressLiveTutorSummaryDto {
  sessions: number;
  sessionsInRange: number;
}

export interface ProgressOverviewDto {
  timeZoneId: string;
  days: number;
  currentStreakDays: number;
  longestStreakDays: number;
  activeDaysInRange: number;
  activity: ProgressDailyActivityDto[];
  vocabulary: ProgressVocabularySummaryDto;
  lessons: ProgressLessonSummaryDto;
  grammar: ProgressGrammarSummaryDto;
  speech: ProgressSpeechSummaryDto;
  liveTutor: ProgressLiveTutorSummaryDto;
}

// --- SignalR Realtime Hub Contracts ---

export interface LanguageCorrectionIssueDto {
  original: string;
  fix: string;
  category: string;
  explanationVi: string;
}

export interface SignalRLanguageCorrectionDto {
  id: string;
  conversationId: string;
  turnId: string;
  originalText: string;
  correctedText: string;
  errorCategory: string;
  explanationVi: string;
  cefrTip: string;
  timestamp: string;
  status?: string | null;
  improvedText?: string | null;
  issues?: LanguageCorrectionIssueDto[] | null;
}

export interface FeedbackAudioResponseDto {
  mimeType: string;
  audioBase64: string;
  durationMs: number;
}

export interface GrammarHubServerEvents {
  ReceiveLanguageCorrection: (correction: SignalRLanguageCorrectionDto) => void;
  GrammarHubReady: (ready: { conversationId: string }) => void;
}

export interface GrammarHubClientMethods {
  InitializeGrammarSession: (request: { conversationId: string; apiKey?: string }) => Promise<void>;
  AnalyzeTurnGrammar: (conversationId: string, turnId: string, transcriptText: string, cefrLevel: string) => Promise<void>;
  RequestFeedbackAudio: (conversationId: string, correctionId: string, variant: string) => Promise<FeedbackAudioResponseDto>;
}

/** Mirrors BE `StartTutorSessionRequest` (hub `/hubs/tutor` → `InitializeTutorSession`). */
export interface StartTutorSessionRequestDto {
  conversationId: string;
  apiKey: string;
  cefrLevel?: string;
  extendedThinking?: boolean;
  thinkingLevel?: string;
  voiceName?: string;
  speechSpeed?: number;
  conversationMode?: string;
  topic?: string;
  customSystemPrompt?: string | null;
  /** TTS accent used for on-demand feedback audio (`US` | `UK`). */
  accent?: string;
  /** TTS model for on-demand feedback audio, e.g. `gemini-3.8-live`. */
  ttsModelCode?: string | null;
}

export interface TutorHubClientMethods {
  InitializeTutorSession: (request: StartTutorSessionRequestDto) => Promise<void>;
  StreamUserAudioToTutor: (payload: { conversationId: string; speaker: string; turnId: string | null; base64Pcm16k: string }) => Promise<void>;
  NotifyUserSpeechEnd: (conversationId: string) => Promise<void>;
  StopSession: (conversationId: string) => Promise<void>;
}
