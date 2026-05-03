'use client'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, KeySquare } from 'lucide-react'
import { z } from 'zod'

import { useChangePassword } from '@/features/auth/api/useAuth'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required'),
  newPassword: z.string().min(6, 'Password must be at least 6 characters'),
  confirmPassword: z.string().min(1, 'Please confirm your password'),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})

type PasswordFormData = z.infer<typeof passwordSchema>

export function SecurityTab() {
  const { mutate: changePassword, isPending } = useChangePassword()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PasswordFormData>({
    resolver: zodResolver(passwordSchema),
  })

  const onSubmit = (data: PasswordFormData) => {
    changePassword({
      currentPassword: data.currentPassword,
      newPassword: data.newPassword,
    }, {
      onSuccess: () => reset()
    })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 max-w-md">
      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">Current Password</Label>
        <Input 
          type="password"
          {...register('currentPassword')} 
          className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]" 
        />
        {errors.currentPassword && <p className="text-sm text-[var(--color-destructive)]">{errors.currentPassword.message}</p>}
      </div>

      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">New Password</Label>
        <Input 
          type="password"
          {...register('newPassword')} 
          className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]" 
        />
        {errors.newPassword && <p className="text-sm text-[var(--color-destructive)]">{errors.newPassword.message}</p>}
      </div>

      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">Confirm New Password</Label>
        <Input 
          type="password"
          {...register('confirmPassword')} 
          className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]" 
        />
        {errors.confirmPassword && <p className="text-sm text-[var(--color-destructive)]">{errors.confirmPassword.message}</p>}
      </div>

      <Button
        type="submit"
        disabled={isPending}
        className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]"
      >
        {isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <KeySquare className="mr-2 h-4 w-4" />}
        Update Password
      </Button>
    </form>
  )
}
