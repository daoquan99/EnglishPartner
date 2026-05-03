'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRouter } from 'next/navigation'
import { toast } from 'sonner'
import {
  checkIn,
  completeOnboarding,
  getCurrentUser,
  getExternalLogins,
  getOnboardingQuestions,
  getStreak,
  removeExternalLogin,
  updateProfile,
} from './identity.api'
import { useAuthStore } from '@/lib/zustand/auth.store'

// ── Onboarding ───────────────────────────────────────────────────────────────

export const useOnboardingQuestions = () => {
  return useQuery({
    queryKey: ['onboarding-questions'],
    queryFn: getOnboardingQuestions,
    staleTime: Infinity, // questions are static
  })
}

// ── Profile ──────────────────────────────────────────────────────────────────

export const useCurrentUser = () => {
  return useQuery({
    queryKey: ['current-user'],
    queryFn: getCurrentUser,
    staleTime: 5 * 60 * 1000,
  })
}

export const useCompleteOnboarding = () => {
  const queryClient = useQueryClient()
  const router = useRouter()
  const { user, setAuth, accessToken } = useAuthStore()

  return useMutation({
    mutationFn: completeOnboarding,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['current-user'] })

      if (user && accessToken) {
        setAuth({
          accessToken,
          user: { ...user, isOnboardingCompleted: true },
        })
      }

      const maxAge = 60 * 60 * 24 * 7
      document.cookie = `ep-onboarding=1; path=/; max-age=${maxAge}; SameSite=Strict`

      toast.success('Profile completed successfully!')
      router.push('/dashboard')
    },
    onError: () => {
      toast.error('Failed to complete onboarding. Please try again.')
    },
  })
}

export const useUpdateProfile = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: updateProfile,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['current-user'] })
      toast.success('Profile updated successfully!')
    },
    onError: () => {
      toast.error('Failed to update profile. Please try again.')
    },
  })
}

// ── External Logins ──────────────────────────────────────────────────────────

export const useExternalLogins = () => {
  return useQuery({
    queryKey: ['external-logins'],
    queryFn: getExternalLogins,
  })
}

export const useRemoveExternalLogin = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: removeExternalLogin,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['external-logins'] })
      toast.success('Social login removed')
    },
    onError: () => toast.error('Failed to remove social login'),
  })
}

// ── Streak ───────────────────────────────────────────────────────────────────

export const useStreak = () => {
  return useQuery({
    queryKey: ['streak'],
    queryFn: getStreak,
    staleTime: 60 * 1000,
  })
}

export const useCheckIn = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: checkIn,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['streak'] })
      queryClient.invalidateQueries({ queryKey: ['current-user'] })
      if (data.isNewRecord) {
        toast.success(`🔥 New record! ${data.longestStreak} day streak!`)
      } else {
        toast.success(`✅ Checked in! ${data.currentStreak} day streak`)
      }
    },
    onError: () => toast.error('Failed to check in'),
  })
}
