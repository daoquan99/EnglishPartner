'use client'

import Link from 'next/link'
import { MessageSquare, Users, ArrowRight } from 'lucide-react'
import type { ConversationResponse } from '../types/curriculum.types'

const GRADIENT_PRESETS = [
  'from-violet-500/20 to-purple-600/20 dark:from-violet-500/40 dark:to-purple-600/40',
  'from-blue-500/20 to-indigo-600/20 dark:from-blue-500/40 dark:to-indigo-600/40',
  'from-emerald-500/20 to-teal-600/20 dark:from-emerald-500/40 dark:to-teal-600/40',
  'from-rose-500/20 to-pink-600/20 dark:from-rose-500/40 dark:to-pink-600/40',
  'from-amber-500/20 to-orange-600/20 dark:from-amber-500/40 dark:to-orange-600/40',
  'from-cyan-500/20 to-sky-600/20 dark:from-cyan-500/40 dark:to-sky-600/40',
  'from-fuchsia-500/20 to-purple-600/20 dark:from-fuchsia-500/40 dark:to-purple-600/40',
  'from-lime-500/20 to-green-600/20 dark:from-lime-500/40 dark:to-green-600/40',
]

const ACCENT_COLORS = [
  { dot: 'bg-violet-500', text: 'text-violet-500', ring: 'group-hover:ring-violet-500/30' },
  { dot: 'bg-blue-500', text: 'text-blue-500', ring: 'group-hover:ring-blue-500/30' },
  { dot: 'bg-emerald-500', text: 'text-emerald-500', ring: 'group-hover:ring-emerald-500/30' },
  { dot: 'bg-rose-500', text: 'text-rose-500', ring: 'group-hover:ring-rose-500/30' },
  { dot: 'bg-amber-500', text: 'text-amber-500', ring: 'group-hover:ring-amber-500/30' },
  { dot: 'bg-cyan-500', text: 'text-cyan-500', ring: 'group-hover:ring-cyan-500/30' },
  { dot: 'bg-fuchsia-500', text: 'text-fuchsia-500', ring: 'group-hover:ring-fuchsia-500/30' },
  { dot: 'bg-lime-500', text: 'text-lime-500', ring: 'group-hover:ring-lime-500/30' },
]

const LEVEL_STYLES: Record<string, string> = {
  A1: 'from-emerald-400 to-green-500',
  A2: 'from-teal-400 to-cyan-500',
  B1: 'from-blue-400 to-indigo-500',
  B2: 'from-violet-400 to-purple-500',
  C1: 'from-fuchsia-400 to-pink-500',
  C2: 'from-rose-400 to-red-500',
}

interface ConversationCardProps {
  conversation: ConversationResponse
}

export function ConversationCard({ conversation: c }: ConversationCardProps) {
  const hash = c.id.charCodeAt(0) % GRADIENT_PRESETS.length
  const gradient = GRADIENT_PRESETS[hash]
  const accent = ACCENT_COLORS[hash]
  const lineCount = c.lines?.length || 0
  const levelGradient = LEVEL_STYLES[c.level || ''] || 'from-gray-400 to-gray-500'

  // Extract speakers from lines
  const speakers = c.lines
    ? [...new Set(c.lines.map(l => l.speaker))].slice(0, 3)
    : []

  return (
    <Link href={`/conversations/${c.id}`}>
      <div className={`group relative rounded-2xl border border-black/5 dark:border-white/10 bg-white/50 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px_rgba(139,92,246,0.15)] dark:hover:shadow-[0_20px_40px_-15px_rgba(139,92,246,0.1)] hover:-translate-y-1.5 hover:border-purple-500/30 cursor-pointer ring-2 ring-transparent ${accent.ring}`}>
        {/* Top section with gradient background */}
        <div className={`relative h-32 bg-gradient-to-br ${gradient} flex items-center justify-center overflow-hidden`}>
          {c.imageUrl ? (
            <img src={c.imageUrl} alt={c.title} className="absolute inset-0 h-full w-full object-cover transition-transform duration-700 group-hover:scale-105" />
          ) : (
            <>
              {/* Decorative chat bubbles */}
              <div className="absolute top-4 left-6 w-24 h-8 rounded-2xl rounded-bl-none bg-white/10 backdrop-blur-sm" />
              <div className="absolute top-14 right-6 w-28 h-8 rounded-2xl rounded-br-none bg-white/10 backdrop-blur-sm" />
              <div className="absolute bottom-4 left-10 w-20 h-7 rounded-2xl rounded-bl-none bg-white/10 backdrop-blur-sm" />
              <MessageSquare className="relative h-10 w-10 text-[var(--color-text-muted)] opacity-20 stroke-[1.5]" />
            </>
          )}

          {/* Level badge */}
          {c.level && (
            <span className={`absolute top-3 right-3 text-[10px] font-bold uppercase tracking-widest px-2.5 py-1 rounded-full bg-gradient-to-r ${levelGradient} text-white shadow-md`}>
              {c.level}
            </span>
          )}

          {/* Bottom fade */}
          <div className="absolute inset-x-0 bottom-0 h-12 bg-gradient-to-t from-[var(--color-surface)] to-transparent" />
        </div>

        {/* Content */}
        <div className="p-5 pt-3 space-y-3">
          <h3 className={`font-bold text-lg text-[var(--color-text-primary)] group-hover:${accent.text} transition-colors line-clamp-1 tracking-tight leading-tight`}>
            {c.title}
          </h3>

          {c.scenario && (
            <p className="text-xs text-[var(--color-text-muted)] line-clamp-2 leading-relaxed">
              {c.scenario}
            </p>
          )}

          {/* Footer meta */}
          <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border)]">
            <div className="flex items-center gap-3">
              {speakers.length > 0 && (
                <div className="flex items-center gap-1.5">
                  <Users className="h-3.5 w-3.5 text-[var(--color-text-muted)]" />
                  <span className="text-[11px] text-[var(--color-text-muted)] font-medium">
                    {speakers.join(' & ')}
                  </span>
                </div>
              )}
              {lineCount > 0 && (
                <span className="text-[11px] text-[var(--color-text-muted)] font-medium">
                  {lineCount} lines
                </span>
              )}
            </div>
            <ArrowRight className={`h-4 w-4 text-[var(--color-text-muted)] group-hover:${accent.text} group-hover:translate-x-0.5 transition-all`} />
          </div>
        </div>

        {/* Hover shine */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
      </div>
    </Link>
  )
}
