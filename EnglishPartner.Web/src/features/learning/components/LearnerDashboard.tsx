'use client'

import Link from 'next/link'
import { BookOpen, Play, Sparkles } from 'lucide-react'
import { Button } from '@/ui/button'
import { Card } from '@/ui/card'
import { DashboardStatsCards } from './DashboardStatsCards'
import { LeaderboardCard } from './LeaderboardCard'
import { useDashboard, useReviewsDue } from '../api/useLearning'

export function LearnerDashboard() {
  const { error: dashError } = useDashboard()
  const { data: dueItems } = useReviewsDue()

  const isProfileNotFound = (dashError as { response?: { status?: number } })?.response?.status === 404

  if (isProfileNotFound) {
    return (
      <div className="flex flex-col items-center justify-center py-20 gap-4">
        <Sparkles className="h-12 w-12 text-[var(--color-primary)]" />
        <h2 className="text-2xl font-bold text-[var(--color-text-primary)]">Welcome to EnglishPartner!</h2>
        <p className="text-[var(--color-text-secondary)] text-center max-w-md">
          Your learning profile is being set up. Please complete your onboarding to get started.
        </p>
        <Button asChild className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
          <Link href="/onboarding">Complete Onboarding</Link>
        </Button>
      </div>
    )
  }

  const dueCount = dueItems?.length ?? 0

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">
          Welcome to English<span className="text-[var(--color-primary)]">Partner</span>
        </h1>
        <p className="mt-1 text-[var(--color-text-secondary)]">Your learning journey starts here.</p>
      </div>

      <DashboardStatsCards />

      {/* Quick Actions */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] p-5">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-violet-500 to-purple-600 text-white shadow-lg">
              <BookOpen className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-[var(--color-text-primary)]">Review Flashcards</h3>
              <p className="text-sm text-[var(--color-text-secondary)] mt-0.5">
                {dueCount > 0 ? `${dueCount} items due for review` : 'All caught up!'}
              </p>
              <Button asChild size="sm" className="mt-3 bg-gradient-to-r from-violet-500 to-purple-600 text-white border-0">
                <Link href="/review">Start Review</Link>
              </Button>
            </div>
          </div>
        </Card>

        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] p-5">
          <div className="flex items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 text-white shadow-lg">
              <Play className="h-6 w-6" />
            </div>
            <div className="flex-1">
              <h3 className="font-semibold text-[var(--color-text-primary)]">Study Session</h3>
              <p className="text-sm text-[var(--color-text-secondary)] mt-0.5">
                Start a guided practice session with AI feedback
              </p>
              <Button asChild size="sm" className="mt-3 bg-gradient-to-r from-emerald-500 to-teal-600 text-white border-0">
                <Link href="/study">Start Session</Link>
              </Button>
            </div>
          </div>
        </Card>
      </div>

      <LeaderboardCard />
    </div>
  )
}
