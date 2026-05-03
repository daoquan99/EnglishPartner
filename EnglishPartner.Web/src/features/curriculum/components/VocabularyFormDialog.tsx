'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/ui/form'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { useCreateVocabulary, useUpdateVocabulary } from '../api/useCurriculum'
import type { VocabularyResponse, TopicResponse } from '../types/curriculum.types'
import { Plus, X } from 'lucide-react'

const vocabSchema = z.object({
  word: z.string().min(1, 'Word is required'),
  definition: z.string().min(1, 'Definition is required'),
  languageCode: z.string().min(1, 'Language code is required'),
  pronunciation: z.string().nullable(),
  phoneticTranscription: z.string().nullable(),
  partOfSpeech: z.string().nullable(),
  level: z.string().nullable(),
  topicId: z.string().nullable(),
  synonyms: z.array(z.string()),
})

type VocabFormData = z.infer<typeof vocabSchema>

interface VocabularyFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  vocab?: VocabularyResponse | null
  defaultLanguageCode?: string
  topics: TopicResponse[]
}

export function VocabularyFormDialog({ open, onOpenChange, vocab, defaultLanguageCode = 'en', topics }: VocabularyFormDialogProps) {
  const isEditing = !!vocab
  const createMutation = useCreateVocabulary()
  const updateMutation = useUpdateVocabulary()
  const isPending = createMutation.isPending || updateMutation.isPending

  const [synonymInput, setSynonymInput] = useState('')

  const form = useForm<VocabFormData>({
    resolver: zodResolver(vocabSchema),
    defaultValues: {
      word: '',
      definition: '',
      languageCode: defaultLanguageCode,
      pronunciation: '',
      phoneticTranscription: '',
      partOfSpeech: '',
      level: '',
      topicId: '',
      synonyms: [],
    },
  })

  useEffect(() => {
    if (open) {
      if (vocab) {
        form.reset({
          word: vocab.word,
          definition: vocab.definition,
          languageCode: vocab.languageCode,
          pronunciation: vocab.pronunciation || '',
          phoneticTranscription: vocab.phoneticTranscription || '',
          partOfSpeech: vocab.partOfSpeech || '',
          level: vocab.level || '',
          topicId: vocab.topicId || '',
          synonyms: vocab.synonyms || [],
        })
      } else {
        form.reset({
          word: '',
          definition: '',
          languageCode: defaultLanguageCode,
          pronunciation: '',
          phoneticTranscription: '',
          partOfSpeech: '',
          level: '',
          topicId: '',
          synonyms: [],
        })
      }
      setSynonymInput('')
    }
  }, [open, vocab, defaultLanguageCode, form])

  const onSubmit = async (values: VocabFormData) => {
    const payload = {
      ...values,
      pronunciation: values.pronunciation || null,
      phoneticTranscription: values.phoneticTranscription || null,
      partOfSpeech: values.partOfSpeech || null,
      level: values.level === 'none' ? null : (values.level || null),
      topicId: values.topicId === 'none' ? null : (values.topicId || null),
    }

    if (isEditing && vocab) {
      await updateMutation.mutateAsync({ id: vocab.id, ...payload })
    } else {
      await createMutation.mutateAsync(payload)
    }
    onOpenChange(false)
  }

  const handleAddSynonym = () => {
    const trimmed = synonymInput.trim()
    if (!trimmed) return
    const current = form.getValues('synonyms')
    if (!current.includes(trimmed)) {
      form.setValue('synonyms', [...current, trimmed])
    }
    setSynonymInput('')
  }

  const handleRemoveSynonym = (syn: string) => {
    const current = form.getValues('synonyms')
    form.setValue('synonyms', current.filter(s => s !== syn))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Vocabulary' : 'Add New Vocabulary'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update word details below.' : 'Add a new word to the dictionary.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            
            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control as any}
                name="word"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Word <span className="text-red-500">*</span></FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. apple" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control as any}
                name="partOfSpeech"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Part of Speech</FormLabel>
                    <Select onValueChange={field.onChange} value={field.value || ''}>
                      <FormControl>
                        <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="e.g. noun" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="none">None</SelectItem>
                        <SelectItem value="noun">Noun (n)</SelectItem>
                        <SelectItem value="verb">Verb (v)</SelectItem>
                        <SelectItem value="adjective">Adjective (adj)</SelectItem>
                        <SelectItem value="adverb">Adverb (adv)</SelectItem>
                        <SelectItem value="pronoun">Pronoun (pron)</SelectItem>
                        <SelectItem value="preposition">Preposition (prep)</SelectItem>
                        <SelectItem value="conjunction">Conjunction (conj)</SelectItem>
                        <SelectItem value="interjection">Interjection (interj)</SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control as any}
              name="definition"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Definition <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <textarea 
                      placeholder="A round fruit with red or green skin..." 
                      {...field} 
                      className="flex min-h-[60px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control as any}
                name="pronunciation"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Pronunciation (IPA)</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. /ˈæp.əl/" {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control as any}
                name="phoneticTranscription"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Phonetic Transcription</FormLabel>
                    <FormControl>
                      <Input placeholder="e.g. ap-uhl" {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control as any}
                name="level"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>CEFR Level</FormLabel>
                    <Select onValueChange={field.onChange} value={field.value || ''}>
                      <FormControl>
                        <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="Select level" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="none">None</SelectItem>
                        <SelectItem value="A1">A1 - Beginner</SelectItem>
                        <SelectItem value="A2">A2 - Elementary</SelectItem>
                        <SelectItem value="B1">B1 - Intermediate</SelectItem>
                        <SelectItem value="B2">B2 - Upper Intermediate</SelectItem>
                        <SelectItem value="C1">C1 - Advanced</SelectItem>
                        <SelectItem value="C2">C2 - Mastery</SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control as any}
                name="topicId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Topic</FormLabel>
                    <Select onValueChange={field.onChange} value={field.value || ''}>
                      <FormControl>
                        <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="Select topic" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="none">No Topic</SelectItem>
                        {topics.map(topic => (
                          <SelectItem key={topic.id} value={topic.id}>{topic.title}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <div>
              <FormLabel className="block mb-2 text-sm font-medium">Synonyms</FormLabel>
              <div className="flex gap-2 mb-2">
                <Input 
                  placeholder="Add synonym..." 
                  value={synonymInput}
                  onChange={(e) => setSynonymInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      handleAddSynonym()
                    }
                  }}
                  className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
                />
                <Button type="button" onClick={handleAddSynonym} variant="secondary" className="shrink-0 bg-[var(--color-surface-2)]">
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
              <div className="flex flex-wrap gap-2">
                {form.watch('synonyms').map(syn => (
                  <div key={syn} className="flex items-center gap-1 bg-[var(--color-primary-dim)] text-[var(--color-primary)] px-2 py-1 rounded-md text-sm border border-[var(--color-primary)]/20">
                    {syn}
                    <button type="button" onClick={() => handleRemoveSynonym(syn)} className="hover:text-red-500 rounded-full p-0.5">
                      <X className="h-3 w-3" />
                    </button>
                  </div>
                ))}
              </div>
            </div>

            <DialogFooter className="pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]"
                disabled={isPending}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white"
                disabled={isPending}
              >
                {isPending ? 'Saving...' : 'Save Word'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
