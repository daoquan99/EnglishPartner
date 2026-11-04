import React, { useEffect, useRef } from 'react';
import { NavLink, Outlet, Link, useNavigate, useLocation } from 'react-router-dom';
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
  Cpu,
  Menu,
  X,
} from 'lucide-react';
import { CanAccess, useCurrentUser, useLogoutMutation } from '@/features/auth';
import { useTutorSessionStore } from '@/features/tutor';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';
import { isAgyLocalEnabled } from '@/shared/config/features';
import { toast } from '@/shared/lib/toast';
import { SelectionVocabularyMenu } from '@/features/vocabulary/components/SelectionVocabularyMenu';
import { useMobileDrawer, useIsDesktop } from '@/shared/hooks/useMobileDrawer';

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
 * Determines whether a given pathname belongs to the Student Lesson group:
 * - /student/lessons
 * - /student/lessons/:id
 * - /student/lesson-attempts/:attemptId
 * - /student/lesson-history
 * Supports trailing slashes and handles boundary matching so sibling/unrelated paths are not matched.
 */
export const isStudentLessonGroupRoute = (pathname: string): boolean => {
  return /^\/student\/(lessons|lesson-attempts|lesson-history)(\/.*)?$/.test(pathname);
};

/**
 * Determines whether a given pathname belongs to the Falling Words Vocabulary Game group:
 * - /student/vocabulary-games/falling-words
 * - /student/vocabulary-games/falling-words/sessions/:id
 * - /student/vocabulary-games/falling-words/history
 */
export const isStudentFallingWordsGroupRoute = (pathname: string): boolean => {
  return /^\/student\/vocabulary-games\/falling-words(\/.*)?$/.test(pathname);
};

export const isStudentGrammarGroupRoute = (pathname: string): boolean => {
  return /^\/student\/grammar(\/.*)?$/.test(pathname);
};

/**
 * Student Portal App Shell:
 * - Outer container: `h-screen w-full overflow-hidden flex` (prevents body scroll)
 * - Mobile (< lg): off-canvas drawer `fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw]`
 * - Desktop (>= lg): fixed static sidebar `h-screen shrink-0 overflow-y-auto`
 * - Mobile sticky top bar in `main` with hamburger button, brand logo, and portal title
 * - Main Content (`main`): `flex-1 h-screen overflow-y-auto` scrolls independently
 * - Houses the single Google AI Studio API Key input for the entire Student Portal
 */
