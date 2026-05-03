import { UserManagement } from '@/features/identity/components/UserManagement'
import { RequirePermission } from '@/shared/components/RequirePermission'

export default function UsersPage() {
  return (
    <RequirePermission permission="identity.users.manage">
      <div className="flex flex-col h-[calc(100vh-4rem)] gap-4">
        <div className="shrink-0">
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">User Management</h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            View, activate, deactivate and manage all user accounts.
          </p>
        </div>
        <div className="flex-1 min-h-0">
          <UserManagement />
        </div>
      </div>
    </RequirePermission>
  )
}
