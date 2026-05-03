import Link from 'next/link'
import { Trophy, Star } from 'lucide-react'
import { Card } from '@/ui/card'
import { Button } from '@/ui/button'

interface SessionCompleteCardProps {
  totalItems: number
  correctCount: number
  score: number
}

export function SessionCompleteCard({ totalItems, correctCount, score }: SessionCompleteCardProps) {
  const xpEstimate = (totalItems * 5) + (score >= 80 ? 10 : 0)

  return (
    <Card className="max-w-md mx-auto p-8 border-[var(--color-border)] bg-[var(--color-surface)] text-center space-y-5">
      <div className="flex justify-center">
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-amber-400 to-orange-500 text-white shadow-lg">
          <Trophy className="h-8 w-8" />
        </div>
      </div>

      <h2 className="text-2xl font-bold text-[var(--color-text-primary)]">Session Complete!</h2>

      <div className="grid grid-cols-3 gap-4">
        <div>
          <p className="text-2xl font-bold text-[var(--color-primary)]">{score}%</p>
          <p className="text-xs text-[var(--color-text-muted)]">Score</p>
        </div>
        <div>
          <p className="text-2xl font-bold text-emerald-500">{correctCount}/{totalItems}</p>
          <p className="text-xs text-[var(--color-text-muted)]">Correct</p>
        </div>
        <div>
          <p className="text-2xl font-bold text-amber-500 flex items-center justify-center gap-1">
            <Star className="h-5 w-5" />{xpEstimate}
          </p>
          <p className="text-xs text-[var(--color-text-muted)]">XP Earned</p>
        </div>
      </div>

      <Button asChild className="w-full bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
        <Link href="/dashboard">Back to Dashboard</Link>
      </Button>
    </Card>
  )
}
