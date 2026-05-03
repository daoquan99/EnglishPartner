'use client'

import { useState, useRef, useEffect, useCallback } from 'react'
import { Mic, Square } from 'lucide-react'
import { Button } from '@/ui/button'

interface AudioRecorderProps {
  onRecordingComplete: (audioBlob: Blob) => void
  onRecordingStart?: () => void
  onDataAvailable?: (data: Blob) => void
  disabled?: boolean
  className?: string
  size?: 'sm' | 'md' | 'lg'
  /** Auto-stop recording after N seconds. Default: 30 */
  maxDuration?: number
}

const sizeMap = { sm: 'w-8 h-8', md: 'w-10 h-10', lg: 'w-12 h-12' }
const iconMap = { sm: 'h-3.5 w-3.5', md: 'h-5 w-5', lg: 'h-6 w-6' }

export function AudioRecorder({
  onRecordingComplete,
  onRecordingStart,
  onDataAvailable,
  disabled,
  className,
  size = 'md',
  maxDuration = 30
}: AudioRecorderProps) {
  const [isRecording, setIsRecording] = useState(false)
  const [seconds, setSeconds] = useState(0)
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  const chunksRef = useRef<Blob[]>([])
  const streamRef = useRef<MediaStream | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const silenceTimerRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const audioContextRef = useRef<AudioContext | null>(null)

  const stopRecording = useCallback(() => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
      mediaRecorderRef.current.stop()
      setIsRecording(false)
      if (timerRef.current) {
        clearInterval(timerRef.current)
        timerRef.current = null
      }
      if (silenceTimerRef.current) {
        clearInterval(silenceTimerRef.current)
        silenceTimerRef.current = null
      }
      if (audioContextRef.current && audioContextRef.current.state !== 'closed') {
        audioContextRef.current.close().catch(() => {})
        audioContextRef.current = null
      }
    }
  }, [])

  // Auto-stop when hitting max duration
  useEffect(() => {
    if (isRecording && seconds >= maxDuration) {
      stopRecording()
    }
  }, [isRecording, seconds, maxDuration, stopRecording])

  useEffect(() => {
    return () => {
      if (streamRef.current) {
        streamRef.current.getTracks().forEach(t => t.stop())
      }
      if (timerRef.current) clearInterval(timerRef.current)
      if (silenceTimerRef.current) clearInterval(silenceTimerRef.current)
      if (audioContextRef.current && audioContextRef.current.state !== 'closed') {
        audioContextRef.current.close().catch(() => {})
      }
    }
  }, [])

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      streamRef.current = stream

      const mediaRecorder = new MediaRecorder(stream)
      mediaRecorderRef.current = mediaRecorder
      chunksRef.current = []

      mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) {
          chunksRef.current.push(e.data)
          if (onDataAvailable) onDataAvailable(e.data)
        }
      }

      mediaRecorder.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: 'audio/webm' })
        onRecordingComplete(blob)
        if (streamRef.current) {
          streamRef.current.getTracks().forEach(t => t.stop())
          streamRef.current = null
        }
      }

      if (onDataAvailable) {
        mediaRecorder.start(250)
      } else {
        mediaRecorder.start()
      }

      // Setup silence detection (5s)
      const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext
      if (AudioContextClass) {
        const audioCtx = new AudioContextClass()
        audioContextRef.current = audioCtx
        const analyser = audioCtx.createAnalyser()
        analyser.fftSize = 256
        const source = audioCtx.createMediaStreamSource(stream)
        source.connect(analyser)

        const bufferLength = analyser.frequencyBinCount
        const dataArray = new Uint8Array(bufferLength)
        let silentChunks = 0 // 500ms per check -> 10 chunks = 5 seconds

        silenceTimerRef.current = setInterval(() => {
          analyser.getByteFrequencyData(dataArray)
          const sum = dataArray.reduce((a, b) => a + b, 0)
          const avg = sum / bufferLength

          if (avg < 5) {
            silentChunks++
            if (silentChunks >= 10) {
              stopRecording()
            }
          } else {
            silentChunks = 0
          }
        }, 500)
      }

      if (onRecordingStart) onRecordingStart()
      setIsRecording(true)
      setSeconds(0)
      timerRef.current = setInterval(() => setSeconds(s => s + 1), 1000)
    } catch {
      alert('Could not access microphone. Please ensure permissions are granted.')
    }
  }

  const formatTime = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
  const remaining = maxDuration - seconds

  return (
    <div className="inline-flex items-center gap-2">
      <Button
        type="button"
        variant={isRecording ? 'destructive' : 'default'}
        onClick={isRecording ? stopRecording : startRecording}
        disabled={disabled}
        className={`rounded-full p-0 shrink-0 ${sizeMap[size]} ${isRecording ? 'animate-pulse shadow-[0_0_12px_rgba(239,68,68,0.5)]' : ''} ${className ?? ''}`}
        title={isRecording ? 'Stop recording' : 'Start recording'}
      >
        {isRecording
          ? <Square className={`${iconMap[size]} fill-current`} />
          : <Mic className={iconMap[size]} />
        }
      </Button>
      {isRecording && (
        <div className="flex items-center gap-1.5">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500" />
          </span>
          <span className={`text-xs font-mono tabular-nums min-w-[2.5rem] ${remaining <= 5 ? 'text-red-500 font-bold' : 'text-[var(--color-text-secondary)]'}`}>
            {formatTime(seconds)}
          </span>
        </div>
      )}
    </div>
  )
}
