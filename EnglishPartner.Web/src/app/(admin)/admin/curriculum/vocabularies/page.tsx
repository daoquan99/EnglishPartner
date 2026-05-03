import { RequirePermission } from '@/shared/components/RequirePermission'
import { VocabularyManagement } from '@/features/curriculum/components/VocabularyManagement'
import { Languages } from 'lucide-react'

export default function VocabulariesPage() {
  return (
    <RequirePermission permission="curriculum.vocabulary.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <Languages className="h-8 w-8 text-[var(--color-primary)]" />
            Vocabulary Management
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage words, definitions, pronunciations, and automatically generate new vocabulary using AI.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <VocabularyManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
