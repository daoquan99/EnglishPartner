'use client'

import { useAuthStore } from '@/lib/zustand/auth.store'
import { ShieldAlert } from 'lucide-react'

interface RequirePermissionProps {
  permission: string | string[]
  children: React.ReactNode
}

export function RequirePermission({ permission, children }: RequirePermissionProps) {
  const { user } = useAuthStore()
  const userPermissions = user?.permissions || []

  const hasPermission = Array.isArray(permission)
    ? permission.some(p => userPermissions.includes(p))
    : userPermissions.includes(permission)

  if (!hasPermission) {
    const requiredText = Array.isArray(permission) ? permission.join(' or ') : permission
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center">
        <div className="rounded-full bg-[var(--color-destructive)]/10 p-4 mb-4">
          <ShieldAlert className="h-10 w-10 text-[var(--color-destructive)]" />
        </div>
        <h2 className="text-2xl font-bold text-[var(--color-text-primary)] mb-2">Access Denied</h2>
        <p className="text-[var(--color-text-secondary)] max-w-md">
          You do not have the required permission (<code className="text-xs bg-[var(--color-surface-2)] px-1 rounded">{requiredText}</code>) to view this page.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
