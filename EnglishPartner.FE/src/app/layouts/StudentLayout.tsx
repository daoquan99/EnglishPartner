import React from 'react';
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
} from 'lucide-react';
import { CanAccess, useCurrentUser, useLogoutMutation } from '@/features/auth';
import { Badge } from '@/shared/components/ui/Badge';
import { cn } from '@/shared/lib/cn';

const ADMIN_ANY_PERMISSIONS = [
  'users.read',
  'users.manage',
  'roles.manage',
  'prompts.manage',
  'voices.manage',
  'topics.manage',
  'rubrics.manage',
];

export const StudentLayout: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { user } = useCurrentUser();
  const logoutMutation = useLogoutMutation();

  const toggleLanguage = () => {
    const next = i18n.language === 'vi' ? 'en' : 'vi';
    void i18n.changeLanguage(next);
  };

  const handleLogout = async () => {
    await logoutMutation.mutateAsync();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen learnerTheme bg-slate-50/70 text-slate-900 flex flex-col">
      {/* Top Student Portal Navigation Bar */}
      <header className="sticky top-0 z-30 bg-white/90 backdrop-blur-md border-b border-slate-200/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between gap-4">
          <div className="flex items-center gap-6">
            <Link to="/student/tutor" className="flex items-center gap-2.5">
              <div className="w-9 h-9 rounded-2xl bg-emerald-600 text-white flex items-center justify-center shadow-xs">
                <Sparkles className="w-5 h-5" />
              </div>
              <div>
                <span className="text-base font-extrabold text-slate-900 tracking-tight block leading-none">
                  {t('appTitle')}
                </span>
                <span className="text-[11px] font-semibold text-emerald-700">
                  {t('studentPortal')}
                </span>
              </div>
            </Link>

            <nav className="hidden md:flex items-center gap-1.5">
              <NavLink
                to="/student/tutor"
                className={({ isActive }) =>
                  cn(
                    'inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all',
                    isActive
                      ? 'bg-emerald-600 text-white shadow-xs'
                      : 'text-slate-600 hover:bg-slate-100'
                  )
                }
              >
                <Mic className="w-3.5 h-3.5" />
                {t('liveTutorNav')}
              </NavLink>

              <NavLink
                to="/student/vocabulary"
                className={({ isActive }) =>
                  cn(
                    'inline-flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all',
                    isActive
                      ? 'bg-emerald-600 text-white shadow-xs'
                      : 'text-slate-600 hover:bg-slate-100'
                  )
                }
              >
                <BookOpen className="w-3.5 h-3.5" />
                {t('dailyVocabNav')}
              </NavLink>
            </nav>
          </div>

          <div className="flex items-center gap-2.5">
            {/* Conditionally render Admin Portal Switcher via CanAccess */}
            <CanAccess anyPermissions={ADMIN_ANY_PERMISSIONS}>
              <Link
                to="/admin"
                className="inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-amber-50 hover:bg-amber-100 border border-amber-200 text-amber-900 text-xs font-bold transition-colors"
              >
                <ShieldCheck className="w-3.5 h-3.5 text-amber-600" />
                {t('adminPortal')}
              </Link>
            </CanAccess>

            <button
              type="button"
              onClick={toggleLanguage}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-xl border border-slate-200 text-xs font-bold text-slate-600 hover:bg-slate-100 cursor-pointer"
              title="Chuyển ngôn ngữ VI / EN"
            >
              <Globe className="w-3.5 h-3.5" />
              {i18n.language.toUpperCase()}
            </button>

            {user && (
              <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-100 text-xs">
                <UserIcon className="w-3.5 h-3.5 text-slate-500" />
                <span className="font-bold text-slate-800">{user.username}</span>
                <Badge variant="emerald">{user.roles[0] || 'LEARNER'}</Badge>
              </div>
            )}

            <button
              type="button"
              onClick={handleLogout}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold text-rose-600 hover:bg-rose-50 border border-transparent hover:border-rose-200 transition-colors cursor-pointer"
            >
              <LogOut className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">{t('logout')}</span>
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-6">
        <Outlet />
      </main>
    </div>
  );
};
