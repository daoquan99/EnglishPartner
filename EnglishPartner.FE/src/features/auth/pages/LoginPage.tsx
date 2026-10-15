import React, { useMemo } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { LogIn, ShieldCheck, GraduationCap, Sparkles, Lock, User, Globe } from 'lucide-react';
import { useLoginMutation } from '../hooks/useAuth';
import { Button } from '@/shared/components/ui/Button';

type LoginFormValues = {
  usernameOrEmail: string;
  password: string;
};

export const LoginPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const loginMutation = useLoginMutation();

  const loginSchema = useMemo(
    () =>
      z.object({
        usernameOrEmail: z.string().min(2, t('auth.usernameRequired')),
        password: z.string().min(4, t('auth.passwordMinLength')),
      }),
    [t]
  );

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      usernameOrEmail: 'default-learner',
      password: 'Learner@123',
    },
  });

  const onSubmit = async (values: LoginFormValues) => {
    const res = await loginMutation.mutateAsync({
      usernameOrEmail: values.usernameOrEmail,
      password: values.password,
      deviceInfo: navigator.userAgent.slice(0, 120),
    });

    const fromPath = (location.state as any)?.from?.pathname;
    if (fromPath && fromPath !== '/login') {
      navigate(fromPath, { replace: true });
      return;
    }

    if (res.user.roles.includes('ADMIN')) {
      navigate('/admin', { replace: true });
    } else {
      navigate('/student/tutor', { replace: true });
    }
  };

  const handleQuickDemoLogin = async (usernameOrEmail: string, password: string) => {
    setValue('usernameOrEmail', usernameOrEmail);
    setValue('password', password);
    await onSubmit({ usernameOrEmail, password });
  };

  const toggleLanguage = () => {
    const next = i18n.language === 'vi' ? 'en' : 'vi';
    void i18n.changeLanguage(next);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-amber-50/40 to-sky-50 flex flex-col items-center justify-center p-4">
      {/* Top right language switch */}
      <div className="max-w-4xl w-full flex justify-end pb-3">
        <button
          type="button"
          onClick={toggleLanguage}
          className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full border border-slate-200 bg-white/90 backdrop-blur-xs text-xs font-bold text-slate-700 hover:bg-white hover:text-slate-900 transition-colors shadow-2xs cursor-pointer"
        >
          <Globe className="w-3.5 h-3.5 text-emerald-600" />
          <span>{i18n.language === 'vi' ? 'English' : 'Tiếng Việt'}</span>
        </button>
      </div>

      <div className="max-w-4xl w-full grid grid-cols-1 lg:grid-cols-12 bg-white rounded-3xl shadow-xl border border-slate-200/80 overflow-hidden">
        {/* Left Hero Panel */}
        <div className="lg:col-span-5 bg-gradient-to-br from-emerald-700 via-emerald-600 to-teal-700 text-white p-8 flex flex-col justify-between">
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-2xl bg-white/20 p-2.5 backdrop-blur-xs flex items-center justify-center shrink-0 shadow-xs">
                <img
                  src="/assets/brand/logo-mark.svg"
                  alt="LingoMint"
                  className="w-full h-full object-contain brightness-0 invert"
                />
              </div>
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/15 backdrop-blur-xs text-xs font-semibold">
                <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                {t('auth.heroTag')}
              </div>
            </div>
            <h1 className="text-2xl sm:text-3xl font-extrabold leading-tight">
              {t('auth.heroTitle')}
            </h1>
            <p className="text-sm text-emerald-100 leading-relaxed">
              {t('auth.heroSubtitle')}
            </p>
          </div>

          <div className="space-y-3 pt-6 border-t border-white/15 text-xs text-emerald-100">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-amber-300 shrink-0" />
              <span>{t('auth.heroFeature1')}</span>
            </div>
            <div className="flex items-center gap-2">
              <GraduationCap className="w-4 h-4 text-amber-300 shrink-0" />
              <span>{t('auth.heroFeature2')}</span>
            </div>
          </div>
        </div>

        {/* Right Login Form */}
        <div className="lg:col-span-7 p-8 sm:p-10 space-y-6">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">{t('auth.loginTitle')}</h2>
            <p className="text-sm text-slate-500 mt-1">
              {t('auth.loginSubtitle')}
            </p>
          </div>

          {/* One-click Seeder Account Switchers */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => handleQuickDemoLogin('default-learner', 'Learner@123')}
              className="p-3 rounded-2xl border border-emerald-200 bg-emerald-50/60 hover:bg-emerald-100/70 text-left transition-all cursor-pointer"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-emerald-800">{t('auth.learnerDemo')}</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-200/70 text-emerald-900">
                  LEARNER
                </span>
              </div>
              <p className="text-xs text-slate-600 mt-1 font-mono">default-learner / Learner@123</p>
            </button>

            <button
              type="button"
              onClick={() => handleQuickDemoLogin('admin', 'Admin@123')}
              className="p-3 rounded-2xl border border-amber-200 bg-amber-50/60 hover:bg-amber-100/70 text-left transition-all cursor-pointer"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-amber-900">{t('auth.adminDemo')}</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-200/80 text-amber-950">
                  ADMIN
                </span>
              </div>
              <p className="text-xs text-slate-600 mt-1 font-mono">admin / Admin@123</p>
            </button>
          </div>

          {loginMutation.isError && (
            <div className="p-3.5 rounded-xl bg-rose-50 border border-rose-200 text-xs font-medium text-rose-700">
              {(loginMutation.error as Error)?.message || t('common.error')}
            </div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1.5">
                {t('auth.usernameOrEmail')}
              </label>
              <div className="relative">
                <User className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  {...register('usernameOrEmail')}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500"
                  placeholder="default-learner hoặc admin@lingomint.local"
                />
              </div>
              {errors.usernameOrEmail && (
                <p className="text-xs text-rose-600 mt-1">{errors.usernameOrEmail.message}</p>
              )}
            </div>

            <div>
              <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1.5">
                {t('auth.password')}
              </label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="password"
                  {...register('password')}
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500"
                  placeholder="••••••••"
                />
              </div>
              {errors.password && (
                <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>
              )}
            </div>

            <Button
              type="submit"
              size="lg"
              disabled={loginMutation.isPending}
              className="w-full"
            >
              <LogIn className="w-4 h-4" />
              {loginMutation.isPending ? t('auth.signingIn') : t('auth.signInButton')}
            </Button>
          </form>

          <p className="text-center text-xs text-slate-500">
            {t('auth.dontHaveAccount')}{' '}
            <Link to="/register" className="font-bold text-emerald-700 hover:underline">
              {t('auth.registerNow')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
