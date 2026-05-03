import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { AuthUser } from '@/features/auth/types/auth.types'
import { getCurrentUser } from '@/features/identity/api/identity.api'

interface AuthState {
  accessToken: string | null
  user: AuthUser | null
  isAuthenticated: boolean
}

interface AuthActions {
  setAuth: (payload: { accessToken: string; user?: AuthUser }) => void
  setUser: (user: Partial<AuthUser>) => void
  clearAuth: () => void
  fetchProfile: () => Promise<void>
}

export const useAuthStore = create<AuthState & AuthActions>()(
  persist(
    (set) => ({
      // State
      accessToken: null,
      user: null,
      isAuthenticated: false,

      // Actions
      setAuth: ({ accessToken, user }) =>
        set((state) => ({
          accessToken,
          user: user ?? state.user,
          isAuthenticated: true,
        })),

      setUser: (user) => set((state) => ({ user: state.user ? { ...state.user, ...user } as AuthUser : user as AuthUser })),

      clearAuth: () =>
        set({
          accessToken: null,
          user: null,
          isAuthenticated: false,
        }),

      fetchProfile: async () => {
        try {
          const profile = await getCurrentUser()
          set((state) => {
            if (!state.user) return state
            return {
              user: {
                ...state.user,
                displayName: profile.displayName,
                isOnboardingCompleted: profile.isOnboardingCompleted,
                nativeLanguage: profile.nativeLanguage,
                targetLanguage: profile.targetLanguage,
                proficiencyLevel: profile.proficiencyLevel,
                avatarUrl: profile.avatarUrl,
              }
            }
          })
        } catch (e) {
          console.error("Failed to fetch profile", e)
        }
      },
    }),
    {
      name: 'ep-auth-storage', // name of the item in the storage (must be unique)
      // by default, it uses localStorage
    }
  )
)
