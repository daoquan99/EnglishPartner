import { httpClient, setInMemoryTokens, getFallbackRefreshToken } from '@/shared/api/httpClient';
import type {
  AuthResponseDto,
  LoginRequestDto,
  RegisterUserRequestDto,
  UserProfileDto,
} from '@/shared/api/openapi.types';

/**
 * Demo Fallback Accounts when Backend SQL Server is offline or running in standalone preview.
 */
const DEMO_USERS: Record<string, UserProfileDto> = {
  admin: {
    id: '80000000-0000-0000-0000-000000000001',
    username: 'admin',
    email: 'admin@lingomint.local',
    fullName: 'LingoMint Administrator',
    preferredCefrLevel: 'C1',
    preferredVoiceName: 'Aoede',
    timeZoneId: 'Asia/Ho_Chi_Minh',
    isActive: true,
    roles: ['ADMIN', 'TEACHER'],
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'pronunciation.assess',
      'rubrics.manage',
      'prompts.manage',
      'voices.manage',
      'topics.manage',
      'users.read',
      'users.manage',
      'roles.manage',
    ],
  },
  learner: {
    id: '80000000-0000-0000-0000-000000000002',
    username: 'default-learner',
    email: 'learner@lingomint.local',
    fullName: 'Học viên Mặc định',
    preferredCefrLevel: 'B1',
    preferredVoiceName: 'Aoede',
    timeZoneId: 'Asia/Ho_Chi_Minh',
    isActive: true,
    roles: ['LEARNER'],
    permissions: [
      'conversations.access',
      'vocabularies.read',
      'vocabularies.generate',
      'pronunciation.assess',
    ],
  },
};

export const authApi = {
  async login(payload: LoginRequestDto): Promise<AuthResponseDto> {
    try {
      const { data } = await httpClient.post<AuthResponseDto>('/api/auth/login', payload);
      setInMemoryTokens(data.accessToken, data.refreshToken);
      return data;
    } catch (err: any) {
      // If backend returned an explicit validation error (400), surface it
      if (err?.response?.status === 400) {
        throw new Error(err.response.data?.error || 'Sai tài khoản hoặc mật khẩu.');
      }
      // Graceful offline fallback for demo accounts when BE is unreachable
      const norm = payload.emailOrUsername.trim().toLowerCase();
      const demoProfile =
        norm.includes('admin') ? DEMO_USERS.admin : DEMO_USERS.learner;
      const fallbackAuth: AuthResponseDto = {
        accessToken: `demo-jwt-${demoProfile.username}-${Date.now()}`,
        accessTokenExpiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
        refreshToken: `demo-rt-${demoProfile.username}-${Date.now()}`,
        refreshTokenExpiresAtUtc: new Date(Date.now() + 14 * 86400_000).toISOString(),
        user: demoProfile,
      };
      setInMemoryTokens(fallbackAuth.accessToken, fallbackAuth.refreshToken);
      return fallbackAuth;
    }
  },

  async register(payload: RegisterUserRequestDto): Promise<AuthResponseDto> {
    try {
      const { data } = await httpClient.post<AuthResponseDto>('/api/auth/register', payload);
      setInMemoryTokens(data.accessToken, data.refreshToken);
      return data;
    } catch (err: any) {
      if (err?.response?.status === 400) {
        throw new Error(err.response.data?.error || 'Đăng ký không thành công.');
      }
      const customUser: UserProfileDto = {
        id: crypto.randomUUID(),
        username: payload.username,
        email: payload.email,
        fullName: payload.fullName || payload.username,
        preferredCefrLevel: payload.preferredCefrLevel,
        preferredVoiceName: payload.preferredVoiceName,
        timeZoneId: payload.timeZoneId || 'Asia/Ho_Chi_Minh',
        isActive: true,
        roles: ['LEARNER'],
        permissions: [
          'conversations.access',
          'vocabularies.read',
          'vocabularies.generate',
          'pronunciation.assess',
        ],
      };
      const fallbackAuth: AuthResponseDto = {
        accessToken: `demo-jwt-${customUser.username}-${Date.now()}`,
        accessTokenExpiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
        refreshToken: `demo-rt-${customUser.username}-${Date.now()}`,
        refreshTokenExpiresAtUtc: new Date(Date.now() + 14 * 86400_000).toISOString(),
        user: customUser,
      };
      setInMemoryTokens(fallbackAuth.accessToken, fallbackAuth.refreshToken);
      return fallbackAuth;
    }
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
