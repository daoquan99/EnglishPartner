'use client'

import { useParams } from 'next/navigation'
import { TopicDetailView } from '@/features/curriculum/learner/TopicDetailView'

export default function TopicDetailPage() {
  const { id } = useParams<{ id: string }>()
  return <TopicDetailView topicId={id} />
}
