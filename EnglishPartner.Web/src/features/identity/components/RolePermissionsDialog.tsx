'use client'

import { useState, useEffect } from 'react'
import { Loader2 } from 'lucide-react'

import { Button } from '@/ui/button'
import { Checkbox } from '@/ui/checkbox'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Badge } from '@/ui/badge'
import { usePermissions, useAddRolePermission, useRemoveRolePermission } from '../api/useAdmin'
import type { RoleResponse, PermissionResponse } from '../types/admin.types'

interface RolePermissionsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  role: RoleResponse | null
}

export function RolePermissionsDialog({ open, onOpenChange, role }: RolePermissionsDialogProps) {
  const { data: allPermissions, isLoading } = usePermissions()
  const { mutateAsync: addPermission } = useAddRolePermission()
  const { mutateAsync: removePermission } = useRemoveRolePermission()

  // Local state for optimistic UI updates
  const [activePermissions, setActivePermissions] = useState<Set<string>>(new Set())
  const [processingPermissions, setProcessingPermissions] = useState<Set<string>>(new Set())

  // Sync local state when dialog opens or role changes
  useEffect(() => {
    if (open && role) {
      setActivePermissions(new Set(role.permissions.map(p => p.id)))
      setProcessingPermissions(new Set())
    }
  }, [open, role])

  if (!role) return null

  // Group permissions by module
  const grouped = (allPermissions ?? []).reduce<Record<string, PermissionResponse[]>>((acc, perm) => {
    const module = perm.module || 'Other'
    if (!acc[module]) acc[module] = []
    acc[module].push(perm)
    return acc
  }, {})

  const handleToggle = async (permissionId: string) => {
    const isAssigned = activePermissions.has(permissionId)

    // Optimistic update
    setActivePermissions(prev => {
      const next = new Set(prev)
      if (isAssigned) next.delete(permissionId)
      else next.add(permissionId)
      return next
    })

    // Mark as processing
    setProcessingPermissions(prev => new Set(prev).add(permissionId))

    try {
      if (isAssigned) {
        await removePermission({ roleId: role.id, permissionId })
      } else {
        await addPermission({ roleId: role.id, permissionId })
      }
      
      // Success: Remove from processing
      setProcessingPermissions(prev => {
        const next = new Set(prev)
        next.delete(permissionId)
        return next
      })
    } catch (error) {
      // Revert optimistic update on failure
      setActivePermissions(prev => {
        const next = new Set(prev)
        if (isAssigned) next.add(permissionId) // Revert to assigned
        else next.delete(permissionId) // Revert to unassigned
        return next
      })
      
      setProcessingPermissions(prev => {
        const next = new Set(prev)
        next.delete(permissionId)
        return next
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[600px] max-h-[85vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Manage Permissions — {role.name}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Toggle permissions for this role. Changes are saved immediately.
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto pr-2 space-y-6 py-4">
          {isLoading ? (
            <div className="flex justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" />
            </div>
          ) : Object.keys(grouped).length === 0 ? (
            <p className="text-center text-[var(--color-text-muted)] py-8">No permissions available.</p>
          ) : (
            Object.entries(grouped).map(([module, permissions]) => (
              <div key={module} className="space-y-3">
                <div className="flex items-center gap-2">
                  <Badge className="bg-[var(--color-primary-dim)] text-[var(--color-primary)] border-0">
                    {module}
                  </Badge>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3 pl-2">
                  {permissions.map(perm => {
                    const isProcessing = processingPermissions.has(perm.id)
                    return (
                      <div key={perm.id} className="flex items-start space-x-2">
                        <Checkbox
                          id={`perm-${perm.id}`}
                          checked={activePermissions.has(perm.id)}
                          disabled={isProcessing}
                          onCheckedChange={() => handleToggle(perm.id)}
                          className="mt-0.5 border-[var(--color-primary)] data-[state=checked]:bg-[var(--color-primary)] data-[state=checked]:text-[var(--color-btn-text)]"
                        />
                        <label htmlFor={`perm-${perm.id}`} className="cursor-pointer flex items-center gap-2">
                          <span className="text-sm font-medium text-[var(--color-text-primary)]">{perm.name}</span>
                          {isProcessing && <Loader2 className="h-3 w-3 animate-spin text-[var(--color-text-muted)]" />}
                        </label>
                      </div>
                    )
                  })}
                </div>
              </div>
            ))
          )}
        </div>

        <div className="flex justify-end pt-2 border-t border-[var(--color-border)]">
          <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]">
            Done
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
