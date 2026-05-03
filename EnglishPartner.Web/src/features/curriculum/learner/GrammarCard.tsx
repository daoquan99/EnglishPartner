'use client'

import Link from 'next/link'
import { BookText, ChevronRight } from 'lucide-react'

interface GrammarCardProps {
  grammar: {
    id: string
    title: string
    formula?: string | null
    level?: string | null
    topicId?: string | null
  }
}

const LEVEL_STYLES: Record<string, string> = {
  A1: 'from-emerald-400 to-green-500',
  A2: 'from-teal-400 to-cyan-500',
  B1: 'from-blue-400 to-indigo-500',
  B2: 'from-violet-400 to-purple-500',
  C1: 'from-fuchsia-400 to-pink-500',
  C2: 'from-rose-400 to-red-500',
}

export function GrammarCard({ grammar }: GrammarCardProps) {
  const levelGradient = LEVEL_STYLES[grammar.level || ''] || 'from-purple-400 to-fuchsia-500'

  return (
    <Link href={`/grammar/${grammar.id}`}>
      <div className="group relative h-full rounded-2xl border border-black/5 dark:border-white/10 bg-white/60 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px_rgba(168,85,247,0.15)] dark:hover:shadow-[0_20px_40px_-15px_rgba(168,85,247,0.1)] hover:-translate-y-1.5 hover:border-purple-500/30 cursor-pointer">
        {/* Top accent */}
        <div className={`h-1.5 w-full bg-gradient-to-r ${levelGradient}`} />

        <div className="p-5 space-y-3">
          {/* Icon + Level */}
          <div className="flex items-start justify-between gap-3">
            <div className="h-10 w-10 shrink-0 rounded-xl bg-gradient-to-br from-purple-500/15 to-fuchsia-500/15 dark:from-purple-500/25 dark:to-fuchsia-500/25 flex items-center justify-center text-purple-500 group-hover:from-purple-500 group-hover:to-fuchsia-500 group-hover:text-white transition-all duration-500 group-hover:shadow-lg group-hover:scale-110">
              <BookText className="h-5 w-5" />
            </div>
            {grammar.level && (
              <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md bg-gradient-to-r ${levelGradient} text-white shadow-sm`}>
                {grammar.level}
              </span>
            )}
          </div>

          {/* Title */}
          <h3 className="font-bold text-base text-[var(--color-text-primary)] group-hover:text-transparent group-hover:bg-clip-text group-hover:bg-gradient-to-r group-hover:from-purple-500 group-hover:to-fuchsia-500 transition-all line-clamp-2 leading-tight tracking-tight">
            {grammar.title}
          </h3>

          {/* Formula */}
          {grammar.formula && (
            <div className="px-3 py-1.5 rounded-lg bg-purple-500/5 dark:bg-purple-500/10 border border-purple-500/10 dark:border-purple-500/20">
              <p className="text-xs font-mono text-purple-600 dark:text-purple-400 line-clamp-1">
                {grammar.formula}
              </p>
            </div>
          )}

          {/* Footer */}
          <div className="flex items-center justify-end pt-1">
            <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)] group-hover:text-purple-500 group-hover:translate-x-0.5 transition-all" />
          </div>
        </div>

        {/* Hover shine */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
      </div>
    </Link>
  )
}
