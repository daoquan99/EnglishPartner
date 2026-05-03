'use client'

import { useState, useRef, useCallback, useEffect } from 'react'
import Link from 'next/link'
import { ArrowLeft, ArrowRight, Loader2, Eye, EyeOff, Volume2, MessageCircle } from 'lucide-react'
import { Badge } from '@/ui/badge'
import { Button } from '@/ui/button'
import { Progress } from '@/ui/progress'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Label } from '@/ui/label'
import { useConversation } from '../api/useCurriculum'
import { useStartSession, useSubmitAnswer, useCompleteSession, useEvaluateConversationLine } from '@/features/learning/api/useLearning'
import { useSessionPing } from '@/features/learning/hooks/useSessionPing'
import { useTts, useTtsVoices } from '@/features/ai/api/useAi'
import { apiClient } from '@/lib/axios'
import { ConversationLineCard } from './ConversationLineCard'
import { ConversationComplete } from './ConversationComplete'
import type { EvaluateConversationLineResponse } from '@/features/learning/types/learning.types'
import type { SttResponse } from '@/features/ai/types/ai.types'
import type { ConversationLineResponse } from '../types/curriculum.types'
import { useAuthStore } from '@/lib/zustand/auth.store'

interface ConversationReaderProps {
  conversationId: string
}

type Phase = 'loading' | 'intro' | 'playing' | 'complete'

