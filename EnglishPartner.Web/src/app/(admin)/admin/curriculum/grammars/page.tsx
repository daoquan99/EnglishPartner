import { RequirePermission } from '@/shared/components/RequirePermission'
import { GrammarManagement } from '@/features/curriculum/components/GrammarManagement'
import { BookType } from 'lucide-react'

export default function GrammarsPage() {
  return (
    <RequirePermission permission="curriculum.grammar.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <BookType className="h-8 w-8 text-[var(--color-primary)]" />
            Grammar Management
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage grammar rules, formulas, and generate grammar content using AI.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <GrammarManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
