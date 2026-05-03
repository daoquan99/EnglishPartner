'use client'

import { useState } from 'react'
import { Search, BookOpen, Sparkles, ChevronLeft, ChevronRight } from 'lucide-react'
import { useVocabularies, useTopics } from '../api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { VocabularyWordCard } from './VocabularyWordCard'
import { useDebounce } from '@/shared/hooks/useDebounce'

export function VocabularyBrowser() {
  const { user } = useAuthStore()
  const languageCode = user?.targetLanguage || 'en'
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedLevel, setSelectedLevel] = useState('')
  const [selectedTopicId, setSelectedTopicId] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 18
  const debouncedSearch = useDebounce(searchTerm, 300)

  const { data: topicsData } = useTopics({ languageCode })
  const topics = Array.isArray(topicsData) ? topicsData : []

  const { data: result, isLoading } = useVocabularies({
    languageCode,
    page,
    pageSize,
    searchTerm: debouncedSearch || undefined,
    level: selectedLevel || undefined,
    topicId: selectedTopicId || undefined,
  })

  const totalCount = result?.totalCount || 0
  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="container mx-auto max-w-6xl px-4 py-8 space-y-8">
      {/* Hero Header */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-indigo-500 via-blue-500 to-cyan-400 p-8 text-white">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_60%,rgba(255,255,255,0.15),transparent_60%)]" />
        <div className="absolute -right-8 -top-8 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -left-6 -bottom-6 h-36 w-36 rounded-full bg-white/10 blur-xl" />
        <div className="relative flex items-center gap-5">
          <div className="h-16 w-16 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-xl">
            <BookOpen className="h-8 w-8" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Vocabulary</h1>
            <p className="text-white/80 mt-1 flex items-center gap-2">
              <Sparkles className="h-4 w-4" />
              {totalCount} words to expand your knowledge
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
            placeholder="Search words..."
            value={searchTerm}
            onChange={e => { setSearchTerm(e.target.value); setPage(1) }}
            className="w-full pl-11 pr-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:ring-2 focus:ring-indigo-500/40 focus:border-indigo-500/60 focus:outline-none transition-all"
          />
        </div>
        <select
          value={selectedLevel}
          onChange={e => { setSelectedLevel(e.target.value); setPage(1) }}
          className="px-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] focus:ring-2 focus:ring-indigo-500/40 focus:outline-none min-w-[120px] cursor-pointer"
        >
          <option value="">All Levels</option>
          {['A1', 'A2', 'B1', 'B2', 'C1', 'C2'].map(l => (
            <option key={l} value={l}>{l}</option>
          ))}
        </select>
        <select
          value={selectedTopicId}
          onChange={e => { setSelectedTopicId(e.target.value); setPage(1) }}
          className="px-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] focus:ring-2 focus:ring-indigo-500/40 focus:outline-none min-w-[160px] cursor-pointer"
        >
          <option value="">All Topics</option>
          {topics.map((t: any) => (
            <option key={t.id} value={t.id}>{t.title}</option>
          ))}
        </select>
      </div>

      {/* Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-36 rounded-2xl bg-[var(--color-surface)] animate-pulse border border-[var(--color-border)]" />
          ))}
        </div>
      ) : result?.items && result.items.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {result.items.map(v => (
            <VocabularyWordCard
              key={v.id}
              id={v.id}
              word={v.word}
              definition={v.definition}
              partOfSpeech={v.partOfSpeech}
              level={v.level}
              pronunciation={v.pronunciation}
            />
          ))}
        </div>
      ) : (
        <div className="text-center py-20 space-y-4">
          <div className="h-20 w-20 mx-auto rounded-full bg-[var(--color-surface-2)] flex items-center justify-center">
            <BookOpen className="h-10 w-10 text-[var(--color-text-muted)] opacity-40" />
          </div>
          <p className="text-lg font-medium text-[var(--color-text-muted)]">No vocabulary found</p>
          <p className="text-sm text-[var(--color-text-muted)]">Try adjusting your filters</p>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 pt-4">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="h-10 w-10 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] disabled:opacity-30 hover:bg-[var(--color-surface-2)] hover:border-indigo-500/30 transition-all flex items-center justify-center"
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
                    ? 'bg-gradient-to-r from-indigo-500 to-blue-500 text-white shadow-lg shadow-indigo-500/25'
                    : 'border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-2)] hover:border-indigo-500/30'
                }`}
              >
                {pageNum}
              </button>
            )
          })}

          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
            className="h-10 w-10 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] disabled:opacity-30 hover:bg-[var(--color-surface-2)] hover:border-indigo-500/30 transition-all flex items-center justify-center"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  )
}
