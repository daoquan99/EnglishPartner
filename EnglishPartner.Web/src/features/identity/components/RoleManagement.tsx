'use client'

import { useState } from 'react'
import { Plus, Loader2 } from 'lucide-react'

import { Button } from '@/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Badge } from '@/ui/badge'
import { useAdminRoles, useDeleteRole } from '../api/useAdmin'
import { RoleFormDialog } from './RoleFormDialog'
import { RolePermissionsDialog } from './RolePermissionsDialog'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { RoleTableRow } from './RoleTableRow'
import type { RoleResponse } from '../types/admin.types'

export function RoleManagement() {
  const { data: roles, isLoading } = useAdminRoles()
  const { mutate: deleteRole } = useDeleteRole()

  // Dialog state
  const [formOpen, setFormOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<RoleResponse | null>(null)

  const [permissionsOpen, setPermissionsOpen] = useState(false)
  const [selectedRole, setSelectedRole] = useState<RoleResponse | null>(null)

  const [confirmDelete, setConfirmDelete] = useState<{ open: boolean; role: RoleResponse | null }>({
    open: false,
    role: null,
  })

  // Handlers
  const handleAdd = () => {
    setEditingRole(null)
    setFormOpen(true)
  }

  const handleEdit = (role: RoleResponse) => {
    setEditingRole(role)
    setFormOpen(true)
  }

  const handleManagePermissions = (role: RoleResponse) => {
    setSelectedRole(role)
    setPermissionsOpen(true)
  }

  const handleDeleteConfirm = (role: RoleResponse) => {
    setConfirmDelete({ open: true, role })
  }

  const handleDeleteExecute = () => {
    if (confirmDelete.role) {
      deleteRole(confirmDelete.role.id)
      setConfirmDelete({ open: false, role: null })
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold text-[var(--color-text-primary)]">System Roles</h2>
        <Button onClick={handleAdd} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
          <Plus className="mr-2 h-4 w-4" /> New Role
        </Button>
      </div>

      <div className="rounded-md border border-[var(--color-border)] bg-[var(--color-surface)]">
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)]">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
              <TableHead className="text-[var(--color-text-secondary)]">Role Name</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Description</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Permissions</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] text-right w-[180px]">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={4} className="h-32 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin text-[var(--color-primary)]" />
                </TableCell>
              </TableRow>
            ) : !roles?.length ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-[var(--color-text-muted)]">
                  No roles found.
                </TableCell>
              </TableRow>
            ) : (
              roles.map(role => (
                <RoleTableRow
                  key={role.id}
                  role={role}
                  onEdit={handleEdit}
                  onManagePermissions={handleManagePermissions}
                  onDelete={handleDeleteConfirm}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <RoleFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        editingRole={editingRole}
      />

      <RolePermissionsDialog
        open={permissionsOpen}
        onOpenChange={setPermissionsOpen}
        role={selectedRole}
      />

      <ConfirmActionDialog
        open={confirmDelete.open}
        onOpenChange={open => setConfirmDelete(prev => ({ ...prev, open }))}
        title="Delete Role?"
        description={`This will permanently delete the role "${confirmDelete.role?.name}". This cannot be undone.`}
        confirmLabel="Delete Role"
        isDestructive
        onConfirm={handleDeleteExecute}
      />
    </div>
  )
}
