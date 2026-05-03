'use client'

import { useState } from 'react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'

interface FillInBlankExerciseProps {
  content: string // e.g. "She ___ to school every day."
  correctAnswer: string
  onSubmit: (userAnswer: string, isCorrect: boolean) => void
}

export function FillInBlankExercise({ content, correctAnswer, onSubmit }: FillInBlankExerciseProps) {
  const [answer, setAnswer] = useState('')
  const [submitted, setSubmitted] = useState(false)

  const isCorrect = answer.trim().toLowerCase() === correctAnswer.trim().toLowerCase()

  const handleSubmit = () => {
    setSubmitted(true)
    onSubmit(answer, isCorrect)
  }

  const parts = content.split('___')

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2 text-lg text-[var(--color-text-primary)]">
        {parts.map((part, i) => (
          <span key={i}>
            {part}
            {i < parts.length - 1 && (
              <Input
                value={answer}
                onChange={e => !submitted && setAnswer(e.target.value)}
                disabled={submitted}
                className={`inline-block w-40 mx-1 text-center font-semibold ${
                  submitted
                    ? isCorrect ? 'border-emerald-500 text-emerald-500' : 'border-red-500 text-red-500'
                    : 'border-[var(--color-primary)] bg-[var(--color-input-bg)]'
                }`}
                onKeyDown={e => e.key === 'Enter' && !submitted && answer.trim() && handleSubmit()}
              />
            )}
          </span>
        ))}
      </div>

      {submitted && !isCorrect && (
        <p className="text-sm text-[var(--color-text-muted)]">
          Correct answer: <span className="font-semibold text-emerald-500">{correctAnswer}</span>
        </p>
      )}

      {!submitted && (
        <Button onClick={handleSubmit} disabled={!answer.trim()} className="w-full bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
          Check Answer
        </Button>
      )}
    </div>
  )
}
