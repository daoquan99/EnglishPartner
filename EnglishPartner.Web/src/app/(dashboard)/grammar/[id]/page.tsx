'use client'

import { useParams } from 'next/navigation'
import { GrammarLessonView } from '@/features/curriculum/learner/GrammarLessonView'

export default function GrammarDetailPage() {
  const { id } = useParams<{ id: string }>()
  return <GrammarLessonView grammarId={id} />
}
