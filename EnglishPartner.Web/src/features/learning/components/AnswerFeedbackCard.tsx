import { CheckCircle2, XCircle, ArrowRight } from 'lucide-react'
import { Card } from '@/ui/card'
import { Button } from '@/ui/button'
import { StarRating } from '@/shared/components/StarRating'
import type { SubmitAnswerResponse } from '../types/learning.types'

interface FeedbackResult {
  qualityScore: number
  isCorrect: boolean
  suggestedCorrection?: string | null
  explanation?: string | null
  transcription?: string
  pronunciationFeedback?: string | null
}

interface AnswerFeedbackCardProps {
  result: FeedbackResult
  onNext: () => void
}

export function AnswerFeedbackCard({ result, onNext }: AnswerFeedbackCardProps) {
  const isCorrect = result.isCorrect

  return (
    <Card className={`p-6 border-2 ${
      isCorrect ? 'border-emerald-500/30 bg-emerald-500/5' : 'border-red-500/30 bg-red-500/5'
    }`}>
      <div className="flex items-start gap-4">
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${
          isCorrect ? 'bg-emerald-500 text-white' : 'bg-red-500 text-white'
        }`}>
          {isCorrect ? <CheckCircle2 className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}
        </div>

        <div className="flex-1 space-y-2">
          <div className="flex items-center gap-2">
            <p className={`font-semibold ${isCorrect ? 'text-emerald-500' : 'text-red-500'}`}>
              {isCorrect ? 'Correct!' : 'Not quite right'}
            </p>
            <StarRating score={result.qualityScore} />
          </div>

          {result.explanation && (
            <p className="text-sm text-[var(--color-text-secondary)]">{result.explanation}</p>
          )}

          {result.transcription && (
            <div className="p-3 rounded-md bg-[var(--color-surface-2)] border border-[var(--color-border)]">
              <p className="text-xs font-medium text-[var(--color-text-muted)] mb-1">AI heard:</p>
              <p className="text-sm text-[var(--color-text-primary)] italic">&quot;{result.transcription}&quot;</p>
            </div>
          )}

          {result.pronunciationFeedback && (
            <div className="p-3 rounded-md bg-amber-500/5 border border-amber-500/20">
              <p className="text-xs font-medium text-amber-600 mb-1">💡 Pronunciation tip:</p>
              <p className="text-sm text-[var(--color-text-secondary)]">{result.pronunciationFeedback}</p>
            </div>
          )}

          {result.suggestedCorrection && !result.pronunciationFeedback && (
            <div className="p-3 rounded-md bg-[var(--color-surface-2)] border border-[var(--color-border)]">
              <p className="text-xs font-medium text-[var(--color-text-muted)] mb-1">Suggested answer:</p>
              <p className="text-sm text-[var(--color-text-primary)] font-medium">{result.suggestedCorrection}</p>
            </div>
          )}
        </div>
      </div>

      <div className="flex justify-end mt-4">
        <Button onClick={onNext} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
          Next <ArrowRight className="ml-1 h-4 w-4" />
        </Button>
      </div>
    </Card>
  )
}
