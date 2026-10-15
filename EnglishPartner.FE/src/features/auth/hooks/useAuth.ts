import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/authApi';
import { useAuthStore, hasPermission, hasAnyPermission, hasRole } from '../store/authStore';
import type { LoginRequestDto, RegisterUserRequestDto } from '@/shared/api/openapi.types';

export const AUTH_QUERY_KEYS = {
  me: ['auth', 'me'] as const,
};

/**
 * Bootstraps authentication session on app startup using silent Refresh Token cookie check.
 */
export function useBootstrapAuth() {
  const { isInitialized, accessToken, currentUser, setSession, clearSession, markInitialized } =
    useAuthStore();

  useEffect(() => {
    if (isInitialized) return;
    let mounted = true;

    (async () => {
      try {
        // Attempt silent token refresh using HttpOnly cookie if we don't have an accessToken in memory yet
        if (!accessToken) {
          const refreshed = await authApi.refreshSession();
          if (mounted) setSession(refreshed);
          return;
        }
      } catch {
        // If we have a demo user hint in sessionStorage, keep it usable in offline mode
        if (mounted && currentUser) {
          markInitialized();
          return;
        }
        if (mounted) clearSession();
      } finally {
        if (mounted) markInitialized();
      }
    })();

    return () => {
      mounted = false;
    };
  }, [isInitialized, accessToken, currentUser, setSession, clearSession, markInitialized]);
}

export function useCurrentUser() {
  const { currentUser, isAuthenticated, accessToken, setUserProfile } = useAuthStore();

  const query = useQuery({
    queryKey: AUTH_QUERY_KEYS.me,
    queryFn: async () => {
      const profile = await authApi.fetchMe();
      setUserProfile(profile);
      return profile;
    },
    enabled: Boolean(isAuthenticated && accessToken && !accessToken.startsWith('demo-jwt-')),
    initialData: currentUser ?? undefined,
    staleTime: 5 * 60_000,
    retry: false,
  });

  const user = query.data ?? currentUser;

  return {
    user,
    isAuthenticated: Boolean(isAuthenticated && user),
    isLoading: query.isLoading,
    hasPermission: (perm: string) => hasPermission(user, perm),
    hasAnyPermission: (perms: string[]) => hasAnyPermission(user, perms),
    hasRole: (role: string) => hasRole(user, role),
  };
}

export function useLoginMutation() {
  const queryClient = useQueryClient();
  const setSession = useAuthStore((s) => s.setSession);

  return useMutation({
    mutationFn: (payload: LoginRequestDto) => authApi.login(payload),
    onSuccess: (auth) => {
      // Clear previous user's cache before setting new user
      queryClient.clear();
      setSession(auth);
      queryClient.setQueryData(AUTH_QUERY_KEYS.me, auth.user);
    },
  });
}

export function useRegisterMutation() {
  const queryClient = useQueryClient();
  const setSession = useAuthStore((s) => s.setSession);

  return useMutation({
    mutationFn: (payload: RegisterUserRequestDto) => authApi.register(payload),
    onSuccess: (auth) => {
      queryClient.clear();
      setSession(auth);
      queryClient.setQueryData(AUTH_QUERY_KEYS.me, auth.user);
    },
  });
}

export function useLogoutMutation() {
  const queryClient = useQueryClient();
  const clearSession = useAuthStore((s) => s.clearSession);

  return useMutation({
    mutationFn: () => authApi.logout(),
    onSettled: () => {
      // Security Requirement: Clear all TanStack Query cached user data on logout
      clearSession();
      queryClient.clear();
    },
  });
}
