'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, Sparkles, FilterX, Filter } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Badge } from '@/ui/badge'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { useVocabularies, useDeleteVocabulary, useLanguages, useTopics } from '../api/useCurriculum'
import type { VocabularyResponse } from '../types/curriculum.types'
import { VocabularyFormDialog } from './VocabularyFormDialog'
import { AIGenerateVocabularyDialog } from './AIGenerateVocabularyDialog'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

export function VocabularyManagement() {
  const [languageCode, setLanguageCode] = useState('en')
  const [searchTerm, setSearchTerm] = useState('')
  const debouncedSearch = useDebounce(searchTerm, 500)
  
  const [page, setPage] = useState(1)
  const pageSize = 10
  
  const [selectedLevel, setSelectedLevel] = useState<string>('all')
  const [selectedTopicId, setSelectedTopicId] = useState<string>('all')

  const [formOpen, setFormOpen] = useState(false)
  const [aiGenerateOpen, setAiGenerateOpen] = useState(false)
  const [selectedVocab, setSelectedVocab] = useState<VocabularyResponse | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    vocabId: string | null
  }>({ open: false, title: '', description: '', vocabId: null })

  const { data: languages } = useLanguages()
  const { data: topics } = useTopics({ languageCode })

  // Reset page when filters change
  useEffect(() => {
    setPage(1)
  }, [languageCode, debouncedSearch, selectedLevel, selectedTopicId])

  // Auto-select first language
  useEffect(() => {
    if (languages && languages.length > 0 && !languages.some(l => l.code === languageCode)) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const { data: pagedData, isLoading } = useVocabularies({ 
    languageCode, 
    page, 
    pageSize, 
    searchTerm: debouncedSearch || undefined,
    level: selectedLevel !== 'all' ? selectedLevel : undefined,
    topicId: selectedTopicId !== 'all' ? selectedTopicId : undefined
  })
  
  const { mutate: deleteVocab } = useDeleteVocabulary()

  const handleOpenAdd = () => {
    setSelectedVocab(null)
    setFormOpen(true)
  }

  const handleOpenEdit = (vocab: VocabularyResponse) => {
    setSelectedVocab(vocab)
    setFormOpen(true)
  }

  const handleDelete = (id: string) => {
    setConfirmDialog({
      open: true,
      title: 'Delete Vocabulary',
      description: 'Are you sure you want to delete this word? Example sentences associated with it might be affected.',
      vocabId: id,
    })
  }

  const confirmDelete = () => {
    if (confirmDialog.vocabId) {
      deleteVocab(confirmDialog.vocabId)
    }
    setConfirmDialog(prev => ({ ...prev, open: false }))
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedLevel('all')
    setSelectedTopicId('all')
    setPage(1)
  }

  const vocabularies = pagedData?.items || []

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
                placeholder="Search word or definition..."
                value={searchTerm}
                onChange={e => setSearchTerm(e.target.value)}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]"
              />
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button onClick={() => setAiGenerateOpen(true)} className="bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white border-0">
              <Sparkles className="h-4 w-4 mr-2" />
              AI Generate
            </Button>
            <Button onClick={handleOpenAdd} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              <Plus className="h-4 w-4 mr-2" />
              Add Word
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-4 pt-4 border-t border-[var(--color-border)]">
          <div className="flex items-center gap-2 text-sm font-medium text-[var(--color-text-secondary)]">
            <Filter className="h-4 w-4" /> Filters:
          </div>

          <Select value={selectedTopicId} onValueChange={setSelectedTopicId}>
            <SelectTrigger className="w-[180px] bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]">
              <SelectValue placeholder="All Topics" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Topics</SelectItem>
              {topics?.map(topic => (
                <SelectItem key={topic.id} value={topic.id}>{topic.title}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={selectedLevel} onValueChange={setSelectedLevel}>
            <SelectTrigger className="w-[140px] bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]">
              <SelectValue placeholder="All Levels" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Levels</SelectItem>
              <SelectItem value="A1">A1</SelectItem>
              <SelectItem value="A2">A2</SelectItem>
              <SelectItem value="B1">B1</SelectItem>
              <SelectItem value="B2">B2</SelectItem>
              <SelectItem value="C1">C1</SelectItem>
              <SelectItem value="C2">C2</SelectItem>
            </SelectContent>
          </Select>

          {(searchTerm || selectedLevel !== 'all' || selectedTopicId !== 'all') && (
            <Button 
              variant="outline" 
              onClick={clearFilters}
              className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)] ml-auto"
              title="Clear filters"
            >
              <FilterX className="h-4 w-4 mr-2" /> Clear Filters
            </Button>
          )}
        </div>
      </div>

      {/* Data Table */}
      <DataTableContainer pagination={{ page, pageSize, totalCount: pagedData?.totalCount || 0, onPageChange: setPage }}>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)] sticky top-0 z-10 shadow-sm">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Word</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Definition</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">POS / Level</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={4} className="h-32 text-center">
                  <Loader2 className="mx-auto h-6 w-6 animate-spin text-[var(--color-primary)]" />
                </TableCell>
              </TableRow>
            ) : vocabularies.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-[var(--color-text-muted)]">
                  No vocabularies found.
                </TableCell>
              </TableRow>
            ) : (
              vocabularies.map(vocab => (
                <TableRow key={vocab.id} className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell>
                    <div className="font-medium text-[var(--color-text-primary)] text-base">{vocab.word}</div>
                    {vocab.pronunciation && (
                      <div className="text-sm text-[var(--color-text-secondary)] mt-0.5">
                        {vocab.pronunciation}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="text-[var(--color-text-secondary)] max-w-md line-clamp-2" title={vocab.definition}>
                      {vocab.definition}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-2 items-center">
                      {vocab.partOfSpeech && (
                        <Badge variant="outline" className="border-[var(--color-border)] text-[var(--color-text-secondary)] bg-[var(--color-surface-2)]">
                          {vocab.partOfSpeech}
                        </Badge>
                      )}
                      {vocab.level && (
                        <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary-dim)]">
                          {vocab.level}
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenEdit(vocab)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(vocab.id)}
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

      {/* Forms (Placeholders to be created next) */}
      <VocabularyFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        vocab={selectedVocab}
        defaultLanguageCode={languageCode}
        topics={topics || []}
      />

      <AIGenerateVocabularyDialog
        open={aiGenerateOpen}
        onOpenChange={setAiGenerateOpen}
        languageCode={languageCode}
        topics={topics || []}
      />

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={open => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        confirmLabel="Delete Word"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
