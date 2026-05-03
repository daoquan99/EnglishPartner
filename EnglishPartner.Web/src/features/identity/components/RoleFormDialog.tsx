'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'

import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { useCreateRole, useUpdateRole } from '../api/useAdmin'
import type { RoleResponse } from '../types/admin.types'

const roleSchema = z.object({
  name: z.string().min(2, 'Role name must be at least 2 characters').max(50),
  description: z.string().max(200).optional(),
})

type RoleFormData = z.infer<typeof roleSchema>

interface RoleFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  editingRole: RoleResponse | null
}

export function RoleFormDialog({ open, onOpenChange, editingRole }: RoleFormDialogProps) {
  const isEditing = !!editingRole
  const { mutate: createRole, isPending: isCreating } = useCreateRole()
  const { mutate: updateRole, isPending: isUpdating } = useUpdateRole()
  const isPending = isCreating || isUpdating

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RoleFormData>({
    resolver: zodResolver(roleSchema),
  })

  useEffect(() => {
    if (open) {
      reset({
        name: editingRole?.name ?? '',
        description: editingRole?.description ?? '',
      })
    }
  }, [open, editingRole, reset])

  const onSubmit = (data: RoleFormData) => {
    if (isEditing) {
      updateRole(
        { id: editingRole.id, ...data },
        { onSuccess: () => onOpenChange(false) }
      )
    } else {
      createRole(data, { onSuccess: () => onOpenChange(false) })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Role' : 'Create New Role'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update the role name and description.' : 'Define a new role for the system.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
          <div className="space-y-2">
            <Label>Role Name</Label>
            <Input
              {...register('name')}
              placeholder="e.g., Moderator"
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
              aria-invalid={!!errors.name}
            />
            {errors.name && (
              <p className="text-xs text-[var(--color-destructive)]">{errors.name.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Description <span className="text-[var(--color-text-muted)]">(optional)</span></Label>
            <Input
              {...register('description')}
              placeholder="Brief description of this role"
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]">
              Cancel
            </Button>
            <Button type="submit" disabled={isPending} className="bg-[var(--color-primary)] text-[var(--color-btn-text)] hover:bg-[var(--color-primary-hover)]">
              {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEditing ? 'Save Changes' : 'Create Role'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
