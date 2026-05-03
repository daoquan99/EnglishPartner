'use client'

import Link from 'next/link'
import { ArrowRight, Layers } from 'lucide-react'
import type { TopicResponse } from '../types/curriculum.types'

interface TopicCardProps {
  topic: TopicResponse
}

const GRADIENT_PRESETS = [
  'from-emerald-500/20 to-teal-600/20 dark:from-emerald-500/40 dark:to-teal-600/40',
  'from-blue-500/20 to-indigo-600/20 dark:from-blue-500/40 dark:to-indigo-600/40',
  'from-violet-500/20 to-purple-600/20 dark:from-violet-500/40 dark:to-purple-600/40',
  'from-fuchsia-500/20 to-pink-600/20 dark:from-fuchsia-500/40 dark:to-pink-600/40',
  'from-rose-500/20 to-red-600/20 dark:from-rose-500/40 dark:to-red-600/40',
  'from-amber-500/20 to-orange-600/20 dark:from-amber-500/40 dark:to-orange-600/40',
  'from-cyan-500/20 to-sky-600/20 dark:from-cyan-500/40 dark:to-sky-600/40',
  'from-lime-500/20 to-green-600/20 dark:from-lime-500/40 dark:to-green-600/40',
]

const ACCENT_COLORS = [
  { hover: 'hover:border-emerald-500/30', text: 'group-hover:text-emerald-500', shadow: 'hover:shadow-emerald-500/10' },
  { hover: 'hover:border-blue-500/30', text: 'group-hover:text-blue-500', shadow: 'hover:shadow-blue-500/10' },
  { hover: 'hover:border-violet-500/30', text: 'group-hover:text-violet-500', shadow: 'hover:shadow-violet-500/10' },
  { hover: 'hover:border-fuchsia-500/30', text: 'group-hover:text-fuchsia-500', shadow: 'hover:shadow-fuchsia-500/10' },
  { hover: 'hover:border-rose-500/30', text: 'group-hover:text-rose-500', shadow: 'hover:shadow-rose-500/10' },
  { hover: 'hover:border-amber-500/30', text: 'group-hover:text-amber-500', shadow: 'hover:shadow-amber-500/10' },
  { hover: 'hover:border-cyan-500/30', text: 'group-hover:text-cyan-500', shadow: 'hover:shadow-cyan-500/10' },
  { hover: 'hover:border-lime-500/30', text: 'group-hover:text-lime-500', shadow: 'hover:shadow-lime-500/10' },
]

const LEVEL_STYLES: Record<string, string> = {
  a1: 'from-emerald-400 to-green-500',
  beginner: 'from-emerald-400 to-green-500',
  a2: 'from-teal-400 to-cyan-500',
  elementary: 'from-teal-400 to-cyan-500',
  b1: 'from-blue-400 to-indigo-500',
  intermediate: 'from-blue-400 to-indigo-500',
  b2: 'from-violet-400 to-purple-500',
  upper_intermediate: 'from-violet-400 to-purple-500',
  c1: 'from-fuchsia-400 to-pink-500',
  advanced: 'from-fuchsia-400 to-pink-500',
  c2: 'from-rose-400 to-red-500',
  proficient: 'from-rose-400 to-red-500',
}

export function TopicCard({ topic }: TopicCardProps) {
  const hash = topic.id.charCodeAt(0) % GRADIENT_PRESETS.length
  const gradient = GRADIENT_PRESETS[hash]
  const accent = ACCENT_COLORS[hash]
  const levelKey = topic.level?.toLowerCase().replace(/ /g, '_') || ''
  const levelGradient = LEVEL_STYLES[levelKey] || 'from-gray-400 to-gray-500'

  return (
    <Link href={`/curriculum/topics/${topic.id}`}>
      <div className={`group relative rounded-2xl border border-black/5 dark:border-white/10 bg-white/50 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px] ${accent.shadow} hover:-translate-y-1.5 ${accent.hover} cursor-pointer`}>
        {/* Image / Gradient header */}
        <div className={`relative h-36 bg-gradient-to-br ${gradient} dark:opacity-100 overflow-hidden`}>
          {topic.imageUrl ? (
            <img src={topic.imageUrl} alt={topic.title} className="absolute inset-0 h-full w-full object-cover transition-transform duration-700 group-hover:scale-105" />
          ) : (
            <>
              {/* Decorative initial letter */}
              <div className="absolute inset-0 flex items-center justify-center">
                <span className="text-7xl font-black text-[var(--color-text-muted)] opacity-[0.08] select-none transition-transform duration-700 group-hover:scale-110">
                  {topic.title.charAt(0).toUpperCase()}
                </span>
              </div>
              {/* Floating icon */}
              <div className="absolute bottom-3 left-4">
                <div className="h-10 w-10 rounded-xl bg-white/10 backdrop-blur-sm flex items-center justify-center border border-white/10">
                  <Layers className="h-5 w-5 text-[var(--color-text-muted)] opacity-50" />
                </div>
              </div>
            </>
          )}

          {/* Level badge */}
          {topic.level && (
            <span className={`absolute top-3 right-3 text-[10px] font-bold uppercase tracking-widest px-2.5 py-1 rounded-full bg-gradient-to-r ${levelGradient} text-white shadow-md`}>
              {topic.level}
            </span>
          )}

          {/* Bottom fade */}
          <div className="absolute inset-x-0 bottom-0 h-12 bg-gradient-to-t from-[var(--color-surface)] to-transparent" />
        </div>

        {/* Content */}
        <div className="p-5 pt-3 space-y-2">
          <h3 className={`font-bold text-lg text-[var(--color-text-primary)] ${accent.text} transition-colors line-clamp-1 tracking-tight leading-tight`}>
            {topic.title}
          </h3>

          {topic.description && (
            <p className="text-xs text-[var(--color-text-muted)] line-clamp-2 leading-relaxed">
              {topic.description}
            </p>
          )}

          <div className="flex items-center justify-end pt-1">
            <ArrowRight className={`h-4 w-4 text-[var(--color-text-muted)] ${accent.text} group-hover:translate-x-0.5 transition-all`} />
          </div>
        </div>

        {/* Hover shine */}
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
      </div>
    </Link>
  )
}
