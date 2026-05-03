import { create } from 'zustand'

export interface AppNotification {
  id: string
  title: string
  message: string
  type: 'reminder' | 'achievement' | 'streak' | 'level_up' | 'system'
  isRead: boolean
  createdAt: Date
}

interface NotificationState {
  notifications: AppNotification[]
  unreadCount: number
}

interface NotificationActions {
  addNotification: (notification: Omit<AppNotification, 'id' | 'isRead' | 'createdAt'>) => void
  markAsRead: (id: string) => void
  markAllAsRead: () => void
  clearAll: () => void
}

export const useNotificationStore = create<NotificationState & NotificationActions>((set) => ({
  // State
  notifications: [],
  unreadCount: 0,

  // Actions
  addNotification: (notification) => {
    const newNotification: AppNotification = {
      ...notification,
      id: crypto.randomUUID(),
      isRead: false,
      createdAt: new Date(),
    }
    set((state) => ({
      notifications: [newNotification, ...state.notifications].slice(0, 50), // max 50
      unreadCount: state.unreadCount + 1,
    }))
  },

  markAsRead: (id) =>
    set((state) => ({
      notifications: state.notifications.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
      unreadCount: Math.max(0, state.unreadCount - 1),
    })),

  markAllAsRead: () =>
    set((state) => ({
      notifications: state.notifications.map((n) => ({ ...n, isRead: true })),
      unreadCount: 0,
    })),

  clearAll: () => set({ notifications: [], unreadCount: 0 }),
}))
