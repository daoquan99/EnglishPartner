import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { UserPlus, ArrowLeft } from 'lucide-react';
import { useRegisterMutation } from '../hooks/useAuth';
import { Button } from '@/shared/components/ui/Button';
import type { CefrLevel } from '@/shared/api/openapi.types';

const registerSchema = z.object({
  username: z.string().min(3, 'Username tối thiểu 3 ký tự'),
  email: z.string().email('Email không hợp lệ'),
  fullName: z.string().min(2, 'Vui lòng nhập họ tên'),
  password: z.string().min(6, 'Mật khẩu tối thiểu 6 ký tự'),
  preferredCefrLevel: z.enum(['A1', 'A2', 'B1', 'B2', 'C1', 'C2']),
  preferredVoiceName: z.string().min(1),
});

type RegisterFormValues = z.infer<typeof registerSchema>;

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const registerMutation = useRegisterMutation();

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

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-amber-50/40 to-sky-50 flex items-center justify-center p-4">
      <div className="max-w-lg w-full bg-white rounded-3xl shadow-xl border border-slate-200/80 p-8 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Đăng ký Học viên mới</h1>
            <p className="text-xs text-slate-500 mt-1">
              Tài khoản mới được gán role LEARNER và khởi tạo lộ trình học theo CEFR.
            </p>
          </div>
          <Link
            to="/login"
            className="inline-flex items-center gap-1 text-xs font-semibold text-slate-600 hover:text-slate-900"
          >
            <ArrowLeft className="w-3.5 h-3.5" /> Đăng nhập
          </Link>
        </div>

        {registerMutation.isError && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700">
            {(registerMutation.error as Error)?.message || 'Đăng ký thất bại.'}
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
              <label className="block text-xs font-bold text-slate-600 mb-1">Họ và tên</label>
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
            <label className="block text-xs font-bold text-slate-600 mb-1">Mật khẩu</label>
            <input
              type="password"
              {...register('password')}
              placeholder="Tối thiểu 6 ký tự"
              className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm"
            />
            {errors.password && (
              <p className="text-xs text-rose-600 mt-1">{errors.password.message}</p>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">Trình độ CEFR</label>
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
                Giọng Teacher yêu thích
              </label>
              <select
                {...register('preferredVoiceName')}
                className="w-full px-3.5 py-2 rounded-xl border border-slate-200 text-sm bg-white"
              >
                <option value="Aoede">Aoede (Nữ — Truyền cảm)</option>
                <option value="Puck">Puck (Nam — Thân thiện)</option>
                <option value="Kore">Kore (Nữ — Chuẩn sư phạm)</option>
                <option value="Charon">Charon (Nam — Học thuật)</option>
                <option value="Fenrir">Fenrir (Nam — Dứt khoát)</option>
                <option value="Leda">Leda (Nữ — Kiên nhẫn A1-B1)</option>
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
            {registerMutation.isPending ? 'Đang tạo tài khoản...' : 'Hoàn tất Đăng ký'}
          </Button>
        </form>
      </div>
    </div>
  );
};
