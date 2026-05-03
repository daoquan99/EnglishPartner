import Link from 'next/link'
import { PartyPopper } from 'lucide-react'
import { Button } from '@/ui/button'

export function ReviewEmptyState() {
  return (
    <div className="flex flex-col items-center justify-center py-20 gap-4">
      <div className="flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-emerald-400 to-teal-500 text-white shadow-lg">
        <PartyPopper className="h-8 w-8" />
      </div>
      <h2 className="text-2xl font-bold text-[var(--color-text-primary)]">All caught up!</h2>
      <p className="text-[var(--color-text-secondary)] text-center max-w-sm">
        You have no items due for review right now. Great job keeping up with your studies!
      </p>
      <Button asChild variant="outline" className="border-[var(--color-border)] text-[var(--color-text-primary)]">
        <Link href="/dashboard">Back to Dashboard</Link>
      </Button>
    </div>
  )
}
