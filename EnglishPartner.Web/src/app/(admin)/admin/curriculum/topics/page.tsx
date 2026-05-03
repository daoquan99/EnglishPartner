import { RequirePermission } from '@/shared/components/RequirePermission'
import { TopicManagement } from '@/features/curriculum/components/TopicManagement'
import { BookOpen } from 'lucide-react'

export default function TopicsPage() {
  return (
    <RequirePermission permission="curriculum.topics.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <BookOpen className="h-8 w-8 text-[var(--color-primary)]" />
            Topic Management
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Create and organize learning topics, specify CEFR levels, and arrange display order.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <TopicManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
