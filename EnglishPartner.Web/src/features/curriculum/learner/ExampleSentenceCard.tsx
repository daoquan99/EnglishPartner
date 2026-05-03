'use client'

import { useState, useRef } from 'react'
import { Volume2, Languages, Mic, Loader2 } from 'lucide-react'
import { ExampleSentenceResponse } from '../types/curriculum.types'
import { useTts } from '@/features/ai/api/useAi'
import { useAuthStore } from '@/lib/zustand/auth.store'

const LEVEL_STYLES: Record<string, string> = {
  A1: 'from-emerald-400 to-green-500',
  A2: 'from-teal-400 to-cyan-500',
  B1: 'from-blue-400 to-indigo-500',
  B2: 'from-violet-400 to-purple-500',
  C1: 'from-fuchsia-400 to-pink-500',
  C2: 'from-rose-400 to-red-500',
}

interface ExampleSentenceCardProps {
  sentence: ExampleSentenceResponse
  onPractice?: (sentence: ExampleSentenceResponse) => void
}

export function ExampleSentenceCard({ sentence, onPractice }: ExampleSentenceCardProps) {
  const { user } = useAuthStore()
  const [isPlaying, setIsPlaying] = useState(false)
  const [isTtsLoading, setIsTtsLoading] = useState(false)
  const audioRef = useRef<HTMLAudioElement | null>(null)
  const { mutateAsync: generateTts } = useTts()

  const levelGradient = LEVEL_STYLES[sentence.level || ''] || 'from-gray-400 to-gray-500'
  const translation = sentence.translations?.find(t => t.languageCode === user?.nativeLanguage) || sentence.translations?.[0]

  const playAudio = async (e: React.MouseEvent) => {
    e.stopPropagation()
    
    if (sentence.audioUrl) {
      if (audioRef.current) { audioRef.current.pause(); audioRef.current = null }
      const audio = new Audio(sentence.audioUrl)
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
      const blob = await generateTts({ text: sentence.text })
      const url = URL.createObjectURL(blob)
      if (audioRef.current) { audioRef.current.pause(); audioRef.current = null }
      const audio = new Audio(url)
      audioRef.current = audio
      setIsPlaying(true)
      audio.onended = () => { setIsPlaying(false); URL.revokeObjectURL(url) }
      audio.onerror = () => { setIsPlaying(false); URL.revokeObjectURL(url) }
      audio.play()
    } catch {
      console.error("Failed to generate TTS")
    } finally {
      setIsTtsLoading(false)
    }
  }

  return (
    <div className="group relative rounded-2xl border border-black/5 dark:border-white/10 bg-white/50 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px_rgba(99,102,241,0.15)] dark:hover:shadow-[0_20px_40px_-15px_rgba(99,102,241,0.1)] hover:-translate-y-1.5 hover:border-indigo-500/30 h-full">
      {/* Left accent bar */}
      <div className={`absolute top-0 left-0 w-1 h-full bg-gradient-to-b ${levelGradient} opacity-0 group-hover:opacity-100 transition-all duration-500 scale-y-0 group-hover:scale-y-100 origin-center`} />

      <div className="p-5 pl-6 space-y-3">
        {/* Sentence text + level */}
        <div className="flex items-start justify-between gap-3">
          <p className="text-sm font-medium text-[var(--color-text-primary)] leading-relaxed flex-1">
            &ldquo;{sentence.text}&rdquo;
          </p>
          <div className="flex flex-col items-end gap-1.5 shrink-0">
            {sentence.level && (
              <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md bg-gradient-to-r ${levelGradient} text-white shadow-sm`}>
                {sentence.level}
              </span>
            )}
          </div>
        </div>

        {/* Translation */}
        {translation && (
          <div className="flex items-start gap-2">
            <Languages className="h-3.5 w-3.5 text-[var(--color-text-muted)] mt-0.5 shrink-0" />
            <p className="text-xs text-[var(--color-text-muted)] leading-relaxed italic">
              {translation.translatedText}
            </p>
          </div>
        )}

        {/* Note */}
        {sentence.note && (
          <p className="text-xs text-[var(--color-text-muted)] bg-[var(--color-surface-2)] rounded-lg px-3 py-1.5">
            💡 {sentence.note}
          </p>
        )}

        {/* Actions */}
        <div className="flex items-center gap-2 pt-1">
          <button
            onClick={playAudio}
            disabled={isTtsLoading}
            className={`h-8 w-8 rounded-lg flex items-center justify-center transition-all ${
              isPlaying
                ? 'bg-indigo-500 text-white shadow-lg shadow-indigo-500/25 scale-110'
                : 'bg-[var(--color-surface-2)] text-[var(--color-text-muted)] hover:bg-indigo-500/10 hover:text-indigo-500 disabled:opacity-50'
            }`}
            title="Play audio"
          >
            {isTtsLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Volume2 className="h-4 w-4" />}
          </button>
          {onPractice && (
            <button
              onClick={(e) => { e.stopPropagation(); onPractice(sentence) }}
              className="h-8 px-3 rounded-lg flex items-center gap-1.5 bg-[var(--color-surface-2)] text-[var(--color-text-muted)] hover:bg-emerald-500/10 hover:text-emerald-500 transition-all text-xs font-medium"
              title="Practice pronunciation"
            >
              <Mic className="h-3.5 w-3.5" />
              Practice
            </button>
          )}
        </div>
      </div>

      {/* Hover shine */}
      <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
    </div>
  )
}
