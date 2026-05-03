import { Metadata } from 'next'
import { ExerciseManagement } from '@/features/curriculum/components/ExerciseManagement'
import { Dumbbell } from 'lucide-react'
import { RequirePermission } from '@/shared/components/RequirePermission'

export const metadata: Metadata = {
  title: 'Exercises | English Partner',
  description: 'Manage exercises for the English Partner curriculum',
}

export default function ExercisesPage() {
  return (
    <RequirePermission permission="curriculum.exercise.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <Dumbbell className="h-8 w-8 text-[var(--color-primary)]" />
            Exercises
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage exercises across all topics, vocabularies, and grammars.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <ExerciseManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
