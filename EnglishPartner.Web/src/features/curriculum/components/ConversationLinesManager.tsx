'use client'

import { useState, useEffect } from 'react'
import { Plus, GripVertical, Trash2, Edit, Volume2, Mic, Save, Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { 
  useConversation, 
  useAddConversationLine, 
  useUpdateConversationLine, 
  useDeleteConversationLine, 
  useReorderConversationLines,
  useLanguages,
  useGenerateLineAudio,
  useGenerateAllAudio
} from '../api/useCurriculum'
import type { ConversationLineResponse } from '../types/curriculum.types'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'

interface TranslationEntry {
  languageCode: string
  translatedText: string
}

interface ConversationLinesManagerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  conversationId: string | null
}

export function ConversationLinesManager({ open, onOpenChange, conversationId }: ConversationLinesManagerProps) {
  const { data: conversation, isLoading } = useConversation(conversationId || '')
  const { data: languages } = useLanguages()

  const [lines, setLines] = useState<ConversationLineResponse[]>([])
  const [draggedItemIndex, setDraggedItemIndex] = useState<number | null>(null)
  
  // Form states
  const [editingId, setEditingId] = useState<string | null>(null)
  const [speaker, setSpeaker] = useState('')
  const [text, setText] = useState('')
  const [translations, setTranslations] = useState<TranslationEntry[]>([])
  const [audioUrl, setAudioUrl] = useState('')

  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)
  const [lineToDelete, setLineToDelete] = useState<string | null>(null)

  const addLineMutation = useAddConversationLine()
  const updateLineMutation = useUpdateConversationLine()
  const deleteLineMutation = useDeleteConversationLine()
  const reorderMutation = useReorderConversationLines()
  const generateLineMutation = useGenerateLineAudio()
  const generateAllMutation = useGenerateAllAudio()

  useEffect(() => {
    if (conversation?.lines) {
      setLines([...conversation.lines].sort((a, b) => a.sortOrder - b.sortOrder))
    }
  }, [conversation?.lines])

  // Drag & drop handlers
  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedItemIndex(index)
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', index.toString())
  }

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    if (draggedItemIndex === null || draggedItemIndex === index) return

    const newLines = [...lines]
    const draggedItem = newLines[draggedItemIndex]
    newLines.splice(draggedItemIndex, 1)
    newLines.splice(index, 0, draggedItem)
    
    setDraggedItemIndex(index)
    setLines(newLines)
  }

  const handleDragEnd = async () => {
    setDraggedItemIndex(null)
    if (conversationId && lines.length > 0) {
      const orderedLineIds = lines.map(l => l.id)
      await reorderMutation.mutateAsync({ conversationId, orderedLineIds })
    }
  }

  const resetForm = () => {
    setEditingId(null)
    setSpeaker('')
    setText('')
    setTranslations([])
    setAudioUrl('')
  }

  const handleEdit = (line: ConversationLineResponse) => {
    setEditingId(line.id)
    setSpeaker(line.speaker)
    setText(line.text)
    setTranslations(
      line.translations?.map(t => ({ languageCode: t.languageCode, translatedText: t.translatedText })) || []
    )
    setAudioUrl(line.audioUrl || '')
  }

  const addTranslation = () => {
    const usedCodes = new Set(translations.map(t => t.languageCode))
    const available = languages?.find(l => !usedCodes.has(l.code))
    setTranslations(prev => [...prev, { languageCode: available?.code || '', translatedText: '' }])
  }

  const removeTranslation = (index: number) => {
    setTranslations(prev => prev.filter((_, i) => i !== index))
  }

  const updateTranslation = (index: number, field: keyof TranslationEntry, value: string) => {
    setTranslations(prev => prev.map((t, i) => i === index ? { ...t, [field]: value } : t))
  }

  const handleSave = async () => {
    if (!speaker.trim() || !text.trim() || !conversationId) return

    // Build translations dict: { "vi": "...", "ja": "..." }
    const validTranslations = translations.filter(t => t.languageCode && t.translatedText.trim())
    const translationsDict: Record<string, string> = {}
    validTranslations.forEach(t => { translationsDict[t.languageCode] = t.translatedText.trim() })

    const payload = {
      speaker: speaker.trim(),
      text: text.trim(),
      translations: Object.keys(translationsDict).length > 0 ? translationsDict : undefined,
      audioUrl: audioUrl.trim() || undefined,
    }

    if (editingId) {
      await updateLineMutation.mutateAsync({ 
        conversationId, 
        lineId: editingId, 
        ...payload 
      })
    } else {
      await addLineMutation.mutateAsync({ 
        conversationId, 
        ...payload 
      })
    }
    
    resetForm()
  }

  const confirmDelete = async () => {
    if (lineToDelete && conversationId) {
      await deleteLineMutation.mutateAsync({ conversationId, lineId: lineToDelete })
    }
    setDeleteConfirmOpen(false)
    setLineToDelete(null)
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) resetForm() }}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[900px] h-[90vh] flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="text-xl flex items-center gap-2">
            <Volume2 className="h-5 w-5 text-[var(--color-primary)]" />
            Dialogue Manager
          </DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Manage lines for <span className="font-semibold text-[var(--color-text-primary)]">{conversation?.title}</span>. Drag and drop to reorder.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col md:flex-row gap-6 flex-1 min-h-0 py-4">
          
          {/* Left Side: Draggable Lines List */}
          <div className="flex-1 border border-[var(--color-border)] rounded-lg bg-[var(--color-surface-2)] overflow-y-auto flex flex-col">
            <div className="p-3 border-b border-[var(--color-border)] bg-[var(--color-surface)] font-medium sticky top-0 z-10 flex justify-between items-center">
              <span>Lines ({lines.length})</span>
              <div className="flex items-center gap-2">
                {reorderMutation.isPending && <Loader2 className="h-4 w-4 animate-spin text-[var(--color-primary)]" />}
                {conversationId && lines.length > 0 && (
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={generateAllMutation.isPending}
                    onClick={() => generateAllMutation.mutate({ conversationId })}
                    className="h-7 text-xs border-[var(--color-border)] text-[var(--color-text-secondary)]"
                  >
                    {generateAllMutation.isPending ? <Loader2 className="h-3 w-3 mr-1 animate-spin" /> : <Volume2 className="h-3 w-3 mr-1" />}
                    Generate All Audio
                  </Button>
                )}
              </div>
            </div>
            
            {isLoading ? (
              <div className="flex-1 flex items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" />
              </div>
            ) : lines.length === 0 ? (
              <div className="flex-1 flex items-center justify-center text-[var(--color-text-muted)] text-sm italic">
                No lines found. Add your first line on the right.
              </div>
            ) : (
              <ul className="flex-1 p-2 space-y-2">
                {lines.map((line, index) => (
                  <li
                    key={line.id}
                    draggable
                    onDragStart={(e) => handleDragStart(e, index)}
                    onDragOver={(e) => handleDragOver(e, index)}
                    onDragEnd={handleDragEnd}
                    className={`
                      flex gap-3 p-3 rounded-md bg-[var(--color-surface)] border border-[var(--color-border)] shadow-sm
                      ${draggedItemIndex === index ? 'opacity-50 border-dashed border-[var(--color-primary)]' : ''}
                      ${editingId === line.id ? 'ring-1 ring-[var(--color-primary)]' : ''}
                      transition-all cursor-move group
                    `}
                  >
                    <div className="mt-1 flex-shrink-0 text-[var(--color-text-muted)] group-hover:text-[var(--color-text-primary)] cursor-grab active:cursor-grabbing">
                      <GripVertical className="h-5 w-5" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex justify-between items-start gap-2">
                        <span className="font-semibold text-[var(--color-primary)] text-sm px-2 py-0.5 bg-[var(--color-primary-dim)] rounded">
                          {line.speaker}
                        </span>
                        <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          {conversationId && (
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-6 w-6 text-emerald-500 hover:text-emerald-600 hover:bg-emerald-500/10"
                              disabled={generateLineMutation.isPending}
                              onClick={() => generateLineMutation.mutate({ conversationId, lineId: line.id })}
                              title={line.audioUrl ? 'Regenerate audio' : 'Generate audio'}
                            >
                              {generateLineMutation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Mic className="h-3.5 w-3.5" />}
                            </Button>
                          )}
                          <Button variant="ghost" size="icon" className="h-6 w-6 text-blue-500 hover:text-blue-600 hover:bg-blue-500/10" onClick={() => handleEdit(line)}>
                            <Edit className="h-3.5 w-3.5" />
                          </Button>
                          <Button variant="ghost" size="icon" className="h-6 w-6 text-red-500 hover:text-red-600 hover:bg-red-500/10" onClick={() => { setLineToDelete(line.id); setDeleteConfirmOpen(true) }}>
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </div>
                      <p className="mt-2 text-[var(--color-text-primary)]">{line.text}</p>
                      {line.translations && line.translations.length > 0 && (
                        <div className="mt-1.5 flex flex-wrap gap-1.5">
                          {line.translations.map(t => (
                            <span key={t.languageCode} className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs bg-[var(--color-surface-2)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                              <span className="font-medium text-[var(--color-primary)]">{t.languageCode}:</span> {t.translatedText}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Right Side: Form */}
          <div className="w-full md:w-[320px] flex-shrink-0 bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-lg p-4 flex flex-col gap-4 overflow-y-auto">
            <h3 className="font-semibold text-[var(--color-text-primary)] pb-2 border-b border-[var(--color-border)]">
              {editingId ? 'Edit Line' : 'Add New Line'}
            </h3>
            
            <div className="space-y-4">
              <div className="space-y-1.5">
                <Label>Speaker <span className="text-red-500">*</span></Label>
                <Input 
                  placeholder="e.g. Waiter, Customer..." 
                  value={speaker} 
                  onChange={e => setSpeaker(e.target.value)}
                  className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
                />
              </div>

              <div className="space-y-1.5">
                <Label>Original Text <span className="text-red-500">*</span></Label>
                <textarea 
                  placeholder="e.g. Good evening! Table for two?" 
                  value={text}
                  onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setText(e.target.value)}
                  className="flex min-h-[80px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border-[var(--color-border)] border ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 resize-none"
                />
              </div>

              {/* Translations Section */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label>Translations</Label>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={addTranslation}
                    className="h-7 text-xs bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[var(--color-primary)]"
                  >
                    <Plus className="h-3 w-3 mr-1" />
                    Add
                  </Button>
                </div>

                {translations.length === 0 ? (
                  <p className="text-xs text-[var(--color-text-muted)] italic">No translations yet.</p>
                ) : (
                  <div className="space-y-2">
                    {translations.map((t, index) => (
                      <div key={index} className="space-y-1.5">
                        <div className="flex items-center gap-2">
                          <Select
                            value={t.languageCode}
                            onValueChange={(val) => updateTranslation(index, 'languageCode', val)}
                          >
                            <SelectTrigger className="w-[100px] h-8 text-xs bg-[var(--color-input-bg)] border-[var(--color-border)]">
                              <SelectValue placeholder="Lang" />
                            </SelectTrigger>
                            <SelectContent>
                              {languages?.map(lang => (
                                <SelectItem key={lang.id} value={lang.code}>{lang.name}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            onClick={() => removeTranslation(index)}
                            className="h-7 w-7 text-[var(--color-text-secondary)] hover:text-[var(--color-destructive)] hover:bg-[var(--color-destructive)]/10 flex-shrink-0"
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                        <textarea
                          placeholder="Translated text..."
                          value={t.translatedText}
                          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => updateTranslation(index, 'translatedText', e.target.value)}
                          className="flex min-h-[60px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border-[var(--color-border)] border ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 resize-none"
                        />
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="space-y-1.5">
                <Label>Audio URL</Label>
                <div className="flex gap-2">
                  <Input 
                    placeholder="https://..." 
                    value={audioUrl}
                    onChange={e => setAudioUrl(e.target.value)}
                    className="bg-[var(--color-input-bg)] border-[var(--color-border)] flex-1"
                  />
                  <Button variant="outline" size="icon" className="flex-shrink-0 bg-transparent border-[var(--color-border)]" title="Auto-generate audio (Coming soon)">
                    <Mic className="h-4 w-4 text-[var(--color-text-secondary)]" />
                  </Button>
                </div>
              </div>
            </div>

            <div className="mt-auto pt-4 flex gap-2">
              {editingId && (
                <Button variant="outline" onClick={resetForm} className="flex-1 bg-transparent border-[var(--color-border)]">
                  Cancel
                </Button>
              )}
              <Button 
                onClick={handleSave} 
                disabled={!speaker.trim() || !text.trim() || addLineMutation.isPending || updateLineMutation.isPending}
                className="flex-1 bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white"
              >
                {(addLineMutation.isPending || updateLineMutation.isPending) ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <>
                    {editingId ? <Save className="h-4 w-4 mr-2" /> : <Plus className="h-4 w-4 mr-2" />}
                    {editingId ? 'Save' : 'Add Line'}
                  </>
                )}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>

      <ConfirmActionDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title="Delete Line"
        description="Are you sure you want to delete this line? This action cannot be undone."
        confirmLabel="Delete"
        isDestructive={true}
        onConfirm={confirmDelete}
      />
    </Dialog>
  )
}
