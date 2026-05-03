'use client'

import { useState, useMemo } from 'react'
import { Loader2, ArrowRight, ArrowLeft, CheckCircle2 } from 'lucide-react'
import { useOnboardingQuestions, useCompleteOnboarding } from '../api/useIdentity'
import { Button } from '@/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/ui/card'
import { Label } from '@/ui/label'
import { RadioGroup, RadioGroupItem } from '@/ui/radio-group'
import { Progress } from '@/ui/progress'
import type { OnboardingQuestion } from '../types/identity.types'

/** Map question key → field name used in POST /complete */
const KEY_TO_FIELD: Record<string, string> = {
  native_language: 'nativeLanguage',
  target_language: 'targetLanguage',
  proficiency_level: 'proficiencyLevel',
  learning_goal: 'learningGoal',
  daily_goal: 'dailyGoalMinutes',
  morning_reminder_hour: 'morningReminderHour',
  evening_reminder_hour: 'eveningReminderHour',
}

/** Numeric keys that need parseInt before submit */
const NUMERIC_KEYS = new Set(['daily_goal', 'morning_reminder_hour', 'evening_reminder_hour'])

/** Friendly display labels for option values */
const DISPLAY_LABELS: Record<string, string> = {
  vi: 'Vietnamese 🇻🇳', en: 'English 🇬🇧', ja: 'Japanese 🇯🇵', ko: 'Korean 🇰🇷', zh: 'Chinese 🇨🇳', other: 'Other',
  Beginner: 'Beginner (A1)', Elementary: 'Elementary (A2)', Intermediate: 'Intermediate (B1)',
  UpperIntermediate: 'Upper Intermediate (B2)', Advanced: 'Advanced (C1)', Mastery: 'Mastery (C2)',
  Communication: '💬 Communication', Academic: '🎓 Academic', Business: '💼 Business',
  Travel: '✈️ Travel', ExamPrep: '📝 Exam Prep', Other: '🌟 Other',
  none: 'Disabled',
}

function getLabel(value: string, key: string): string {
  // For reminder hours, format as time
  if ((key === 'morning_reminder_hour' || key === 'evening_reminder_hour') && value !== 'none') {
    const h = parseInt(value)
    if (h <= 12) return `${h}:00 AM`
    return `${h - 12}:00 PM`
  }
  // For daily_goal, show as minutes
  if (key === 'daily_goal') return `${value} minutes / day`
  return DISPLAY_LABELS[value] || value
}

