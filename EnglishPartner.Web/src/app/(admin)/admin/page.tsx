'use client'

import Link from 'next/link'
import { Users, ShieldCheck } from 'lucide-react'
import { Card, CardDescription, CardHeader, CardTitle } from '@/ui/card'
import { useAuthStore } from '@/lib/zustand/auth.store'

export default function AdminPage() {
  const { user } = useAuthStore()
  const userPermissions = user?.permissions || []
  
  const canManageUsers = userPermissions.includes('identity.users.manage')
  const canManageRoles = userPermissions.includes('identity.roles.manage')

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">Admin Dashboard</h1>
        <p className="mt-1 text-[var(--color-text-secondary)]">
          Welcome to the EnglishPartner admin panel.
        </p>
      </div>

      {(canManageUsers || canManageRoles) && (
        <div>
          <h2 className="text-lg font-semibold text-[var(--color-text-primary)] mb-4">
            Identity & Access Management
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {canManageUsers && (
              <Link href="/admin/users">
                <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group">
                  <CardHeader>
                    <div className="flex items-center gap-3">
                      <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                        <Users className="h-5 w-5" />
                      </div>
                      <div>
                        <CardTitle className="text-[var(--color-text-primary)] text-base">User Management</CardTitle>
                        <CardDescription className="text-[var(--color-text-secondary)]">
                      View, activate, deactivate and manage users
                    </CardDescription>
                  </div>
                </div>
              </CardHeader>
            </Card>
          </Link>
            )}

            {canManageRoles && (
              <Link href="/admin/roles">
                <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group">
                  <CardHeader>
                    <div className="flex items-center gap-3">
                      <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                        <ShieldCheck className="h-5 w-5" />
                      </div>
                      <div>
                        <CardTitle className="text-[var(--color-text-primary)] text-base">Role Management</CardTitle>
                        <CardDescription className="text-[var(--color-text-secondary)]">
                          Configure roles and permission assignments
                        </CardDescription>
                      </div>
                    </div>
                  </CardHeader>
                </Card>
              </Link>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
