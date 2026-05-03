'use client'

import { useState } from 'react'
import { Button } from '@/ui/button'
import { cn } from '@/shared/utils/cn'

interface MultipleChoiceExerciseProps {
  options: { id: string; text: string; isCorrect: boolean }[]
  onSubmit: (selectedId: string, isCorrect: boolean) => void
}

export function MultipleChoiceExercise({ options, onSubmit }: MultipleChoiceExerciseProps) {
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [submitted, setSubmitted] = useState(false)

  const handleSubmit = () => {
    if (!selectedId) return
    const option = options.find(o => o.id === selectedId)
    setSubmitted(true)
    onSubmit(selectedId, option?.isCorrect ?? false)
  }

  return (
    <div className="space-y-3">
      {options.map(opt => (
        <button
          key={opt.id}
          onClick={() => !submitted && setSelectedId(opt.id)}
          disabled={submitted}
          className={cn(
            'w-full text-left p-4 rounded-xl border-2 transition-all',
            submitted && opt.isCorrect && 'border-emerald-500 bg-emerald-500/10',
            submitted && !opt.isCorrect && opt.id === selectedId && 'border-red-500 bg-red-500/10',
            !submitted && opt.id === selectedId && 'border-[var(--color-primary)] bg-[var(--color-primary-dim)]',
            !submitted && opt.id !== selectedId && 'border-[var(--color-border)] bg-[var(--color-surface)] hover:border-[var(--color-primary)]/50',
            submitted && opt.id !== selectedId && !opt.isCorrect && 'border-[var(--color-border)] bg-[var(--color-surface)] opacity-50',
          )}
        >
          <span className={cn(
            'text-sm font-medium',
            submitted && opt.isCorrect ? 'text-emerald-500' : 'text-[var(--color-text-primary)]',
            submitted && !opt.isCorrect && opt.id === selectedId && 'text-red-500',
          )}>
            {opt.text}
          </span>
        </button>
      ))}

      {!submitted && (
        <Button onClick={handleSubmit} disabled={!selectedId} className="w-full bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
          Check Answer
        </Button>
      )}
    </div>
  )
}
