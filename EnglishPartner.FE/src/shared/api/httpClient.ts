import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { AuthResponseDto } from './openapi.types';

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

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
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

    return Promise.reject(error);
  }
);
