import React from 'react';
import { NavLink, Link, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  ShieldCheck,
  MessageSquareCode,
  Volume2,
  Tags,
  Award,
  PanelLeftClose,
  PanelLeftOpen,
  ArrowLeft,
  LogOut,
  BookOpen,
} from 'lucide-react';
import { hasAnyPermission, useCurrentUser, useLogoutMutation } from '@/features/auth';
import { useTutorSessionStore } from '@/features/tutor';
import type { UserProfileDto } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';

export interface AdminNavItem {
  to: string;
  label: string;
  description: string;
  requiredPermissions: string[];
  iconName: 'dashboard' | 'vocabularies' | 'users' | 'roles' | 'prompts' | 'voices' | 'topics' | 'rubrics';
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
      'vocabularies.manage',
      'vocabularies.read',
    ],
    iconName: 'dashboard',
  },
  {
    to: '/admin/vocabularies',
    label: 'Từ vựng / Flashcards',
    description: 'CRUD Từ vựng, IPA, Ví dụ & Nghĩa',
    requiredPermissions: ['vocabularies.manage', 'vocabularies.generate', 'topics.manage'],
    iconName: 'vocabularies',
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
    label: 'CEFR, Modes & Tốc độ nói',
    description: 'Prompts & SpeechSpeeds trong DB',
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
      return <LayoutDashboard className="w-5 h-5" />;
    case 'vocabularies':
      return <BookOpen className="w-5 h-5" />;
    case 'topics':
      return <Tags className="w-5 h-5" />;
    case 'prompts':
      return <MessageSquareCode className="w-5 h-5" />;
    case 'voices':
      return <Volume2 className="w-5 h-5" />;
    case 'rubrics':
      return <Award className="w-5 h-5" />;
    case 'users':
      return <Users className="w-5 h-5" />;
    case 'roles':
      return <ShieldCheck className="w-5 h-5" />;
  }
}

/**
 * Collapsible Left Sidebar Menu for Admin Portal:
 * - Automatically filters navigation items by user's RBAC permissions
 * - Has a toggle button to collapse/expand: when collapsed, ONLY shows icons!
 */
export const AdminSidebar: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();
  const { isAdminSidebarCollapsed, toggleAdminSidebar } = useTutorSessionStore();
  const visibleMenu = filterAdminMenuByUserPermissions(user);

  const handleLogout = async () => {
    await logoutMutation.mutateAsync();
    navigate('/login', { replace: true });
  };

  return (
    <aside
      className={cn(
        'h-screen shrink-0 bg-slate-900 text-slate-100 border-r border-slate-800 flex flex-col justify-between transition-all duration-200 select-none overflow-y-auto overflow-x-hidden z-30',
        isAdminSidebarCollapsed ? 'w-20 px-2.5 py-4' : 'w-72 px-4 py-5'
      )}
    >
      {/* Top Brand + Collapse Toggle + Filtered Navigation Items */}
      <div className="space-y-4">
        <div
          className={cn(
            'flex items-center border-b border-slate-800 pb-4',
            isAdminSidebarCollapsed ? 'justify-center flex-col gap-2' : 'justify-between gap-2 px-1'
          )}
        >
          <div className="flex items-center gap-2.5 min-w-0">
            <div
              title="LingoMint Admin Portal"
              className="w-10 h-10 rounded-2xl bg-emerald-500 text-slate-950 flex items-center justify-center font-extrabold shrink-0 shadow-xs"
            >
              <ShieldCheck className="w-5 h-5" />
            </div>
            {!isAdminSidebarCollapsed && (
              <div className="min-w-0">
                <div className="text-sm font-extrabold text-white truncate">Admin Portal</div>
                <div className="text-[11px] text-emerald-400 font-semibold truncate">
                  RBAC · {user?.roles.join(', ')}
                </div>
              </div>
            )}
          </div>

          <button
            type="button"
            onClick={toggleAdminSidebar}
            title={isAdminSidebarCollapsed ? 'Mở rộng Left Menu' : 'Thu gọn Left Menu (Chỉ hiện Icon)'}
            className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition-colors cursor-pointer shrink-0"
          >
            {isAdminSidebarCollapsed ? (
              <PanelLeftOpen className="w-4 h-4" />
            ) : (
              <PanelLeftClose className="w-4 h-4" />
            )}
          </button>
        </div>

        {/* Dynamic Permission-Filtered Menu Items */}
        <nav className="space-y-1.5">
          {visibleMenu.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/admin'}
              title={`${item.label} — ${item.description}`}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isAdminSidebarCollapsed
                    ? 'justify-center w-12 h-12 mx-auto'
                    : 'gap-3 px-3.5 py-2.5',
                  isActive
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                )
              }
            >
              <div className="shrink-0">{renderIcon(item.iconName)}</div>
              {!isAdminSidebarCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{item.label}</div>
                  <div className="text-[10px] opacity-75 truncate">{item.description}</div>
                </div>
              )}
            </NavLink>
          ))}
        </nav>
      </div>

      {/* Bottom Actions: Switch to Student Portal & Logout */}
      <div className="space-y-2 pt-4 border-t border-slate-800">
        <Link
          to="/student/tutor"
          title="Chuyển sang Student Portal"
          className={cn(
            'flex items-center rounded-2xl bg-slate-800 hover:bg-slate-700 text-emerald-300 font-bold text-xs transition-colors',
            isAdminSidebarCollapsed ? 'justify-center w-12 h-12 mx-auto' : 'gap-2.5 px-3.5 py-2.5'
          )}
        >
          <ArrowLeft className="w-4 h-4 shrink-0" />
          {!isAdminSidebarCollapsed && <span className="truncate">Về Cổng Học Viên</span>}
        </Link>

        <button
          type="button"
          onClick={handleLogout}
          title="Đăng xuất"
          className={cn(
            'w-full flex items-center rounded-2xl bg-rose-600/15 hover:bg-rose-600/25 text-rose-300 font-bold text-xs transition-colors cursor-pointer',
            isAdminSidebarCollapsed ? 'justify-center w-12 h-12 mx-auto' : 'gap-2.5 px-3.5 py-2.5'
          )}
        >
          <LogOut className="w-4 h-4 shrink-0" />
          {!isAdminSidebarCollapsed && <span className="truncate">Đăng xuất ({user?.username})</span>}
        </button>
      </div>
    </aside>
  );
};
