import { apiClient } from '@/lib/axios'
import type {
  AuthResponse,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RefreshTokenResponse,
  RegisterRequest,
  ResetPasswordRequest,
  SocialLoginRequest,
} from '../types/auth.types'

/** POST /api/v1/auth/register */
export const register = async (body: RegisterRequest): Promise<AuthResponse> => {
  const { data } = await apiClient.post<AuthResponse>('/api/v1/auth/register', body)
  return data
}

/** POST /api/v1/auth/login */
export const login = async (body: LoginRequest): Promise<AuthResponse> => {
  const { data } = await apiClient.post<AuthResponse>('/api/v1/auth/login', body)
  return data
}

/** POST /api/v1/auth/social-login */
export const socialLogin = async (body: SocialLoginRequest): Promise<AuthResponse> => {
  const { data } = await apiClient.post<AuthResponse>('/api/v1/auth/social-login', body)
  return data
}

/**
 * POST /api/v1/auth/refresh
 * No body — httpOnly cookie __Secure-ep-refresh is sent automatically (withCredentials: true)
 */
export const refreshToken = async (): Promise<RefreshTokenResponse> => {
  const { data } = await apiClient.post<RefreshTokenResponse>('/api/v1/auth/refresh')
  return data
}

/** POST /api/v1/auth/logout */
export const logout = async (): Promise<void> => {
  await apiClient.post('/api/v1/auth/logout')
}

/** POST /api/v1/auth/change-password */
export const changePassword = async (body: ChangePasswordRequest): Promise<void> => {
  await apiClient.post('/api/v1/auth/change-password', body)
}

/** POST /api/v1/auth/forgot-password */
export const forgotPassword = async (body: ForgotPasswordRequest): Promise<void> => {
  await apiClient.post('/api/v1/auth/forgot-password', body)
}

/** POST /api/v1/auth/reset-password */
export const resetPassword = async (body: ResetPasswordRequest): Promise<void> => {
  await apiClient.post('/api/v1/auth/reset-password', body)
}
