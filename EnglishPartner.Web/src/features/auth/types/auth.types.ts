// ── Auth Response Shapes (mirrors BE identity.md contracts) ──────────────────

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  userId: string
  email: string
  displayName: string
  isOnboardingCompleted: boolean
  permissions: string[] | null // null = Learner, string[] = Admin
}

export interface RefreshTokenResponse {
  accessToken: string
  expiresAt: string
  permissions?: string[] | null
}

// ── Request Shapes ───────────────────────────────────────────────────────────

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface SocialLoginRequest {
  provider: string
  token: string
}

// ── Stored User (from auth response, kept in Zustand) ────────────────────────

export interface AuthUser {
  id: string
  email: string
  displayName: string
  isOnboardingCompleted: boolean
  isAdmin: boolean // true if permissions?.length > 0
  permissions: string[]
  nativeLanguage?: string
  targetLanguage?: string
  proficiencyLevel?: string
  avatarUrl?: string | null
}

// ── Change Password ──────────────────────────────────────────────────────────

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

// ── Forgot / Reset Password ──────────────────────────────────────────────────

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  email: string
  code: string
  newPassword: string
}
