import { create } from 'zustand';

export type ToastType = 'success' | 'error' | 'warning' | 'info' | 'loading';

export interface ToastAction {
  label: string;
  onClick: () => void;
}

export interface ToastOptions {
  id?: string;
  title?: string;
  duration?: number; // Duration in milliseconds. Default 4000ms. If 0 or Infinity, does not auto-dismiss.
  action?: ToastAction;
}

export interface ToastItem {
  id: string;
  type: ToastType;
  message: string;
  title?: string;
  duration: number;
  action?: ToastAction;
  createdAt: number;
}

interface ToastState {
  toasts: ToastItem[];
  addToast: (type: ToastType, message: string, options?: ToastOptions) => string;
  removeToast: (id: string) => void;
  clearToasts: () => void;
  updateToast: (id: string, updates: Partial<Omit<ToastItem, 'id'>>) => void;
  pauseToast: (id: string) => void;
  resumeToast: (id: string) => void;
}

let toastCounter = 0;

interface ToastTimerRecord {
  timer: ReturnType<typeof setTimeout>;
  duration: number;
  remaining: number;
  startedAt: number;
}

const timerRecords = new Map<string, ToastTimerRecord>();

function clearToastTimer(id: string) {
  const record = timerRecords.get(id);
  if (record) {
    clearTimeout(record.timer);
    timerRecords.delete(id);
  }
}

export const useToastStore = create<ToastState>((set, get) => ({
  toasts: [],

  addToast: (type, message, options = {}) => {
    const id = options.id || `toast-${Date.now()}-${++toastCounter}`;
    const duration = options.duration !== undefined ? options.duration : type === 'loading' ? 0 : 4000;

    // Clear any previous timer for this ID
    clearToastTimer(id);

    const newToast: ToastItem = {
      id,
      type,
      message,
      title: options.title,
      duration,
      action: options.action,
      createdAt: Date.now(),
    };

    set((state) => {
      // If toast with same id already exists, update it in place
      const exists = state.toasts.some((t) => t.id === id);
      if (exists) {
        return {
          toasts: state.toasts.map((t) => (t.id === id ? newToast : t)),
        };
      }
      // Keep max 5 most recent toasts
      const updated = [newToast, ...state.toasts].slice(0, 5);
      return { toasts: updated };
    });

    if (duration > 0 && duration !== Infinity) {
      const timer = setTimeout(() => {
        get().removeToast(id);
        timerRecords.delete(id);
      }, duration);
      timerRecords.set(id, {
        timer,
        duration,
        remaining: duration,
        startedAt: Date.now(),
      });
    }

    return id;
  },

  updateToast: (id, updates) => {
    set((state) => ({
      toasts: state.toasts.map((t) => (t.id === id ? { ...t, ...updates } : t)),
    }));

    if (updates.duration && updates.duration > 0 && updates.duration !== Infinity) {
      clearToastTimer(id);
      const timer = setTimeout(() => {
        get().removeToast(id);
        timerRecords.delete(id);
      }, updates.duration);
      timerRecords.set(id, {
        timer,
        duration: updates.duration,
        remaining: updates.duration,
        startedAt: Date.now(),
      });
    }
  },

  pauseToast: (id) => {
    const record = timerRecords.get(id);
    if (record) {
      clearTimeout(record.timer);
      const elapsed = Date.now() - record.startedAt;
      record.remaining = Math.max(0, record.remaining - elapsed);
    }
  },

  resumeToast: (id) => {
    const record = timerRecords.get(id);
    if (record && record.remaining > 0) {
      record.startedAt = Date.now();
      record.timer = setTimeout(() => {
        get().removeToast(id);
        timerRecords.delete(id);
      }, record.remaining);
    }
  },

  removeToast: (id) => {
    clearToastTimer(id);
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
    }));
  },

  clearToasts: () => {
    timerRecords.forEach((rec) => clearTimeout(rec.timer));
    timerRecords.clear();
    set({ toasts: [] });
  },
}));

/**
 * Global imperative toast API for showing notifications anywhere in the app:
 * - toast.success("Lưu thành công!")
 * - toast.error("Đã xảy ra lỗi")
 * - toast.warning("Cảnh báo")
 * - toast.info("Thông tin")
 * - toast.promise(asyncFn(), { loading: 'Đang xử lý...', success: 'Hoàn tất!', error: 'Thất bại' })
 * - toast.dismiss(id)
 */
export const toast = {
  success: (message: string, options?: ToastOptions): string => {
    return useToastStore.getState().addToast('success', message, options);
  },

  error: (message: string, options?: ToastOptions): string => {
    return useToastStore.getState().addToast('error', message, options);
  },

  warning: (message: string, options?: ToastOptions): string => {
    return useToastStore.getState().addToast('warning', message, options);
  },

  info: (message: string, options?: ToastOptions): string => {
    return useToastStore.getState().addToast('info', message, options);
  },

  loading: (message: string, options?: ToastOptions): string => {
    return useToastStore.getState().addToast('loading', message, options);
  },

  dismiss: (id?: string) => {
    if (id) {
      useToastStore.getState().removeToast(id);
    } else {
      useToastStore.getState().clearToasts();
    }
  },

  pause: (id: string) => {
    useToastStore.getState().pauseToast(id);
  },

  resume: (id: string) => {
    useToastStore.getState().resumeToast(id);
  },

  /**
   * Helper for async operations with loading -> success/error lifecycle
   */
  promise: async <T>(
    promise: Promise<T>,
    messages: {
      loading: string;
      success: string | ((data: T) => string);
      error: string | ((err: unknown) => string);
    },
    options?: ToastOptions
  ): Promise<T> => {
    const id = toast.loading(messages.loading, options);
    try {
      const data = await promise;
      const successMsg =
        typeof messages.success === 'function' ? messages.success(data) : messages.success;
      useToastStore.getState().updateToast(id, {
        type: 'success',
        message: successMsg,
        duration: options?.duration ?? 4000,
      });
      return data;
    } catch (err) {
      const errorMsg =
        typeof messages.error === 'function' ? messages.error(err) : messages.error;
      useToastStore.getState().updateToast(id, {
        type: 'error',
        message: errorMsg,
        duration: options?.duration ?? 5000,
      });
      throw err;
    }
  },
};
