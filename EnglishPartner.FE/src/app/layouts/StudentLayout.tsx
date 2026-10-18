import React, { useEffect, useRef } from 'react';
import { NavLink, Outlet, Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Mic,
  BookOpen,
  ShieldCheck,
  LogOut,
  Globe,
  Sparkles,
  User as UserIcon,
  PanelLeftClose,
  PanelLeftOpen,
  KeyRound,
  Eye,
  EyeOff,
} from 'lucide-react';
import { CanAccess, useCurrentUser, useLogoutMutation } from '@/features/auth';
import { useTutorSessionStore } from '@/features/tutor';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import { toast } from '@/shared/lib/toast';

const ADMIN_ANY_PERMISSIONS = [
  'users.read',
  'users.manage',
  'roles.manage',
  'prompts.manage',
  'voices.manage',
  'topics.manage',
  'rubrics.manage',
];

/**
 * Student Portal App Shell:
 * - Outer container: `h-screen w-full overflow-hidden flex` (prevents body scroll)
 * - Left Menu (`aside`): `h-screen shrink-0 overflow-y-auto` fixed on the left, collapsible to icon-only (`w-20`)
 * - Main Content (`main`): `flex-1 h-screen overflow-y-auto` scrolls independently
 * - Houses the single Google AI Studio API Key input for the entire Student Portal
 */
