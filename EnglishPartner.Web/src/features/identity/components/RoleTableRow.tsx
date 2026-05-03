'use client'

import { Edit2, Trash2, Shield } from 'lucide-react'
import { Button } from '@/ui/button'
import { Badge } from '@/ui/badge'
import { TableCell, TableRow } from '@/ui/table'
import type { RoleResponse } from '../types/admin.types'

interface RoleTableRowProps {
  role: RoleResponse
  onEdit: (role: RoleResponse) => void
  onManagePermissions: (role: RoleResponse) => void
  onDelete: (role: RoleResponse) => void
}

export function RoleTableRow({ role, onEdit, onManagePermissions, onDelete }: RoleTableRowProps) {
  return (
    <TableRow className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)] transition-colors">
      <TableCell className="font-medium text-[var(--color-text-primary)]">{role.name}</TableCell>
      <TableCell className="text-[var(--color-text-secondary)] text-sm">
        {role.description || <span className="text-[var(--color-text-muted)] italic">No description</span>}
      </TableCell>
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {role.permissions.length === 0 ? (
            <span className="text-sm text-[var(--color-text-muted)]">No permissions</span>
          ) : (
            <>
              {role.permissions.slice(0, 3).map(p => (
                <Badge key={p.id} variant="secondary" className="bg-[var(--color-surface-2)] border-[var(--color-border)] text-[var(--color-text-secondary)]">
                  {p.name}
                </Badge>
              ))}
              {role.permissions.length > 3 && (
                <Badge variant="secondary" className="bg-[var(--color-primary-dim)] text-[var(--color-primary)]">
                  +{role.permissions.length - 3} more
                </Badge>
              )}
            </>
          )}
        </div>
      </TableCell>
      <TableCell className="text-right">
        <div className="flex justify-end gap-1">
          <Button onClick={() => onManagePermissions(role)} variant="ghost" size="icon" className="h-8 w-8 text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]" title="Manage permissions">
            <Shield className="h-4 w-4" />
          </Button>
          <Button onClick={() => onEdit(role)} variant="ghost" size="icon" className="h-8 w-8 text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]" title="Edit role">
            <Edit2 className="h-4 w-4" />
          </Button>
          <Button onClick={() => onDelete(role)} variant="ghost" size="icon" className="h-8 w-8 text-[var(--color-text-secondary)] hover:text-[var(--color-destructive)] hover:bg-[var(--color-destructive-dim)]" title="Delete role">
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      </TableCell>
    </TableRow>
  )
}
