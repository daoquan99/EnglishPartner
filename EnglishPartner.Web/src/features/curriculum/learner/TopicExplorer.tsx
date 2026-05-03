'use client'

import { useState } from 'react'
import { Search, Compass, Sparkles, BookMarked } from 'lucide-react'
import { useTopics } from '../api/useCurriculum'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { TopicCard } from './TopicCard'
import { useDebounce } from '@/shared/hooks/useDebounce'

export function TopicExplorer() {
  const { user } = useAuthStore()
  const languageCode = user?.targetLanguage || 'en'
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedSearch = useDebounce(searchTerm, 300)

  const { data: topics, isLoading } = useTopics({
    languageCode,
    searchTerm: debouncedSearch || undefined,
  })

  const topicCount = topics?.length || 0

  return (
    <div className="container mx-auto max-w-6xl px-4 py-8 space-y-8">
      {/* Hero Header */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-teal-500 via-emerald-500 to-green-400 p-8 text-white">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_80%_20%,rgba(255,255,255,0.2),transparent_60%)]" />
        <div className="absolute -right-10 -bottom-10 h-44 w-44 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -left-6 top-4 h-28 w-28 rounded-full bg-white/10 blur-xl" />
        <div className="relative flex items-center gap-5">
          <div className="h-16 w-16 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-xl">
            <Compass className="h-8 w-8" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Explore Topics</h1>
            <p className="text-white/80 mt-1 flex items-center gap-2">
              <Sparkles className="h-4 w-4" />
              {topicCount} topics to discover and master
            </p>
          </div>
        </div>
      </div>

      {/* Search */}
      <div className="relative max-w-md">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--color-text-muted)]" />
        <input
          type="text"
          placeholder="Search topics..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="w-full pl-11 pr-4 py-3 rounded-2xl border border-[var(--color-border)] bg-[var(--color-surface)] text-sm text-[var(--color-text-primary)] placeholder:text-[var(--color-text-muted)] focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/60 focus:outline-none transition-all"
        />
      </div>

      {/* Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-56 rounded-2xl bg-[var(--color-surface)] animate-pulse border border-[var(--color-border)]" />
          ))}
        </div>
      ) : topics && topics.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {topics.map(topic => <TopicCard key={topic.id} topic={topic} />)}
        </div>
      ) : (
        <div className="text-center py-20 space-y-4">
          <div className="h-20 w-20 mx-auto rounded-full bg-[var(--color-surface-2)] flex items-center justify-center">
            <BookMarked className="h-10 w-10 text-[var(--color-text-muted)] opacity-40" />
          </div>
          <p className="text-lg font-medium text-[var(--color-text-muted)]">No topics found</p>
          <p className="text-sm text-[var(--color-text-muted)]">Try adjusting your search</p>
        </div>
      )}
    </div>
  )
}

