'use client'

import { useParams } from 'next/navigation'
import { ConversationReader } from '@/features/curriculum/learner/ConversationReader'

export default function ConversationDetailPage() {
  const { id } = useParams<{ id: string }>()
  return <ConversationReader conversationId={id} />
}
