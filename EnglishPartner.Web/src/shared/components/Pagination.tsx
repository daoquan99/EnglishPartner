import { ChevronLeft, ChevronRight, MoreHorizontal } from 'lucide-react'
import { Button } from '@/ui/button'

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, pageSize, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  // If there's only 1 page, don't show pagination
  if (totalPages <= 1) return null

  const getPageNumbers = () => {
    const pages: (number | 'ellipsis')[] = []
    
    // Always show first page
    pages.push(1)

    if (page > 3) {
      pages.push('ellipsis')
    }

    // Show pages around current
    for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) {
      pages.push(i)
    }

    if (page < totalPages - 2) {
      pages.push('ellipsis')
    }

    // Always show last page
    if (totalPages > 1) {
      pages.push(totalPages)
    }

    return pages
  }

  const pages = getPageNumbers()

  return (
    <div className="flex items-center justify-between px-2 py-4">
      <div className="text-sm text-[var(--color-text-secondary)]">
        Showing <span className="font-medium">{(page - 1) * pageSize + 1}</span> to{' '}
        <span className="font-medium">{Math.min(page * pageSize, totalCount)}</span> of{' '}
        <span className="font-medium">{totalCount}</span> entries
      </div>
      
      <div className="flex items-center space-x-2">
        <Button
          variant="outline"
          size="sm"
          className="h-8 w-8 p-0 border-[var(--color-border)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-2)]"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          <span className="sr-only">Go to previous page</span>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        
        {pages.map((p, i) => {
          if (p === 'ellipsis') {
            return (
              <div key={`ellipsis-${i}`} className="flex h-8 w-8 items-center justify-center">
                <MoreHorizontal className="h-4 w-4 text-[var(--color-text-secondary)]" />
              </div>
            )
          }

          return (
            <Button
              key={p}
              variant={p === page ? "default" : "outline"}
              size="sm"
              className={`h-8 w-8 p-0 font-medium ${
                p === page 
                  ? 'bg-[var(--color-primary)] text-white hover:bg-[var(--color-primary-hover)] border-transparent' 
                  : 'border-[var(--color-border)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-2)]'
              }`}
              onClick={() => onPageChange(p)}
            >
              {p}
            </Button>
          )
        })}

        <Button
          variant="outline"
          size="sm"
          className="h-8 w-8 p-0 border-[var(--color-border)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-2)]"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          <span className="sr-only">Go to next page</span>
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  )
}
