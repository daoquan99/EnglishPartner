'use client'

import { useState } from 'react'
import Link from 'next/link'
import { ArrowLeft, Loader2, BookOpen, MessageSquare, Dumbbell, Languages, Layers, Star, ChevronRight } from 'lucide-react'
import { Button } from '@/ui/button'
import { useAuthStore } from '@/lib/zustand/auth.store'
import { useTopic, useVocabularies, useGrammars, useConversations, useExercises } from '../api/useCurriculum'
import { getExerciseTypeLabel } from '../constants/exercise-types'
import { VocabularyWordCard } from './VocabularyWordCard'
import { GrammarCard } from './GrammarCard'
import { ConversationCard } from './ConversationCard'

interface TopicDetailViewProps {
  topicId: string
}

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

type TabKey = 'vocabulary' | 'grammar' | 'conversations' | 'exercises'

export function TopicDetailView({ topicId }: TopicDetailViewProps) {
  const { user } = useAuthStore()
  const languageCode = user?.targetLanguage || 'en'
  const [activeTab, setActiveTab] = useState<TabKey>('vocabulary')

  const { data: topic, isLoading } = useTopic(topicId)
  const { data: vocabResult } = useVocabularies({ languageCode, topicId, page: 1, pageSize: 50 })
  const { data: grammars } = useGrammars({ languageCode })
  const { data: conversationsResponse } = useConversations({ languageCode, topicId })
  const { data: exercisesResponse } = useExercises({ languageCode, topicId })

  const conversations = conversationsResponse?.items || []
  const exercises = exercisesResponse?.items || []
  const topicGrammars = grammars?.items?.filter(g => g.topicId === topicId) || []
  const vocabItems = vocabResult?.items || []

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!topic) {
    return <div className="text-center py-20 text-[var(--color-text-muted)]">Topic not found.</div>
  }

  const levelKey = topic.level?.toLowerCase().replace(/ /g, '_') || ''
  const levelGradient = LEVEL_STYLES[levelKey] || 'from-teal-400 to-emerald-500'

  const tabs: { key: TabKey; label: string; count: number; icon: typeof BookOpen }[] = [
    { key: 'vocabulary', label: 'Vocabulary', count: vocabItems.length, icon: BookOpen },
    { key: 'grammar', label: 'Grammar', count: topicGrammars.length, icon: Languages },
    { key: 'conversations', label: 'Conversations', count: conversations.length, icon: MessageSquare },
    { key: 'exercises', label: 'Exercises', count: exercises.length, icon: Dumbbell },
  ]

  return (
    <div className="container mx-auto max-w-6xl px-4 py-8 space-y-8">
      {/* Back */}
      <Button asChild variant="ghost" size="sm" className="text-[var(--color-text-secondary)]">
        <Link href="/curriculum"><ArrowLeft className="h-4 w-4 mr-1" /> Back to Topics</Link>
      </Button>

      {/* Hero Header */}
      <div className={`relative overflow-hidden rounded-3xl bg-gradient-to-br ${levelGradient} p-8 text-white`}>
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_70%_30%,rgba(255,255,255,0.2),transparent_60%)]" />
        <div className="absolute -right-10 -bottom-10 h-44 w-44 rounded-full bg-white/10 blur-2xl" />
        <div className="absolute -left-6 top-4 h-28 w-28 rounded-full bg-white/10 blur-xl" />
        <div className="relative flex items-center gap-5">
          {topic.imageUrl ? (
            <img src={topic.imageUrl} alt={topic.title} className="h-20 w-20 rounded-2xl object-cover shadow-xl border-2 border-white/20" />
          ) : (
            <div className="h-20 w-20 rounded-2xl bg-white/20 backdrop-blur-sm flex items-center justify-center shadow-xl border border-white/10">
              <Layers className="h-9 w-9" />
            </div>
          )}
          <div>
            <h1 className="text-3xl font-bold tracking-tight">{topic.title}</h1>
            {topic.level && (
              <span className="inline-block mt-1.5 text-[11px] font-bold uppercase tracking-widest bg-white/20 backdrop-blur-sm px-3 py-1 rounded-full border border-white/20">
                {topic.level}
              </span>
            )}
            {topic.description && (
              <p className="text-white/80 mt-2 text-sm max-w-xl leading-relaxed">{topic.description}</p>
            )}
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex gap-2 overflow-x-auto pb-1">
        {tabs.map(tab => {
          const Icon = tab.icon
          const isActive = activeTab === tab.key
          return (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`flex items-center gap-2 px-5 py-2.5 rounded-xl text-sm font-semibold transition-all whitespace-nowrap ${
                isActive
                  ? 'bg-gradient-to-r from-[var(--color-primary)] to-[color-mix(in_srgb,var(--color-primary),#000_15%)] text-white shadow-lg shadow-[var(--color-primary)]/20'
                  : 'bg-[var(--color-surface)] text-[var(--color-text-secondary)] border border-[var(--color-border)] hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)]'
              }`}
            >
              <Icon className="h-4 w-4" />
              {tab.label}
              <span className={`text-xs px-1.5 py-0.5 rounded-md ${isActive ? 'bg-white/20' : 'bg-[var(--color-surface-2)]'}`}>
                {tab.count}
              </span>
            </button>
          )
        })}
      </div>

      {/* Tab Content */}
      <div>
        {activeTab === 'vocabulary' && (
          vocabItems.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {vocabItems.map(v => (
                <VocabularyWordCard key={v.id} {...v} />
              ))}
            </div>
          ) : <EmptyTab label="vocabulary" />
        )}

        {activeTab === 'grammar' && (
          topicGrammars.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {topicGrammars.map(g => (
                <GrammarCard key={g.id} grammar={g} />
              ))}
            </div>
          ) : <EmptyTab label="grammar lessons" />
        )}

        {activeTab === 'conversations' && (
          conversations.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
              {conversations.map(c => (
                <ConversationCard key={c.id} conversation={c} />
              ))}
            </div>
          ) : <EmptyTab label="conversations" />
        )}

        {activeTab === 'exercises' && (
          exercises.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {exercises.map(e => (
                <Link key={e.id} href={`/exercises/${e.id}`}>
                  <div className="group flex items-center gap-4 p-4 rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] hover:border-orange-500/30 transition-all cursor-pointer">
                    <div className="h-10 w-10 shrink-0 rounded-xl bg-gradient-to-br from-orange-500/15 to-amber-500/15 flex items-center justify-center text-orange-500 group-hover:from-orange-500 group-hover:to-amber-500 group-hover:text-white transition-all">
                      <Dumbbell className="h-5 w-5" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-sm text-[var(--color-text-primary)] group-hover:text-orange-500 transition-colors line-clamp-1">{e.title}</p>
                      <p className="text-xs text-[var(--color-text-muted)] mt-0.5">
                        {getExerciseTypeLabel(e.type)}
                        {e.xpReward > 0 && <span className="text-amber-500 ml-2">+{e.xpReward} XP</span>}
                      </p>
                    </div>
                    {e.level && (
                      <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md bg-gradient-to-r ${LEVEL_STYLES[e.level.toLowerCase()] || 'from-gray-400 to-gray-500'} text-white`}>
                        {e.level}
                      </span>
                    )}
                    <ChevronRight className="h-4 w-4 text-[var(--color-text-muted)] group-hover:text-orange-500 transition-colors shrink-0" />
                  </div>
                </Link>
              ))}
            </div>
          ) : <EmptyTab label="exercises" />
        )}
      </div>
    </div>
  )
}

function EmptyTab({ label }: { label: string }) {
  return (
    <div className="text-center py-16 space-y-3">
      <div className="h-16 w-16 mx-auto rounded-full bg-[var(--color-surface-2)] flex items-center justify-center">
        <BookOpen className="h-8 w-8 text-[var(--color-text-muted)] opacity-40" />
      </div>
      <p className="text-[var(--color-text-muted)]">No {label} in this topic yet.</p>
    </div>
  )
}
