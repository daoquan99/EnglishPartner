import { create } from 'zustand'

type Theme = 'dark' | 'light'

interface ThemeState {
  theme: Theme
}

interface ThemeActions {
  setTheme: (theme: Theme) => void
  toggleTheme: () => void
}

export const useThemeStore = create<ThemeState & ThemeActions>((set, get) => ({
  theme: 'dark',

  setTheme: (theme) => {
    set({ theme })
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', theme)
      localStorage.setItem('ep-theme', theme)
    }
  },

  toggleTheme: () => {
    const next = get().theme === 'dark' ? 'light' : 'dark'
    get().setTheme(next)
  },
}))

/** Call once on app mount to restore saved theme */
export const initializeTheme = () => {
  if (typeof window === 'undefined') return
  const saved = localStorage.getItem('ep-theme') as Theme | null
  const theme = saved ?? 'dark'
  document.documentElement.setAttribute('data-theme', theme)
  useThemeStore.getState().setTheme(theme)
}
