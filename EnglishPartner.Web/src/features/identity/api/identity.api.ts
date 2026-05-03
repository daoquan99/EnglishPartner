import { apiClient } from '@/lib/axios'
import type {
  CheckInResponse,
  CompleteOnboardingRequest,
  ExternalLoginResponse,
  OnboardingQuestion,
  StreakResponse,
  UpdateProfileRequest,
  UserProfileResponse,
} from '../types/identity.types'

// ── Onboarding ───────────────────────────────────────────────────────────────

export const getOnboardingQuestions = async (): Promise<OnboardingQuestion[]> => {
  const { data } = await apiClient.get<OnboardingQuestion[]>('/api/v1/onboarding/questions')
  return data
}

export const completeOnboarding = async (body: CompleteOnboardingRequest): Promise<void> => {
  await apiClient.post('/api/v1/onboarding/complete', body)
}

// ── Profile ──────────────────────────────────────────────────────────────────

export const getCurrentUser = async (): Promise<UserProfileResponse> => {
  const { data } = await apiClient.get<UserProfileResponse>('/api/v1/profile/me')
  return data
}

export const updateProfile = async (body: UpdateProfileRequest): Promise<void> => {
  await apiClient.put('/api/v1/profile/me', body)
}

// ── External Logins ──────────────────────────────────────────────────────────

export const getExternalLogins = async (): Promise<ExternalLoginResponse[]> => {
  const { data } = await apiClient.get<ExternalLoginResponse[]>('/api/v1/profile/me/external-logins')
  return data
}

export const removeExternalLogin = async (provider: string): Promise<void> => {
  await apiClient.delete(`/api/v1/profile/me/external-logins/${provider}`)
}

// ── Streak ───────────────────────────────────────────────────────────────────

export const getStreak = async (): Promise<StreakResponse> => {
  const { data } = await apiClient.get<StreakResponse>('/api/v1/streaks')
  return data
}

export const checkIn = async (): Promise<CheckInResponse> => {
  const { data } = await apiClient.post<CheckInResponse>('/api/v1/streaks/check-in')
  return data
}
