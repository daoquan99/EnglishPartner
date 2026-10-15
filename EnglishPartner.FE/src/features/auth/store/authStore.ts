import { create } from 'zustand';
import type { AuthResponseDto, UserProfileDto } from '@/shared/api/openapi.types';
import { registerAuthSyncCallback, setInMemoryTokens } from '@/shared/api/httpClient';

/**
 * Zustand store strictly for Client Authentication UI status & In-Memory Token reference.
 * Note: Access Token is kept in memory only (NOT stored in localStorage).
 */
interface AuthUiState {
  isAuthenticated: boolean;
  isInitialized: boolean;
  accessToken: string | null;
  currentUser: UserProfileDto | null;
  setSession: (auth: AuthResponseDto) => void;
  setUserProfile: (user: UserProfileDto) => void;
  clearSession: () => void;
  markInitialized: () => void;
}

const SESSION_USER_HINT_KEY = 'elt_session_user_hint_v2';

function loadInitialUserHint(): UserProfileDto | null {
  try {
    const raw = sessionStorage.getItem(SESSION_USER_HINT_KEY);
    return raw ? (JSON.parse(raw) as UserProfileDto) : null;
  } catch {
    return null;
  }
}

const initialHint = loadInitialUserHint();

export const useAuthStore = create<AuthUiState>((set) => ({
  isAuthenticated: Boolean(initialHint),
  isInitialized: false,
  accessToken: null,
  currentUser: initialHint,

  setSession: (auth: AuthResponseDto) => {
    setInMemoryTokens(auth.accessToken, auth.refreshToken);
    try {
      sessionStorage.setItem(SESSION_USER_HINT_KEY, JSON.stringify(auth.user));
    } catch {
      // ignore storage errors
    }
    set({
      isAuthenticated: true,
      isInitialized: true,
      accessToken: auth.accessToken,
      currentUser: auth.user,
    });
  },

  setUserProfile: (user: UserProfileDto) => {
    try {
      sessionStorage.setItem(SESSION_USER_HINT_KEY, JSON.stringify(user));
    } catch {
      // ignore
    }
    set({
      isAuthenticated: true,
      currentUser: user,
    });
  },

  clearSession: () => {
    setInMemoryTokens(null, null);
    try {
      sessionStorage.removeItem(SESSION_USER_HINT_KEY);
    } catch {
      // ignore
    }
    set({
      isAuthenticated: false,
      isInitialized: true,
      accessToken: null,
      currentUser: null,
    });
  },

  markInitialized: () => set({ isInitialized: true }),
}));

// Synchronize Axios single-flight 401 refresh results with Zustand store
registerAuthSyncCallback((auth) => {
  if (auth) {
    useAuthStore.getState().setSession(auth);
  } else {
    useAuthStore.getState().clearSession();
  }
});

/**
 * Pure RBAC helper functions used by RequirePermission, CanAccess, and AdminSidebar.
 */
export function hasPermission(user: UserProfileDto | null | undefined, permission: string): boolean {
  if (!user) return false;
  if (user.roles.some((r) => r.toUpperCase() === 'ADMIN')) return true;
  return user.permissions.some((p) => p.toLowerCase() === permission.toLowerCase());
}

export function hasAnyPermission(
  user: UserProfileDto | null | undefined,
  permissions: string[]
): boolean {
  if (!permissions || permissions.length === 0) return true;
  return permissions.some((p) => hasPermission(user, p));
}

export function hasRole(user: UserProfileDto | null | undefined, role: string): boolean {
  if (!user) return false;
  return user.roles.some((r) => r.toUpperCase() === role.toUpperCase());
}
