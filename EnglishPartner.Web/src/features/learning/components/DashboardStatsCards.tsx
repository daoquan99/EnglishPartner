'use client'

import { Flame, Trophy, Zap, BookOpen } from 'lucide-react'
import { Card } from '@/ui/card'
import { useDashboard, useReviewsDue } from '../api/useLearning'

const STAT_CARDS = [
  { key: 'xp', label: 'Total XP', icon: Zap, color: 'from-amber-500 to-orange-600' },
  { key: 'level', label: 'Current Level', icon: Trophy, color: 'from-emerald-500 to-teal-600' },
  { key: 'streak', label: 'Day Streak', icon: Flame, color: 'from-rose-500 to-pink-600' },
  { key: 'due', label: 'Items Due', icon: BookOpen, color: 'from-blue-500 to-indigo-600' },
] as const

export function DashboardStatsCards() {
  const { data: dashboard, isLoading: dashLoading } = useDashboard()
  const { data: dueItems, isLoading: dueLoading } = useReviewsDue()

  const isLoading = dashLoading || dueLoading

  const values: Record<string, string | number> = {
    xp: dashboard?.totalXp ?? 0,
    level: dashboard?.currentLevel ?? '—',
    streak: dashboard?.currentStreak ?? 0,
    due: dueItems?.length ?? 0,
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {STAT_CARDS.map(({ key, label, icon: Icon, color }) => (
        <Card
          key={key}
          className="relative overflow-hidden border-[var(--color-border)] bg-[var(--color-surface)] p-5"
        >
          <div className={`absolute top-0 right-0 h-20 w-20 rounded-bl-full bg-gradient-to-br ${color} opacity-10`} />
          <div className="flex items-center gap-3">
            <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br ${color} text-white shadow-md`}>
              <Icon className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs font-medium text-[var(--color-text-secondary)] uppercase tracking-wider">{label}</p>
              {isLoading ? (
                <div className="h-7 w-16 mt-1 rounded bg-[var(--color-surface-2)] animate-pulse" />
              ) : (
                <p className="text-2xl font-bold text-[var(--color-text-primary)]">{values[key]}</p>
              )}
            </div>
          </div>
        </Card>
      ))}
    </div>
  )
}
