'use client'

import { useState, useRef, useCallback } from 'react'
import { Volume2, Mic, Square, X, RotateCcw, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/ui/dialog'
import { useEvaluatePronunciation } from '@/features/learning/api/useLearning'
import { useTts } from '@/features/ai/api/useAi'
import type { PronunciationEvaluationResponse } from '@/features/learning/types/learning.types'

interface PronunciationPracticeProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** The text user should read aloud */
  expectedText: string
  /** Optional audio URL for reference playback */
  audioUrl?: string | null
  /** BCP-47 language code, default "en-US" */
  languageCode?: string
  /** Optional subtitle — e.g. translation */
  subtitle?: string | null
}

const SCORE_CONFIG = [
  { min: 80, label: 'Excellent!', color: 'text-emerald-500', bg: 'bg-emerald-500', ring: 'ring-emerald-500/30', gradient: 'from-emerald-500 to-green-400' },
  { min: 60, label: 'Good job!', color: 'text-blue-500', bg: 'bg-blue-500', ring: 'ring-blue-500/30', gradient: 'from-blue-500 to-indigo-400' },
  { min: 40, label: 'Keep trying!', color: 'text-amber-500', bg: 'bg-amber-500', ring: 'ring-amber-500/30', gradient: 'from-amber-500 to-yellow-400' },
  { min: 0, label: 'Try again', color: 'text-red-500', bg: 'bg-red-500', ring: 'ring-red-500/30', gradient: 'from-red-500 to-rose-400' },
] as const

function getScoreConfig(score: number) {
  return SCORE_CONFIG.find(c => score >= c.min) ?? SCORE_CONFIG[3]
}

