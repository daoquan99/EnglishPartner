import { httpClient, setInMemoryTokens, getFallbackRefreshToken } from '@/shared/api/httpClient';
import type {
  AuthResponseDto,
  LoginRequestDto,
  RegisterUserRequestDto,
  UserProfileDto,
} from '@/shared/api/openapi.types';

export const authApi = {
  async login(payload: LoginRequestDto): Promise<AuthResponseDto> {
    const { data } = await httpClient.post<AuthResponseDto>('/api/auth/login', payload);
    setInMemoryTokens(data.accessToken, data.refreshToken);
    return data;
  },

  async register(payload: RegisterUserRequestDto): Promise<AuthResponseDto> {
    const { data } = await httpClient.post<AuthResponseDto>('/api/auth/register', payload);
    setInMemoryTokens(data.accessToken, data.refreshToken);
    return data;
  },

  async refreshSession(): Promise<AuthResponseDto> {
    const { data } = await httpClient.post<AuthResponseDto>('/api/auth/refresh', {
      refreshToken: getFallbackRefreshToken() ?? '',
      deviceInfo: navigator.userAgent.slice(0, 120),
    });
    setInMemoryTokens(data.accessToken, data.refreshToken);
    return data;
  },

  async fetchMe(): Promise<UserProfileDto> {
    const { data } = await httpClient.get<UserProfileDto>('/api/auth/me');
    return data;
  },

  async logout(): Promise<void> {
    try {
      await httpClient.post('/api/auth/revoke', {
        refreshToken: getFallbackRefreshToken() ?? '',
        reason: 'User logout from FE',
      });
    } catch {
      // Ignore network errors on logout
    } finally {
      setInMemoryTokens(null, null);
    }
  },
};
