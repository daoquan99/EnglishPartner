'use client'

import { useState } from 'react'
import { Loader2, ArrowRight } from 'lucide-react'
import { Button } from '@/ui/button'
import { Card } from '@/ui/card'
import { Badge } from '@/ui/badge'
import { Progress } from '@/ui/progress'
import { useReviewsDue, useSubmitReview } from '../api/useLearning'
import { ReviewEmptyState } from './ReviewEmptyState'
import { ItemType } from '../types/learning.types'

const ITEM_TYPE_LABELS: Record<ItemType, string> = {
  [ItemType.Vocabulary]: 'Vocabulary',
  [ItemType.Grammar]: 'Grammar',
  [ItemType.ExampleSentence]: 'Example Sentence',
  [ItemType.Topic]: 'Topic',
  [ItemType.Conversation]: 'Conversation',
  [ItemType.Exercise]: 'Exercise',
}

const QUALITY_OPTIONS = [
  { score: 0, label: 'Blackout', color: 'bg-red-600' },
  { score: 1, label: 'Wrong', color: 'bg-red-500' },
  { score: 2, label: 'Partial', color: 'bg-orange-500' },
  { score: 3, label: 'OK', color: 'bg-yellow-500' },
  { score: 4, label: 'Good', color: 'bg-emerald-500' },
  { score: 5, label: 'Perfect', color: 'bg-green-600' },
]

export function FlashcardReview() {
  const { data: dueItems, isLoading } = useReviewsDue()
  const { mutate: submitReview, isPending } = useSubmitReview()
  const [currentIndex, setCurrentIndex] = useState(0)
  const [isFlipped, setIsFlipped] = useState(false)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" />
      </div>
    )
  }

  if (!dueItems || dueItems.length === 0) return <ReviewEmptyState />
  if (currentIndex >= dueItems.length) return <ReviewEmptyState />

  const item = dueItems[currentIndex]
  const progress = ((currentIndex) / dueItems.length) * 100

  const handleRate = (qualityRating: number) => {
    submitReview(
      { itemId: item.itemId, type: item.type, qualityRating },
      { onSuccess: () => { setCurrentIndex(prev => prev + 1); setIsFlipped(false) } }
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-[var(--color-text-primary)]">Review Session</h2>
        <span className="text-sm text-[var(--color-text-secondary)]">
          {currentIndex + 1} / {dueItems.length}
        </span>
      </div>

      <Progress value={progress} className="h-2" />

      <Card
        className="min-h-[250px] flex flex-col items-center justify-center p-8 border-[var(--color-border)] bg-[var(--color-surface)] cursor-pointer transition-all hover:shadow-lg"
        onClick={() => setIsFlipped(!isFlipped)}
      >
        <Badge variant="outline" className="mb-4 border-[var(--color-primary)] text-[var(--color-primary)]">
          {ITEM_TYPE_LABELS[item.type] ?? 'Unknown'}
        </Badge>

        {!isFlipped ? (
          <div className="text-center">
            <p className="text-lg text-[var(--color-text-primary)] font-medium">
              Item: {item.itemId.slice(0, 8)}…
            </p>
            <p className="text-sm text-[var(--color-text-muted)] mt-2">Tap to reveal</p>
          </div>
        ) : (
          <div className="text-center space-y-2">
            <p className="text-sm text-[var(--color-text-secondary)]">
              Interval: {item.intervalDays}d • Reps: {item.repetitions} • EF: {item.easinessFactor.toFixed(2)}
            </p>
            <p className="text-sm text-[var(--color-text-muted)] mt-4 flex items-center gap-1 justify-center">
              <ArrowRight className="h-3 w-3" /> Rate your recall below
            </p>
          </div>
        )}
      </Card>

      {isFlipped && (
        <div className="grid grid-cols-6 gap-2">
          {QUALITY_OPTIONS.map(opt => (
            <Button
              key={opt.score}
              variant="outline"
              disabled={isPending}
              onClick={() => handleRate(opt.score)}
              className={`flex flex-col items-center gap-1 py-3 h-auto border-[var(--color-border)] hover:text-white hover:${opt.color}`}
            >
              <span className="text-lg font-bold">{opt.score}</span>
              <span className="text-[10px]">{opt.label}</span>
            </Button>
          ))}
        </div>
      )}
    </div>
  )
}
