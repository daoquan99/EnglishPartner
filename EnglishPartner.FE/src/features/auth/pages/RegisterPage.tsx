import React, { useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  UserPlus,
  Globe,
  Sparkles,
  User,
  IdCard,
  Mail,
  Lock,
  GraduationCap,
  Mic,
  AudioLines,
} from 'lucide-react';
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

const inputClass =
  'w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500';
const selectClass =
  'w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500';
const labelClass = 'block text-xs font-bold uppercase tracking-wider text-slate-600 mb-1.5';
const iconClass = 'w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2';

export const RegisterPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const registerMutation = useRegisterMutation();
  const isVi = i18n.language === 'vi';

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

  // Errors are reported by the global API error toast (httpClient), so the form shows no inline banner.
  const onSubmit = async (values: RegisterFormValues) => {
    await registerMutation.mutateAsync({
      ...values,
      preferredCefrLevel: values.preferredCefrLevel as CefrLevel,
      timeZoneId: Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Ho_Chi_Minh',
    });
    navigate('/student/tutor', { replace: true });
  };

  const toggleLanguage = () => {
    const next = isVi ? 'en' : 'vi';
    void i18n.changeLanguage(next);
  };

  const heroPoints = [
    { icon: GraduationCap, text: t('auth.registerHeroPoint1') },
    { icon: Mic, text: t('auth.registerHeroPoint2') },
    { icon: AudioLines, text: t('auth.registerHeroPoint3') },
  ];

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
          <span>{isVi ? 'English' : 'Tiếng Việt'}</span>
        </button>
      </div>

      <div className="max-w-4xl w-full grid grid-cols-1 lg:grid-cols-12 bg-white rounded-3xl shadow-xl border border-slate-200/80 overflow-hidden">
        {/* Left Hero Panel */}
        <div className="lg:col-span-5 bg-gradient-to-br from-emerald-700 via-emerald-600 to-teal-700 text-white p-5 lg:p-8 flex flex-col justify-between gap-4 lg:gap-6">
          <div className="space-y-3 lg:space-y-4">
            <div className="flex items-center gap-3">
              <img
                src="/assets/brand/logo-mark.svg"
                alt="LingoMint"
                className="w-10 h-10 lg:w-12 lg:h-12 shrink-0 drop-shadow-sm"
              />
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/15 backdrop-blur-xs text-xs font-semibold">
                <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                {t('auth.heroTag')}
              </div>
            </div>
            <h1 className="text-lg font-extrabold leading-snug lg:hidden">
              {t('auth.registerHeroTitleShort')}
            </h1>
            <h1 className="hidden lg:block text-2xl sm:text-3xl font-extrabold leading-tight">
              {t('auth.registerHeroTitle')}
            </h1>
            <p className="hidden lg:block text-sm text-emerald-100 leading-relaxed">{t('auth.registerHeroSubtitle')}</p>
          </div>

          <ul className="hidden lg:block space-y-3 pt-6 border-t border-white/15 text-sm text-emerald-50">
            {heroPoints.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-start gap-2.5">
                <Icon className="w-4 h-4 mt-0.5 text-amber-300 shrink-0" aria-hidden="true" />
                <span>{text}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Right Register Form */}
        <div className="lg:col-span-7 p-8 sm:p-10 space-y-6">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">{t('auth.registerTitle')}</h2>
            <p className="text-sm text-slate-500 mt-1">{t('auth.registerSubtitle')}</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="register-username" className={labelClass}>
                  Username
                </label>
                <div className="relative">
                  <User className={iconClass} aria-hidden="true" />
                  <input
                    id="register-username"
                    type="text"
                    autoComplete="username"
                    {...register('username')}
                    placeholder="nguyenvana"
                    className={inputClass}
                  />
                </div>
                {errors.username && <p className="text-xs text-rose-600 mt-1">{errors.username.message}</p>}
              </div>
              <div>
                <label htmlFor="register-fullname" className={labelClass}>
                  {t('auth.fullName')}
                </label>
                <div className="relative">
                  <IdCard className={iconClass} aria-hidden="true" />
                  <input
                    id="register-fullname"
                    type="text"
                    autoComplete="name"
                    {...register('fullName')}
                    placeholder="Nguyễn Văn A"
                    className={inputClass}
                  />
                </div>
                {errors.fullName && <p className="text-xs text-rose-600 mt-1">{errors.fullName.message}</p>}
              </div>
            </div>

            <div>
              <label htmlFor="register-email" className={labelClass}>
                Email
              </label>
              <div className="relative">
                <Mail className={iconClass} aria-hidden="true" />
                <input
                  id="register-email"
                  type="email"
                  autoComplete="email"
                  {...register('email')}
                  placeholder="ban@example.com"
                  className={inputClass}
                />
              </div>
              {errors.email && <p className="text-xs text-rose-600 mt-1">{errors.email.message}</p>}
            </div>

            <div>
              <label htmlFor="register-password" className={labelClass}>
                {t('auth.password')}
              </label>
              <div className="relative">
                <Lock className={iconClass} aria-hidden="true" />
                <input
                  id="register-password"
                  type="password"
                  autoComplete="new-password"
                  {...register('password')}
                  placeholder="••••••••"
                  className={inputClass}
                />
              </div>
              {errors.password && <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>}
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label htmlFor="register-cefr" className={labelClass}>
                  {t('auth.preferredCefr')}
                </label>
                <select id="register-cefr" {...register('preferredCefrLevel')} className={selectClass}>
                  <option value="A1">A1 — Beginner</option>
                  <option value="A2">A2 — Elementary</option>
                  <option value="B1">B1 — Intermediate</option>
                  <option value="B2">B2 — Upper-Intermediate</option>
                  <option value="C1">C1 — Advanced</option>
                  <option value="C2">C2 — Proficiency</option>
                </select>
              </div>
              <div>
                <label htmlFor="register-voice" className={labelClass}>
                  {t('auth.preferredVoice')}
                </label>
                <select id="register-voice" {...register('preferredVoiceName')} className={selectClass}>
                  <option value="Aoede">Aoede ({isVi ? 'Nữ — Truyền cảm' : 'Female — Expressive'})</option>
                  <option value="Puck">Puck ({isVi ? 'Nam — Thân thiện' : 'Male — Friendly'})</option>
                  <option value="Kore">Kore ({isVi ? 'Nữ — Chuẩn sư phạm' : 'Female — Pedagogical'})</option>
                  <option value="Charon">Charon ({isVi ? 'Nam — Học thuật' : 'Male — Academic'})</option>
                  <option value="Fenrir">Fenrir ({isVi ? 'Nam — Dứt khoát' : 'Male — Decisive'})</option>
                  <option value="Leda">Leda ({isVi ? 'Nữ — Kiên nhẫn A1-B1' : 'Female — Patient A1-B1'})</option>
                </select>
              </div>
            </div>

            <Button type="submit" size="lg" disabled={registerMutation.isPending} className="w-full">
              <UserPlus className="w-4 h-4" />
              {registerMutation.isPending ? t('auth.signingUp') : t('auth.signUpButton')}
            </Button>
          </form>

          <p className="text-center text-xs text-slate-500">
            {t('auth.alreadyHaveAccount')}{' '}
            <Link to="/login" className="font-bold text-emerald-700 hover:underline">
              {t('auth.loginNow')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};
