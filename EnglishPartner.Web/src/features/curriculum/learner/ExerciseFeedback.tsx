import { CheckCircle2, XCircle, Star, RotateCcw, ArrowRight, Trophy } from 'lucide-react'
import { Card } from '@/ui/card'
import { Button } from '@/ui/button'

interface ExerciseFeedbackProps {
  isCorrect: boolean
  explanation?: string | null
  xpReward: number
  onRetry?: () => void
  onNext?: () => void
  hasNext?: boolean
}

export function ExerciseFeedback({ isCorrect, explanation, xpReward, onRetry, onNext, hasNext }: ExerciseFeedbackProps) {
  return (
    <Card className={`p-5 border-2 ${isCorrect ? 'border-emerald-500/30 bg-emerald-500/5' : 'border-red-500/30 bg-red-500/5'}`}>
      <div className="flex items-start gap-3">
        <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${isCorrect ? 'bg-emerald-500' : 'bg-red-500'} text-white`}>
          {isCorrect ? <CheckCircle2 className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}
        </div>
        <div className="flex-1">
          <p className={`font-semibold ${isCorrect ? 'text-emerald-500' : 'text-red-500'}`}>
            {isCorrect ? 'Correct!' : 'Not quite right'}
          </p>
          {explanation && <p className="text-sm text-[var(--color-text-secondary)] mt-1">{explanation}</p>}
          {isCorrect && (
            <p className="text-sm text-amber-500 mt-2 flex items-center gap-1">
              <Star className="h-4 w-4 fill-amber-400" /> +{xpReward} XP
            </p>
          )}
        </div>
      </div>

      {/* Action buttons */}
      <div className="flex items-center gap-3 mt-4 pt-4 border-t border-[var(--color-border)]">
        {onRetry && (
          <Button
            variant="outline"
            size="sm"
            onClick={onRetry}
            className="flex items-center gap-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Try Again
          </Button>
        )}
        {onNext && (
          <Button
            size="sm"
            onClick={onNext}
            className={`flex items-center gap-1.5 ml-auto ${
              isCorrect
                ? 'bg-gradient-to-r from-emerald-500 to-teal-600 text-white hover:shadow-md border-0'
                : 'bg-gradient-to-r from-orange-500 to-amber-600 text-white hover:shadow-md border-0'
            }`}
          >
            {hasNext ? (
              <>Next Exercise <ArrowRight className="h-3.5 w-3.5" /></>
            ) : (
              <>Finish <Trophy className="h-3.5 w-3.5" /></>
            )}
          </Button>
        )}
      </div>
    </Card>
  )
}
