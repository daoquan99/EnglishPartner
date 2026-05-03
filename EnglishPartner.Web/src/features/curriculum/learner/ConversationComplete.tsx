import Link from 'next/link'
import { Trophy, Sparkles } from 'lucide-react'
import { Card } from '@/ui/card'
import { Button } from '@/ui/button'
import { StarRating } from '@/shared/components/StarRating'

interface ConversationCompleteProps {
  avgScore: number
  lineScores: number[]
}

export function ConversationComplete({ avgScore, lineScores }: ConversationCompleteProps) {
  const qualityScore = Math.round(avgScore / 20)
  const perfectLines = lineScores.filter(s => s >= 90).length

  return (
    <div className="max-w-md mx-auto animate-in fade-in zoom-in-95 duration-500">
      <Card className="overflow-hidden border-[var(--color-border)] bg-[var(--color-surface)] shadow-xl">
        {/* Gradient header */}
        <div className="relative bg-gradient-to-br from-amber-400 via-orange-500 to-rose-500 p-8 text-center text-white">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_0%,rgba(255,255,255,0.15),transparent_70%)]" />
          <div className="relative z-10">
            <div className="mx-auto mb-3 flex h-16 w-16 items-center justify-center rounded-full bg-white/20 backdrop-blur-sm shadow-lg">
              <Trophy className="h-8 w-8" />
            </div>
            <h2 className="text-2xl font-bold">Role-play Complete!</h2>
            <p className="text-sm opacity-80 mt-1">Great job practicing your speaking</p>
          </div>
        </div>

        <div className="p-6 space-y-5">
          {/* Score display */}
          <div className="text-center space-y-2">
            <div className="flex justify-center">
              <StarRating score={qualityScore} className="[&_svg]:h-6 [&_svg]:w-6" />
            </div>
            <p className="text-4xl font-bold bg-gradient-to-r from-amber-500 to-orange-500 bg-clip-text text-transparent">
              {avgScore}<span className="text-lg font-normal text-[var(--color-text-muted)]">/100</span>
            </p>
            <p className="text-xs text-[var(--color-text-muted)]">Average pronunciation score</p>
          </div>

          {/* Stats row */}
          <div className="grid grid-cols-3 gap-2">
            <div className="text-center p-2 rounded-lg bg-[var(--color-surface-2)]">
              <p className="text-lg font-bold text-[var(--color-primary)]">{lineScores.length}</p>
              <p className="text-[10px] text-[var(--color-text-muted)]">Lines spoken</p>
            </div>
            <div className="text-center p-2 rounded-lg bg-[var(--color-surface-2)]">
              <p className="text-lg font-bold text-emerald-500">{perfectLines}</p>
              <p className="text-[10px] text-[var(--color-text-muted)]">Perfect lines</p>
            </div>
            <div className="text-center p-2 rounded-lg bg-[var(--color-surface-2)]">
              <p className="text-lg font-bold text-amber-500 flex items-center justify-center gap-0.5">
                <Sparkles className="h-3.5 w-3.5" />{qualityScore}
              </p>
              <p className="text-[10px] text-[var(--color-text-muted)]">Quality</p>
            </div>
          </div>

          {/* Per-line breakdown */}
          {lineScores.length > 0 && (
            <div className="space-y-1.5">
              <p className="text-xs font-medium text-[var(--color-text-muted)]">Score breakdown</p>
              <div className="flex items-center gap-1.5 flex-wrap">
                {lineScores.map((score, i) => (
                  <span
                    key={i}
                    className={`text-xs font-medium px-2.5 py-1 rounded-lg ${
                      score >= 80 ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
                      : score >= 60 ? 'bg-amber-500/10 text-amber-600 dark:text-amber-400'
                      : 'bg-red-500/10 text-red-600 dark:text-red-400'
                    }`}
                  >
                    #{i + 1}: {score}
                  </span>
                ))}
              </div>
            </div>
          )}

          <Button asChild className="w-full h-11 bg-gradient-to-r from-[var(--color-primary)] to-[color-mix(in_srgb,var(--color-primary),#000_10%)] text-[var(--color-btn-text)] font-semibold shadow-lg">
            <Link href="/conversations">Back to Conversations</Link>
          </Button>
        </div>
      </Card>
    </div>
  )
}
