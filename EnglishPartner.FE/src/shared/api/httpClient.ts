import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { AuthResponseDto } from './openapi.types';
import { toast } from '@/shared/lib/toast';

/**
 * Resolve backend base URL cleanly in development & production.
 */
export function resolveBackendBaseUrl(): string {
  const envUrl = import.meta.env.VITE_BACKEND_URL as string | undefined;
  if (envUrl && envUrl.trim().length > 0) {
    return envUrl.trim().replace(/\/+$/, '');
  }
  return 'http://localhost:5099';
}

/**
 * In-Memory Access Token Holder (Security Rule: Never persist Access Token in plain localStorage).
 * Refresh Token is prioritized in HttpOnly, Secure cookie (with fallback in session memory).
 */
let inMemoryAccessToken: string | null = null;
let inMemoryFallbackRefreshToken: string | null = null;
let onAuthStateUpdatedCallback: ((auth: AuthResponseDto | null) => void) | null = null;

export function getAccessToken(): string | null {
  return inMemoryAccessToken;
}

export function setInMemoryTokens(accessToken: string | null, fallbackRefreshToken?: string | null): void {
  inMemoryAccessToken = accessToken;
  if (fallbackRefreshToken !== undefined) {
    inMemoryFallbackRefreshToken = fallbackRefreshToken;
  }
}

export function getFallbackRefreshToken(): string | null {
  return inMemoryFallbackRefreshToken;
}

export function registerAuthSyncCallback(cb: (auth: AuthResponseDto | null) => void): void {
  onAuthStateUpdatedCallback = cb;
}

/**
 * Shared Axios Client configured with:
 * - withCredentials: true (sends HttpOnly Secure Refresh Token cookie automatically)
 * - X-Requested-With: XMLHttpRequest (CSRF mitigation header verified by .NET Backend)
 */
export const httpClient = axios.create({
  baseURL: resolveBackendBaseUrl(),
  timeout: 30_000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
  },
});

// Attach In-Memory Bearer Access Token to outgoing requests
httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (inMemoryAccessToken && config.headers) {
    config.headers.Authorization = `Bearer ${inMemoryAccessToken}`;
  }
  return config;
});

/**
 * Single-Flight Refresh Token Lock:
 * When multiple parallel requests receive 401 Unauthorized simultaneously,
 * only ONE /api/auth/refresh call is executed; all other 401 requests await the same Promise.
 */
let singleFlightRefreshPromise: Promise<string | null> | null = null;

export interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
  skipErrorToast?: boolean;
}

/**
 * Extracts a human-friendly error message from ASP.NET ProblemDetails or Axios error
 */
export function extractApiErrorMessage(error: AxiosError): string {
  if (!error.response) {
    if (error.code === 'ECONNABORTED' || error.message?.toLowerCase().includes('timeout')) {
      return 'Yêu cầu kết nối quá thời gian chờ (Timeout). Vui lòng thử lại.';
    }
    return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra đường truyền hoặc Backend API.';
  }

  const rawData = error.response.data;

  if (typeof rawData === 'string') {
    const trimmed = rawData.trim();
    if (trimmed) return trimmed;
  } else if (rawData && typeof rawData === 'object') {
    const data = rawData as {
      detail?: unknown;
      message?: unknown;
      title?: unknown;
      errors?: Record<string, unknown>;
    };

    if (typeof data.detail === 'string' && data.detail.trim()) {
      return data.detail.trim();
    }
    if (typeof data.message === 'string' && data.message.trim()) {
      return data.message.trim();
    }
    if (data.errors && typeof data.errors === 'object') {
      const firstKey = Object.keys(data.errors)[0];
      if (firstKey) {
        const messages = data.errors[firstKey];
        if (Array.isArray(messages) && messages.length > 0 && typeof messages[0] === 'string') {
          return messages[0];
        }
        if (typeof messages === 'string') {
          return messages;
        }
      }
    }
    if (typeof data.title === 'string' && data.title.trim()) {
      return data.title.trim();
    }
  }
  if (error.response.status === 400) {
    return 'Dữ liệu gửi lên không hợp lệ (400 Bad Request).';
  }
  if (error.response.status === 401) {
    return 'Phiên đăng nhập đã hết hạn hoặc thông tin không hợp lệ.';
  }
  if (error.response.status === 403) {
    return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
  }
  if (error.response.status === 404) {
    return 'Không tìm thấy tài nguyên yêu cầu (404 Not Found).';
  }
  if (error.response.status >= 500) {
    return 'Máy chủ gặp sự cố nội bộ (500 Server Error). Vui lòng thử lại sau.';
  }

  return error.message || 'Yêu cầu API thất bại. Vui lòng thử lại.';
}

export async function performSingleFlightTokenRefresh(): Promise<string | null> {
  if (singleFlightRefreshPromise) {
    return singleFlightRefreshPromise;
  }

  singleFlightRefreshPromise = (async () => {
    try {
      const response = await axios.post<AuthResponseDto>(
        `${resolveBackendBaseUrl()}/api/auth/refresh`,
        {
          refreshToken: inMemoryFallbackRefreshToken ?? '',
          deviceInfo: navigator.userAgent.slice(0, 120),
        },
        {
          withCredentials: true,
          headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
          },
        }
      );

      const data = response.data;
      setInMemoryTokens(data.accessToken, data.refreshToken);
      onAuthStateUpdatedCallback?.(data);
      return data.accessToken;
    } catch {
      setInMemoryTokens(null, null);
      onAuthStateUpdatedCallback?.(null);
      return null;
    } finally {
      singleFlightRefreshPromise = null;
    }
  })();

  return singleFlightRefreshPromise;
}

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined;
    if (!originalRequest) {
      return Promise.reject(error);
    }

    const isAuthEndpoint =
      originalRequest.url?.includes('/api/auth/login') ||
      originalRequest.url?.includes('/api/auth/register') ||
      originalRequest.url?.includes('/api/auth/refresh') ||
      originalRequest.url?.includes('/api/auth/revoke');

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      originalRequest._retry = true;
      const newAccessToken = await performSingleFlightTokenRefresh();
      if (newAccessToken && originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return httpClient(originalRequest);
      }
    }

    // Automatically trigger error notification toast for failed API calls
    const isSilentRefresh = originalRequest.url?.includes('/api/auth/refresh');
    const shouldSkipToast = Boolean(originalRequest.skipErrorToast || isSilentRefresh);

    if (!shouldSkipToast) {
      const errorMsg = extractApiErrorMessage(error);
      const status = error.response?.status;
      const title = status ? `Lỗi API (${status})` : 'Lỗi kết nối';
      const toastKey = `api-err-${status ?? 'net'}-${errorMsg.slice(0, 50)}`;
      toast.error(errorMsg, {
        id: toastKey,
        title,
      });
    }

    return Promise.reject(error);
  }
);
