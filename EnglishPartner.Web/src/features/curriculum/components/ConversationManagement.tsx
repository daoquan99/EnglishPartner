'use client'

import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2, Sparkles, FilterX, Filter, Image as ImageIcon, List } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/ui/table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Badge } from '@/ui/badge'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { useConversations, useDeleteConversation, useLanguages, useTopics } from '../api/useCurriculum'
import type { ConversationResponse } from '../types/curriculum.types'
import { ConversationFormDialog } from './ConversationFormDialog'
import { AIGenerateConversationDialog } from './AIGenerateConversationDialog'
import { ConversationLinesManager } from './ConversationLinesManager'
import { DataTableContainer } from '@/shared/components/DataTableContainer'

export function ConversationManagement() {
  const [languageCode, setLanguageCode] = useState('en')
  const [searchTerm, setSearchTerm] = useState('')
  
  const [selectedLevel, setSelectedLevel] = useState<string>('all')
  const [selectedTopicId, setSelectedTopicId] = useState<string>('all')

  const [formOpen, setFormOpen] = useState(false)
  const [aiGenerateOpen, setAiGenerateOpen] = useState(false)
  const [linesManagerOpen, setLinesManagerOpen] = useState(false)
  const [selectedConversation, setSelectedConversation] = useState<ConversationResponse | null>(null)
  const [selectedLinesConversationId, setSelectedLinesConversationId] = useState<string | null>(null)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean
    title: string
    description: string
    conversationId: string | null
  }>({ open: false, title: '', description: '', conversationId: null })

  const { data: languages } = useLanguages()
  const { data: topics } = useTopics({ languageCode })
  const [page, setPage] = useState(1)
  const pageSize = 10
  const [debouncedSearch, setDebouncedSearch] = useState(searchTerm)

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchTerm)
    }, 500)
    return () => clearTimeout(handler)
  }, [searchTerm])

  const { data: conversationsResponse, isLoading } = useConversations({
    languageCode,
    page,
    pageSize,
    topicId: selectedTopicId !== 'all' ? selectedTopicId : undefined,
    level: selectedLevel !== 'all' ? selectedLevel : undefined,
    // Note: The API might not have a searchTerm parameter yet, but we'll include it if it's there. Wait, the specs don't list a searchTerm for conversations. Let's filter locally if possible, but the API is paginated now.
  })
  
  const conversations = conversationsResponse?.items || []
  const totalCount = conversationsResponse?.totalCount || 0
  const { mutate: deleteConversation } = useDeleteConversation()

  useEffect(() => {
    if (languages && languages.length > 0 && !languages.some(l => l.code === languageCode)) {
      setLanguageCode(languages[0].code)
    }
  }, [languages, languageCode])

  const handleOpenAdd = () => {
    setSelectedConversation(null)
    setFormOpen(true)
  }

  const handleOpenEdit = (conversation: ConversationResponse) => {
    setSelectedConversation(conversation)
    setFormOpen(true)
  }

  const handleDelete = (id: string) => {
    setConfirmDialog({
      open: true,
      title: 'Delete Conversation',
      description: 'Are you sure you want to delete this conversation and all its lines?',
      conversationId: id,
    })
  }

  const confirmDelete = () => {
    if (confirmDialog.conversationId) {
      deleteConversation(confirmDialog.conversationId)
    }
    setConfirmDialog(prev => ({ ...prev, open: false }))
  }

  const clearFilters = () => {
    setSearchTerm('')
    setSelectedLevel('all')
    setSelectedTopicId('all')
  }

  const filteredConversations = conversations.filter(c => {
    if (debouncedSearch) {
      const lowerSearch = debouncedSearch.toLowerCase()
      const titleMatch = c.title.toLowerCase().includes(lowerSearch)
      const scenarioMatch = c.scenario?.toLowerCase().includes(lowerSearch)
      if (!titleMatch && !scenarioMatch) return false
    }
    return true
  })

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, selectedLevel, selectedTopicId, languageCode])

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
                placeholder="Search title or scenario..."
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
              Add Conversation
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
      <DataTableContainer pagination={{ page, pageSize, totalCount, onPageChange: setPage }}>
        <Table>
          <TableHeader className="bg-[var(--color-surface-2)] sticky top-0 z-10 shadow-sm">
            <TableRow className="border-[var(--color-border)] hover:bg-transparent">
                <TableHead className="w-[80px] text-center text-[var(--color-text-secondary)] font-semibold">Image</TableHead>
                <TableHead className="text-[var(--color-text-secondary)] font-semibold">Title / Scenario</TableHead>
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
            ) : filteredConversations.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-[var(--color-text-muted)]">
                  No conversations found.
                </TableCell>
              </TableRow>
            ) : (
              filteredConversations.map(conv => (
                <TableRow key={conv.id} className="border-[var(--color-border)] hover:bg-[var(--color-surface-2)]">
                  <TableCell>
                    <div className="h-10 w-10 mx-auto rounded overflow-hidden bg-[var(--color-surface-2)] flex items-center justify-center border border-[var(--color-border)]">
                      {conv.imageUrl ? (
                        <img src={conv.imageUrl} alt={conv.title} className="object-cover h-full w-full" />
                      ) : (
                        <ImageIcon className="h-4 w-4 text-[var(--color-text-muted)]" />
                      )}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="font-medium text-[var(--color-text-primary)] text-base">{conv.title}</div>
                    {conv.scenario && (
                      <div className="text-sm text-[var(--color-text-secondary)] mt-0.5 line-clamp-1" title={conv.scenario}>
                        {conv.scenario}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>
                    {conv.level ? (
                      <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)] bg-[var(--color-primary-dim)]">
                        {conv.level}
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
                        onClick={() => { setSelectedLinesConversationId(conv.id); setLinesManagerOpen(true) }}
                        className="text-[var(--color-primary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                        title="Manage Lines"
                      >
                        <List className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleOpenEdit(conv)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
                        title="Edit Conversation"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleDelete(conv.id)}
                        className="text-[var(--color-text-secondary)] hover:text-[var(--color-destructive)] hover:bg-[var(--color-destructive)]/10"
                        title="Delete Conversation"
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

      <ConversationFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        conversation={selectedConversation}
        defaultLanguageCode={languageCode}
        topics={topics || []}
      />

      <AIGenerateConversationDialog
        open={aiGenerateOpen}
        onOpenChange={setAiGenerateOpen}
        languageCode={languageCode}
        topics={topics || []}
      />

      <ConversationLinesManager
        open={linesManagerOpen}
        onOpenChange={setLinesManagerOpen}
        conversationId={selectedLinesConversationId}
      />

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={open => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        confirmLabel="Delete Conversation"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </div>
  )
}
