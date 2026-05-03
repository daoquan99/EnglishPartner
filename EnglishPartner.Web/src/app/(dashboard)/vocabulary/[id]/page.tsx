'use client'

import { useParams } from 'next/navigation'
import { VocabularyDetailView } from '@/features/curriculum/learner/VocabularyDetailView'

export default function VocabularyDetailPage() {
  const { id } = useParams<{ id: string }>()
  return <VocabularyDetailView vocabularyId={id} />
}
