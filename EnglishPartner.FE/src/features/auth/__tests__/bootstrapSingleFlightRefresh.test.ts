import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { performSingleFlightTokenRefresh } from '@/shared/api/httpClient';
import { useAuthStore } from '../store/authStore';

const authResponse = {
  accessToken: 'access-1',
  refreshToken: 'refresh-1',
  user: { id: 'u1', username: 'learner', email: 'l@example.com', roles: ['LEARNER'], permissions: [] },
};

describe('silent refresh on page reload', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.stubGlobal('navigator', { userAgent: 'vitest' });
    useAuthStore.getState().clearSession();
  });

  it('sends one refresh request when bootstrap runs twice concurrently (StrictMode)', async () => {
    let resolvePost: (v: unknown) => void = () => {};
    const post = vi.spyOn(axios, 'post').mockImplementation(
      () => new Promise((resolve) => { resolvePost = resolve; })
    );

    const first = performSingleFlightTokenRefresh();
    const second = performSingleFlightTokenRefresh();
    resolvePost({ data: authResponse });

    expect(await first).toBe('access-1');
    expect(await second).toBe('access-1');
    expect(post).toHaveBeenCalledTimes(1);
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });
});
