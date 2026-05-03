'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, Sparkles, FilterX, Filter } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Badge } from '@/ui/badge'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { useGrammars, useDeleteGrammar, useLanguages, useTopics } from '../api/useCurriculum'
import { GrammarFormDialog } from './GrammarFormDialog'
import { AIGenerateGrammarDialog } from './AIGenerateGrammarDialog'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

export function GrammarManagement() {
  const [languageCode, setLanguageCode] = useState('en')
  const [searchTerm, setSearchTerm] = useState('')
  
  const [selectedLevel, setSelectedLevel] = useState<string>('all')
  const [selectedTopicId, setSelectedTopicId] = useState<string>('all')

  const [formOpen, setFormOpen] = useState(false)
  const [aiGenerateOpen, setAiGenerateOpen] = useState(false)
  const [selectedGrammarId, setSelectedGrammarId] = useState<string | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    grammarId: string | null
  }>({ open: false, title: '', description: '', grammarId: null })

  const { data: languages } = useLanguages()
  const { data: topics } = useTopics({ languageCode })
  const { data: grammarsData, isLoading } = useGrammars({ languageCode, page: 1, pageSize: 100 })
  const grammars = grammarsData?.items || []
  const { mutate: deleteGrammar } = useDeleteGrammar()

  // Auto-select first language
  useEffect(() => {
    if (languages && languages.length > 0 && !languages.some(l => l.code === languageCode)) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const handleOpenAdd = () => {
    setSelectedGrammarId(null)
    setFormOpen(true)
  }

  const handleOpenEdit = (grammarId: string) => {
    setSelectedGrammarId(grammarId)
    setFormOpen(true)
  }

  const handleDelete = (id: string) => {
    setConfirmDialog({
      open: true,
      title: 'Delete Grammar Rule',
      description: 'Are you sure you want to delete this grammar rule? Associated examples and exercises may be affected.',
      grammarId: id,
    })
  }

  const confirmDelete = () => {
    if (confirmDialog.grammarId) {
      deleteGrammar(confirmDialog.grammarId)
    }
    setConfirmDialog(prev => ({ ...prev, open: false }))
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedLevel('all')
    setSelectedTopicId('all')
  }

  // Client-side filtering since API doesn't support server-side search/filters yet
  const filteredGrammars = grammars?.filter(g => {
    if (selectedLevel !== 'all' && g.level !== selectedLevel) return false
    if (selectedTopicId !== 'all' && g.topicId !== selectedTopicId) return false
    if (searchTerm) {
      const lowerSearch = searchTerm.toLowerCase()
      const titleMatch = g.title.toLowerCase().includes(lowerSearch)
      const formulaMatch = g.formula?.toLowerCase().includes(lowerSearch)
      if (!titleMatch && !formulaMatch) return false
    }
    return true
  }) || []

  // Client-side pagination
  const [page, setPage] = useState(1)
  const pageSize = 20
  const totalCount = filteredGrammars.length
  const paginatedGrammars = filteredGrammars.slice((page - 1) * pageSize, page * pageSize)

  useEffect(() => {
    setPage(1)
  }, [searchTerm, selectedLevel, selectedTopicId, languageCode])

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
                placeholder="Search grammar title or formula..."
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
              Add Grammar
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

          <Button 
            variant="outline" 
            onClick={clearFilters}
            className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)] ml-auto"
            title="Clear filters"
          >
            <FilterX className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Data Table */}
      <DataTableContainer pagination={{ page, pageSize, totalCount, onPageChange: setPage }}>
        <Table>
            <TableHeader className="bg-[var(--color-surface-2)] sticky top-0 z-10 shadow-sm">
              <TableRow className="border-[var(--color-border)] hover:bg-transparent">
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Rule / Title</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Formula</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Level</TableHead>
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
            ) : filteredGrammars.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-[var(--color-text-muted)]">
                  No grammar rules found.
                </TableCell>
              </TableRow>
            ) : (
              paginatedGrammars.map(grammar => (
                <TableRow key={grammar.id} className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell>
                    <div className="font-medium text-[var(--color-text-primary)] text-base">{grammar.title}</div>
                    {grammar.explanation && (
                      <div className="text-sm text-[var(--color-text-secondary)] mt-0.5 max-w-md line-clamp-1" title={grammar.explanation}>
                        {grammar.explanation}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="text-[var(--color-primary)] font-mono text-sm bg-[var(--color-primary-dim)] px-2 py-1 rounded inline-block">
                      {grammar.formula || '—'}
                    </div>
                  </TableCell>
                  <TableCell>
                    {grammar.level ? (
                      <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary-dim)]">
                        {grammar.level}
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
                        onClick={() => handleOpenEdit(grammar.id)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(grammar.id)}
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

      <GrammarFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        grammarId={selectedGrammarId}
        defaultLanguageCode={languageCode}
        topics={topics || []}
      />

      <AIGenerateGrammarDialog
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
        confirmLabel="Delete Grammar"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
