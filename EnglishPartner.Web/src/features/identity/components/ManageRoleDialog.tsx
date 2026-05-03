'use client'

import { useState, useEffect } from 'react'
import { Loader2 } from 'lucide-react'
import { Button } from '@/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Checkbox } from '@/ui/checkbox'
import { useAdminRoles, useManageUserRole } from '../api/useAdmin'
import type { AdminUserResponse } from '../types/admin.types'

interface ManageRoleDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: AdminUserResponse | null
}

export function ManageRoleDialog({
  open,
  onOpenChange,
  user,
}: ManageRoleDialogProps) {
  const { data: roles, isLoading: isLoadingRoles } = useAdminRoles()
  const { mutateAsync: manageRole } = useManageUserRole()

  // Keep a local state of roles for optimistic UI updates
  const [userRoles, setUserRoles] = useState<Set<string>>(new Set())
  const [processingRoles, setProcessingRoles] = useState<Set<string>>(new Set())

  // Sync local state when dialog opens or user changes
  useEffect(() => {
    if (open && user) {
      setUserRoles(new Set(user.roles?.map(r => r.id) || []))
      setProcessingRoles(new Set())
    }
  }, [open, user])

  const handleToggleRole = async (roleId: string) => {
    if (!user) return

    // We check current state to determine operation
    const isCurrentlyAssigned = userRoles.has(roleId)
    const operation = isCurrentlyAssigned ? 'Revoke' : 'Assign'

    // Optimistic update
    setUserRoles(prev => {
      const next = new Set(prev)
      if (isCurrentlyAssigned) next.delete(roleId)
      else next.add(roleId)
      return next
    })

    // Mark as processing
    setProcessingRoles(prev => new Set(prev).add(roleId))

    try {
      await manageRole({ id: user.id, roleId, operation })
      // Success: Remove from processing
      setProcessingRoles(prev => {
        const next = new Set(prev)
        next.delete(roleId)
        return next
      })
    } catch (error) {
      // Revert optimistic update on failure
      setUserRoles(prev => {
        const next = new Set(prev)
        if (isCurrentlyAssigned) next.add(roleId) // Revert to assigned
        else next.delete(roleId) // Revert to unassigned
        return next
      })
      setProcessingRoles(prev => {
        const next = new Set(prev)
        next.delete(roleId)
        return next
      })
    }
  }

  if (!user) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Manage User Roles</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Assign or revoke roles for{' '}
            <span className="font-semibold text-[var(--color-primary)]">{user.displayName}</span>.
            Changes are saved immediately.
          </DialogDescription>
        </DialogHeader>

        <div className="py-4 space-y-4 max-h-[60vh] overflow-y-auto pr-2">
          {isLoadingRoles ? (
            <div className="flex justify-center py-6">
              <Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" />
            </div>
          ) : !roles?.length ? (
            <p className="text-center text-[var(--color-text-muted)] text-sm">No roles available.</p>
          ) : (
            <div className="space-y-3 border border-[var(--color-border)] p-4 rounded-md bg-[var(--color-surface-2)]">
              {roles.map((role) => {
                const isProcessing = processingRoles.has(role.id)
                return (
                  <div key={role.id} className="flex items-center space-x-2">
                    <Checkbox
                      id={`role-${role.id}`}
                      checked={userRoles.has(role.id)}
                      disabled={isProcessing}
                      onCheckedChange={() => handleToggleRole(role.id)}
                      className="border-[var(--color-primary)] data-[state=checked]:bg-[var(--color-primary)] data-[state=checked]:text-[var(--color-btn-text)]"
                    />
                    <label
                      htmlFor={`role-${role.id}`}
                      className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 text-[var(--color-text-primary)] cursor-pointer flex items-center gap-2"
                    >
                      {role.name}
                      {isProcessing && <Loader2 className="h-3 w-3 animate-spin text-[var(--color-text-muted)]" />}
                    </label>
                  </div>
                )
              })}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)] w-full sm:w-auto"
          >
            Done
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
