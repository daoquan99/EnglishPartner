'use client'

import Link from 'next/link'
import { ThemeToggle } from '@/shared/components/ThemeToggle'
import { User, LogOut } from 'lucide-react'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/ui/dropdown-menu'
import { Button } from '@/ui/button'
import { useLogout } from '@/features/auth/api/useAuth'
import { useAuthStore } from '@/lib/zustand/auth.store'

export function LearnerNavbar() {
  const { mutate: logout } = useLogout()
  const isAdmin = useAuthStore(s => s.user?.isAdmin)

  return (
    <header className="sticky top-0 z-50 w-full border-b border-[var(--color-border)] bg-[var(--color-surface)]/80 backdrop-blur">
      <div className="container mx-auto flex h-16 max-w-7xl items-center justify-between px-4">
        <Link href="/dashboard" className="flex items-center space-x-2">
          <span className="text-xl font-bold text-[var(--color-text-primary)]">
            English<span className="text-[var(--color-primary)]">Partner</span>
          </span>
        </Link>
        
        <nav className="flex items-center space-x-4">
          <Link href="/dashboard" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Home
          </Link>
          <Link href="/curriculum" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Learn
          </Link>
          <Link href="/vocabulary" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Vocabulary
          </Link>
          <Link href="/grammar" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Grammar
          </Link>
          <Link href="/conversations" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Conversations
          </Link>
          <Link href="/sentences" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Sentences
          </Link>
          <Link href="/exercises" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Exercises
          </Link>
          <Link href="/review" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Review
          </Link>
          <Link href="/study" className="text-sm font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors">
            Study
          </Link>
          
          <div className="pl-4 flex items-center space-x-3 border-l border-[var(--color-border)]">
            <ThemeToggle />
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="rounded-full bg-[var(--color-surface-2)] text-[var(--color-text-primary)] hover:bg-[var(--color-primary-dim)] hover:text-[var(--color-primary)]">
                  <User className="h-5 w-5" />
                  <span className="sr-only">Toggle user menu</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56 bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                <DropdownMenuLabel>My Account</DropdownMenuLabel>
                <DropdownMenuSeparator className="bg-[var(--color-border)]" />
                <DropdownMenuItem asChild className="focus:bg-[var(--color-surface-2)] focus:text-[var(--color-text-primary)] cursor-pointer">
                  <Link href="/settings">Profile Settings</Link>
                </DropdownMenuItem>
                
                {/* For Admin users to go back to Admin Panel */}
                {isAdmin && (
                  <DropdownMenuItem asChild className="focus:bg-[var(--color-surface-2)] focus:text-[var(--color-text-primary)] cursor-pointer">
                    <Link href="/admin">Admin Panel</Link>
                  </DropdownMenuItem>
                )}

                <DropdownMenuSeparator className="bg-[var(--color-border)]" />
                <DropdownMenuItem 
                  onClick={() => logout()}
                  className="text-[var(--color-destructive)] focus:bg-[var(--color-destructive-dim)] focus:text-[var(--color-destructive)] cursor-pointer"
                >
                  <LogOut className="mr-2 h-4 w-4" />
                  Log out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </nav>
      </div>
    </header>
  )
}
