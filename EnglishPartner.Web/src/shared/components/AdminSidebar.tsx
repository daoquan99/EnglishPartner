'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { useState } from 'react'
import {
  LayoutDashboard, Users, ShieldCheck, Settings, LogOut,
  ChevronDown, ChevronRight, UserCog, BookOpen, GraduationCap,
} from 'lucide-react'
import { ThemeToggle } from '@/shared/components/ThemeToggle'
import { cn } from '@/shared/utils/cn'
import { useLogout } from '@/features/auth/api/useAuth'
import { useAuthStore } from '@/lib/zustand/auth.store'

interface NavItem {
  label: string
  href?: string
  icon: React.ReactNode
  requiredPermission?: string | string[]
  children?: { label: string; href: string; requiredPermission?: string | string[] }[]
}

const checkPermission = (required?: string | string[], userPermissions: string[] = []) => {
  if (!required) return true
  if (Array.isArray(required)) {
    return required.some(p => userPermissions.includes(p))
  }
  return userPermissions.includes(required)
}

const NAV_ITEMS: NavItem[] = [
  {
    label: 'Dashboard',
    href: '/admin',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    label: 'IAM',
    icon: <Users className="h-5 w-5" />,
    children: [
      { label: 'User Management', href: '/admin/users', requiredPermission: 'identity.users.manage' },
      { label: 'Role Management', href: '/admin/roles', requiredPermission: 'identity.roles.manage' },
    ],
  },
  {
    label: 'Curriculum',
    icon: <BookOpen className="h-5 w-5" />,
    children: [
      { 
        label: 'Overview', 
        href: '/admin/curriculum', 
        requiredPermission: [
          'curriculum.topics.write', 
          'curriculum.vocabulary.write',
          'curriculum.grammar.write',
          'curriculum.exercise.write',
          'curriculum.conversation.write'
        ] 
      },
      { label: 'Topics', href: '/admin/curriculum/topics', requiredPermission: 'curriculum.topics.write' },
      { label: 'Vocabularies', href: '/admin/curriculum/vocabularies', requiredPermission: 'curriculum.vocabulary.write' },
      { label: 'Grammars', href: '/admin/curriculum/grammars', requiredPermission: 'curriculum.grammar.write' },
      { label: 'Exercises', href: '/admin/curriculum/exercises', requiredPermission: 'curriculum.exercise.write' },
      { label: 'Conversations', href: '/admin/curriculum/conversations', requiredPermission: 'curriculum.conversation.write' },
      { label: 'Example Sentences', href: '/admin/curriculum/example-sentences', requiredPermission: 'curriculum.grammar.write' }, // Reusing grammar.write or another permission
      { label: 'Languages', href: '/admin/curriculum/languages', requiredPermission: 'curriculum.topics.write' },
    ],
  },
  {
    label: 'Learning',
    icon: <GraduationCap className="h-5 w-5" />,
    children: [
      { label: 'Management', href: '/admin/learning', requiredPermission: 'learning.admin.write' },
    ],
  },
  {
    label: 'Settings',
    href: '/admin/settings',
    icon: <Settings className="h-5 w-5" />,
  },
]

function NavItemRow({ item, userPermissions }: { item: NavItem; userPermissions: string[] }) {
  const pathname = usePathname()
  
  // Filter children based on permissions
  const filteredChildren = item.children?.filter(
    (child) => checkPermission(child.requiredPermission, userPermissions)
  )

  const isChildActive = filteredChildren?.some(c => pathname.startsWith(c.href))
  const [isOpen, setIsOpen] = useState(isChildActive ?? false)

  if (filteredChildren && filteredChildren.length > 0) {
    return (
      <div>
        <button
          onClick={() => setIsOpen(prev => !prev)}
          className={cn(
            'w-full flex items-center justify-between rounded-md px-3 py-2.5 text-sm font-medium transition-colors',
            isChildActive
              ? 'bg-[var(--color-primary-dim)] text-[var(--color-primary)]'
              : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)]'
          )}
        >
          <span className="flex items-center gap-3">
            {item.icon}
            {item.label}
          </span>
          {isOpen
            ? <ChevronDown className="h-4 w-4 shrink-0" />
            : <ChevronRight className="h-4 w-4 shrink-0" />
          }
        </button>

        {isOpen && (
          <div className="mt-1 ml-4 pl-4 border-l border-[var(--color-border)] space-y-1">
            {filteredChildren.map(child => (
              <Link
                key={child.href}
                href={child.href}
                className={cn(
                  'flex items-center rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  pathname === child.href
                    ? 'bg-[var(--color-primary-dim)] text-[var(--color-primary)]'
                    : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)]'
                )}
              >
                {child.label}
              </Link>
            ))}
          </div>
        )}
      </div>
    )
  }

  // If it's a parent with children but they are all filtered out, don't render the parent
  if (item.children && (!filteredChildren || filteredChildren.length === 0)) {
    return null
  }

  const isActive = item.href ? pathname === item.href : false

  return (
    <Link
      href={item.href!}
      className={cn(
        'flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium transition-colors',
        isActive
          ? 'bg-[var(--color-primary-dim)] text-[var(--color-primary)]'
          : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)]'
      )}
    >
      {item.icon}
      {item.label}
    </Link>
  )
}

export function AdminSidebar() {
  const { mutate: logout } = useLogout()
  const { user } = useAuthStore()
  const userPermissions = user?.permissions || []
  console.log(user)

  // Filter top-level items based on permissions
  const filteredNavItems = NAV_ITEMS.filter(
    (item) => checkPermission(item.requiredPermission, userPermissions)
  )

  return (
    <div className="flex h-screen w-64 shrink-0 flex-col border-r border-[var(--color-border)] bg-[var(--color-surface)]">
      <div className="flex h-16 items-center justify-between px-6 border-b border-[var(--color-border)]">
        <h2 className="text-lg font-bold text-[var(--color-text-primary)]">
          EP <span className="text-[var(--color-primary)]">Admin</span>
        </h2>
      </div>

      <div className="px-3 py-2 border-b border-[var(--color-border)]">
        <Link 
          href="/dashboard"
          className="w-full flex justify-center items-center gap-2 rounded-md px-3 py-2 text-sm font-medium border border-[var(--color-primary)] text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)] transition-colors"
        >
          View Learner App
        </Link>
      </div>

      <nav className="flex-1 overflow-y-auto py-4 px-3 space-y-1">
        {filteredNavItems.map(item => (
          <NavItemRow key={item.label} item={item} userPermissions={userPermissions} />
        ))}
      </nav>

      <div className="border-t border-[var(--color-border)] p-4 flex justify-between items-center">
        <button 
          onClick={() => logout()}
          className="flex items-center text-sm font-medium text-[var(--color-destructive)] hover:opacity-80 transition-opacity"
        >
          <LogOut className="mr-2 h-5 w-5" />
          Logout
        </button>
        <ThemeToggle />
      </div>
    </div>
  )
}
