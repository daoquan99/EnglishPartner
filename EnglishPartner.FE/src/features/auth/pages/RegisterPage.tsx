import React, { useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { UserPlus, ArrowLeft, Globe } from 'lucide-react';
import { useRegisterMutation } from '../hooks/useAuth';
import { Button } from '@/shared/components/ui/Button';
import type { CefrLevel } from '@/shared/api/openapi.types';

type RegisterFormValues = {
  username: string;
  email: string;
  fullName: string;
  password: string;
  preferredCefrLevel: CefrLevel;
  preferredVoiceName: string;
};

export const RegisterPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const registerMutation = useRegisterMutation();

  const registerSchema = useMemo(
    () =>
      z.object({
        username: z.string().min(3, t('auth.usernameFormat')),
        email: z.string().email(t('auth.emailInvalid')),
        fullName: z.string().min(2, t('auth.fullNameRequired')),
        password: z.string().min(6, t('auth.passwordMinLength')),
        preferredCefrLevel: z.enum(['A1', 'A2', 'B1', 'B2', 'C1', 'C2']),
        preferredVoiceName: z.string().min(1),
      }),
    [t]
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      username: '',
      email: '',
      fullName: '',
      password: '',
      preferredCefrLevel: 'B1',
      preferredVoiceName: 'Aoede',
    },
  });

  const onSubmit = async (values: RegisterFormValues) => {
    await registerMutation.mutateAsync({
      ...values,
      preferredCefrLevel: values.preferredCefrLevel as CefrLevel,
      timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Ho_Chi_Minh',
    });
    navigate('/student/tutor', { replace: true });
  };

  const toggleLanguage = () => {
    const next = i18n.language === 'vi' ? 'en' : 'vi';
    void i18n.changeLanguage(next);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-amber-50/40 to-sky-50 flex flex-col items-center justify-center p-4">
      {/* Top right language switch */}
      <div className="max-w-lg w-full flex justify-end pb-3">
        <button
          type="button"
          onClick={toggleLanguage}
          className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full border border-slate-200 bg-white/90 backdrop-blur-xs text-xs font-bold text-slate-700 hover:bg-white hover:text-slate-900 transition-colors shadow-2xs cursor-pointer"
        >
          <Globe className="w-3.5 h-3.5 text-emerald-600" />
          <span>{i18n.language === 'vi' ? 'English' : 'Tiếng Việt'}</span>
        </button>
      </div>

      <div className="max-w-lg w-full bg-white rounded-3xl shadow-xl border border-slate-200/80 p-8 space-y-6">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-emerald-500 to-teal-400 p-2 flex items-center justify-center shrink-0 shadow-xs">
              <img
                src="/assets/brand/logo-mark.svg"
                alt="LingoMint"
                className="w-full h-full object-contain brightness-0 invert"
              />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-900">{t('auth.registerTitle')}</h1>
              <p className="text-xs text-slate-500 mt-1">
                {t('auth.registerSubtitle')}
              </p>
            </div>
          </div>
          <Link
            to="/login"
            className="inline-flex items-center gap-1 text-xs font-semibold text-slate-600 hover:text-slate-900"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> {t('common.login')}
          </Link>
        </div>

        {registerMutation.isError && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
            {(registerMutation.error as Error)?.message || t('common.error')}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">Username</label>
              <input
                type="text"
                {...register('username')}
                placeholder="nguyenvana"
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm"
              />
              {errors.username && (
                <p className="text-xs text-rose-600 mt-1">{errors.username.message}</p>
              )}
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">{t('auth.fullName')}</label>
              <input
                type="text"
                {...register('fullName')}
                placeholder="Nguyễn Văn A"
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm"
              />
              {errors.fullName && (
                <p className="text-xs text-rose-600 mt-1">{errors.fullName.message}</p>
              )}
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-600 mb-1">Email</label>
            <input
              type="email"
              {...register('email')}
              placeholder="ban@example.com"
              className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm"
            />
            {errors.email && <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>}
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-600 mb-1">{t('auth.password')}</label>
            <input
              type="password"
              {...register('password')}
              placeholder={t('auth.passwordMinLength')}
              className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm"
            />
            {errors.password && (
              <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">{t('auth.preferredCefr')}</label>
              <select
                {...register('preferredCefrLevel')}
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm bg-white"
              >
                <option value="A1">A1 — Beginner</option>
                <option value="A2">A2 — Elementary</option>
                <option value="B1">B1 — Intermediate</option>
                <option value="B2">B2 — Upper-Intermediate</option>
                <option value="C1">C1 — Advanced</option>
                <option value="C2">C2 — Proficiency</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">
                {t('auth.preferredVoice')}
              </label>
              <select
                {...register('preferredVoiceName')}
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm bg-white"
              >
                <option value="Aoede">Aoede ({i18n.language === 'vi' ? 'Nữ — Truyền cảm' : 'Female — Expressive'})</option>
                <option value="Puck">Puck ({i18n.language === 'vi' ? 'Nam — Thân thiện' : 'Male — Friendly'})</option>
                <option value="Kore">Kore ({i18n.language === 'vi' ? 'Nữ — Chuẩn sư phạm' : 'Female — Pedagogical'})</option>
                <option value="Charon">Charon ({i18n.language === 'vi' ? 'Nam — Học thuật' : 'Male — Academic'})</option>
                <option value="Fenrir">Fenrir ({i18n.language === 'vi' ? 'Nam — Dứt khoát' : 'Male — Decisive'})</option>
                <option value="Leda">Leda ({i18n.language === 'vi' ? 'Nữ — Kiên nhẫn A1-B1' : 'Female — Patient A1-B1'})</option>
              </select>
            </div>
          </div>

          <Button
            type="submit"
            size="lg"
            disabled={registerMutation.isPending}
            className="w-full"
          >
            <UserPlus className="w-4 h-4" />
            {registerMutation.isPending ? t('auth.signingUp') : t('auth.signUpButton')}
          </Button>
        </form>
      </div>
    </div>
  );
};
