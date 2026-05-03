'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Loader2, ArrowLeft } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { z } from 'zod'
import { useResetPassword } from '../api/useAuth'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { cn } from '@/shared/utils/cn'
import { toast } from 'sonner'

const resetSchema = z.object({
  email: z.string().email('Please enter a valid email'),
  code: z.string().length(6, 'Code must be 6 digits'),
  newPassword: z.string().min(8, 'Password must be at least 8 characters'),
})

type ResetFormData = z.infer<typeof resetSchema>

export function ResetPasswordForm() {
  const searchParams = useSearchParams()
  const prefillEmail = searchParams.get('email') || ''
  const [showPassword, setShowPassword] = useState(false)
  const { mutate: resetPwd, isPending } = useResetPassword()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetFormData>({
    resolver: zodResolver(resetSchema),
    defaultValues: {
      email: prefillEmail,
      code: '',
      newPassword: '',
    },
  })

  const onSubmit = (data: ResetFormData) => {
    resetPwd(data, {
      onError: (err) => {
        const code = (err as { response?: { data?: { code?: string } } })?.response?.data?.code
        if (code === 'Identity.PasswordReset.InvalidToken') {
          toast.error('Invalid or expired reset code. Please try again.')
        } else if (code === 'Identity.User.NotFound') {
          toast.error('No account found with that email.')
        } else {
          toast.error('Failed to reset password. Please try again.')
        }
      },
    })
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">Reset password</h1>
        <p className="text-sm text-[var(--color-text-secondary)]">
          Enter the 6-digit code from your email and choose a new password.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
        {/* Email */}
        <div className="space-y-2">
          <Label className="text-[var(--color-text-secondary)]">Email address</Label>
          <Input
            {...register('email')}
            type="email"
            placeholder="you@example.com"
            autoComplete="email"
            className={cn(
              'h-11 bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)]',
              'focus-visible:border-[var(--color-primary)] focus-visible:ring-[var(--color-primary-dim)]',
              errors.email && 'border-[var(--color-destructive)]'
            )}
          />
          {errors.email && (
            <p className="text-xs text-[var(--color-destructive)]">{errors.email.message}</p>
          )}
        </div>

        {/* OTP Code */}
        <div className="space-y-2">
          <Label className="text-[var(--color-text-secondary)]">Reset Code</Label>
          <Input
            {...register('code')}
            type="text"
            inputMode="numeric"
            maxLength={6}
            placeholder="123456"
            autoComplete="one-time-code"
            autoFocus
            className={cn(
              'h-11 text-center text-lg tracking-[0.5em] font-mono bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] placeholder:tracking-[0.5em]',
              'focus-visible:border-[var(--color-primary)] focus-visible:ring-[var(--color-primary-dim)]',
              errors.code && 'border-[var(--color-destructive)]'
            )}
          />
          {errors.code && (
            <p className="text-xs text-[var(--color-destructive)]">{errors.code.message}</p>
          )}
        </div>

        {/* New Password */}
        <div className="space-y-2">
          <Label className="text-[var(--color-text-secondary)]">New Password</Label>
          <div className="relative">
            <Input
              {...register('newPassword')}
              type={showPassword ? 'text' : 'password'}
              placeholder="Min. 8 characters"
              autoComplete="new-password"
              className={cn(
                'h-11 pr-11 bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)]',
                'focus-visible:border-[var(--color-primary)] focus-visible:ring-[var(--color-primary-dim)]',
                errors.newPassword && 'border-[var(--color-destructive)]'
              )}
            />
            <button
              type="button"
              onClick={() => setShowPassword((v) => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] transition-colors"
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.newPassword && (
            <p className="text-xs text-[var(--color-destructive)]">{errors.newPassword.message}</p>
          )}
        </div>

        <Button
          type="submit"
          disabled={isPending}
          className="mt-1 h-11 w-full font-semibold text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_20px_rgba(0,212,176,0.3)]"
        >
          {isPending && <Loader2 size={16} className="animate-spin mr-2" />}
          {isPending ? 'Resetting…' : 'Reset Password'}
        </Button>
      </form>

      <div className="text-center">
        <Link href="/login" className="inline-flex items-center text-sm text-[var(--color-primary)] hover:underline">
          <ArrowLeft size={14} className="mr-1" /> Back to login
        </Link>
      </div>
    </div>
  )
}
