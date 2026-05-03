import { RoleManagement } from '@/features/identity/components/RoleManagement'
import { RequirePermission } from '@/shared/components/RequirePermission'

export default function RolesPage() {
  return (
    <RequirePermission permission="identity.roles.manage">
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">Role Management</h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Create and manage roles and their associated permissions.
          </p>
        </div>
        <RoleManagement />
      </div>
    </RequirePermission>
  )
}
