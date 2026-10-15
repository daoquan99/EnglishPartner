import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  ShieldCheck,
  MessageSquareCode,
  Volume2,
  Tags,
  Award,
} from 'lucide-react';
import { hasAnyPermission, useCurrentUser } from '@/features/auth';
import type { UserProfileDto } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';

export interface AdminNavItem {
  to: string;
  label: string;
  description: string;
  requiredPermissions: string[];
  iconName: 'dashboard' | 'users' | 'roles' | 'prompts' | 'voices' | 'topics' | 'rubrics';
}

export const ADMIN_NAVIGATION_ITEMS: AdminNavItem[] = [
  {
    to: '/admin',
    label: 'Tổng quan Quản trị',
    description: 'Thống kê hệ thống & trạng thái DB',
    requiredPermissions: [
      'users.read',
      'roles.manage',
      'prompts.manage',
      'voices.manage',
      'topics.manage',
      'rubrics.manage',
    ],
    iconName: 'dashboard',
  },
  {
    to: '/admin/topics',
    label: 'Quản lý Topics (Aggregate)',
    description: 'Chủ đề hội thoại & từ vựng gợi ý',
    requiredPermissions: ['topics.manage'],
    iconName: 'topics',
  },
  {
    to: '/admin/prompts',
    label: 'CEFR & 19 Chế độ Luyện nói',
    description: 'CefrPromptBuilder trong Database',
    requiredPermissions: ['prompts.manage'],
    iconName: 'prompts',
  },
  {
    to: '/admin/voices',
    label: '30 Giọng đọc Teacher',
    description: 'Google Prebuilt Voices Catalog',
    requiredPermissions: ['voices.manage'],
    iconName: 'voices',
  },
  {
    to: '/admin/rubrics',
    label: 'Rubric & AI Audio Models',
    description: 'Tiêu chí chấm phát âm & Model AI',
    requiredPermissions: ['rubrics.manage'],
    iconName: 'rubrics',
  },
  {
    to: '/admin/users',
    label: 'Người dùng & Phân quyền',
    description: 'Quản lý Users & gán Role',
    requiredPermissions: ['users.read', 'users.manage'],
    iconName: 'users',
  },
  {
    to: '/admin/roles',
    label: 'Roles & Permissions',
    description: 'Ma trận quyền RBAC hệ thống',
    requiredPermissions: ['roles.manage'],
    iconName: 'roles',
  },
];

/**
 * Pure helper exported for unit testing: filters Admin navigation items
 * based on the authenticated user's actual roles & permissions.
 */
export function filterAdminMenuByUserPermissions(
  user: UserProfileDto | null | undefined,
  items: AdminNavItem[] = ADMIN_NAVIGATION_ITEMS
): AdminNavItem[] {
  if (!user) return [];
  return items.filter((item) => hasAnyPermission(user, item.requiredPermissions));
}

function renderIcon(name: AdminNavItem['iconName']) {
  switch (name) {
    case 'dashboard':
      return <LayoutDashboard className="w-4 h-4" />;
    case 'topics':
      return <Tags className="w-4 h-4" />;
    case 'prompts':
      return <MessageSquareCode className="w-4 h-4" />;
    case 'voices':
      return <Volume2 className="w-4 h-4" />;
    case 'rubrics':
      return <Award className="w-4 h-4" />;
    case 'users':
      return <Users className="w-4 h-4" />;
    case 'roles':
      return <ShieldCheck className="w-4 h-4" />;
  }
}

export const AdminSidebar: React.FC = () => {
  const { user } = useCurrentUser();
  const visibleMenu = filterAdminMenuByUserPermissions(user);

  return (
    <aside className="w-full lg:w-72 shrink-0 bg-slate-900 text-slate-100 rounded-3xl p-4 space-y-4 shadow-lg">
      <div className="px-3 py-2 border-b border-slate-800">
        <div className="text-xs font-bold uppercase tracking-wider text-emerald-400">
          Admin Portal · RBAC
        </div>
        <div className="text-sm font-extrabold text-white mt-0.5">
          {user?.fullName || user?.username}
        </div>
        <div className="text-[11px] text-slate-400 mt-0.5">
          Roles: {user?.roles.join(', ') || 'None'}
        </div>
      </div>

      <nav className="space-y-1">
        {visibleMenu.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/admin'}
            className={({ isActive }) =>
              cn(
                'flex items-start gap-3 px-3.5 py-2.5 rounded-2xl transition-all',
                isActive
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              )
            }
          >
            <div className="mt-0.5 shrink-0">{renderIcon(item.iconName)}</div>
            <div className="min-w-0">
              <div className="text-xs font-bold truncate">{item.label}</div>
              <div className="text-[10px] opacity-75 truncate">{item.description}</div>
            </div>
          </NavLink>
        ))}
      </nav>
    </aside>
  );
};