export const StudentLayout: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();
  const leftMenuApiKeyRef = useRef<HTMLInputElement | null>(null);

  const {
    apiKey,
    showApiKey,
    apiKeyFocusTrigger,
    isStudentSidebarCollapsed,
    setApiKey,
    toggleShowApiKey,
    toggleStudentSidebar,
  } = useTutorSessionStore();

  useEffect(() => {
    if (apiKeyFocusTrigger > 0) {
      setTimeout(() => {
        leftMenuApiKeyRef.current?.focus();
      }, 60);
    }
  }, [apiKeyFocusTrigger]);

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
    <div className="h-screen w-full overflow-hidden learnerTheme bg-slate-50/70 text-slate-900 flex">
      {/* Fixed h-screen Collapsible Left Menu (Sidebar) */}
      <aside
        className={cn(
          'h-screen shrink-0 bg-white border-r border-slate-200/90 flex flex-col justify-between transition-all duration-200 select-none overflow-y-auto overflow-x-hidden z-30 shadow-2xs',
          isStudentSidebarCollapsed ? 'w-20 px-2.5 py-4' : 'w-64 sm:w-72 px-4 py-5'
        )}
      >
        {/* Top Section: Brand + Collapse Toggle + Navigation Links + Single API Key Input */}
        <div className="space-y-4">
          <div
            className={cn(
              'flex items-center border-b border-slate-100 pb-4',
              isStudentSidebarCollapsed ? 'justify-center flex-col gap-2' : 'justify-between gap-2 px-1'
            )}
          >
            <Link
              to="/student/tutor"
              title={`${t('appTitle')} — ${t('studentPortal')}`}
              className="flex items-center gap-2.5 min-w-0"
            >
              <div className="w-10 h-10 rounded-2xl bg-emerald-50 border border-emerald-200/80 flex items-center justify-center shadow-xs shrink-0 p-1">
                <img
                  src="/assets/brand/logo-mark.svg"
                  alt="LingoMint"
                  className="w-8 h-8 object-contain"
                />
              </div>
              {!isStudentSidebarCollapsed && (
                <div className="min-w-0">
                  <span className="text-sm font-extrabold text-slate-900 tracking-tight block truncate">
                    {t('appTitle')}
                  </span>
                  <span className="text-[11px] font-semibold text-emerald-700 block truncate">
                    {t('studentPortal')}
                  </span>
                </div>
              )}
            </Link>

            <button
              type="button"
              onClick={toggleStudentSidebar}
              title={
                isStudentSidebarCollapsed
                  ? t('nav.expandMenu')
                  : t('nav.collapseMenu')
              }
              className="p-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer shrink-0"
            >
              {isStudentSidebarCollapsed ? (
                <PanelLeftOpen className="w-4 h-4" />
              ) : (
                <PanelLeftClose className="w-4 h-4" />
              )}
            </button>
          </div>

          {/* Primary Student Navigation Items */}
          <nav className="space-y-1.5">
            <NavLink
              to="/student/tutor"
              title={t('nav.liveTutor')}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isStudentSidebarCollapsed
                    ? 'justify-center w-12 h-12 mx-auto'
                    : 'gap-3 px-3.5 py-3',
                  isActive
                    ? 'bg-emerald-600 text-white shadow-xs'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )
              }
            >
              <img
                src="/assets/icons/navigation/nav-conversations.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isStudentSidebarCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{t('nav.liveTutor')}</div>
                  <div className="text-[10px] opacity-80 truncate">
                    {t('nav.liveTutorSub')}
                  </div>
                </div>
              )}
            </NavLink>

            <NavLink
              to="/student/vocabulary"
              title={t('nav.dailyVocab')}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isStudentSidebarCollapsed
                    ? 'justify-center w-12 h-12 mx-auto'
                    : 'gap-3 px-3.5 py-3',
                  isActive
                    ? 'bg-emerald-600 text-white shadow-xs'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )
              }
            >
              <img
                src="/assets/icons/navigation/nav-study.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isStudentSidebarCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{t('nav.dailyVocab')}</div>
                  <div className="text-[10px] opacity-80 truncate">
                    {t('nav.dailyVocabSub')}
                  </div>
                </div>
              )}
            </NavLink>

            <NavLink
              to="/student/lessons"
              title={t('nav.lessons') || 'Bài học & Luyện tập'}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isStudentSidebarCollapsed
                    ? 'justify-center w-12 h-12 mx-auto'
                    : 'gap-3 px-3.5 py-3',
                  isActive
                    ? 'bg-emerald-600 text-white shadow-xs'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )
              }
            >
              <img
                src="/assets/icons/navigation/nav-reviews.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isStudentSidebarCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">Bài học & Luyện tập</div>
                  <div className="text-[10px] opacity-80 truncate">
                    16 loại bài tập đa kỹ năng
                  </div>
                </div>
              )}
            </NavLink>
          </nav>

          {/* Single Google AI Studio API Key Setup inside Left Menu */}
          {isStudentSidebarCollapsed ? (
            <div className="pt-2 border-t border-slate-100 flex justify-center">
              <button
                type="button"
                onClick={toggleStudentSidebar}
                title={
                  apiKey.trim()
                    ? `${t('common.apiKey')}: ${t('common.apiKeySaved')}`
                    : `${t('common.apiKey')}: ${t('common.apiKeyMissing')}`
                }
                className={cn(
                  'w-12 h-12 rounded-2xl flex items-center justify-center border transition-colors cursor-pointer',
                  apiKey.trim()
                    ? 'bg-emerald-50 border-emerald-200 text-emerald-600'
                    : 'bg-amber-50 border-amber-300 text-amber-600 animate-pulse'
                )}
              >
                <img src="/assets/icons/navigation/nav-roles.svg" alt="" className="w-5 h-5 object-contain" />
              </button>
            </div>
          ) : (
            <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/90 space-y-1.5">
              <div className="flex items-center justify-between">
                <label className="text-[11px] font-bold text-slate-700 flex items-center gap-1.5">
                  <img src="/assets/icons/navigation/nav-roles.svg" alt="" className="w-3.5 h-3.5 object-contain" />
                  {t('common.apiKey')}
                </label>
                <span
                  className={cn(
                    'text-[10px] font-bold px-1.5 py-0.2 rounded-full',
                    apiKey.trim()
                      ? 'bg-emerald-100 text-emerald-800'
                      : 'bg-amber-100 text-amber-800'
                  )}
                >
                  {apiKey.trim() ? t('common.apiKeySaved') : t('common.apiKeyMissing')}
                </span>
              </div>
              <div className="relative flex items-center">
                <input
                  ref={leftMenuApiKeyRef}
                  type={showApiKey ? 'text' : 'password'}
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                  placeholder="AIzaSy..."
                  className="w-full pl-2.5 pr-9 py-1.5 text-xs font-mono rounded-xl border border-slate-200 bg-white text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500"
                />
                <button
                  type="button"
                  onClick={toggleShowApiKey}
                  title={showApiKey ? 'Ẩn API Key' : 'Hiện API Key'}
                  className="absolute right-2 text-slate-400 hover:text-slate-700 cursor-pointer"
                >
                  {showApiKey ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Bottom Section: Admin Portal Switcher + Language + User Info + Logout */}
        <div className="space-y-2 pt-4 border-t border-slate-100">
          <CanAccess anyPermissions={ADMIN_ANY_PERMISSIONS}>
            <Link
              to="/admin"
              title={t('adminPortal')}
              className={cn(
                'flex items-center rounded-2xl bg-amber-50 hover:bg-amber-100 border border-amber-200 text-amber-900 font-bold text-xs transition-colors',
                isStudentSidebarCollapsed
                  ? 'justify-center w-12 h-12 mx-auto'
                  : 'gap-2.5 px-3.5 py-2.5'
              )}
            >
              <img
                src="/assets/icons/navigation/nav-admin.svg"
                alt=""
                className="w-4 h-4 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isStudentSidebarCollapsed && <span className="truncate">{t('adminPortal')}</span>}
            </Link>
          </CanAccess>

          <button
            type="button"
            onClick={toggleLanguage}
            title={`${t('common.language')}: ${i18n.language.toUpperCase()}`}
            className={cn(
              'w-full flex items-center rounded-2xl border border-slate-200 text-xs font-bold text-slate-600 hover:bg-slate-100 transition-colors cursor-pointer',
              isStudentSidebarCollapsed
                ? 'justify-center w-12 h-12 mx-auto'
                : 'justify-between px-3.5 py-2'
            )}
          >
            <div className="flex items-center gap-2">
              <Globe className="w-4 h-4 shrink-0 text-emerald-600" />
              {!isStudentSidebarCollapsed && <span>{t('common.language')}</span>}
            </div>
            {!isStudentSidebarCollapsed && (
              <Badge variant="default">{i18n.language.toUpperCase()}</Badge>
            )}
          </button>

          {user && !isStudentSidebarCollapsed && (
            <div className="flex items-center justify-between gap-2 px-3 py-2 rounded-2xl bg-slate-100/80 text-xs">
              <div className="flex items-center gap-2 min-w-0">
                <img
                  src="/assets/mascot/cat/cat-avatar.svg"
                  alt=""
                  className="w-5 h-5 rounded-full object-contain shrink-0"
                />
                <span className="font-bold text-slate-800 truncate">{user.username}</span>
              </div>
              <Badge variant="emerald">{user.roles[0] || 'LEARNER'}</Badge>
            </div>
          )}

          <button
            type="button"
            onClick={handleLogout}
            title={`${t('common.logout')} (${user?.username ?? ''})`}
            className={cn(
              'w-full flex items-center rounded-2xl text-xs font-bold text-rose-600 hover:bg-rose-50 border border-transparent hover:border-rose-200 transition-colors cursor-pointer',
              isStudentSidebarCollapsed
                ? 'justify-center w-12 h-12 mx-auto'
                : 'gap-2.5 px-3.5 py-2.5'
            )}
          >
            <LogOut className="w-4 h-4 shrink-0" />
            {!isStudentSidebarCollapsed && <span className="truncate">{t('common.logout')}</span>}
          </button>
        </div>
      </aside>

      {/* Independent Scrollable Main Content */}
      <main className="flex-1 h-screen overflow-y-auto overflow-x-hidden min-w-0 p-4 sm:p-6">
        <div className="max-w-7xl mx-auto">
          <Outlet />
        </div>
      </main>
    </div>
  );
};
