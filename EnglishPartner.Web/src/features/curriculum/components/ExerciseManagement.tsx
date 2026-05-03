'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, ListChecks, FilterX, Settings, Sparkles } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { 
  useExercises, 
  useDeleteExercise, 
  useLanguages,
  useTopics,
  useVocabularies,
  useGrammars
} from '../api/useCurriculum'
import type { ExerciseResponse } from '../types/curriculum.types'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { ExerciseFormDialog } from './ExerciseFormDialog'
import { ExerciseOptionsManager } from './ExerciseOptionsManager'
import { AIGenerateExerciseDialog } from './AIGenerateExerciseDialog'
import { getExerciseTypeLabel } from '../constants/exercise-types'
import { VocabularyAsyncSelect } from './VocabularyAsyncSelect'
import { GrammarAsyncSelect } from './GrammarAsyncSelect'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

export function ExerciseManagement() {
  const [languageCode, setLanguageCode] = useState<string>('en')
  
  // Filter states
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState(searchTerm)
  const [page, setPage] = useState(1)
  const pageSize = 20

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchTerm)
    }, 500)
    return () => clearTimeout(handler)
  }, [searchTerm])
  const [selectedTopicId, setSelectedTopicId] = useState<string>('all')
  const [selectedVocabId, setSelectedVocabId] = useState<string>('all')
  const [selectedGrammarId, setSelectedGrammarId] = useState<string>('all')

  const { data: languages } = useLanguages()
  const { data: topics } = useTopics({ languageCode })
  const { data: vocabData } = useVocabularies({ languageCode, page: 1, pageSize: 10 })
  const { data: grammarsData } = useGrammars({ languageCode, page: 1, pageSize: 100 })
  const grammars = grammarsData?.items || []

  // Build API filter params
  const queryParams = {
    languageCode,
    page,
    pageSize,
    topicId: selectedTopicId === 'all' ? undefined : selectedTopicId,
    vocabularyId: selectedVocabId === 'all' || !selectedVocabId ? undefined : selectedVocabId,
    grammarId: selectedGrammarId === 'all' || !selectedGrammarId ? undefined : selectedGrammarId,
  }

  const { data: exercisesResponse, isLoading } = useExercises(queryParams)
  const exercises = exercisesResponse?.items || []
  const totalCount = exercisesResponse?.totalCount || 0
  const { mutate: deleteExercise } = useDeleteExercise()

  const vocabularies = vocabData?.items || []

  // Auto-select first language
  useEffect(() => {
    if (languages && languages.length > 0 && !languageCode) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const [formOpen, setFormOpen] = useState(false)
  const [selectedExercise, setSelectedExercise] = useState<ExerciseResponse | null>(null)
  
  const [optionsManagerOpen, setOptionsManagerOpen] = useState(false)
  const [optionsExerciseId, setOptionsExerciseId] = useState<string | null>(null)
  
  const [aiGenerateOpen, setAiGenerateOpen] = useState(false)
  
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [exerciseToDelete, setExerciseToDelete] = useState<string | null>(null)

  // Filter in memory for text search
  const filteredExercises = exercises.filter(e => {
    if (!debouncedSearch) return true
    const term = debouncedSearch.toLowerCase()
    return e.title.toLowerCase().includes(term) || 
           e.instruction.toLowerCase().includes(term) ||
           e.content?.toLowerCase().includes(term)
  })

  // Reset page when filters change
  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, selectedTopicId, selectedVocabId, selectedGrammarId, languageCode])

  const resetFilters = () => {
    setSelectedTopicId('all')
    setSelectedVocabId('all')
    setSelectedGrammarId('all')
    setSearchTerm('')
  }

  const handleEdit = (exercise: ExerciseResponse) => {
    setSelectedExercise(exercise)
    setFormOpen(true)
  }

  const confirmDelete = async () => {
    if (exerciseToDelete) {
      await deleteExercise(exerciseToDelete)
    }
    setDeleteConfirmOpen(false)
    setExerciseToDelete(null)
  }

  return (
    <div className="flex flex-col h-full gap-4">
      {/* Filters Toolbar */}
      <div className="shrink-0 flex flex-col gap-4 p-4 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg shadow-sm">
        <div className="flex flex-wrap items-center gap-4">
          <Select value={languageCode} onValueChange={setLanguageCode}>
            <SelectTrigger className="w-[180px] bg-[var(--color-input-bg)] border-[var(--color-border)]">
              <SelectValue placeholder="Select language" />
            </SelectTrigger>
            <SelectContent>
              {languages?.map(lang => (
                <SelectItem key={lang.id} value={lang.code}>{lang.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <div className="flex-1 min-w-[200px]">
            <Input 
              placeholder="Search by title or instruction..." 
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          <div className="flex items-center gap-2">
            <Button 
              variant="outline"
              onClick={() => setAiGenerateOpen(true)}
              className="bg-gradient-to-r from-indigo-500/10 to-purple-500/10 border-indigo-500/20 text-indigo-400 hover:text-indigo-300 hover:bg-indigo-500/20"
            >
              <Sparkles className="mr-2 h-4 w-4" />
              AI Generate
            </Button>
            <Button 
              onClick={() => { setSelectedExercise(null); setFormOpen(true) }}
              className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white"
            >
              <Plus className="mr-2 h-4 w-4" />
              Create Exercise
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-4">
          <div className="flex-1 min-w-[150px]">
            <Select value={selectedTopicId} onValueChange={setSelectedTopicId}>
              <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                <SelectValue placeholder="Filter by Topic" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Topics</SelectItem>
                {topics?.map(t => (
                  <SelectItem key={t.id} value={t.id}>{t.title}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex-1 min-w-[150px]">
            <VocabularyAsyncSelect
              languageCode={languageCode}
              value={selectedVocabId === 'all' ? null : selectedVocabId}
              onValueChange={(val) => setSelectedVocabId(val || 'all')}
              placeholder="Filter by Vocab"
            />
          </div>

          <div className="flex-1 min-w-[150px]">
            <GrammarAsyncSelect
              languageCode={languageCode}
              value={selectedGrammarId === 'all' ? null : selectedGrammarId}
              onValueChange={(val) => setSelectedGrammarId(val || 'all')}
              placeholder="Filter by Grammar"
            />
          </div>

          <Button 
            variant="outline" 
            onClick={resetFilters}
            className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]"
            title="Clear filters"
          >
            <FilterX className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Main Table */}
      <DataTableContainer pagination={{ page, pageSize, totalCount, onPageChange: setPage }}>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)] sticky top-0 z-10 shadow-sm">
            <TableRow className="border-b-[var(--color-border)] hover:bg-transparent">
              <TableHead className="w-[30%] font-semibold text-[var(--color-text-secondary)]">Title / Instruction</TableHead>
              <TableHead className="font-semibold text-[var(--color-text-secondary)]">Type</TableHead>
              <TableHead className="font-semibold text-[var(--color-text-secondary)]">Level / XP</TableHead>
              <TableHead className="font-semibold text-[var(--color-text-secondary)]">Associations</TableHead>
              <TableHead className="text-right font-semibold text-[var(--color-text-secondary)]">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center">
                  <div className="flex items-center justify-center text-[var(--color-text-secondary)]">
                    <Loader2 className="h-6 w-6 animate-spin mr-2" />
                    Loading exercises...
                  </div>
                </TableCell>
              </TableRow>
            ) : filteredExercises.length > 0 ? (
              filteredExercises.map((ex) => (
                <TableRow key={ex.id} className="border-b-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell>
                    <p className="font-medium text-[var(--color-text-primary)]">{ex.title}</p>
                    <p className="text-xs text-[var(--color-text-secondary)] truncate max-w-md">{ex.instruction}</p>
                    {ex.content && (
                      <p className="text-xs text-[var(--color-text-muted)] italic truncate max-w-md mt-1">"{ex.content}"</p>
                    )}
                  </TableCell>
                  <TableCell>
                    <span className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-[var(--color-primary-dim)] text-[var(--color-primary)]">
                      <ListChecks className="mr-1 h-3 w-3" />
                      {getExerciseTypeLabel(ex.type)}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-col gap-1">
                      {ex.level && (
                        <span className="text-xs font-semibold px-2 py-0.5 rounded bg-[var(--color-surface-2)] text-[var(--color-text-primary)] border border-[var(--color-border)] w-fit">
                          {ex.level}
                        </span>
                      )}
                      <span className="text-xs text-yellow-600 dark:text-yellow-500 font-medium">+{ex.xpReward} XP</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-col gap-1 text-xs text-[var(--color-text-secondary)]">
                      {ex.topicId && <span>Topic: {topics?.find(t => t.id === ex.topicId)?.title || '...'}</span>}
                      {ex.vocabularyId && <span>Vocab: {vocabularies.find(v => v.id === ex.vocabularyId)?.word || '...'}</span>}
                      {ex.grammarId && <span>Grammar: {grammars?.find(g => g.id === ex.grammarId)?.title || '...'}</span>}
                      {!ex.topicId && !ex.vocabularyId && !ex.grammarId && <span className="text-[var(--color-text-muted)] italic">None</span>}
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Button variant="ghost" size="sm" className="h-8 px-2 text-green-500 hover:text-green-600 hover:bg-green-500/10" onClick={() => { setOptionsExerciseId(ex.id); setOptionsManagerOpen(true) }} title="Manage Options">
                        <ListChecks className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-8 px-2 text-blue-500 hover:text-blue-600 hover:bg-blue-500/10" onClick={() => handleEdit(ex)}>
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-8 px-2 text-red-500 hover:text-red-600 hover:bg-red-500/10" onClick={() => { setExerciseToDelete(ex.id); setDeleteConfirmOpen(true) }}>
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-[var(--color-text-muted)]">
                  No exercises found matching your criteria.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </DataTableContainer>

      <ExerciseFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        exerciseId={selectedExercise?.id || null}
        defaultLanguageCode={languageCode}
        topics={topics || []}
      />

      <ExerciseOptionsManager
        open={optionsManagerOpen}
        onOpenChange={setOptionsManagerOpen}
        exerciseId={optionsExerciseId}
      />

      <AIGenerateExerciseDialog
        open={aiGenerateOpen}
        onOpenChange={setAiGenerateOpen}
        languageCode={languageCode}
      />

      <ConfirmActionDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title="Delete Exercise"
        description="Are you sure you want to delete this exercise? This action cannot be undone."
        confirmLabel="Delete"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
