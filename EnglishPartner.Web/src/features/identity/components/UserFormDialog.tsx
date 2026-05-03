'use client'

import { Button } from '@/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import type { AdminUserResponse } from '../types/admin.types'

interface UserFormData {
  id: string
  email: string
  displayName: string
  password: string
}

interface UserFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  formData: UserFormData
  isEditing: boolean
  onChange: (data: UserFormData) => void
  onSave: () => void
}

export function UserFormDialog({
  open,
  onOpenChange,
  formData,
  isEditing,
  onChange,
  onSave,
}: UserFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit User' : 'Add New User'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update user details.' : 'Create a new user account.'}
          </DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="user-email">Email</Label>
            <Input
              id="user-email"
              value={formData.email}
              onChange={e => onChange({ ...formData, email: e.target.value })}
              placeholder="user@example.com"
              disabled={isEditing}
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="user-display-name">Display Name</Label>
            <Input
              id="user-display-name"
              value={formData.displayName}
              onChange={e => onChange({ ...formData, displayName: e.target.value })}
              placeholder="John Doe"
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          {!isEditing && (
            <div className="space-y-2">
              <Label htmlFor="user-password">Password</Label>
              <Input
                id="user-password"
                type="password"
                value={formData.password}
                onChange={e => onChange({ ...formData, password: e.target.value })}
                placeholder="Temporary password"
                className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
              />
            </div>
          )}
        </div>

        {/* TODO(#BE-task): Wire to CreateUserCommand / UpdateUserCommand when BE adds these endpoints */}
        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]"
          >
            Cancel
          </Button>
          <Button
            onClick={onSave}
            className="bg-[var(--color-primary)] text-[var(--color-btn-text)] hover:bg-[var(--color-primary-hover)]"
          >
            {isEditing ? 'Save Changes' : 'Create User'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
