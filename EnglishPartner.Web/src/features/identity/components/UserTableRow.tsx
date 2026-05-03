'use client'

import { Shield, UserX, UserCheck, KeySquare, Trash2, MoreHorizontal } from 'lucide-react'
import { Button } from '@/ui/button'
import { Badge } from '@/ui/badge'
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/ui/dropdown-menu'
import { TableCell, TableRow } from '@/ui/table'
import type { AdminUserResponse } from '../types/admin.types'

interface UserTableRowProps {
  user: AdminUserResponse
  onEdit: (user: AdminUserResponse) => void
  onManageRole: (user: AdminUserResponse) => void
  onResetPassword: (id: string) => void
  onActivate: (id: string) => void
  onDeactivate: (id: string) => void
  onDelete: (id: string) => void
}

export function UserTableRow({
  user,
  onEdit,
  onManageRole,
  onResetPassword,
  onActivate,
  onDeactivate,
  onDelete,
}: UserTableRowProps) {
  return (
    <TableRow className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)] transition-colors">
      <TableCell className="font-medium text-[var(--color-text-primary)]">
        {user.displayName}
      </TableCell>

      <TableCell className="text-[var(--color-text-secondary)]">
        {user.email}
      </TableCell>

      <TableCell>
        <div className="flex flex-wrap gap-1">
          {user.roles && user.roles.length > 0 ? (
            user.roles.map(r => (
              <Badge
                key={r.id || r.name}
                variant="outline"
                className="border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary-dim)]"
              >
                {r.name}
              </Badge>
            ))
          ) : (
            <span className="text-sm text-[var(--color-text-muted)]">Learner</span>
          )}
        </div>
      </TableCell>

      <TableCell>
        {user.isActive ? (
          <Badge className="bg-[var(--color-success)] text-white hover:bg-[var(--color-success)]/80">
            Active
          </Badge>
        ) : (
          <Badge variant="secondary" className="bg-[var(--color-surface-2)] text-[var(--color-text-muted)]">
            Inactive
          </Badge>
        )}
      </TableCell>

      <TableCell className="text-right">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              className="h-8 w-8 p-0 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
            >
              <span className="sr-only">Open menu</span>
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            align="end"
            className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]"
          >
            <DropdownMenuLabel className="text-[var(--color-text-secondary)]">Actions</DropdownMenuLabel>
            <DropdownMenuSeparator className="bg-[var(--color-border)]" />

            <DropdownMenuItem
              onClick={() => onEdit(user)}
              className="focus:bg-[var(--color-surface-2)] cursor-pointer"
            >
              Edit User
            </DropdownMenuItem>

            <DropdownMenuItem
              onClick={() => onManageRole(user)}
              className="focus:bg-[var(--color-surface-2)] cursor-pointer"
            >
              <Shield className="mr-2 h-4 w-4" /> Manage Roles
            </DropdownMenuItem>

            <DropdownMenuItem
              onClick={() => onResetPassword(user.id)}
              className="focus:bg-[var(--color-surface-2)] cursor-pointer"
            >
              <KeySquare className="mr-2 h-4 w-4" /> Reset Password
            </DropdownMenuItem>

            <DropdownMenuSeparator className="bg-[var(--color-border)]" />

            {user.isActive ? (
              <DropdownMenuItem
                onClick={() => onDeactivate(user.id)}
                className="text-[var(--color-destructive)] focus:bg-[var(--color-destructive-dim)] focus:text-[var(--color-destructive)] cursor-pointer"
              >
                <UserX className="mr-2 h-4 w-4" /> Deactivate
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                onClick={() => onActivate(user.id)}
                className="text-[var(--color-success)] focus:bg-[var(--color-success)]/10 focus:text-[var(--color-success)] cursor-pointer"
              >
                <UserCheck className="mr-2 h-4 w-4" /> Activate
              </DropdownMenuItem>
            )}

            <DropdownMenuItem
              onClick={() => onDelete(user.id)}
              className="text-[var(--color-destructive)] focus:bg-[var(--color-destructive-dim)] focus:text-[var(--color-destructive)] cursor-pointer"
            >
              <Trash2 className="mr-2 h-4 w-4" /> Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  )
}