export function ConversationReader({ conversationId }: ConversationReaderProps) {
  const { data: conversation, isLoading } = useConversation(conversationId)
  const { mutateAsync: startSession, isPending: starting } = useStartSession()
  const { mutateAsync: submitAnswer } = useSubmitAnswer()
  const { mutateAsync: completeSession } = useCompleteSession()
  const { mutateAsync: evaluateLine, isPending: evaluating } = useEvaluateConversationLine()
  const { mutateAsync: tts, isPending: loadingTts } = useTts()

  const { data: voices } = useTtsVoices()

  const [phase, setPhase] = useState<Phase>('intro')
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [currentLineIndex, setCurrentLineIndex] = useState(0)
  const [showTranslations, setShowTranslations] = useState(false)
  const [evaluations, setEvaluations] = useState<Record<string, EvaluateConversationLineResponse>>({})
  const [sttResults, setSttResults] = useState<Record<string, SttResponse>>({})
  const [lineScores, setLineScores] = useState<number[]>([])
  const [avgScore, setAvgScore] = useState(0)
  const [isTranscribing, setIsTranscribing] = useState(false)
  const accessToken = useAuthStore(state => state.accessToken)

  // AI Configuration
  const [selectedVoice, setSelectedVoice] = useState<string>('en-US-Neural2-C')
  const [speakingRate, setSpeakingRate] = useState<number>(1.0)

  const ttsCacheRef = useRef<Map<string, string>>(new Map())
  const timerRef = useRef(Date.now())
  const wsRef = useRef<WebSocket | null>(null)
  const chatBottomRef = useRef<HTMLDivElement>(null)

  useSessionPing(sessionId)

  useEffect(() => {
    // Scroll to bottom when new line appears
    if (chatBottomRef.current) {
      chatBottomRef.current.scrollIntoView({ behavior: 'smooth' })
    }
  }, [currentLineIndex, sttResults])

  // ── Derived state ──────────────────────────────────────────────────────────

  const lines = conversation?.lines?.sort((a, b) => a.sortOrder - b.sortOrder) ?? []
  const aiSpeaker = lines[0]?.speaker ?? ''
  const userSpeaker = lines.find(l => l.speaker !== aiSpeaker)?.speaker ?? 'You'
  const isAiTurn = (line: ConversationLineResponse) => line.speaker === aiSpeaker
  const currentLine = lines[currentLineIndex]
  const progressPercent = lines.length > 0 ? ((currentLineIndex + 1) / lines.length) * 100 : 0
  const userLineCount = lines.filter(l => !isAiTurn(l)).length

  // ── Handlers & Effects ─────────────────────────────────────────────────────

  const playLineAudio = useCallback(async (line: ConversationLineResponse) => {
    if (line.audioUrl) {
      new Audio(line.audioUrl).play()
      return
    }
    const cached = ttsCacheRef.current.get(line.id)
    if (cached) {
      new Audio(cached).play()
      return
    }
    try {
      const blob = await tts({ text: line.text, voice: selectedVoice, speakingRate })
      const url = URL.createObjectURL(blob)
      ttsCacheRef.current.set(line.id, url)
      new Audio(url).play()
    } catch {
      // Silently fail
    }
  }, [tts, selectedVoice, speakingRate])

  // Auto-play AI turn when advancing
  useEffect(() => {
    if (phase === 'playing' && currentLine && isAiTurn(currentLine)) {
      playLineAudio(currentLine)
    }
  }, [phase, currentLineIndex, currentLine]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleStart = async () => {
    const id = await startSession({ sessionTitle: `Conversation: ${conversation?.title ?? ''}`, totalItems: 1 })
    setSessionId(id)
    setPhase('playing')
    timerRef.current = Date.now()
  }

  const handleRecordingStart = () => {
    if (!currentLine) return
    setIsTranscribing(true)
    
    // Clear old result for this line
    setSttResults(prev => {
      const next = { ...prev }
      delete next[currentLine.id]
      return next
    })

    const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? window.location.origin
    const wsUrl = baseUrl.replace(/^http/, 'ws')
    const ws = new WebSocket(`${wsUrl}/api/v1/ai/stt/stream?languages=en-US&mimeType=audio/webm&access_token=${accessToken}`)
    
    ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data)
        if (data.error) {
          console.error('STT WebSocket Error:', data.error)
          return
        }
        setSttResults(prev => {
          const existing = prev[currentLine.id]
          const acc = existing?.accumulated || ''
          if (data.isFinal) {
            return {
              ...prev,
              [currentLine.id]: {
                ...data,
                accumulated: acc + data.transcript,
                transcript: ''
              }
            }
          } else {
            return {
              ...prev,
              [currentLine.id]: {
                ...data,
                accumulated: acc
              }
            }
          }
        })
      } catch (e) {
        console.error('Failed to parse STT message', e)
      }
    }
    
    ws.onclose = async (event) => {
      // 1008 (Policy Violation) typically happens if auth is missing/invalid
      if (event.code === 1008 || event.code === 1011 || event.code === 1006) {
        try {
          // Trigger the global axios interceptor to refresh the token
          await apiClient.get('/api/v1/profile/me')
        } catch {}
      }
    }
    
    wsRef.current = ws
  }

  const handleRecordingData = (blob: Blob) => {
    if (wsRef.current?.readyState === WebSocket.OPEN) {
      wsRef.current.send(blob)
    }
  }

  const handleRecord = async (blob: Blob) => {
    if (!currentLine) return

    if (wsRef.current) {
      wsRef.current.close()
      wsRef.current = null
    }

    try {
      const evalRes = await evaluateLine({ conversationId, lineId: currentLine.id, audioFile: blob })
      setEvaluations(prev => ({ ...prev, [currentLine.id]: evalRes }))
      setLineScores(prev => [...prev, evalRes.score])
    } catch (error) {
      console.error('Failed to process audio:', error)
    } finally {
      setIsTranscribing(false)
    }
  }

  const handleNext = () => {
    const nextIndex = currentLineIndex + 1
    if (nextIndex >= lines.length) {
      handleFinish()
    } else {
      setCurrentLineIndex(nextIndex)
    }
  }

  const handleFinish = async () => {
    if (!sessionId) return
    const scores = lineScores
    const avg = scores.length > 0 ? Math.round(scores.reduce((a, b) => a + b, 0) / scores.length) : 0
    setAvgScore(avg)

    const totalTime = Math.round((Date.now() - timerRef.current) / 1000)
    await submitAnswer({
      sessionId,
      itemId: conversationId,
      itemType: 4,
      userAnswer: String(avg),
      timeSpentSeconds: totalTime,
    })
    await completeSession({ sessionId, score: avg })
    setPhase('complete')
  }

  const canAdvance = currentLine
    ? isAiTurn(currentLine) || !!evaluations[currentLine.id]
    : false

  // ── Render: Loading ────────────────────────────────────────────────────────

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!conversation) {
    return <div className="text-center py-20 text-[var(--color-text-muted)]">Conversation not found.</div>
  }

  if (phase === 'complete') {
    return <ConversationComplete avgScore={avgScore} lineScores={lineScores} />
  }

  // ── Render: Intro ──────────────────────────────────────────────────────────

  if (phase === 'intro') {
    return (
      <div className="max-w-2xl mx-auto space-y-6">
        <Button asChild variant="ghost" size="sm" className="text-[var(--color-text-secondary)]">
          <Link href="/conversations"><ArrowLeft className="h-4 w-4 mr-1" /> Back</Link>
        </Button>

        <div className="rounded-2xl overflow-hidden border border-[var(--color-border)] bg-[var(--color-surface)] shadow-lg">
          {/* Hero gradient */}
          <div className="relative h-32 bg-gradient-to-br from-violet-600 via-indigo-600 to-blue-700 flex items-end p-6">
            <div className="absolute inset-0 bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PGNpcmNsZSBjeD0iMSIgY3k9IjEiIHI9IjEiIGZpbGw9InJnYmEoMjU1LDI1NSwyNTUsMC4wNSkiLz48L3N2Zz4=')] opacity-50" />
            <div className="relative z-10">
              <h1 className="text-2xl font-bold text-white drop-shadow-sm">{conversation.title}</h1>
              {conversation.level && (
                <Badge className="mt-1.5 bg-white/20 text-white border-0 backdrop-blur-sm text-xs">{conversation.level}</Badge>
              )}
            </div>
          </div>

          <div className="p-6 space-y-5">
            {conversation.scenario && (
              <p className="text-sm text-[var(--color-text-secondary)] leading-relaxed">{conversation.scenario}</p>
            )}

            {/* Role cards */}
            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-xl p-3 bg-gradient-to-br from-violet-500/10 to-indigo-500/10 border border-violet-500/20 flex flex-col justify-between">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="flex h-7 w-7 items-center justify-center rounded-full bg-gradient-to-br from-violet-500 to-indigo-600 text-white text-xs shadow-sm">🤖</span>
                    <span className="text-sm font-semibold text-[var(--color-text-primary)]">{aiSpeaker}</span>
                  </div>
                  <p className="text-xs text-[var(--color-text-muted)] mb-3">AI will play this role</p>
                </div>
                
                {/* Voice Configuration */}
                <div className="space-y-3 pt-3 border-t border-violet-500/10">
                  <div className="space-y-1.5">
                    <Label className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider font-semibold">AI Voice</Label>
                    <Select value={selectedVoice} onValueChange={setSelectedVoice}>
                      <SelectTrigger className="h-8 text-xs bg-white dark:bg-[var(--color-surface)] border-violet-500/20">
                        <SelectValue placeholder="Select voice" />
                      </SelectTrigger>
                      <SelectContent>
                        {voices?.map(v => (
                          <SelectItem key={v.id} value={v.id} className="text-xs">
                            {v.displayName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  
                  <div className="space-y-1.5">
                    <Label className="text-[10px] text-[var(--color-text-muted)] uppercase tracking-wider font-semibold">Speaking Rate: {speakingRate}x</Label>
                    <Select value={speakingRate.toString()} onValueChange={(val) => setSpeakingRate(parseFloat(val))}>
                      <SelectTrigger className="h-8 text-xs bg-white dark:bg-[var(--color-surface)] border-violet-500/20">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {Array.from({ length: 17 }, (_, i) => 0.4 + i * 0.1).map(rate => {
                          const val = rate.toFixed(1)
                          return (
                            <SelectItem key={val} value={val} className="text-xs">
                              {val}x
                            </SelectItem>
                          )
                        })}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>
              <div className="rounded-xl p-3 bg-gradient-to-br from-emerald-500/10 to-teal-500/10 border border-emerald-500/20">
                <div className="flex items-center gap-2 mb-1">
                  <span className="flex h-7 w-7 items-center justify-center rounded-full bg-gradient-to-br from-emerald-500 to-teal-600 text-white text-xs shadow-sm">👤</span>
                  <span className="text-sm font-semibold text-[var(--color-text-primary)]">{userSpeaker}</span>
                </div>
                <p className="text-xs text-[var(--color-text-muted)]">You will speak this part</p>
              </div>
            </div>

            <div className="flex items-center justify-between text-xs text-[var(--color-text-muted)] px-1">
              <span><MessageCircle className="inline h-3.5 w-3.5 mr-1" />{lines.length} lines</span>
              <span>🎤 {userLineCount} speaking turns</span>
            </div>

            <Button
              onClick={handleStart}
              disabled={starting}
              className="w-full h-12 bg-gradient-to-r from-violet-600 to-indigo-600 hover:from-violet-700 hover:to-indigo-700 text-white font-semibold shadow-lg shadow-violet-500/25 transition-all"
            >
              {starting ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Volume2 className="h-4 w-4 mr-2" />}
              Start Role-play
            </Button>
          </div>
        </div>
      </div>
    )
  }

  // ── Render: Playing ────────────────────────────────────────────────────────

  return (
    <div className="max-w-2xl mx-auto space-y-5">
      {/* Header bar */}
      <div className="flex items-center justify-between bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-4 py-2.5 shadow-sm">
        <div className="flex items-center gap-2 min-w-0">
          <div className="h-8 w-8 rounded-full bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center text-white text-xs shrink-0">🎭</div>
          <h2 className="text-sm font-semibold text-[var(--color-text-primary)] truncate">{conversation.title}</h2>
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowTranslations(!showTranslations)}
            className="h-7 w-7 p-0 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]"
          >
            {showTranslations ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
          </Button>
          <span className="text-xs font-mono text-[var(--color-text-muted)] bg-[var(--color-surface-2)] px-2 py-0.5 rounded-full">
            {currentLineIndex + 1}/{lines.length}
          </span>
        </div>
      </div>

      <Progress value={progressPercent} className="h-1" />

      {/* Chat Window */}
      <div className="bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-2xl p-4 sm:p-6 overflow-y-auto max-h-[65vh] flex flex-col gap-5 shadow-inner">
        {/* Previous lines */}
        {lines.slice(0, currentLineIndex).map(line => (
          <ConversationLineCard
            key={line.id}
            speaker={line.speaker}
            text={line.text}
            translation={line.translations?.[0]?.translatedText}
            audioUrl={line.audioUrl}
            isAiTurn={isAiTurn(line)}
            evaluation={evaluations[line.id]}
            sttResult={sttResults[line.id]}
            showTranslation={showTranslations}
            isActive={false}
          />
        ))}

        {/* Current line */}
        {currentLine && (
          <div key={currentLine.id} className="animate-in fade-in slide-in-from-bottom-4 duration-300">
            <ConversationLineCard
              speaker={currentLine.speaker}
              text={currentLine.text}
              translation={currentLine.translations?.[0]?.translatedText}
              audioUrl={currentLine.audioUrl}
              isAiTurn={isAiTurn(currentLine)}
              evaluation={evaluations[currentLine.id]}
              sttResult={sttResults[currentLine.id]}
              onRecord={!isAiTurn(currentLine) ? handleRecord : undefined}
              onRecordingStart={!isAiTurn(currentLine) ? handleRecordingStart : undefined}
              onDataAvailable={!isAiTurn(currentLine) ? handleRecordingData : undefined}
              onPlaySample={() => playLineAudio(currentLine)}
              isEvaluating={evaluating}
              isTranscribing={isTranscribing}
              isLoadingTts={loadingTts}
              showTranslation={showTranslations}
              isActive={true}
            />
          </div>
        )}

        <div ref={chatBottomRef} className="h-1" />
      </div>

      {/* Navigation */}
      <div className="flex items-center justify-between">
        <div className="text-xs text-[var(--color-text-muted)]">
          {!isAiTurn(currentLine) && !evaluations[currentLine?.id]
            ? '🎤 Record your answer to continue'
            : canAdvance ? '' : ''
          }
        </div>
        <Button
          onClick={handleNext}
          disabled={!canAdvance}
          className="bg-gradient-to-r from-[var(--color-primary)] to-[color-mix(in_srgb,var(--color-primary),#000_10%)] hover:shadow-lg text-[var(--color-btn-text)] transition-all"
        >
          {currentLineIndex >= lines.length - 1 ? '🏁 Finish' : 'Next'}
          <ArrowRight className="ml-1.5 h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}
