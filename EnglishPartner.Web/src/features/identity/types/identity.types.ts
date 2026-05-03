// ── Onboarding ───────────────────────────────────────────────────────────────

export interface OnboardingQuestion {
  key: string
  text: string
  options: string[]
  order: number
}

export interface CompleteOnboardingRequest {
  nativeLanguage: string
  targetLanguage: string
  proficiencyLevel: string
  learningGoal: string
  dailyGoalMinutes: number
  morningReminderHour?: number | null
  eveningReminderHour?: number | null
}

// ── Profile ──────────────────────────────────────────────────────────────────

export interface UserProfileResponse {
  userId: string
  email: string
  displayName: string
  nativeLanguage: string
  targetLanguage: string
  proficiencyLevel: string
  learningGoal: string
  dailyGoalMinutes: number
  avatarUrl: string | null
  dateOfBirth: string | null
  morningReminderHour: number | null
  eveningReminderHour: number | null
  utcOffsetHours: number
  isOnboardingCompleted: boolean
}

export interface UpdateProfileRequest {
  displayName?: string
  nativeLanguage?: string
  targetLanguage?: string
  proficiencyLevel?: string
  learningGoal?: string
  dailyGoalMinutes?: number
  avatarUrl?: string | null
  dateOfBirth?: string | null
  morningReminderHour?: number | null
  eveningReminderHour?: number | null
  utcOffsetHours?: number
}

// ── Streak ───────────────────────────────────────────────────────────────────

export interface StreakResponse {
  currentStreak: number
  longestStreak: number
  lastActivityDate: string
  freezeTokens: number
}

export interface CheckInResponse {
  currentStreak: number
  longestStreak: number
  isNewRecord: boolean
}

// ── External Login ───────────────────────────────────────────────────────────

export interface ExternalLoginResponse {
  provider: string
  providerKey: string
  linkedAt: string
}
