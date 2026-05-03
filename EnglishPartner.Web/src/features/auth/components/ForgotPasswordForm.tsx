'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, ArrowLeft, Mail } from 'lucide-react'
import { useState } from 'react'
import Link from 'next/link'
import { z } from 'zod'
import { useForgotPassword } from '../api/useAuth'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { cn } from '@/shared/utils/cn'

const forgotSchema = z.object({
  email: z.string().email('Please enter a valid email'),
})

type ForgotFormData = z.infer<typeof forgotSchema>

export function ForgotPasswordForm() {
  const [submitted, setSubmitted] = useState(false)
  const { mutate: sendCode, isPending } = useForgotPassword()

  const {
    register,
    handleSubmit,
    getValues,
    formState: { errors },
  } = useForm<ForgotFormData>({
    resolver: zodResolver(forgotSchema),
  })

  const onSubmit = (data: ForgotFormData) => {
    sendCode(data, {
      onSuccess: () => setSubmitted(true),
      onError: () => setSubmitted(true), // Always show success to prevent email enumeration
    })
  }

  if (submitted) {
    return (
      <div className="text-center space-y-6">
        <div className="mx-auto w-16 h-16 rounded-full bg-[var(--color-primary-dim)] flex items-center justify-center">
          <Mail className="h-8 w-8 text-[var(--color-primary)]" />
        </div>
        <div className="space-y-2">
          <h2 className="text-xl font-bold text-[var(--color-text-primary)]">Check your email</h2>
          <p className="text-sm text-[var(--color-text-secondary)]">
            If an account exists for <strong className="text-[var(--color-text-primary)]">{getValues('email')}</strong>, we&apos;ve sent a 6-digit reset code.
          </p>
        </div>
        <Link
          href={`/reset-password?email=${encodeURIComponent(getValues('email'))}`}
          className="inline-flex items-center justify-center w-full h-11 rounded-md font-semibold text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_20px_rgba(0,212,176,0.3)] transition-all"
        >
          Enter Reset Code
        </Link>
        <p className="text-xs text-[var(--color-text-muted)]">
          Didn&apos;t get it?{' '}
          <button onClick={() => setSubmitted(false)} className="text-[var(--color-primary)] hover:underline">
            Try again
          </button>
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">Forgot password?</h1>
        <p className="text-sm text-[var(--color-text-secondary)]">
          Enter your email and we&apos;ll send you a code to reset your password.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5">
        <div className="space-y-2">
          <Label className="text-[var(--color-text-secondary)]">Email address</Label>
          <Input
            {...register('email')}
            type="email"
            placeholder="you@example.com"
            autoComplete="email"
            autoFocus
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

        <Button
          type="submit"
          disabled={isPending}
          className="h-11 w-full font-semibold text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_20px_rgba(0,212,176,0.3)]"
        >
          {isPending && <Loader2 size={16} className="animate-spin mr-2" />}
          {isPending ? 'Sending…' : 'Send Reset Code'}
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
