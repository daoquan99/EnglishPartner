'use client'

import { useMutation } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import { changePassword, forgotPassword, login, logout, register, resetPassword, socialLogin } from './auth.api'
import { useAuthStore } from '@/lib/zustand/auth.store'
import type { AuthUser, AuthResponse } from '../types/auth.types'

// ── Helper: convert API response → store user ────────────────────────────

const toAuthUser = (res: AuthResponse): AuthUser => ({
  id: res.userId,
  email: res.email,
  displayName: res.displayName,
  isOnboardingCompleted: res.isOnboardingCompleted,
  isAdmin: Array.isArray(res.permissions) && res.permissions.length > 0,
  permissions: res.permissions || [],
})

/**
 * Set lightweight cookies for middleware route protection.
 * These are NOT security-sensitive — just routing hints.
 * The actual auth is via __Secure-ep-refresh (httpOnly, set by BE).
 */
const setRoutingCookies = (user: AuthUser) => {
  const maxAge = 60 * 60 * 24 * 7 // 7 days, matches refresh token TTL
  document.cookie = `ep-role=${user.isAdmin ? 'admin' : 'learner'}; path=/; max-age=${maxAge}; SameSite=Strict`
  document.cookie = `ep-onboarding=${user.isOnboardingCompleted ? '1' : '0'}; path=/; max-age=${maxAge}; SameSite=Strict`
}

const clearRoutingCookies = () => {
  document.cookie = 'ep-role=; path=/; max-age=0'
  document.cookie = 'ep-onboarding=; path=/; max-age=0'
}

// ── Shared success handler ───────────────────────────────────────────────

const useAuthSuccess = () => {
  const { setAuth } = useAuthStore()
  const router = useRouter()

  return (data: AuthResponse) => {
    const user = toAuthUser(data)
    setAuth({ accessToken: data.accessToken, user })
    setRoutingCookies(user)

    if (!data.isOnboardingCompleted) {
      router.push('/onboarding')
    } else if (user.isAdmin) {
      router.push('/admin')
    } else {
      router.push('/dashboard')
    }
  }
}

// ── useRegister ───────────────────────────────────────────────────────────

export const useRegister = () => {
  const onSuccess = useAuthSuccess()

  return useMutation({
    mutationFn: register,
    onSuccess,
  })
}

// ── useLogin ──────────────────────────────────────────────────────────────

export const useLogin = () => {
  const onSuccess = useAuthSuccess()

  return useMutation({
    mutationFn: login,
    onSuccess,
  })
}

// ── useSocialLogin ────────────────────────────────────────────────────────

export const useSocialLogin = () => {
  const onSuccess = useAuthSuccess()

  return useMutation({
    mutationFn: socialLogin,
    onSuccess,
  })
}

// ── useLogout ─────────────────────────────────────────────────────────────

export const useLogout = () => {
  const { clearAuth } = useAuthStore()
  const router = useRouter()

  return useMutation({
    mutationFn: logout,
    onSuccess: () => {
      clearAuth()
      clearRoutingCookies()
      router.push('/login')
    },
    onError: () => {
      // Even on error, clear local state and redirect
      clearAuth()
      clearRoutingCookies()
      router.push('/login')
    },
  })
}

// ── useChangePassword ─────────────────────────────────────────────────────

export const useChangePassword = () => {
  return useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      toast.success('Password changed successfully')
    },
    onError: () => toast.error('Failed to change password'),
  })
}

// ── useForgotPassword ─────────────────────────────────────────────────────

export const useForgotPassword = () => {
  return useMutation({
    mutationFn: forgotPassword,
  })
}

// ── useResetPassword ──────────────────────────────────────────────────────

export const useResetPassword = () => {
  const router = useRouter()

  return useMutation({
    mutationFn: resetPassword,
    onSuccess: () => {
      toast.success('Password reset successfully! Please login.')
      router.push('/login')
    },
  })
}
