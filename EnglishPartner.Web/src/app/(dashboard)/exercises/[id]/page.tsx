'use client'

import { useParams } from 'next/navigation'
import { ExerciseSolver } from '@/features/curriculum/learner/ExerciseSolver'

export default function ExercisePage() {
  const { id } = useParams<{ id: string }>()
  return <ExerciseSolver exerciseId={id} />
}
