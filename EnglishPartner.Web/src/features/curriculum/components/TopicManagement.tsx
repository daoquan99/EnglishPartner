'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, Image as ImageIcon } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Badge } from '@/ui/badge'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { useTopics, useDeleteTopic, useLanguages } from '../api/useCurriculum'
import { TopicFormDialog } from './TopicFormDialog'
import type { TopicResponse } from '../types/curriculum.types'
import Image from 'next/image'
import { DataTableContainer } from '@/shared/components/DataTableContainer'
import { Pagination } from '@/shared/components/Pagination'

export function TopicManagement() {
  const [languageCode, setLanguageCode] = useState('en')
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedSearch = useDebounce(searchTerm, 500)

  const [formOpen, setFormOpen] = useState(false)
  const [selectedTopic, setSelectedTopic] = useState<TopicResponse | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    topicId: string | null
  }>({ open: false, title: '', description: '', topicId: null })

  const { data: languages } = useLanguages()
  const { data: topics, isLoading } = useTopics({ languageCode, searchTerm: debouncedSearch || undefined })
  const { mutate: deleteTopic } = useDeleteTopic()

  // Auto-select first language if not set and languages load
  useEffect(() => {
    if (languages && languages.length > 0 && !languages.some(l => l.code === languageCode)) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const handleOpenAdd = () => {
    setSelectedTopic(null)
    setFormOpen(true)
  }

  const handleOpenEdit = (topic: TopicResponse) => {
    setSelectedTopic(topic)
    setFormOpen(true)
  }

  const handleDelete = (id: string) => {
    setConfirmDialog({
      open: true,
      title: 'Delete Topic',
      description: 'Are you sure you want to delete this topic? This action cannot be undone and may orphan related vocabularies and grammars.',
      topicId: id,
    })
  }

  const confirmDelete = () => {
    if (confirmDialog.topicId) {
      deleteTopic(confirmDialog.topicId)
    }
    setConfirmDialog(prev => ({ ...prev, open: false }))
  }

  return (
    <div className="flex flex-col h-full gap-4">
      {/* Filters Toolbar */}
      <div className="shrink-0 flex flex-col gap-4 p-4 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex flex-1 flex-wrap items-center gap-4">
            <Select value={languageCode} onValueChange={setLanguageCode}>
              <SelectTrigger className="w-[180px] bg-[var(--color-input-bg)] border-[var(--color-border)]">
                <SelectValue placeholder="Select language" />
              </SelectTrigger>
              <SelectContent>
                {languages?.map(lang => (
                  <SelectItem key={lang.id} value={lang.code}>
                    {lang.name} ({lang.code})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <div className="flex-1 min-w-[200px]">
              <Input
                placeholder="Search topics..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button onClick={handleOpenAdd} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              <Plus className="h-4 w-4 mr-2" />
              Add Topic
            </Button>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <DataTableContainer>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)]">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
              <TableHead className="text-[var(--color-text-secondary)] w-16 text-center">Order</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] w-16"></TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Title</TableHead>
              <TableHead className="text-[var(--color-text-secondary)]">Level</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin text-[var(--color-primary)]" />
                </TableCell>
              </TableRow>
            ) : !topics || topics.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-[var(--color-text-muted)]">
                  No topics found.
                </TableCell>
              </TableRow>
            ) : (
              topics.map(topic => (
                <TableRow key={topic.id} className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell className="text-center font-medium text-[var(--color-text-secondary)]">
                    {topic.sortOrder}
                  </TableCell>
                  <TableCell>
                    {topic.imageUrl ? (
                      <div className="relative h-10 w-10 rounded overflow-hidden bg-[var(--color-surface-2)] border border-[var(--color-border)]">
                        <Image src={topic.imageUrl} alt={topic.title} fill className="object-cover" />
                      </div>
                    ) : (
                      <div className="h-10 w-10 rounded bg-[var(--color-surface-2)] border border-[var(--color-border)] flex items-center justify-center text-[var(--color-text-muted)]">
                        <ImageIcon className="h-5 w-5" />
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="font-medium text-[var(--color-text-primary)]">{topic.title}</div>
                    {topic.description && (
                      <div className="text-sm text-[var(--color-text-secondary)] line-clamp-1 mt-1">
                        {topic.description}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    {topic.level ? (
                      <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)]">
                        {topic.level}
                      </Badge>
                    ) : (
                      <span className="text-sm text-[var(--color-text-muted)]">—</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenEdit(topic)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(topic.id)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-destructive)] hover:bg-[var(--color-destructive)]/10"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </DataTableContainer>

      <TopicFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        topic={selectedTopic}
        defaultLanguageCode={languageCode}
      />

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={open => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        confirmLabel="Delete Topic"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
