import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/authApi';
import { performSingleFlightTokenRefresh } from '@/shared/api/httpClient';
import { useAuthStore, hasPermission, hasAnyPermission, hasRole } from '../store/authStore';
import type { LoginRequestDto, RegisterUserRequestDto } from '@/shared/api/openapi.types';

export const AUTH_QUERY_KEYS = {
  me: ['auth', 'me'] as const,
};

/**
 * Bootstraps authentication session on app startup using silent Refresh Token cookie check.
 */
export function useBootstrapAuth() {
  const { isInitialized, accessToken, markInitialized } = useAuthStore();

  useEffect(() => {
    if (isInitialized) return;
    if (accessToken) {
      markInitialized();
      return;
    }

    // Shared single-flight refresh: StrictMode runs this effect twice, and two parallel refreshes with the
    // same rotating cookie make the second one fail (BE then deletes the cookie) and log the user out.
    // The refresh result reaches the store through registerAuthSyncCallback, even after unmount.
    void performSingleFlightTokenRefresh().finally(() => useAuthStore.getState().markInitialized());
  }, [isInitialized, accessToken, markInitialized]);
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
    enabled: Boolean(isAuthenticated && accessToken),
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
