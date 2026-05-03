import { ReactNode } from 'react'
import { Pagination } from './Pagination'

interface DataTableContainerProps {
  children: ReactNode
  pagination?: {
    page: number
    pageSize: number
    totalCount: number
    onPageChange: (page: number) => void
  }
  className?: string
}

export function DataTableContainer({ 
  children, 
  pagination,
  className
}: DataTableContainerProps) {
  return (
    <div className={`rounded-md border border-[var(--color-border)] bg-[var(--color-surface)] flex flex-col shadow-sm flex-1 min-h-0 ${className || ''}`}>
      <div className="overflow-auto relative flex-1 min-h-0">
        {children}
      </div>
      
      {pagination && pagination.totalCount > 0 && (
        <div className="border-t border-[var(--color-border)]">
          <Pagination
            page={pagination.page}
            pageSize={pagination.pageSize}
            totalCount={pagination.totalCount}
            onPageChange={pagination.onPageChange}
          />
        </div>
      )}
    </div>
  )
}
