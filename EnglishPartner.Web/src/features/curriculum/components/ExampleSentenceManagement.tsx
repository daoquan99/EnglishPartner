'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, Sparkles, FilterX, Filter } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Badge } from '@/ui/badge'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import {
  useExampleSentences,
  useDeleteExampleSentence,
  useLanguages,
  useTopics,
  useVocabularies,
  useGrammars,
} from '../api/useCurriculum'
import type { ExampleSentenceResponse } from '../types/curriculum.types'
import { ExampleSentenceFormDialog } from './ExampleSentenceFormDialog'
import { AIGenerateExampleSentenceDialog } from './AIGenerateExampleSentenceDialog'
import { VocabularyAsyncSelect } from './VocabularyAsyncSelect'
import { GrammarAsyncSelect } from './GrammarAsyncSelect'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

export function ExampleSentenceManagement() {
  const [languageCode, setLanguageCode] = useState('en')
  const [searchTerm, setSearchTerm] = useState('')

  // Filter states matching API query params
  const [selectedTopicId, setSelectedTopicId] = useState<string>('all')
  const [selectedVocabId, setSelectedVocabId] = useState<string>('all')
  const [selectedGrammarId, setSelectedGrammarId] = useState<string>('all')

  const [formOpen, setFormOpen] = useState(false)
  const [aiGenerateOpen, setAiGenerateOpen] = useState(false)
  const [selectedSentence, setSelectedSentence] = useState<ExampleSentenceResponse | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    sentenceId: string | null
  }>({ open: false, title: '', description: '', sentenceId: null })

  const { data: languages } = useLanguages()
  const { data: topics } = useTopics({ languageCode })
  const { data: vocabData } = useVocabularies({ languageCode, page: 1, pageSize: 10 })
  const { data: grammarsData } = useGrammars({ languageCode, page: 1, pageSize: 10 })
  const grammars = grammarsData?.items || []

  // Pagination & Search
  const [page, setPage] = useState(1)
  const pageSize = 20
  const debouncedSearchTerm = useDebounce(searchTerm, 500)

  // Build API filter params
  const queryParams: { vocabularyId?: string; grammarId?: string; topicId?: string; languageCode: string; page: number; pageSize: number; searchTerm?: string } = { 
    languageCode, 
    page, 
    pageSize 
  }
  if (selectedTopicId !== 'all') queryParams.topicId = selectedTopicId
  if (selectedVocabId && selectedVocabId !== 'all') queryParams.vocabularyId = selectedVocabId
  if (selectedGrammarId && selectedGrammarId !== 'all') queryParams.grammarId = selectedGrammarId
  if (debouncedSearchTerm) queryParams.searchTerm = debouncedSearchTerm

  const { data: paginatedData, isLoading } = useExampleSentences(queryParams)
  const sentences = paginatedData?.items || []
  const { mutate: deleteSentence } = useDeleteExampleSentence()

  const vocabularies = vocabData?.items || []

  // Auto-select first language
  useEffect(() => {
    if (languages && languages.length > 0 && !languages.some(l => l.code === languageCode)) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const handleOpenAdd = () => {
    setSelectedSentence(null)
    setFormOpen(true)
  }

  const handleOpenEdit = (sentence: ExampleSentenceResponse) => {
    setSelectedSentence(sentence)
    setFormOpen(true)
  }

  const handleDelete = (id: string) => {
    setConfirmDialog({
      open: true,
      title: 'Delete Example Sentence',
      description: 'Are you sure you want to delete this sentence? This action cannot be undone.',
      sentenceId: id,
    })
  }

  const confirmDelete = () => {
    if (confirmDialog.sentenceId) {
      deleteSentence(confirmDialog.sentenceId)
    }
    setConfirmDialog(prev => ({ ...prev, open: false }))
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedTopicId('all')
    setSelectedVocabId('all')
    setSelectedGrammarId('all')
  }

  const hasFilters = searchTerm || selectedTopicId !== 'all' || selectedVocabId !== 'all' || selectedGrammarId !== 'all'

  // Client-side search is no longer needed since backend handles it
  const filteredSentences = sentences || []

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
                placeholder="Search text or note..."
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
              Add Sentence
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

          <div className="w-[180px]">
            <VocabularyAsyncSelect
              languageCode={languageCode}
              value={selectedVocabId === 'all' ? null : selectedVocabId}
              onValueChange={(val) => setSelectedVocabId(val || 'all')}
              placeholder="All Vocabularies"
            />
          </div>

          <div className="w-[180px]">
            <GrammarAsyncSelect
              languageCode={languageCode}
              value={selectedGrammarId === 'all' ? null : selectedGrammarId}
              onValueChange={(val) => setSelectedGrammarId(val || 'all')}
              placeholder="All Grammars"
            />
          </div>

          {hasFilters && (
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
      <DataTableContainer pagination={paginatedData ? { page, pageSize, totalCount: paginatedData.totalCount, onPageChange: setPage } : undefined}>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)] sticky top-0 z-10 shadow-sm">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
              <TableHead className="text-[var(--color-text-secondary)] font-semibold">Sentence</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] font-semibold">Level</TableHead>
              <TableHead className="text-[var(--color-text-secondary)] font-semibold">Translations</TableHead>
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
            ) : filteredSentences.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-[var(--color-text-muted)]">
                  No example sentences found.
                </TableCell>
              </TableRow>
            ) : (
              filteredSentences.map(sentence => (
                <TableRow key={sentence.id} className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell>
                    <div className="font-medium text-[var(--color-text-primary)] text-base">{sentence.text}</div>
                    {sentence.note && (
                      <div className="text-sm text-[var(--color-text-secondary)] mt-0.5 max-w-md line-clamp-1" title={sentence.note}>
                        {sentence.note}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    {sentence.level ? (
                      <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary-dim)]">
                        {sentence.level}
                      </Badge>
                    ) : (
                      <span className="text-sm text-[var(--color-text-muted)]">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    {sentence.translations && sentence.translations.length > 0 ? (
                      <div className="flex flex-wrap gap-1.5">
                        {sentence.translations.map(t => (
                          <span key={t.languageCode} className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs bg-[var(--color-surface-2)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                            <span className="font-medium text-[var(--color-primary)]">{t.languageCode}:</span> {t.translatedText}
                          </span>
                        ))}
                      </div>
                    ) : (
                      <span className="text-sm text-[var(--color-text-muted)]">—</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenEdit(sentence)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(sentence.id)}
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

      <ExampleSentenceFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        sentence={selectedSentence}
        defaultLanguageCode={languageCode}
        topics={topics || []}
      />

      <AIGenerateExampleSentenceDialog
        open={aiGenerateOpen}
        onOpenChange={setAiGenerateOpen}
        contextTitle=""
        contextFilters={queryParams}
        languageCode={languageCode}
      />

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={open => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        confirmLabel="Delete Sentence"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