export const StudentLayout: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();
  const leftMenuApiKeyRef = useRef<HTMLInputElement | null>(null);

  const mobileDrawer = useMobileDrawer();
  const isDesktop = useIsDesktop();

  const isLessonGroupActive = isStudentLessonGroupRoute(location.pathname);
  const isFallingWordsActive = isStudentFallingWordsGroupRoute(location.pathname);
  const isGrammarGroupActive = isStudentGrammarGroupRoute(location.pathname);

  const {
    apiKey,
    showApiKey,
    rememberApiKey,
    apiKeyFocusTrigger,
    aiProvider,
    agyModelCode,
    isStudentSidebarCollapsed,
    setApiKey,
    setRememberApiKey,
    toggleShowApiKey,
    setAiProvider,
    setAgyModelCode,
    toggleStudentSidebar,
  } = useTutorSessionStore();
  const isAgyActive = isAgyLocalEnabled && aiProvider === 'agy';

  // On mobile (< lg), drawer is ALWAYS expanded, ignoring collapsed flag
  const isEffectiveCollapsed = isDesktop && isStudentSidebarCollapsed;

  // Auto-close mobile drawer when switching to desktop
  const prevDesktopRef = useRef(isDesktop);
  useEffect(() => {
    if (!prevDesktopRef.current && isDesktop && mobileDrawer.isOpen) {
      mobileDrawer.close();
    }
    prevDesktopRef.current = isDesktop;
  }, [isDesktop, mobileDrawer.isOpen, mobileDrawer.close]);

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
      {/* Mobile Drawer Backdrop Overlay */}
      {mobileDrawer.isOpen && (
        <div
          role="presentation"
          aria-hidden="true"
          onClick={mobileDrawer.close}
          className="fixed inset-0 z-40 bg-slate-900/40 lg:hidden backdrop-blur-[1px] transition-opacity cursor-pointer"
        />
      )}

      {/* Off-canvas Mobile Drawer (< lg) / Fixed Collapsible Sidebar (>= lg) */}
      <aside
        id="student-sidebar"
        ref={mobileDrawer.drawerRef}
        tabIndex={-1}
        inert={!isDesktop && !mobileDrawer.isOpen ? true : undefined}
        role={!isDesktop && mobileDrawer.isOpen ? 'dialog' : undefined}
        aria-modal={!isDesktop && mobileDrawer.isOpen ? 'true' : undefined}
        aria-label={!isDesktop && mobileDrawer.isOpen ? t('studentPortal') : undefined}
        className={cn(
          'h-screen bg-white border-r border-slate-200/90 flex flex-col justify-between select-none overflow-y-auto overflow-x-hidden shadow-2xs outline-none',
          // Mobile (< lg): off-canvas drawer
          'fixed inset-y-0 left-0 z-50 w-72 max-w-[85vw] transition-transform duration-200 ease-in-out px-4 py-5',
          mobileDrawer.isOpen ? 'translate-x-0' : '-translate-x-full',
          // Desktop (>= lg): static sidebar
          'lg:static lg:inset-auto lg:z-30 lg:shrink-0 lg:translate-x-0 lg:max-w-none lg:transition-all',
          isEffectiveCollapsed ? 'lg:w-20 lg:px-2.5 lg:py-4' : 'lg:w-72 lg:px-4 lg:py-5'
        )}
      >
        {/* Top Section: Brand + Collapse Toggle + Navigation Links + Single API Key Input */}
        <div className="space-y-4">
          <div
            className={cn(
              'flex items-center border-b border-slate-100 pb-4',
              isEffectiveCollapsed ? 'justify-center flex-col gap-2' : 'justify-between gap-2 px-1'
            )}
          >
            <Link
              to="/student/tutor"
              onClick={mobileDrawer.close}
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
              {!isEffectiveCollapsed && (
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

            {/* Desktop collapse toggle (hidden on mobile) */}
            <button
              type="button"
              onClick={toggleStudentSidebar}
              title={
                isEffectiveCollapsed
                  ? t('nav.expandMenu')
                  : t('nav.collapseMenu')
              }
              className="hidden lg:flex p-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer shrink-0"
            >
              {isEffectiveCollapsed ? (
                <PanelLeftOpen className="w-4 h-4" />
              ) : (
                <PanelLeftClose className="w-4 h-4" />
              )}
            </button>

            {/* Mobile close button (hidden on desktop) */}
            <button
              type="button"
              onClick={mobileDrawer.close}
              title={t('nav.closeMenu')}
              aria-label={t('nav.closeMenu')}
              className="flex lg:hidden p-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer shrink-0"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          {/* Primary Student Navigation Items */}
          <nav className="space-y-1.5">
            <NavLink
              to="/student/tutor"
              onClick={mobileDrawer.close}
              title={t('nav.liveTutor')}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isEffectiveCollapsed
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
              {!isEffectiveCollapsed && (
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
              onClick={mobileDrawer.close}
              title={t('nav.dailyVocab')}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isEffectiveCollapsed
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
              {!isEffectiveCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{t('nav.dailyVocab')}</div>
                  <div className="text-[10px] opacity-80 truncate">
                    {t('nav.dailyVocabSub')}
                  </div>
                </div>
              )}
            </NavLink>

            <Link
              to="/student/vocabulary-games/falling-words"
              onClick={mobileDrawer.close}
              title="Từ vựng rơi"
              aria-current={isFallingWordsActive ? 'page' : undefined}
              className={cn(
                'flex items-center rounded-2xl transition-all',
                isEffectiveCollapsed
                  ? 'justify-center w-12 h-12 mx-auto'
                  : 'gap-3 px-3.5 py-3',
                isFallingWordsActive
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
              )}
            >
              <span className="w-5 h-5 flex items-center justify-center text-base shrink-0 select-none">
                🎮
              </span>
              {!isEffectiveCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">Từ vựng rơi</div>
                  <div className="text-[10px] opacity-80 truncate">
                    Game gõ từ rơi theo thời gian
                  </div>
                </div>
              )}
            </Link>

            <Link
              to="/student/lessons"
              onClick={mobileDrawer.close}
              title={t('nav.lessons') || 'Bài học & Luyện tập'}
              aria-current={isLessonGroupActive ? 'page' : undefined}
              className={cn(
                'flex items-center rounded-2xl transition-all',
                isEffectiveCollapsed
                  ? 'justify-center w-12 h-12 mx-auto'
                  : 'gap-3 px-3.5 py-3',
                isLessonGroupActive
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
              )}
            >
              <img
                src="/assets/icons/navigation/nav-reviews.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isEffectiveCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">Bài học & Luyện tập</div>
                  <div className="text-[10px] opacity-80 truncate">
                    16 loại bài tập đa kỹ năng
                  </div>
                </div>
              )}
            </Link>

            <Link
              to="/student/grammar"
              onClick={mobileDrawer.close}
              title={t('nav.grammar')}
              aria-current={isGrammarGroupActive ? 'page' : undefined}
              className={cn(
                'flex items-center rounded-2xl transition-all',
                isEffectiveCollapsed
                  ? 'justify-center w-12 h-12 mx-auto'
                  : 'gap-3 px-3.5 py-3',
                isGrammarGroupActive
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
              )}
            >
              <img
                src="/assets/icons/navigation/nav-sentences.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isEffectiveCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{t('nav.grammar')}</div>
                  <div className="text-[10px] opacity-80 truncate">
                    {t('nav.grammarSub')}
                  </div>
                </div>
              )}
            </Link>
            <NavLink
              to="/student/progress"
              onClick={mobileDrawer.close}
              title={t('nav.progress')}
              className={({ isActive }) =>
                cn(
                  'flex items-center rounded-2xl transition-all',
                  isEffectiveCollapsed
                    ? 'justify-center w-12 h-12 mx-auto'
                    : 'gap-3 px-3.5 py-3',
                  isActive
                    ? 'bg-emerald-600 text-white shadow-xs'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )
              }
            >
              <img
                src="/assets/icons/navigation/nav-dashboard.svg"
                alt=""
                className="w-5 h-5 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isEffectiveCollapsed && (
                <div className="min-w-0 flex-1">
                  <div className="text-xs font-bold truncate">{t('nav.progress')}</div>
                  <div className="text-[10px] opacity-80 truncate">
                    {t('nav.progressSub')}
                  </div>
                </div>
              )}
            </NavLink>
          </nav>

          {/* AI Model Provider & Google AI Studio API Key Setup inside Left Menu */}
          {isEffectiveCollapsed ? (
            <div className="pt-2 border-t border-slate-100 flex justify-center">
              <button
                type="button"
                onClick={toggleStudentSidebar}
                title={
                  isAgyActive
                    ? `Cổng AI: Antigravity CLI Local (${agyModelCode})`
                    : apiKey.trim()
                    ? `${t('common.apiKey')}: ${t('common.apiKeySaved')}`
                    : `${t('common.apiKey')}: ${t('common.apiKeyMissing')}`
                }
                className={cn(
                  'w-12 h-12 rounded-2xl flex items-center justify-center border transition-colors cursor-pointer',
                  isAgyActive || apiKey.trim()
                    ? 'bg-emerald-50 border-emerald-200 text-emerald-600'
                    : 'bg-amber-50 border-amber-300 text-amber-600 animate-pulse'
                )}
              >
                <img src="/assets/icons/navigation/nav-roles.svg" alt="" className="w-5 h-5 object-contain" />
              </button>
            </div>
          ) : (
            <div className="p-3 rounded-2xl bg-slate-50 border border-slate-200/90 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-bold text-slate-700 flex items-center gap-1.5">
                  <Cpu className="w-3.5 h-3.5 text-slate-600" />
                  Cổng AI Model
                </span>
                <span
                  className={cn(
                    'text-[10px] font-bold px-1.5 py-0.2 rounded-full',
                    isAgyActive
                      ? 'bg-emerald-100 text-emerald-800'
                      : apiKey.trim()
                      ? 'bg-emerald-100 text-emerald-800'
                      : 'bg-amber-100 text-amber-800'
                  )}
                >
                  {isAgyActive ? 'AGY Local' : apiKey.trim() ? t('common.apiKeySaved') : t('common.apiKeyMissing')}
                </span>
              </div>

              {/* Mode switch: Google API vs AGY Local */}
              <div className={cn(
                'grid gap-1 p-0.5 rounded-lg bg-slate-200/70 text-[11px] font-medium',
                isAgyLocalEnabled ? 'grid-cols-2' : 'grid-cols-1'
              )}>
                <button
                  type="button"
                  onClick={() => setAiProvider('gemini')}
                  className={cn(
                    'py-1 rounded-md transition-all cursor-pointer text-center',
                    aiProvider === 'gemini'
                      ? 'bg-white text-emerald-700 font-bold shadow-xs'
                      : 'text-slate-600 hover:text-slate-900'
                  )}
                >
                  Google API
                </button>
                {isAgyLocalEnabled && (
                  <button
                    type="button"
                    onClick={() => setAiProvider('agy')}
                    className={cn(
                      'py-1 rounded-md transition-all cursor-pointer text-center',
                      isAgyActive
                        ? 'bg-white text-emerald-700 font-bold shadow-xs'
                        : 'text-slate-600 hover:text-slate-900'
                    )}
                  >
                    AGY Local
                  </button>
                )}
              </div>

              {!isAgyActive ? (
                <div className="space-y-1.5">
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

                  <label className="flex items-center gap-2 pt-0.5 text-[11px] text-slate-600 cursor-pointer select-none">
                    <input
                      type="checkbox"
                      checked={rememberApiKey}
                      onChange={(e) => setRememberApiKey(e.target.checked)}
                      className="rounded border-slate-300 text-emerald-600 focus:ring-emerald-500/30 w-3.5 h-3.5 cursor-pointer"
                    />
                    <span>{t('common.apiKeyRemember')}</span>
                  </label>

                  {rememberApiKey && (
                    <p className="text-[10px] text-amber-700 bg-amber-50 border border-amber-200/80 rounded-lg p-1.5 leading-tight">
                      {t('common.apiKeyRememberWarning')}
                    </p>
                  )}
                </div>
              ) : (
                <div className="space-y-1">
                  <select
                    value={agyModelCode}
                    onChange={(e) => setAgyModelCode(e.target.value as any)}
                    className="w-full px-2 py-1.5 text-xs rounded-xl border border-slate-200 bg-white text-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500 cursor-pointer font-medium"
                  >
                    <option value="gemini-3.8-flash-medium">Flash Medium (Mặc định)</option>
                    <option value="gemini-3.8-flash-low">Flash Low (Tốc độ cao)</option>
                    <option value="gemini-3.8-flash-high">Flash High (Suy luận sâu)</option>
                  </select>
                  <p className="text-[10px] text-slate-500 px-0.5 leading-tight">
                    Chạy qua Antigravity CLI máy local (không tốn quota).
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Bottom Section: Admin Portal Switcher + Language + User Info + Logout */}
        <div className="space-y-2 pt-4 border-t border-slate-100">
          <CanAccess anyPermissions={ADMIN_ANY_PERMISSIONS}>
            <Link
              to="/admin"
              onClick={mobileDrawer.close}
              title={t('adminPortal')}
              className={cn(
                'flex items-center rounded-2xl bg-amber-50 hover:bg-amber-100 border border-amber-200 text-amber-900 font-bold text-xs transition-colors',
                isEffectiveCollapsed
                  ? 'justify-center w-12 h-12 mx-auto'
                  : 'gap-2.5 px-3.5 py-2.5'
              )}
            >
              <img
                src="/assets/icons/navigation/nav-admin.svg"
                alt=""
                className="w-4 h-4 shrink-0 object-contain drop-shadow-2xs"
              />
              {!isEffectiveCollapsed && <span className="truncate">{t('adminPortal')}</span>}
            </Link>
          </CanAccess>

          <button
            type="button"
            onClick={toggleLanguage}
            title={`${t('common.language')}: ${i18n.language.toUpperCase()}`}
            className={cn(
              'w-full flex items-center rounded-2xl border border-slate-200 text-xs font-bold text-slate-600 hover:bg-slate-100 transition-colors cursor-pointer',
              isEffectiveCollapsed
                ? 'justify-center w-12 h-12 mx-auto'
                : 'justify-between px-3.5 py-2'
            )}
          >
            <div className="flex items-center gap-2">
              <Globe className="w-4 h-4 shrink-0 text-emerald-600" />
              {!isEffectiveCollapsed && <span>{t('common.language')}</span>}
            </div>
            {!isEffectiveCollapsed && (
              <Badge variant="default">{i18n.language.toUpperCase()}</Badge>
            )}
          </button>

          {user && !isEffectiveCollapsed && (
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
              isEffectiveCollapsed
                ? 'justify-center w-12 h-12 mx-auto'
                : 'gap-2.5 px-3.5 py-2.5'
            )}
          >
            <LogOut className="w-4 h-4 shrink-0" />
            {!isEffectiveCollapsed && <span className="truncate">{t('common.logout')}</span>}
          </button>
        </div>
      </aside>

      {/* Independent Scrollable Main Content */}
      <main
        inert={!isDesktop && mobileDrawer.isOpen ? true : undefined}
        className={cn(
          'flex-1 h-screen min-w-0 flex flex-col',
          mobileDrawer.isOpen && !isDesktop ? 'overflow-hidden' : 'overflow-y-auto overflow-x-hidden'
        )}
      >
        {/* Mobile Sticky Top Bar (< lg) */}
        <header className="lg:hidden sticky top-0 z-20 shrink-0 bg-white/95 backdrop-blur-xs border-b border-slate-200/90 px-4 py-2.5 flex items-center justify-between">
          <div className="flex items-center gap-3 min-w-0">
            <button
              ref={mobileDrawer.triggerRef}
              type="button"
              onClick={mobileDrawer.toggle}
              aria-label={t('nav.openMenu')}
              aria-expanded={mobileDrawer.isOpen}
              aria-controls="student-sidebar"
              className="min-h-[44px] min-w-[44px] flex items-center justify-center rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 hover:text-slate-900 transition-colors cursor-pointer shrink-0"
            >
              <Menu className="w-5 h-5" />
            </button>

            <div className="flex items-center gap-2.5 min-w-0">
              <div className="w-9 h-9 rounded-xl bg-emerald-50 border border-emerald-200/80 flex items-center justify-center shadow-2xs shrink-0 p-0.5">
                <img
                  src="/assets/brand/logo-mark.svg"
                  alt=""
                  className="w-7 h-7 object-contain"
                />
              </div>
              <div className="min-w-0">
                <span className="text-sm font-extrabold text-slate-900 tracking-tight block truncate">
                  {t('appTitle')}
                </span>
                <span className="text-[11px] font-semibold text-emerald-700 block truncate">
                  {t('studentPortal')}
                </span>
              </div>
            </div>
          </div>
        </header>

        {/* Content Container */}
        <div className="flex-1 p-4 sm:p-6">
          <div className="max-w-7xl mx-auto">
            <Outlet />
          </div>
        </div>
      </main>

      {/* Global Right-Click Targeted Vocabulary Selection Menu */}
      <SelectionVocabularyMenu />
    </div>
  );
};
