'use client'

import { useState, useRef } from 'react'
import { Loader2, Send } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Card } from '@/ui/card'
import { Label } from '@/ui/label'
import { Progress } from '@/ui/progress'
import { useReviewsDue, useStartSession, useSubmitAnswer, useSubmitAudioAnswer, useCompleteSession } from '../api/useLearning'
import { useSessionPing } from '../hooks/useSessionPing'
import { AnswerFeedbackCard } from './AnswerFeedbackCard'
import { SessionCompleteCard } from './SessionCompleteCard'
import { AudioRecorder } from '@/shared/components/AudioRecorder'
import type { SubmitAnswerResponse, SubmitAudioAnswerResponse } from '../types/learning.types'

type Phase = 'setup' | 'answering' | 'feedback' | 'complete'

export function StudySessionFlow() {
  const { data: dueItems, isLoading: itemsLoading } = useReviewsDue()
  const { mutateAsync: startSession, isPending: starting } = useStartSession()
  const { mutateAsync: submitAnswer, isPending: submittingText } = useSubmitAnswer()
  const { mutateAsync: submitAudioAnswer, isPending: submittingAudio } = useSubmitAudioAnswer()
  const { mutateAsync: completeSession } = useCompleteSession()
  
  const submitting = submittingText || submittingAudio

  const [phase, setPhase] = useState<Phase>('setup')
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [currentIndex, setCurrentIndex] = useState(0)
  const [answer, setAnswer] = useState('')
  const [feedbackResult, setFeedbackResult] = useState<SubmitAnswerResponse | (SubmitAudioAnswerResponse & { isCorrect: boolean; suggestedCorrection: string | null; explanation: string }) | null>(null)
  const [correctCount, setCorrectCount] = useState(0)
  const timerRef = useRef<number>(0)

  useSessionPing(sessionId)

  const items = dueItems ?? []
  const totalItems = items.length

  const handleStart = async () => {
    if (totalItems === 0) return
    const id = await startSession({ sessionTitle: 'Review Session', totalItems })
    setSessionId(id)
    setPhase('answering')
    timerRef.current = Date.now()
  }

  const handleSubmitAnswer = async () => {
    if (!sessionId || !items[currentIndex]) return
    const item = items[currentIndex]
    const timeSpent = Math.round((Date.now() - timerRef.current) / 1000)

    const result = await submitAnswer({
      sessionId,
      itemId: item.itemId,
      itemType: item.type,
      userAnswer: answer,
      timeSpentSeconds: timeSpent,
    })

    setFeedbackResult(result)
    if (result.isCorrect) setCorrectCount(prev => prev + 1)
    setPhase('feedback')
  }

  const handleAudioAnswer = async (audioBlob: Blob) => {
    if (!sessionId || !items[currentIndex]) return
    const item = items[currentIndex]
    const timeSpent = Math.round((Date.now() - timerRef.current) / 1000)

    const result = await submitAudioAnswer({
      sessionId,
      itemId: item.itemId,
      itemType: item.type,
      timeSpentSeconds: timeSpent,
      audioFile: audioBlob,
    })

    // Map audio response to feedback format
    const isCorrect = result.qualityScore >= 3
    setFeedbackResult({
      ...result,
      isCorrect,
      suggestedCorrection: result.pronunciationFeedback,
      explanation: `Transcription: "${result.transcription}"`,
    })
    
    if (isCorrect) setCorrectCount(prev => prev + 1)
    setPhase('feedback')
  }

  const handleNext = async () => {
    const nextIndex = currentIndex + 1
    if (nextIndex >= totalItems) {
      const score = Math.round((correctCount / totalItems) * 100)
      await completeSession({ sessionId: sessionId!, score })
      setPhase('complete')
    } else {
      setCurrentIndex(nextIndex)
      setAnswer('')
      setFeedbackResult(null)
      setPhase('answering')
      timerRef.current = Date.now()
    }
  }

  if (phase === 'complete') {
    const score = Math.round((correctCount / totalItems) * 100)
    return <SessionCompleteCard totalItems={totalItems} correctCount={correctCount} score={score} />
  }

  if (phase === 'setup') {
    return (
      <div className="max-w-md mx-auto space-y-6">
        <h2 className="text-2xl font-bold text-[var(--color-text-primary)]">Start Study Session</h2>

        {itemsLoading ? (
          <div className="flex justify-center py-10">
            <Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" />
          </div>
        ) : totalItems === 0 ? (
          <Card className="p-6 border-[var(--color-border)] bg-[var(--color-surface)] text-center">
            <p className="text-[var(--color-text-secondary)]">No items due for review. Come back later!</p>
          </Card>
        ) : (
          <Card className="p-6 border-[var(--color-border)] bg-[var(--color-surface)] space-y-4">
            <p className="text-sm text-[var(--color-text-secondary)]">
              You have <span className="font-semibold text-[var(--color-primary)]">{totalItems}</span> items ready to study.
            </p>
            <Button onClick={handleStart} disabled={starting} className="w-full bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              {starting ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
              Start Session
            </Button>
          </Card>
        )}
      </div>
    )
  }

  const progress = ((currentIndex) / totalItems) * 100
  const currentItem = items[currentIndex]

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-[var(--color-text-primary)]">Study Session</h2>
        <span className="text-sm text-[var(--color-text-secondary)]">{currentIndex + 1} / {totalItems}</span>
      </div>

      <Progress value={progress} className="h-2" />

      {phase === 'answering' && currentItem && (
        <Card className="p-6 border-[var(--color-border)] bg-[var(--color-surface)] space-y-4">
          <Label className="text-sm font-medium text-[var(--color-text-secondary)]">
            Item: {currentItem.itemId.slice(0, 8)}… (Interval: {currentItem.intervalDays}d)
          </Label>
          <div className="flex gap-2">
            <Input
              value={answer}
              onChange={e => setAnswer(e.target.value)}
              placeholder="Type your answer..."
              className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]"
              onKeyDown={e => e.key === 'Enter' && answer.trim() && handleSubmitAnswer()}
              disabled={submitting}
            />
            <AudioRecorder
              onRecordingComplete={handleAudioAnswer}
              disabled={submitting}
            />
            <Button onClick={handleSubmitAnswer} disabled={submitting || !answer.trim()} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              {submittingText ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            </Button>
          </div>
        </Card>
      )}

      {phase === 'feedback' && feedbackResult && (
        <AnswerFeedbackCard result={feedbackResult} onNext={handleNext} />
      )}
    </div>
  )
}
