import { RequirePermission } from '@/shared/components/RequirePermission'
import { ConversationManagement } from '@/features/curriculum/components/ConversationManagement'
import { MessageSquare } from 'lucide-react'

export default function ConversationsPage() {
  return (
    <RequirePermission permission="curriculum.conversation.write">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <MessageSquare className="h-8 w-8 text-[var(--color-primary)]" />
            Conversation Management
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage dialogue scenarios, lines, and generate full conversations using AI.
          </p>
        </div>

        <div className="flex-1 min-h-0">
          <ConversationManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
