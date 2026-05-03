'use client'

import { useState } from 'react'
import { useExercises, useTopics } from '@/features/curriculum/api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { getExerciseTypeLabel, EXERCISE_TYPES } from '@/features/curriculum/constants/exercise-types'
import { Dumbbell, Search, Sparkles, ChevronLeft, ChevronRight, Star } from 'lucide-react'
import Link from 'next/link'

const LEVEL_STYLES: Record<string, { bg: string; text: string; glow: string }> = {
  A1: { bg: 'from-emerald-400 to-green-500', text: 'text-emerald-400', glow: 'shadow-emerald-500/20' },
  A2: { bg: 'from-teal-400 to-cyan-500', text: 'text-teal-400', glow: 'shadow-teal-500/20' },
  B1: { bg: 'from-blue-400 to-indigo-500', text: 'text-blue-400', glow: 'shadow-blue-500/20' },
  B2: { bg: 'from-violet-400 to-purple-500', text: 'text-violet-400', glow: 'shadow-violet-500/20' },
  C1: { bg: 'from-fuchsia-400 to-pink-500', text: 'text-fuchsia-400', glow: 'shadow-fuchsia-500/20' },
  C2: { bg: 'from-rose-400 to-red-500', text: 'text-rose-400', glow: 'shadow-rose-500/20' },
}

const TYPE_ICONS: Record<string, string> = {
  multiplechoice: '🔘',
  fillintheblank: '✏️',
  truefalse: '✅',
  matching: '🔗',
  translation: '🌐',
  sentencecompletion: '📝',
  wordform: '🔤',
}

