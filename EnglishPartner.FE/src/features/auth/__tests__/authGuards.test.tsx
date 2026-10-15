import { describe, it, expect } from 'vitest';
import { hasPermission, hasAnyPermission, hasRole } from '../store/authStore';
import type { UserProfileDto } from '@/shared/api/openapi.types';

const mockLearner: UserProfileDto = {
  id: '1',
  username: 'student1',
  email: 'student1@lingomint.local',
  fullName: 'Student One',
  preferredCefrLevel: 'B1',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['LEARNER'],
  permissions: ['conversations.access', 'vocabularies.read'],
};

const mockAdmin: UserProfileDto = {
  ...mockLearner,
  id: '2',
  username: 'admin',
  roles: ['ADMIN'],
  permissions: [],
};

describe('Auth RBAC & Permission Helpers', () => {
  it('checks exact permission for Learner', () => {
    expect(hasPermission(mockLearner, 'conversations.access')).toBe(true);
    expect(hasPermission(mockLearner, 'users.manage')).toBe(false);
  });

  it('grants all permissions automatically to ADMIN role', () => {
    expect(hasPermission(mockAdmin, 'users.manage')).toBe(true);
    expect(hasPermission(mockAdmin, 'topics.manage')).toBe(true);
  });

  it('evaluates hasAnyPermission and hasRole accurately', () => {
    expect(hasAnyPermission(mockLearner, ['users.manage', 'vocabularies.read'])).toBe(true);
    expect(hasRole(mockLearner, 'LEARNER')).toBe(true);
    expect(hasRole(mockLearner, 'ADMIN')).toBe(false);
  });
});
