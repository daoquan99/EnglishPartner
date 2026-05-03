'use client'

import { useState, useRef, useEffect } from 'react'
import { Check, ChevronsUpDown, Loader2, Search } from 'lucide-react'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { useInfiniteVocabularies } from '../api/useCurriculum'
import { useDebounce } from '@/shared/hooks/useDebounce'

interface VocabularyAsyncSelectProps {
  languageCode: string
  value: string | null
  onValueChange: (value: string | null) => void
  placeholder?: string
  className?: string
}

export function VocabularyAsyncSelect({
  languageCode,
  value,
  onValueChange,
  placeholder = 'Select vocabulary...',
  className = ''
}: VocabularyAsyncSelectProps) {
  const [open, setOpen] = useState(false)
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedSearch = useDebounce(searchTerm, 300)

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading,
  } = useInfiniteVocabularies({
    languageCode,
    searchTerm: debouncedSearch,
    pageSize: 20
  })

  // We need to resolve the selected value's label if it's already set
  // This is a bit tricky with infinite scroll if the item isn't loaded.
  // Ideally, the parent passes the initial label or we just show "Selected"
  const items = data?.pages.flatMap((page: any) => Array.isArray(page) ? page : (page?.items || [])) || []
  const selectedItem = items.find((item: any) => item.id === value)
  
  // Use a ref for detecting clicks outside to close the dropdown
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleScroll = (e: React.UIEvent<HTMLDivElement>) => {
    const bottom = e.currentTarget.scrollHeight - e.currentTarget.scrollTop <= e.currentTarget.clientHeight + 10
    if (bottom && hasNextPage && !isFetchingNextPage) {
      fetchNextPage()
    }
  }

  return (
    <div className={`relative ${className}`} ref={containerRef}>
      <Button
        type="button"
        variant="outline"
        onClick={() => setOpen(!open)}
        className="w-full justify-between bg-[var(--color-input-bg)] border-[var(--color-border)] hover:bg-[var(--color-surface-2)] font-normal text-left px-3 py-2 h-auto min-h-[40px]"
      >
        <span className={value ? "text-[var(--color-text-primary)]" : "text-muted-foreground"}>
          {selectedItem ? selectedItem.word : (value ? 'Vocabulary selected' : placeholder)}
        </span>
        <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
      </Button>

      {open && (
        <div className="absolute z-50 top-[calc(100%+4px)] left-0 w-full min-w-[200px] bg-[var(--color-surface)] border border-[var(--color-border)] rounded-md shadow-md overflow-hidden">
          <div className="flex items-center border-b border-[var(--color-border)] px-3">
            <Search className="mr-2 h-4 w-4 shrink-0 opacity-50" />
            <input
              className="flex h-10 w-full rounded-md bg-transparent py-3 text-sm outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
              placeholder="Search vocabulary..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              autoFocus
            />
          </div>
          <div 
            className="max-h-[250px] overflow-y-auto p-1"
            onScroll={handleScroll}
          >
            {isLoading ? (
              <div className="flex items-center justify-center p-4 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                Loading...
              </div>
            ) : items.length === 0 ? (
              <div className="p-4 text-center text-sm text-muted-foreground">
                No vocabularies found.
              </div>
            ) : (
              <div className="space-y-1">
                {value && !selectedItem && (
                  <div
                    className="flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none bg-[var(--color-primary-dim)] text-[var(--color-primary)] mb-1"
                  >
                    <Check className="mr-2 h-4 w-4" />
                    Selected ID: {value.substring(0, 8)}...
                  </div>
                )}
                
                {items.map(item => (
                  <div
                    key={item.id}
                    onClick={() => {
                      onValueChange(item.id === value ? null : item.id)
                      setOpen(false)
                    }}
                    className={`flex cursor-pointer select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none hover:bg-[var(--color-surface-2)] hover:text-[var(--color-text-primary)] ${
                      item.id === value ? 'bg-[var(--color-primary-dim)] text-[var(--color-primary)]' : ''
                    }`}
                  >
                    <Check className={`mr-2 h-4 w-4 ${item.id === value ? 'opacity-100' : 'opacity-0'}`} />
                    <div className="flex flex-col">
                      <span>{item.word}</span>
                      {item.definition && (
                        <span className="text-xs text-[var(--color-text-secondary)] line-clamp-1">{item.definition}</span>
                      )}
                    </div>
                  </div>
                ))}
                
                {isFetchingNextPage && (
                  <div className="flex items-center justify-center p-2 text-xs text-muted-foreground">
                    <Loader2 className="h-3 w-3 animate-spin mr-1" />
                    Loading more...
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
