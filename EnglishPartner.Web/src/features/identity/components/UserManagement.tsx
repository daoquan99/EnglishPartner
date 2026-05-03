'use client'

import { useState } from 'react'
import { ChevronLeft, ChevronRight, Copy, Loader2, Plus } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { useAdminUsers, useActivateUser, useDeactivateUser, useDeleteUser, useResetUserPassword, useManageUserRole } from '../api/useAdmin'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { UserTableRow } from './UserTableRow'
import { UserFormDialog } from './UserFormDialog'
import { ManageRoleDialog } from './ManageRoleDialog'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { toast } from 'sonner'
import type { AdminUserResponse } from '../types/admin.types'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

const EMPTY_FORM = { id: '', email: '', displayName: '', password: '' }

export function UserManagement() {
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedSearch = useDebounce(searchTerm, 500)
  const [page, setPage] = useState(1)

  // Dialog visibility state
  const [userFormOpen, setUserFormOpen] = useState(false)
  const [isEditing, setIsEditing] = useState(false)
  const [userFormData, setUserFormData] = useState(EMPTY_FORM)

  const [roleDialogOpen, setRoleDialogOpen] = useState(false)
  const [selectedUserForRole, setSelectedUserForRole] = useState<AdminUserResponse | null>(null)

  const [resetPasswordResult, setResetPasswordResult] = useState<string | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    confirmLabel: string
    isDestructive: boolean
    onConfirm: () => void
  }>({ open: false, title: '', description: '', confirmLabel: 'Confirm', isDestructive: false, onConfirm: () => {} })

  // API hooks
  const { data, isLoading } = useAdminUsers({ search: debouncedSearch || undefined, page, pageSize: 10 })
  const { mutate: activate } = useActivateUser()
  const { mutate: deactivate } = useDeactivateUser()
  const { mutate: remove } = useDeleteUser()
  const { mutateAsync: resetPwd } = useResetUserPassword()

  const users = data?.items ?? []

  // Handlers
  const handleOpenAdd = () => { 
    setUserFormData(EMPTY_FORM); 
    setIsEditing(false); 
    setUserFormOpen(true) 
  }
  const handleOpenEdit = (user: AdminUserResponse) => {
    setUserFormData({ id: user.id, email: user.email, displayName: user.displayName, password: '' })
    setIsEditing(true)
    setUserFormOpen(true)
  }
  const handleSaveUser = () => {
    // TODO(#BE-task): Connect to CreateUserCommand / UpdateUserCommand when BE adds endpoints
    toast.info('Backend API for user CRUD is under construction!')
    setUserFormOpen(false)
  }

  const handleOpenRoleDialog = (user: AdminUserResponse) => {
    setSelectedUserForRole(user)
    setRoleDialogOpen(true)
  }

  const openConfirm = (config: typeof confirmDialog) => setConfirmDialog(config)
  const closeConfirm = () => setConfirmDialog(prev => ({ ...prev, open: false }))

  const handleResetPassword = (id: string) => openConfirm({
    open: true,
    title: 'Reset Password?',
    description: "A new password will be generated. The admin can share it with the user or choose to email them.",
    confirmLabel: 'Reset Password',
    isDestructive: false,
    onConfirm: async () => {
      closeConfirm()
      try {
        const newPassword = await resetPwd({ id, sendEmail: false })
        if (newPassword) setResetPasswordResult(newPassword)
      } catch { /* error toast handled by hook */ }
    },
  })

  const handleDelete = (id: string) => openConfirm({
    open: true,
    title: 'Delete User?',
    description: 'This action is permanent and cannot be undone. All user data will be lost.',
    confirmLabel: 'Delete permanently',
    isDestructive: true,
    onConfirm: () => { remove(id); closeConfirm() },
  })

  return (
    <div className="flex flex-col h-full gap-4">
      {/* Filters Toolbar */}
      <div className="shrink-0 flex flex-col gap-4 p-4 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex flex-1 flex-wrap items-center gap-4">
            <div className="flex-1 min-w-[200px] max-w-sm">
              <Input
                placeholder="Search users by name or email..."
                value={searchTerm}
                onChange={e => { setSearchTerm(e.target.value); setPage(1) }}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button onClick={handleOpenAdd} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              <Plus className="h-4 w-4 mr-2" />
              Add User
            </Button>
          </div>
        </div>
      </div>

      <DataTableContainer pagination={data ? { page: data.page, pageSize: data.pageSize, totalCount: data.totalCount, onPageChange: setPage } : undefined}>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)]">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
              <TableHead className="text-[var(--color-text-secondary)]">Name</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Email</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Roles</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Status</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin text-[var(--color-primary)]" />
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-[var(--color-text-muted)]">
                  No users found.
                </TableCell>
              </TableRow>
            ) : (
              users.map(user => (
                <UserTableRow
                  key={user.id}
                  user={user}
                  onEdit={handleOpenEdit}
                  onManageRole={handleOpenRoleDialog}
                  onResetPassword={handleResetPassword}
                  onActivate={id => activate(id)}
                  onDeactivate={id => deactivate(id)}
                  onDelete={handleDelete}
                />
              ))
            )}
          </TableBody>
        </Table>
      </DataTableContainer>

      <UserFormDialog
        open={userFormOpen}
        onOpenChange={setUserFormOpen}
        formData={userFormData}
        isEditing={isEditing}
        onChange={setUserFormData}
        onSave={handleSaveUser}
      />

      <ManageRoleDialog
        open={roleDialogOpen}
        onOpenChange={setRoleDialogOpen}
        user={selectedUserForRole}
      />

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={open => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        confirmLabel={confirmDialog.confirmLabel}
        isDestructive={confirmDialog.isDestructive}
        onConfirm={confirmDialog.onConfirm}
      />

      {/* Reset Password Result Dialog */}
      <Dialog open={!!resetPasswordResult} onOpenChange={(open) => { if (!open) setResetPasswordResult(null) }}>
        <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>Password Reset Successfully</DialogTitle>
            <DialogDescription className="text-[var(--color-text-secondary)]">
              Copy the new password below and share it with the user.
            </DialogDescription>
          </DialogHeader>
          <div className="flex items-center gap-2 p-3 bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-md">
            <code className="flex-1 text-sm font-mono text-[var(--color-primary)] select-all">{resetPasswordResult}</code>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                if (resetPasswordResult) {
                  navigator.clipboard.writeText(resetPasswordResult)
                  toast.success('Password copied to clipboard')
                }
              }}
              className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
            >
              <Copy className="h-4 w-4" />
            </Button>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setResetPasswordResult(null)}
              className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]"
            >
              Done
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
