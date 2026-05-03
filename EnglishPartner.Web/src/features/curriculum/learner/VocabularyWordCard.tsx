'use client'

import Link from 'next/link'
import { Volume2 } from 'lucide-react'

interface VocabularyWordCardProps {
  id: string
  word: string
  definition: string
  partOfSpeech?: string | null
  level?: string | null
  pronunciation?: string | null
}

const POS_STYLES: Record<string, { bg: string; text: string }> = {
  noun: { bg: 'bg-blue-500/10', text: 'text-blue-500' },
  verb: { bg: 'bg-emerald-500/10', text: 'text-emerald-500' },
  adjective: { bg: 'bg-amber-500/10', text: 'text-amber-500' },
  adverb: { bg: 'bg-purple-500/10', text: 'text-purple-500' },
  preposition: { bg: 'bg-rose-500/10', text: 'text-rose-500' },
  conjunction: { bg: 'bg-cyan-500/10', text: 'text-cyan-500' },
  pronoun: { bg: 'bg-teal-500/10', text: 'text-teal-500' },
}

const LEVEL_STYLES: Record<string, string> = {
  A1: 'from-emerald-400 to-green-500',
  A2: 'from-teal-400 to-cyan-500',
  B1: 'from-blue-400 to-indigo-500',
  B2: 'from-violet-400 to-purple-500',
  C1: 'from-fuchsia-400 to-pink-500',
  C2: 'from-rose-400 to-red-500',
}

export function VocabularyWordCard({ id, word, definition, partOfSpeech, level, pronunciation }: VocabularyWordCardProps) {
  const posKey = partOfSpeech?.toLowerCase() || ''
  const posStyle = POS_STYLES[posKey] || { bg: 'bg-gray-500/10', text: 'text-[var(--color-text-muted)]' }
  const levelGradient = LEVEL_STYLES[level || ''] || 'from-gray-400 to-gray-500'

  return (
    <Link href={`/vocabulary/${id}`}>
      <div className="group relative rounded-2xl border border-black/5 dark:border-white/10 bg-white/50 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px_rgba(99,102,241,0.15)] dark:hover:shadow-[0_20px_40px_-15px_rgba(99,102,241,0.1)] hover:-translate-y-1.5 hover:border-indigo-500/30 cursor-pointer h-full">
        {/* Left accent bar */}
        <div className={`absolute top-0 left-0 w-1 h-full bg-gradient-to-b ${levelGradient} opacity-0 group-hover:opacity-100 transition-all duration-500 scale-y-0 group-hover:scale-y-100 origin-center`} />

        <div className="p-5 pl-6 space-y-3">
          {/* Word + Level */}
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h3 className="font-bold text-xl text-[var(--color-text-primary)] group-hover:text-transparent group-hover:bg-clip-text group-hover:bg-gradient-to-r group-hover:from-indigo-500 group-hover:to-blue-500 transition-all tracking-tight leading-tight">
                {word}
              </h3>
              {pronunciation && (
                <div className="flex items-center gap-1.5 mt-1">
                  <Volume2 className="h-3 w-3 text-[var(--color-text-muted)]" />
                  <span className="text-xs text-[var(--color-text-muted)] italic font-light">{pronunciation}</span>
                </div>
              )}
            </div>
            <div className="flex flex-col items-end gap-1.5 shrink-0">
              {level && (
                <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md bg-gradient-to-r ${levelGradient} text-white shadow-sm`}>
                  {level}
                </span>
              )}
              {partOfSpeech && (
                <span className={`text-[10px] font-semibold uppercase tracking-wider px-2 py-0.5 rounded-md ${posStyle.bg} ${posStyle.text}`}>
                  {partOfSpeech}
                </span>
              )}
            </div>
          </div>

          {/* Definition */}
          <p className="text-sm text-[var(--color-text-secondary)] line-clamp-2 leading-relaxed">
            {definition}
          </p>
        </div>

        {/* Hover shine */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
      </div>
    </Link>
  )
}
