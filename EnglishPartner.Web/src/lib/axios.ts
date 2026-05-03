import axios from 'axios'
import { useAuthStore } from '@/lib/zustand/auth.store'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'https://localhost:7001'

export const apiClient = axios.create({
  baseURL: API_URL,
  withCredentials: true, // send httpOnly refresh cookie automatically
  headers: {
    'Content-Type': 'application/json',
  },
})

// ── Request Interceptor ──────────────────────────────────────────────────────

apiClient.interceptors.request.use((config) => {
  // 1. Attach access token
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  // 2. Attach timezone offset (e.g., Vietnam = +7)
  const offsetHours = new Date().getTimezoneOffset() / -60
  config.headers['X-Timezone-Offset'] = String(offsetHours)

  // 3. Let browser set Content-Type automatically for FormData (with boundary)
  if (config.data instanceof FormData) {
    delete config.headers['Content-Type']
  }

  return config
})

// ── Response Interceptor ─────────────────────────────────────────────────────

let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (err: unknown) => void
}> = []

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token!)
    }
  })
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject })
      })
        .then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return apiClient(originalRequest)
        })
        .catch((err) => Promise.reject(err))
    }

    originalRequest._retry = true
    isRefreshing = true

    try {
      const { data } = await apiClient.post<{ accessToken: string; permissions?: string[] | null }>('/api/v1/auth/refresh')
      const newToken = data.accessToken

      const store = useAuthStore.getState()
      const currentUser = store.user

      store.setAuth({ 
        accessToken: newToken,
        user: currentUser ? {
          ...currentUser,
          permissions: data.permissions || [],
          isAdmin: Array.isArray(data.permissions) && data.permissions.length > 0
        } : undefined
      })
      apiClient.defaults.headers.common.Authorization = `Bearer ${newToken}`
      processQueue(null, newToken)
      originalRequest.headers.Authorization = `Bearer ${newToken}`

      return apiClient(originalRequest)
    } catch (refreshError) {
      processQueue(refreshError, null)
      useAuthStore.getState().clearAuth()
      if (typeof window !== 'undefined') {
        window.location.href = '/login'
      }
      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  }
)
