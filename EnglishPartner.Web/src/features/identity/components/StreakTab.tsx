'use client'

import { Flame, Calendar, Trophy, Shield, Loader2, CheckCircle2 } from 'lucide-react'
import { useStreak, useCheckIn } from '../api/useIdentity'
import { Button } from '@/ui/button'

export function StreakTab() {
  const { data: streak, isLoading } = useStreak()
  const { mutate: checkIn, isPending } = useCheckIn()

  if (isLoading) {
    return <div className="py-8 flex justify-center"><Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!streak) {
    return <p className="text-[var(--color-text-muted)]">Unable to load streak data.</p>
  }

  const today = new Date().toISOString().split('T')[0]
  const isCheckedInToday = streak.lastActivityDate === today

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {/* Current Streak */}
        <div className="flex flex-col items-center justify-center p-6 border border-[var(--color-primary-dim)] bg-[var(--color-surface-2)] rounded-lg relative overflow-hidden">
          <div className="absolute top-0 right-0 p-2">
            <Flame className="h-24 w-24 text-[var(--color-primary)] opacity-10 absolute -right-4 -top-4" />
          </div>
          <Flame className="h-8 w-8 text-[var(--color-primary)] mb-2" />
          <h3 className="text-3xl font-bold text-[var(--color-text-primary)]">{streak.currentStreak}</h3>
          <p className="text-sm font-medium text-[var(--color-text-secondary)]">Day Streak</p>
        </div>

        {/* Longest Streak */}
        <div className="flex flex-col items-center justify-center p-6 border border-[var(--color-border)] bg-[var(--color-surface)] rounded-lg">
          <Trophy className="h-8 w-8 text-amber-500 mb-2" />
          <h3 className="text-3xl font-bold text-[var(--color-text-primary)]">{streak.longestStreak}</h3>
          <p className="text-sm font-medium text-[var(--color-text-secondary)]">Longest Streak</p>
        </div>

        {/* Freeze Tokens */}
        <div className="flex flex-col items-center justify-center p-6 border border-[var(--color-border)] bg-[var(--color-surface)] rounded-lg">
          <Shield className="h-8 w-8 text-blue-500 mb-2" />
          <h3 className="text-3xl font-bold text-[var(--color-text-primary)]">{streak.freezeTokens}</h3>
          <p className="text-sm font-medium text-[var(--color-text-secondary)]">Freeze Tokens</p>
        </div>
      </div>

      <div className="flex items-center justify-between p-4 bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-md">
        <div>
          <h4 className="font-medium text-[var(--color-text-primary)] flex items-center gap-2">
            <Calendar className="h-4 w-4" /> Daily Check-in
          </h4>
          <p className="text-sm text-[var(--color-text-secondary)] mt-1">
            {isCheckedInToday 
              ? 'You have already checked in today. Keep up the good work!' 
              : 'Complete a lesson or manually check in to maintain your streak.'}
          </p>
        </div>
        
        <Button 
          onClick={() => checkIn()} 
          disabled={isPending || isCheckedInToday}
          className={isCheckedInToday 
            ? "bg-[var(--color-success)] text-white hover:bg-[var(--color-success)] opacity-80 cursor-default" 
            : "bg-[var(--color-primary)] text-[var(--color-btn-text)] hover:bg-[var(--color-primary-hover)]"}
        >
          {isPending ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : isCheckedInToday ? (
            <CheckCircle2 className="mr-2 h-4 w-4" />
          ) : (
            <Flame className="mr-2 h-4 w-4" />
          )}
          {isCheckedInToday ? 'Checked In' : 'Check In Now'}
        </Button>
      </div>
    </div>
  )
}
