'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { useRegister } from '../api/useAuth'
import { registerSchema, type RegisterFormData } from '../types/auth.schemas'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { cn } from '@/shared/utils/cn'

export function RegisterForm() {
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const { mutate: registerUser, isPending } = useRegister()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  const onSubmit = ({ confirmPassword: _, ...data }: RegisterFormData) => {
    registerUser(data, {
      onError: (err) => {
        const code = (err as { response?: { data?: { code?: string } } })?.response?.data?.code
        if (code === 'Identity.User.EmailAlreadyExists') {
          toast.error('This email is already registered.')
        } else {
          toast.error('Registration failed. Please try again.')
        }
      },
    })
  }

  const inputClass = cn(
    'h-11 bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)]',
    'focus-visible:border-[var(--color-primary)] focus-visible:ring-[var(--color-primary-dim)]'
  )

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
      {/* Display Name */}
      <div className="space-y-2">
        <Label className="text-[var(--color-text-secondary)]">Display Name</Label>
        <Input
          {...register('displayName')}
          type="text"
          placeholder="Your name"
          aria-invalid={!!errors.displayName}
          className={cn(inputClass, errors.displayName && 'border-[var(--color-destructive)]')}
        />
        {errors.displayName && (
          <p className="text-xs text-[var(--color-destructive)]">{errors.displayName.message}</p>
        )}
      </div>

      {/* Email */}
      <div className="space-y-2">
        <Label className="text-[var(--color-text-secondary)]">Email address</Label>
        <Input
          {...register('email')}
          type="email"
          placeholder="you@example.com"
          autoComplete="email"
          aria-invalid={!!errors.email}
          className={cn(inputClass, errors.email && 'border-[var(--color-destructive)]')}
        />
        {errors.email && (
          <p className="text-xs text-[var(--color-destructive)]">{errors.email.message}</p>
        )}
      </div>

      {/* Password */}
      <div className="space-y-2">
        <Label className="text-[var(--color-text-secondary)]">Password</Label>
        <div className="relative">
          <Input
            {...register('password')}
            type={showPassword ? 'text' : 'password'}
            placeholder="Min. 8 characters"
            autoComplete="new-password"
            aria-invalid={!!errors.password}
            className={cn(inputClass, 'pr-11', errors.password && 'border-[var(--color-destructive)]')}
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

      {/* Confirm Password */}
      <div className="space-y-2">
        <Label className="text-[var(--color-text-secondary)]">Confirm Password</Label>
        <div className="relative">
          <Input
            {...register('confirmPassword')}
            type={showConfirm ? 'text' : 'password'}
            placeholder="Re-enter your password"
            autoComplete="new-password"
            aria-invalid={!!errors.confirmPassword}
            className={cn(inputClass, 'pr-11', errors.confirmPassword && 'border-[var(--color-destructive)]')}
          />
          <button
            type="button"
            onClick={() => setShowConfirm((v) => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] transition-colors"
          >
            {showConfirm ? <EyeOff size={16} /> : <Eye size={16} />}
          </button>
        </div>
        {errors.confirmPassword && (
          <p className="text-xs text-[var(--color-destructive)]">{errors.confirmPassword.message}</p>
        )}
      </div>

      {/* Submit */}
      <Button
        type="submit"
        disabled={isPending}
        className="mt-1 h-11 w-full font-semibold text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_20px_rgba(0,212,176,0.3)]"
      >
        {isPending && <Loader2 size={16} className="animate-spin" />}
        {isPending ? 'Creating account…' : 'Create account'}
      </Button>
    </form>
  )
}