export function OnboardingWizard() {
  const { data: questions, isLoading } = useOnboardingQuestions()
  const { mutate: completeOnboarding, isPending } = useCompleteOnboarding()

  const [currentStep, setCurrentStep] = useState(0)
  const [answers, setAnswers] = useState<Record<string, string>>({})

  // Sort questions by order
  const sortedQuestions = useMemo(
    () => [...(questions ?? [])].sort((a, b) => a.order - b.order),
    [questions]
  )

  const totalSteps = sortedQuestions.length
  const currentQuestion: OnboardingQuestion | undefined = sortedQuestions[currentStep]
  const progress = totalSteps > 0 ? ((currentStep + 1) / totalSteps) * 100 : 0

  const canGoNext = !!answers[currentQuestion?.key ?? '']
  const isLastStep = currentStep === totalSteps - 1

  const handleSelect = (value: string) => {
    if (!currentQuestion) return
    setAnswers(prev => ({ ...prev, [currentQuestion.key]: value }))
  }

  const handleNext = () => {
    if (canGoNext) setCurrentStep(prev => Math.min(prev + 1, totalSteps - 1))
  }

  const handleBack = () => setCurrentStep(prev => Math.max(prev - 1, 0))

  const handleSubmit = () => {
    // Build the request body from answers
    const body: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(answers)) {
      const field = KEY_TO_FIELD[key]
      if (!field) continue

      if (value === 'none') {
        body[field] = null
      } else if (NUMERIC_KEYS.has(key)) {
        body[field] = parseInt(value)
      } else {
        body[field] = value
      }
    }
    completeOnboarding(body as any)
  }

  // Loading state
  if (isLoading) {
    return (
      <Card className="w-full max-w-2xl mx-auto border-[var(--color-border)] bg-[var(--color-surface)] shadow-[0_20px_60px_var(--color-card-shadow)]">
        <CardContent className="flex items-center justify-center py-24">
          <Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" />
        </CardContent>
      </Card>
    )
  }

  if (!currentQuestion) return null

  return (
    <Card className="w-full max-w-2xl mx-auto border-[var(--color-border)] bg-[var(--color-surface)] shadow-[0_20px_60px_var(--color-card-shadow)]">
      <CardHeader>
        <div className="mb-4">
          <Progress value={progress} className="h-2 bg-[var(--color-surface-2)] [&>div]:bg-gradient-to-r [&>div]:from-[#00d4b0] [&>div]:to-[#00a8e0]" />
          <p className="text-xs text-[var(--color-text-muted)] mt-2 text-right">
            {currentStep + 1} / {totalSteps}
          </p>
        </div>
        <CardTitle className="text-2xl text-[var(--color-text-primary)]">{currentQuestion.text}</CardTitle>
        <CardDescription className="text-[var(--color-text-secondary)]">
          Choose one option below to continue.
        </CardDescription>
      </CardHeader>

      <CardContent className="min-h-[300px]">
        <div className="animate-in fade-in slide-in-from-right-4 duration-300" key={currentQuestion.key}>
          <RadioGroup
            value={answers[currentQuestion.key] || ''}
            onValueChange={handleSelect}
            className="grid grid-cols-1 md:grid-cols-2 gap-3"
          >
            {currentQuestion.options.map(option => {
              const displayLabel = getLabel(option, currentQuestion.key)
              return (
                <div key={option}>
                  <RadioGroupItem value={option} id={`${currentQuestion.key}-${option}`} className="peer sr-only" />
                  <Label
                    htmlFor={`${currentQuestion.key}-${option}`}
                    className="flex items-center justify-center rounded-lg border-2 border-[var(--color-border)] bg-[var(--color-input-bg)] p-4 hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)] peer-data-[state=checked]:border-[var(--color-primary)] peer-data-[state=checked]:bg-[var(--color-primary-dim)] cursor-pointer text-[var(--color-text-primary)] transition-all font-medium text-center min-h-[56px]"
                  >
                    {displayLabel}
                  </Label>
                </div>
              )
            })}
          </RadioGroup>
        </div>

        {/* Summary on last step */}
        {isLastStep && canGoNext && (
          <div className="mt-8 p-4 rounded-lg bg-[var(--color-primary-dim)] border border-[var(--color-primary)]/20 flex items-start gap-3">
            <CheckCircle2 className="text-[var(--color-primary)] shrink-0 mt-0.5" />
            <div>
              <h4 className="font-semibold text-[var(--color-text-primary)]">You&apos;re all set!</h4>
              <p className="text-sm text-[var(--color-text-secondary)] mt-1">
                Click complete below to start your learning journey.
              </p>
            </div>
          </div>
        )}
      </CardContent>

      <CardFooter className="flex justify-between border-t border-[var(--color-border)] pt-6">
        <Button
          type="button"
          variant="outline"
          onClick={handleBack}
          disabled={currentStep === 0 || isPending}
          className="border-[var(--color-border)] bg-[var(--color-surface-2)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] w-28"
        >
          <ArrowLeft className="mr-2 h-4 w-4" /> Back
        </Button>

        {!isLastStep ? (
          <Button
            type="button"
            onClick={handleNext}
            disabled={!canGoNext}
            className="w-28 text-[var(--color-btn-text)] bg-gradient-to-r from-[#00d4b0] to-[#00a8e0] hover:from-[#00b898] hover:to-[#0096cc] shadow-[0_4px_15px_rgba(0,212,176,0.3)] disabled:opacity-50"
          >
            Next <ArrowRight className="ml-2 h-4 w-4" />
          </Button>
        ) : (
          <Button
            type="button"
            onClick={handleSubmit}
            disabled={isPending || !canGoNext}
            className="w-36 font-bold text-[var(--color-btn-text)] bg-gradient-to-r from-[#f59e0b] to-[#ea580c] hover:from-[#d97706] hover:to-[#c2410c] shadow-[0_4px_15px_rgba(245,158,11,0.3)]"
          >
            {isPending ? <Loader2 className="animate-spin mr-2 h-4 w-4" /> : <CheckCircle2 className="mr-2 h-4 w-4" />}
            {isPending ? 'Saving...' : 'Complete!'}
          </Button>
        )}
      </CardFooter>
    </Card>
  )
}
