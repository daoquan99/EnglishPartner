import { describe, it, expect } from 'vitest';
import { filterAdminMenuByUserPermissions } from '../components/AdminSidebar';
import type { UserProfileDto } from '@/shared/api/openapi.types';

const baseUser: UserProfileDto = {
  id: 'u1',
  username: 'teacher1',
  email: 'teacher1@lingomint.local',
  fullName: 'Teacher User',
  preferredCefrLevel: 'B2',
  preferredVoiceName: 'Aoede',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  isActive: true,
  roles: ['TEACHER'],
  permissions: ['topics.manage', 'prompts.manage'],
};

describe('AdminSidebar Automatic Permission Filtering', () => {
  it('shows only menu items matching the user permissions', () => {
    const visible = filterAdminMenuByUserPermissions(baseUser);
    const paths = visible.map((i) => i.to);
    expect(paths).toContain('/admin');
    expect(paths).toContain('/admin/topics');
    expect(paths).toContain('/admin/prompts');
    expect(paths).not.toContain('/admin/users');
    expect(paths).not.toContain('/admin/roles');
  });

  it('hides all admin menu items for a regular LEARNER without admin permissions', () => {
    const learner: UserProfileDto = {
      ...baseUser,
      roles: ['LEARNER'],
      permissions: ['conversations.access', 'vocabularies.read'],
    };
    expect(filterAdminMenuByUserPermissions(learner)).toHaveLength(0);
  });
});