export function PronunciationPractice({
  open, onOpenChange, expectedText, audioUrl, languageCode = 'en-US', subtitle,
}: PronunciationPracticeProps) {
  const [isRecording, setIsRecording] = useState(false)
  const [seconds, setSeconds] = useState(0)
  const [result, setResult] = useState<PronunciationEvaluationResponse | null>(null)
  const [isPlayingRef_, setIsPlaying] = useState(false)
  const [isTtsLoading, setIsTtsLoading] = useState(false)

  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  const chunksRef = useRef<Blob[]>([])
  const streamRef = useRef<MediaStream | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const audioRef = useRef<HTMLAudioElement | null>(null)

  const { mutate: evaluate, isPending } = useEvaluatePronunciation()
  const { mutateAsync: generateTts } = useTts()

  const cleanup = useCallback(() => {
    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null }
    if (streamRef.current) { streamRef.current.getTracks().forEach(t => t.stop()); streamRef.current = null }
  }, [])

  const startRecording = async () => {
    try {
      setResult(null)
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      streamRef.current = stream

      const recorder = new MediaRecorder(stream)
      mediaRecorderRef.current = recorder
      chunksRef.current = []

      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data)
      }

      recorder.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: 'audio/webm' })
        cleanup()
        evaluate(
          { expectedText, audioFile: blob, languageCode },
          { onSuccess: (data) => setResult(data) }
        )
      }

      recorder.start()
      setIsRecording(true)
      setSeconds(0)
      timerRef.current = setInterval(() => setSeconds(s => s + 1), 1000)
    } catch {
      alert('Could not access microphone. Please ensure permissions are granted.')
    }
  }

  const stopRecording = () => {
    if (mediaRecorderRef.current?.state === 'recording') {
      mediaRecorderRef.current.stop()
    }
    setIsRecording(false)
    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null }
  }

  // Auto-stop at 15 seconds
  if (isRecording && seconds >= 15) stopRecording()

  const playReference = async () => {
    if (audioUrl) {
      if (audioRef.current) { audioRef.current.pause(); audioRef.current = null }
      const audio = new Audio(audioUrl)
      audioRef.current = audio
      setIsPlaying(true)
      audio.onended = () => setIsPlaying(false)
      audio.onerror = () => setIsPlaying(false)
      audio.play()
      return
    }

    if (isTtsLoading) return
    setIsTtsLoading(true)
    try {
      const blob = await generateTts({ text: expectedText })
      const url = URL.createObjectURL(blob)
      if (audioRef.current) { audioRef.current.pause(); audioRef.current = null }
      const audio = new Audio(url)
      audioRef.current = audio
      setIsPlaying(true)
      audio.onended = () => { setIsPlaying(false); URL.revokeObjectURL(url) }
      audio.onerror = () => { setIsPlaying(false); URL.revokeObjectURL(url) }
      audio.play()
    } catch {
      alert("Failed to generate TTS")
    } finally {
      setIsTtsLoading(false)
    }
  }

  const retry = () => {
    setResult(null)
    setSeconds(0)
  }

  const handleOpenChange = (v: boolean) => {
    if (!v) { cleanup(); setResult(null); setSeconds(0); setIsRecording(false) }
    onOpenChange(v)
  }

  const scoreConfig = result ? getScoreConfig(result.score) : null
  const formatTime = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
        <DialogHeader>
          <DialogTitle className="text-lg font-bold flex items-center gap-2">
            <Mic className="h-5 w-5 text-emerald-500" />
            Pronunciation Practice
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-5 py-2">
          {/* Expected text */}
          <div className="rounded-2xl bg-[var(--color-surface-2)] p-5 space-y-2">
            <p className="text-base font-medium text-[var(--color-text-primary)] leading-relaxed text-center">
              &ldquo;{expectedText}&rdquo;
            </p>
            {subtitle && (
              <p className="text-xs text-[var(--color-text-muted)] text-center italic">
                {subtitle}
              </p>
            )}
            <div className="flex justify-center pt-1">
              <button
                onClick={playReference}
                disabled={isTtsLoading}
                className={`h-9 px-4 rounded-xl flex items-center gap-2 text-xs font-medium transition-all ${
                  isPlayingRef_
                    ? 'bg-indigo-500 text-white shadow-lg shadow-indigo-500/25'
                    : 'bg-[var(--color-surface)] text-[var(--color-text-secondary)] hover:bg-indigo-500/10 hover:text-indigo-500 border border-[var(--color-border)] disabled:opacity-50'
                }`}
              >
                {isTtsLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Volume2 className="h-4 w-4" />}
                Listen
              </button>
            </div>
          </div>

          {/* Recording / Result area */}
          {!result && !isPending && (
            <div className="flex flex-col items-center gap-4">
              <button
                onClick={isRecording ? stopRecording : startRecording}
                className={`h-20 w-20 rounded-full flex items-center justify-center transition-all duration-300 ${
                  isRecording
                    ? 'bg-red-500 text-white shadow-[0_0_30px_rgba(239,68,68,0.4)] animate-pulse scale-110'
                    : 'bg-gradient-to-br from-emerald-500 to-teal-500 text-white shadow-lg shadow-emerald-500/25 hover:shadow-emerald-500/40 hover:scale-105'
                }`}
              >
                {isRecording
                  ? <Square className="h-8 w-8 fill-current" />
                  : <Mic className="h-8 w-8" />
                }
              </button>

              {isRecording ? (
                <div className="flex items-center gap-2">
                  <span className="relative flex h-2.5 w-2.5">
                    <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75" />
                    <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500" />
                  </span>
                  <span className="text-sm font-mono tabular-nums text-[var(--color-text-secondary)]">
                    {formatTime(seconds)}
                  </span>
                </div>
              ) : (
                <p className="text-sm text-[var(--color-text-muted)]">
                  Tap the microphone and read the text aloud
                </p>
              )}
            </div>
          )}

          {/* Loading */}
          {isPending && (
            <div className="flex flex-col items-center gap-3 py-4">
              <div className="h-12 w-12 rounded-full border-4 border-[var(--color-border)] border-t-emerald-500 animate-spin" />
              <p className="text-sm text-[var(--color-text-muted)]">Analyzing your pronunciation...</p>
            </div>
          )}

          {/* Result */}
          {result && scoreConfig && (
            <div className="space-y-4 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
              {/* Score gauge */}
              <div className="flex flex-col items-center gap-2">
                <div className={`relative h-24 w-24 rounded-full flex items-center justify-center ring-4 ${scoreConfig.ring}`}>
                  <div className={`absolute inset-1 rounded-full bg-gradient-to-br ${scoreConfig.gradient} opacity-10`} />
                  <span className={`text-3xl font-black ${scoreConfig.color}`}>{result.score}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  {result.score >= 60
                    ? <CheckCircle2 className={`h-4 w-4 ${scoreConfig.color}`} />
                    : <AlertCircle className={`h-4 w-4 ${scoreConfig.color}`} />
                  }
                  <span className={`text-sm font-semibold ${scoreConfig.color}`}>{scoreConfig.label}</span>
                </div>
              </div>

              {/* Transcription */}
              <div className="rounded-xl bg-[var(--color-surface-2)] p-4 space-y-2">
                <p className="text-[10px] uppercase tracking-wider font-semibold text-[var(--color-text-muted)]">You said:</p>
                <p className="text-sm text-[var(--color-text-primary)] leading-relaxed">
                  &ldquo;{result.transcription}&rdquo;
                </p>
              </div>

              {/* Feedback */}
              {result.feedback && (
                <div className="rounded-xl bg-[var(--color-surface-2)] p-4 space-y-2">
                  <p className="text-[10px] uppercase tracking-wider font-semibold text-[var(--color-text-muted)]">Feedback:</p>
                  <p className="text-sm text-[var(--color-text-secondary)] leading-relaxed">
                    {result.feedback}
                  </p>
                </div>
              )}

              {/* Actions */}
              <div className="flex gap-3 pt-1">
                <button
                  onClick={retry}
                  className="flex-1 h-11 rounded-xl flex items-center justify-center gap-2 bg-[var(--color-surface-2)] text-[var(--color-text-secondary)] hover:bg-emerald-500/10 hover:text-emerald-500 transition-all text-sm font-medium border border-[var(--color-border)]"
                >
                  <RotateCcw className="h-4 w-4" />
                  Try Again
                </button>
                <button
                  onClick={() => handleOpenChange(false)}
                  className="flex-1 h-11 rounded-xl flex items-center justify-center gap-2 bg-gradient-to-r from-emerald-500 to-teal-500 text-white shadow-lg shadow-emerald-500/25 hover:shadow-emerald-500/40 transition-all text-sm font-medium"
                >
                  <X className="h-4 w-4" />
                  Close
                </button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
