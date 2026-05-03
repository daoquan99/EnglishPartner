import { Metadata } from 'next'
import { ExampleSentenceManagement } from '@/features/curriculum/components/ExampleSentenceManagement'
import { BookOpen } from 'lucide-react'
import { RequirePermission } from '@/shared/components/RequirePermission'

export const metadata: Metadata = {
  title: 'Example Sentences | English Partner',
  description: 'Manage example sentences for the English Partner curriculum',
}

export default function ExampleSentencesPage() {
  return (
    <RequirePermission permission="curriculum.grammar.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <BookOpen className="h-8 w-8 text-[var(--color-primary)]" />
            Example Sentences
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage reusable example sentences and translations.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <ExampleSentenceManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
