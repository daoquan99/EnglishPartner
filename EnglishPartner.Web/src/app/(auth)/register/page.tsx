import Link from 'next/link'
import type { Metadata } from 'next'
import { RegisterForm } from '@/features/auth/components/RegisterForm'
import { GoogleLoginButton } from '@/features/auth/components/GoogleLoginButton'

export const metadata: Metadata = {
  title: 'Create account — EnglishPartner',
  description: 'Sign up for EnglishPartner and start your English learning journey.',
}

export default function RegisterPage() {
  return (
    <div>
      {/* Brand */}
      <div className="mb-8 text-center">
        <div className="mb-3 inline-flex items-center gap-2.5">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-[#00d4b0] to-[#00a8e0] text-xl shadow-[0_4px_15px_rgba(0,212,176,0.35)]">
            📚
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-[var(--color-text-primary)]">
            English<span className="text-[var(--color-primary)]">Partner</span>
          </h1>
        </div>
        <p className="text-sm text-[var(--color-text-secondary)]">
          Your AI-powered English learning companion
        </p>
      </div>

      {/* Card */}
      <div className="rounded-[var(--radius-xl)] border border-[var(--color-border)] bg-[var(--color-surface)] p-8 shadow-[0_20px_60px_var(--color-card-shadow)] backdrop-blur-lg">
        <h2 className="mb-6 text-xl font-semibold text-[var(--color-text-primary)]">
          Create your account
        </h2>

        <GoogleLoginButton />

        {/* Divider */}
        <div className="my-5 flex items-center gap-3">
          <div className="h-px flex-1 bg-[var(--color-border)]" />
          <span className="whitespace-nowrap text-xs text-[var(--color-text-muted)]">or continue with email</span>
          <div className="h-px flex-1 bg-[var(--color-border)]" />
        </div>

        <RegisterForm />

        <p className="mt-6 text-center text-sm text-[var(--color-text-secondary)]">
          Already have an account?{' '}
          <Link href="/login" className="font-medium text-[var(--color-primary)] hover:underline">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  )
}