export default function ExercisesPage() {
  const { user } = useAuthStore()
  const languageCode = user?.targetLanguage || ''
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedLevel, setSelectedLevel] = useState('')
  const [selectedTopicId, setSelectedTopicId] = useState('')
  const [selectedType, setSelectedType] = useState('')
  const [page, setPage] = useState(1)

  const { data: topicsData } = useTopics({ languageCode }, { enabled: !!languageCode })
  const topics = Array.isArray(topicsData) ? topicsData : []

  const { data: exercisesResponse, isLoading } = useExercises({
    languageCode,
    page,
    pageSize: 12,
    level: selectedLevel || undefined,
    topicId: selectedTopicId || undefined,
    exerciseType: selectedType ? Number(selectedType) : undefined,
  })

  const exercises = exercisesResponse?.items || []
  const totalCount = exercisesResponse?.totalCount || 0
  const totalPages = Math.ceil(totalCount / 12)

  const filtered = exercises.filter(e =>
    !searchTerm ||
    e.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
    e.instruction.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="container mx-auto max-w-6xl px-4 py-8 space-y-8">
      {/* Hero Header */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-orange-500 via-amber-500 to-yellow-500 p-8 text-white">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_50%,rgba(255,255,255,0.15),transparent_70%)]" />
        <div className="absolute -right-8 -top-8 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -left-4 -bottom-4 h-32 w-32 rounded-full bg-white/10 blur-xl" />
        <div className="relative flex items-center gap-5">
          <div className="h-16 w-16 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-xl">
            <Dumbbell className="h-8 w-8" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Exercises</h1>
            <p className="text-white/80 mt-1 flex items-center gap-2">
              <Sparkles className="h-4 w-4" />
              {totalCount} exercises to sharpen your skills
            </p>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <div className="relative flex-1 min-w-[240px]">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--color-text-muted)]" />
          <input
            type="text"
            placeholder="Search exercises..."
            value={searchTerm}
            onChange={e => { setSearchTerm(e.target.value); setPage(1) }}
            className="w-full pl-11 pr-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:ring-2 focus:ring-orange-500/40 focus:border-orange-500/60 focus:outline-none transition-all"
          />
        </div>
        <select
          value={selectedLevel}
          onChange={e => { setSelectedLevel(e.target.value); setPage(1) }}
          className="px-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] focus:ring-2 focus:ring-orange-500/40 focus:outline-none min-w-[120px] cursor-pointer"
        >
          <option value="">All Levels</option>
          {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map(l => (
            <option key={l} value={l}>{l}</option>
          ))}
        </select>
        <select
          value={selectedType}
          onChange={e => { setSelectedType(e.target.value); setPage(1) }}
          className="px-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] focus:ring-2 focus:ring-orange-500/40 focus:outline-none min-w-[180px] cursor-pointer"
        >
          <option value="">All Types</option>
          {EXERCISE_TYPES.map(t => (
            <option key={t.value} value={t.value}>{t.label}</option>
          ))}
        </select>
        <select
          value={selectedTopicId}
          onChange={e => { setSelectedTopicId(e.target.value); setPage(1) }}
          className="px-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] focus:ring-2 focus:ring-orange-500/40 focus:outline-none min-w-[160px] cursor-pointer"
        >
          <option value="">All Topics</option>
          {topics.map((t: any) => (
            <option key={t.id} value={t.id}>{t.title}</option>
          ))}
        </select>
      </div>

      {/* Exercise Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-48 rounded-2xl bg-[var(--color-surface)] animate-pulse border border-[var(--color-border)]" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-20 space-y-4">
          <div className="h-20 w-20 mx-auto rounded-full bg-[var(--color-surface-2)] flex items-center justify-center">
            <Dumbbell className="h-10 w-10 text-[var(--color-text-muted)] opacity-40" />
          </div>
          <p className="text-lg font-medium text-[var(--color-text-muted)]">No exercises found</p>
          <p className="text-sm text-[var(--color-text-muted)]">Try adjusting your filters</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map(exercise => {
            const typeKey = exercise.type?.toLowerCase().replace(/[\s/]/g, '') || ''
            const levelStyle = LEVEL_STYLES[exercise.level || '']
            const emoji = TYPE_ICONS[typeKey] || '📋'

            return (
              <Link key={exercise.id} href={`/exercises/${exercise.id}`}>
                <div className="group relative h-full rounded-2xl border border-black/5 dark:border-white/10 bg-white/50 dark:bg-black/20 backdrop-blur-xl overflow-hidden transition-all duration-500 hover:shadow-[0_20px_40px_-15px_rgba(249,115,22,0.15)] dark:hover:shadow-[0_20px_40px_-15px_rgba(249,115,22,0.1)] hover:-translate-y-1.5 hover:border-orange-500/30 cursor-pointer">
                  {/* Top gradient bar */}
                  <div className={`h-1.5 w-full bg-gradient-to-r ${levelStyle?.bg || 'from-orange-400 to-amber-500'}`} />

                  <div className="p-5 space-y-4">
                    {/* Emoji + Type */}
                    <div className="flex items-center justify-between">
                      <span className="text-2xl">{emoji}</span>
                      <span className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-text-muted)] bg-[var(--color-surface-2)] px-2.5 py-1 rounded-full">
                        {getExerciseTypeLabel(exercise.type)}
                      </span>
                    </div>

                    {/* Title */}
                    <div>
                      <h3 className="font-bold text-[var(--color-text-primary)] group-hover:text-orange-500 dark:group-hover:text-orange-400 transition-colors line-clamp-2 leading-snug">
                        {exercise.title}
                      </h3>
                      {exercise.instruction && (
                        <p className="text-xs text-[var(--color-text-muted)] mt-1.5 line-clamp-2 leading-relaxed">
                          {exercise.instruction}
                        </p>
                      )}
                    </div>

                    {/* Footer */}
                    <div className="flex items-center justify-between pt-1">
                      <div className="flex items-center gap-2">
                        {exercise.level && (
                          <span className={`text-xs font-bold px-2 py-0.5 rounded-md bg-gradient-to-r ${levelStyle?.bg || 'from-gray-400 to-gray-500'} text-white shadow-sm ${levelStyle?.glow || ''}`}>
                            {exercise.level}
                          </span>
                        )}
                      </div>
                      {exercise.xpReward > 0 && (
                        <span className="flex items-center gap-1 text-xs font-semibold text-amber-500">
                          <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
                          +{exercise.xpReward} XP
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Hover shine */}
                  <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-[150%] group-hover:translate-x-[150%] transition-transform duration-1000 ease-in-out pointer-events-none" />
                </div>
              </Link>
            )
          })}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 pt-4">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="h-10 w-10 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] disabled:opacity-30 hover:bg-[var(--color-surface-2)] hover:border-orange-500/30 transition-all flex items-center justify-center"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>

          {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
            let pageNum: number
            if (totalPages <= 5) {
              pageNum = i + 1
            } else if (page <= 3) {
              pageNum = i + 1
            } else if (page >= totalPages - 2) {
              pageNum = totalPages - 4 + i
            } else {
              pageNum = page - 2 + i
            }
            return (
              <button
                key={pageNum}
                onClick={() => setPage(pageNum)}
                className={`h-10 w-10 rounded-xl text-sm font-semibold transition-all ${
                  page === pageNum
                    ? 'bg-gradient-to-r from-orange-500 to-amber-500 text-white shadow-lg shadow-orange-500/25'
                    : 'border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-2)] hover:border-orange-500/30'
                }`}
              >
                {pageNum}
              </button>
            )
          })}

          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
            className="h-10 w-10 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] disabled:opacity-30 hover:bg-[var(--color-surface-2)] hover:border-orange-500/30 transition-all flex items-center justify-center"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  )
}
