import React from 'react';
import { NavLink, Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
  Globe,
} from 'lucide-react';
import { hasAnyPermission, useCurrentUser, useLogoutMutation } from '@/features/auth';
import { useTutorSessionStore } from '@/features/tutor';
import type { UserProfileDto } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';
import { toast } from '@/shared/lib/toast';

export interface AdminNavItem {
  to: string;
  label: string;
  description: string;
  labelKey?: string;
  descriptionKey?: string;
  requiredPermissions: string[];
  iconName: 'dashboard' | 'vocabularies' | 'users' | 'roles' | 'prompts' | 'voices' | 'topics' | 'rubrics';
}

export const ADMIN_NAVIGATION_ITEMS: AdminNavItem[] = [
  {
    to: '/admin',
    label: 'Tổng quan Quản trị',
    description: 'Thống kê hệ thống & trạng thái DB',
    labelKey: 'nav.adminDashboard',
    descriptionKey: 'nav.adminDashboardSub',
    requiredPermissions: [
      'users.read',
      'roles.manage',
      'prompts.manage',
      'voices.manage',
      'topics.manage',
      'rubrics.manage',
      'vocabularies.manage',
    ],
    iconName: 'dashboard',
  },
  {
    to: '/admin/vocabularies',
    label: 'Từ vựng / Flashcards',
    description: 'CRUD Từ vựng, IPA, Ví dụ & Nghĩa',
    labelKey: 'nav.adminVocabularies',
    descriptionKey: 'nav.adminVocabulariesSub',
    requiredPermissions: ['vocabularies.manage', 'vocabularies.generate', 'topics.manage'],
    iconName: 'vocabularies',
  },
  {
    to: '/admin/topics',
    label: 'Quản lý Topics (Aggregate)',
    description: 'Chủ đề hội thoại & từ vựng gợi ý',
    labelKey: 'nav.adminTopics',
    descriptionKey: 'nav.adminTopicsSub',
    requiredPermissions: ['topics.manage'],
    iconName: 'topics',
  },
  {
    to: '/admin/prompts',
    label: 'CEFR, Modes & Tốc độ nói',
    description: 'Prompts & SpeechSpeeds trong DB',
    labelKey: 'nav.adminPrompts',
    descriptionKey: 'nav.adminPromptsSub',
    requiredPermissions: ['prompts.manage'],
    iconName: 'prompts',
  },
  {
    to: '/admin/voices',
    label: '30 Giọng đọc Teacher',
    description: 'Google Prebuilt Voices Catalog',
    labelKey: 'nav.adminVoices',
    descriptionKey: 'nav.adminVoicesSub',
    requiredPermissions: ['voices.manage'],
    iconName: 'voices',
  },
  {
    to: '/admin/rubrics',
    label: 'Rubric & AI Audio Models',
    description: 'Tiêu chí chấm phát âm & Model AI',
    labelKey: 'nav.adminRubrics',
    descriptionKey: 'nav.adminRubricsSub',
    requiredPermissions: ['rubrics.manage'],
    iconName: 'rubrics',
  },
  {
    to: '/admin/users',
    label: 'Người dùng & Phân quyền',
    description: 'Quản lý Users & gán Role',
    labelKey: 'nav.adminUsers',
    descriptionKey: 'nav.adminUsersSub',
    requiredPermissions: ['users.read', 'users.manage'],
    iconName: 'users',
  },
  {
    to: '/admin/roles',
    label: 'Roles & Permissions',
    description: 'Ma trận quyền RBAC hệ thống',
    labelKey: 'nav.adminRoles',
    descriptionKey: 'nav.adminRolesSub',
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

const ADMIN_NAV_SVG_ICONS: Record<AdminNavItem['iconName'], string> = {
  dashboard: '/assets/icons/navigation/nav-dashboard.svg',
  vocabularies: '/assets/icons/navigation/nav-reviews.svg',
  topics: '/assets/icons/navigation/nav-topics.svg',
  prompts: '/assets/icons/navigation/nav-prompts.svg',
  voices: '/assets/icons/navigation/nav-voices.svg',
  rubrics: '/assets/icons/navigation/nav-rubrics.svg',
  users: '/assets/icons/navigation/nav-account.svg',
  roles: '/assets/icons/navigation/nav-roles.svg',
};

function renderIcon(name: AdminNavItem['iconName']) {
  const iconSrc = ADMIN_NAV_SVG_ICONS[name];
  if (iconSrc) {
    return <img src={iconSrc} alt="" className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs" />;
  }
  return <LayoutDashboard className="w-5 h-5" />;
}

/**
 * Collapsible Left Sidebar Menu for Admin Portal:
 * - Automatically filters navigation items by user's RBAC permissions
 * - Has a toggle button to collapse/expand: when collapsed, ONLY shows icons!
 */
export const AdminSidebar: React.FC = () => {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();
  const { isAdminSidebarCollapsed, toggleAdminSidebar } = useTutorSessionStore();
  const visibleMenu = filterAdminMenuByUserPermissions(user);

  const toggleLanguage = () => {
    const next = i18n.language === 'vi' ? 'en' : 'vi';
    void i18n.changeLanguage(next);
    toast.info(next === 'vi' ? 'Đã đổi ngôn ngữ sang Tiếng Việt' : 'Language switched to English');
  };

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
              title={`${t('appTitle')} Admin Portal`}
              className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-emerald-500 to-teal-400 p-2 flex items-center justify-center font-extrabold shrink-0 shadow-md shadow-emerald-500/20"
            >
              <img
                src="/assets/brand/logo-mark.svg"
                alt="Admin"
                className="w-full h-full object-contain brightness-0 invert"
              />
            </div>
            {!isAdminSidebarCollapsed && (
              <div className="min-w-0">
                <div className="text-sm font-extrabold text-white truncate">{t('admin.portalTitle')}</div>
                <div className="text-[11px] text-emerald-400 font-semibold truncate">
                  RBAC · {user?.roles.join(', ')}
                </div>
              </div>
            )}
          </div>

          <button
            type="button"
            onClick={toggleAdminSidebar}
            title={isAdminSidebarCollapsed ? t('nav.expandMenu') : t('nav.collapseMenu')}
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
              title={`${item.labelKey ? t(item.labelKey) : item.label} — ${item.descriptionKey ? t(item.descriptionKey) : item.description}`}
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
                  <div className="text-xs font-bold truncate">
                    {item.labelKey ? t(item.labelKey) : item.label}
                  </div>
                  <div className="text-[10px] opacity-75 truncate">
                    {item.descriptionKey ? t(item.descriptionKey) : item.description}
                  </div>
                </div>
              )}
            </NavLink>
          ))}
        </nav>
      </div>

      {/* Bottom Actions: Language Switch, Switch to Student Portal & Logout */}
      <div className="space-y-2 pt-4 border-t border-slate-800">
        <button
          type="button"
          onClick={toggleLanguage}
          title={`${t('common.language')}: ${i18n.language.toUpperCase()}`}
          className={cn(
            'w-full flex items-center rounded-2xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white font-bold text-xs transition-colors cursor-pointer',
            isAdminSidebarCollapsed ? 'justify-center w-12 h-12 mx-auto' : 'justify-between px-3.5 py-2.5'
          )}
        >
          <div className="flex items-center gap-2.5">
            <Globe className="w-4 h-4 shrink-0 text-emerald-400" />
            {!isAdminSidebarCollapsed && <span>{t('common.language')}</span>}
          </div>
          {!isAdminSidebarCollapsed && (
            <span className="px-2 py-0.5 text-[10px] font-extrabold uppercase rounded bg-slate-900 text-emerald-400 border border-slate-700">
              {i18n.language.toUpperCase()}
            </span>
          )}
        </button>

        <Link
          to="/student/tutor"
          title={t('common.backToStudent')}
          className={cn(
            'flex items-center rounded-2xl bg-slate-800 hover:bg-slate-700 text-emerald-300 font-bold text-xs transition-colors',
            isAdminSidebarCollapsed ? 'justify-center w-12 h-12 mx-auto' : 'gap-2.5 px-3.5 py-2.5'
          )}
        >
          <img
            src="/assets/icons/navigation/nav-study.svg"
            alt=""
            className="w-4 h-4 shrink-0 object-contain drop-shadow-2xs"
          />
          {!isAdminSidebarCollapsed && <span className="truncate">{t('common.backToStudent')}</span>}
        </Link>

        <button
          type="button"
          onClick={handleLogout}
          title={`${t('common.logout')} (${user?.username ?? ''})`}
          className={cn(
            'w-full flex items-center rounded-2xl bg-rose-600/15 hover:bg-rose-600/25 text-rose-300 font-bold text-xs transition-colors cursor-pointer',
            isAdminSidebarCollapsed ? 'justify-center w-12 h-12 mx-auto' : 'gap-2.5 px-3.5 py-2.5'
          )}
        >
          <LogOut className="w-4 h-4 shrink-0" />
          {!isAdminSidebarCollapsed && (
            <span className="truncate">
              {t('common.logout')} ({user?.username})
            </span>
          )}
        </button>
      </div>
    </aside>
  );
};
