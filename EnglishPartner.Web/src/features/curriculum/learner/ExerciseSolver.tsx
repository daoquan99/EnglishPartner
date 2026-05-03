'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { ArrowLeft, Loader2, Dumbbell } from 'lucide-react'
import { Badge } from '@/ui/badge'
import { Button } from '@/ui/button'
import { Card } from '@/ui/card'
import { useExercise, useExercises, useSubmitStandaloneExercise } from '../api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { MultipleChoiceExercise } from './MultipleChoiceExercise'
import { FillInBlankExercise } from './FillInBlankExercise'
import { ExerciseFeedback } from './ExerciseFeedback'

interface ExerciseSolverProps {
  exerciseId: string
}

export function ExerciseSolver({ exerciseId }: ExerciseSolverProps) {
  const router = useRouter()
  const { user } = useAuthStore()
  const { data: exercise, isLoading } = useExercise(exerciseId)
  const [answered, setAnswered] = useState(false)
  const [isCorrect, setIsCorrect] = useState(false)
  const [key, setKey] = useState(0) // force re-render on retry
  const { mutate: submitExercise } = useSubmitStandaloneExercise()

  // Fetch siblings to find next exercise
  const { data: siblingData } = useExercises({
    languageCode: user?.targetLanguage || 'en',
    topicId: exercise?.topicId || undefined,
    pageSize: 50,
  })
  const siblings = siblingData?.items || []
  const currentIdx = siblings.findIndex(e => e.id === exerciseId)
  const nextExercise = currentIdx >= 0 && currentIdx < siblings.length - 1 ? siblings[currentIdx + 1] : null

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!exercise) {
    return <div className="text-center py-20 text-[var(--color-text-muted)]">Exercise not found.</div>
  }

  const handleAnswer = (_answer: string, correct: boolean) => {
    setIsCorrect(correct)
    setAnswered(true)
    if (!answered) {
      submitExercise({ id: exerciseId, isCorrect: correct })
    }
  }

  const handleRetry = () => {
    setAnswered(false)
    setIsCorrect(false)
    setKey(k => k + 1)
  }

  const handleNext = () => {
    if (nextExercise) {
      router.push(`/exercises/${nextExercise.id}`)
    } else {
      router.push('/exercises')
    }
  }

  const renderExercise = () => {
    const type = exercise.type?.toLowerCase() ?? ''

    if (type === 'multiplechoice' && exercise.options) {
      return (
        <MultipleChoiceExercise
          key={key}
          options={exercise.options.map(o => ({ id: o.id, text: o.text, isCorrect: o.isCorrect }))}
          onSubmit={handleAnswer}
        />
      )
    }

    if ((type === 'fillintheblank' || type === 'gapfilling' || type === 'sentencecompletion') && exercise.content) {
      return (
        <FillInBlankExercise
          key={key}
          content={exercise.content}
          correctAnswer={exercise.correctAnswer}
          onSubmit={handleAnswer}
        />
      )
    }

    // Fallback: generic text input
    return (
      <FillInBlankExercise
        key={key}
        content={exercise.content ?? '___'}
        correctAnswer={exercise.correctAnswer}
        onSubmit={handleAnswer}
      />
    )
  }

  // Progress indicator
  const progress = siblings.length > 0 ? `${currentIdx + 1} / ${siblings.length}` : null

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <Button asChild variant="ghost" size="sm" className="text-[var(--color-text-secondary)]">
          <Link href="/exercises"><ArrowLeft className="h-4 w-4 mr-1" /> Back</Link>
        </Button>
        {progress && (
          <span className="text-xs font-medium text-[var(--color-text-muted)] bg-[var(--color-surface-2)] px-3 py-1.5 rounded-full">
            Exercise {progress}
          </span>
        )}
      </div>

      {/* Header */}
      <Card className="p-5 border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="flex items-center gap-3 mb-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-amber-500 to-orange-600 text-white shadow-md">
            <Dumbbell className="h-5 w-5" />
          </div>
          <div>
            <h1 className="text-lg font-bold text-[var(--color-text-primary)]">{exercise.title}</h1>
            <div className="flex items-center gap-2 mt-0.5">
              <Badge variant="outline" className="text-[10px] border-[var(--color-border)]">{exercise.type}</Badge>
              {exercise.level && <Badge variant="outline" className="text-[10px] border-[var(--color-primary)] text-[var(--color-primary)]">{exercise.level}</Badge>}
              <span className="text-xs text-amber-500 font-medium">{exercise.xpReward} XP</span>
            </div>
          </div>
        </div>

        {exercise.instruction && (
          <p className="text-sm text-[var(--color-text-secondary)] mb-4">{exercise.instruction}</p>
        )}

        {renderExercise()}
      </Card>

      {answered && (
        <ExerciseFeedback
          isCorrect={isCorrect}
          explanation={exercise.explanation}
          xpReward={exercise.xpReward}
          onRetry={handleRetry}
          onNext={handleNext}
          hasNext={!!nextExercise}
        />
      )}
    </div>
  )
}
