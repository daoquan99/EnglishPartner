'use client'

import { Medal, TrendingUp } from 'lucide-react'
import { Card } from '@/ui/card'
import { Badge } from '@/ui/badge'
import { useLeaderboard } from '../api/useLearning'
import { useAuthStore } from '@/lib/zustand/auth.store'

const RANK_STYLES: Record<number, string> = {
  1: 'bg-gradient-to-br from-yellow-400 to-amber-500 text-white shadow-md',
  2: 'bg-gradient-to-br from-gray-300 to-gray-400 text-white shadow-md',
  3: 'bg-gradient-to-br from-orange-400 to-orange-600 text-white shadow-md',
}

export function LeaderboardCard() {
  const { data: entries, isLoading } = useLeaderboard(10)
  const currentUserId = useAuthStore(s => s.user?.id)

  return (
    <Card className="border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden">
      <div className="flex items-center gap-2 px-5 py-4 border-b border-[var(--color-border)]">
        <TrendingUp className="h-5 w-5 text-[var(--color-primary)]" />
        <h3 className="text-lg font-semibold text-[var(--color-text-primary)]">Leaderboard</h3>
      </div>

      <div className="divide-y divide-[var(--color-border)]">
        {isLoading ? (
          Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-center gap-3 px-5 py-3">
              <div className="h-8 w-8 rounded-full bg-[var(--color-surface-2)] animate-pulse" />
              <div className="flex-1">
                <div className="h-4 w-24 rounded bg-[var(--color-surface-2)] animate-pulse" />
              </div>
              <div className="h-4 w-12 rounded bg-[var(--color-surface-2)] animate-pulse" />
            </div>
          ))
        ) : entries && entries.length > 0 ? (
          entries.map((entry, idx) => {
            const rank = idx + 1
            const isCurrentUser = entry.userId === currentUserId

            return (
              <div
                key={entry.userId}
                className={`flex items-center gap-3 px-5 py-3 transition-colors ${
                  isCurrentUser ? 'bg-[var(--color-primary-dim)]' : 'hover:bg-[var(--color-surface-2)]'
                }`}
              >
                <div className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold ${
                  RANK_STYLES[rank] || 'bg-[var(--color-surface-2)] text-[var(--color-text-secondary)]'
                }`}>
                  {rank}
                </div>

                <div className="flex-1 min-w-0">
                  <p className={`text-sm font-medium truncate ${
                    isCurrentUser ? 'text-[var(--color-primary)]' : 'text-[var(--color-text-primary)]'
                  }`}>
                    {entry.displayName}
                    {isCurrentUser && <span className="ml-1 text-xs">(You)</span>}
                  </p>
                </div>

                <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)] text-xs">
                  {entry.currentLevel}
                </Badge>

                <div className="flex items-center gap-1 text-sm font-semibold text-[var(--color-text-secondary)] tabular-nums">
                  <Medal className="h-3.5 w-3.5" />
                  {entry.totalXp.toLocaleString()}
                </div>
              </div>
            )
          })
        ) : (
          <div className="px-5 py-8 text-center text-sm text-[var(--color-text-muted)]">
            No leaderboard data yet.
          </div>
        )}
      </div>
    </Card>
  )
}
