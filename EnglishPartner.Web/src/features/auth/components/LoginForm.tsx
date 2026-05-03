'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import Link from 'next/link'
import { useLogin } from '../api/useAuth'
import { loginSchema, type LoginFormData } from '../types/auth.schemas'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { cn } from '@/shared/utils/cn'

export function LoginForm() {
  const [showPassword, setShowPassword] = useState(false)
  const { mutate: loginUser, isPending } = useLogin()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = (data: LoginFormData) => {
    loginUser(data, {
      onError: (err) => {
        const code = (err as { response?: { data?: { code?: string } } })?.response?.data?.code
        if (code === 'Identity.User.InvalidCredentials') {
          toast.error('Incorrect email or password.')
        } else if (code === 'Identity.User.Deactivated') {
          toast.error('Your account has been deactivated.')
        } else {
          toast.error('Login failed. Please try again.')
        }
      },
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
      {/* Email */}
      <div className="space-y-2">
        <Label className="text-[var(--color-text-secondary)]">Email address</Label>
        <Input
          {...register('email')}
          type="email"
          placeholder="you@example.com"
          autoComplete="email"
          aria-invalid={!!errors.email}
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

      {/* Password */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label className="text-[var(--color-text-secondary)]">Password</Label>
          <Link href="/forgot-password" className="text-xs text-[var(--color-primary)] hover:underline">
            Forgot password?
          </Link>
        </div>
        <div className="relative">
          <Input
            {...register('password')}
            type={showPassword ? 'text' : 'password'}
            placeholder="Your password"
            autoComplete="current-password"
            aria-invalid={!!errors.password}
            className={cn(
              'h-11 pr-11 bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)]',
              'focus-visible:border-[var(--color-primary)] focus-visible:ring-[var(--color-primary-dim)]',
              errors.password && 'border-[var(--color-destructive)]'
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
        {errors.password && (
          <p className="text-xs text-[var(--color-destructive)]">{errors.password.message}</p>
        )}
      </div>

      {/* Submit */}
      <Button
        type="submit"
        disabled={isPending}
        className="mt-1 h-11 w-full font-semibold text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_20px_rgba(0,212,176,0.3)]"
      >
        {isPending && <Loader2 size={16} className="animate-spin" />}
        {isPending ? 'Signing in…' : 'Sign in'}
      </Button>
    </form>
  )
}
