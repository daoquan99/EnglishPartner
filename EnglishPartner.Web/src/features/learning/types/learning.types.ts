// ── Enums ────────────────────────────────────────────────────────────────────

export enum ItemType {
  Vocabulary = 0,
  Grammar = 1,
  ExampleSentence = 2,
  Topic = 3,
  Conversation = 4,
  Exercise = 5,
}

// ── Dashboard & Leaderboard ──────────────────────────────────────────────────

export interface DashboardResponse {
  userId: string
  currentLevel: string
  totalXp: number
  currentStreak: number
  longestStreak: number
}

export interface LeaderboardEntry {
  userId: string
  displayName: string
  totalXp: number
  currentLevel: string
  currentStreak: number
}

// ── Reviews (Spaced Repetition) ──────────────────────────────────────────────

export interface ReviewDueItem {
  itemId: string
  type: ItemType
  easinessFactor: number
  intervalDays: number
  repetitions: number
}

export interface SubmitReviewRequest {
  itemId: string
  type: ItemType
  qualityRating: number // 0-5
}

// ── Study Sessions ───────────────────────────────────────────────────────────

export interface StartSessionRequest {
  sessionTitle: string
  totalItems: number
}

export interface SubmitAnswerRequest {
  itemId: string
  itemType: ItemType
  userAnswer: string
  timeSpentSeconds: number
}

export interface SubmitAnswerResponse {
  qualityScore: number
  isCorrect: boolean
  suggestedCorrection: string | null
  explanation: string
  newIntervalDays: number
  newEasinessFactor: number
  nextReviewDate: string
}

export interface CompleteSessionRequest {
  score: number // 0-100
}

export interface SubmitAudioAnswerRequest {
  itemId: string
  itemType: ItemType
  timeSpentSeconds: number
  audioFile: File | Blob
}

export interface SubmitAudioAnswerResponse {
  qualityScore: number
  transcription: string
  pronunciationFeedback: string | null
  newIntervalDays: number
  newEasinessFactor: number
  nextReviewDate: string
}

// ── Conversations ────────────────────────────────────────────────────────────

export interface EvaluateConversationLineRequest {
  conversationId: string
  lineId: string
  audioFile: File | Blob
}

export interface EvaluateConversationLineResponse {
  score: number
  transcription: string
  pronunciationFeedback: string | null
}

// ── Pronunciation ────────────────────────────────────────────────────────────

export interface PronunciationEvaluationRequest {
  expectedText: string
  audioFile: File | Blob
  languageCode?: string
}

export interface PronunciationEvaluationResponse {
  score: number
  transcription: string
  feedback: string | null
}

// ── Admin ────────────────────────────────────────────────────────────────────

export interface AdjustXpRequest {
  amount: number
  reason: string
}
