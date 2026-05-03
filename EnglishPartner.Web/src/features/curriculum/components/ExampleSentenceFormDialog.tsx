'use client'

import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Trash2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/ui/form'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { useCreateExampleSentence, useUpdateExampleSentence, useLanguages } from '../api/useCurriculum'
import type { ExampleSentenceResponse, TopicResponse } from '../types/curriculum.types'
import { VocabularyAsyncSelect } from './VocabularyAsyncSelect'
import { GrammarAsyncSelect } from './GrammarAsyncSelect'

const exampleSentenceSchema = z.object({
  text: z.string().min(1, 'Sentence text is required'),
  languageCode: z.string().min(1, 'Language is required'),
  audioUrl: z.string().nullable(),
  note: z.string().nullable(),
  level: z.string().nullable(),
  vocabularyId: z.string().nullable(),
  grammarId: z.string().nullable(),
  topicId: z.string().nullable(),
})

type ExampleSentenceFormData = z.infer<typeof exampleSentenceSchema>

interface TranslationEntry {
  languageCode: string
  translatedText: string
}

interface ExampleSentenceFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  sentence?: ExampleSentenceResponse | null
  defaultLanguageCode?: string
  topics: TopicResponse[]
}

export function ExampleSentenceFormDialog({
  open,
  onOpenChange,
  sentence,
  defaultLanguageCode = 'en',
  topics,
}: ExampleSentenceFormDialogProps) {
  const isEditing = !!sentence
  const createMutation = useCreateExampleSentence()
  const updateMutation = useUpdateExampleSentence()
  const { data: languages } = useLanguages()
  const isPending = createMutation.isPending || updateMutation.isPending

  // Translations state (managed outside react-hook-form for dynamic list)
  const [translations, setTranslations] = useState<TranslationEntry[]>([])

  const form = useForm<ExampleSentenceFormData>({
    resolver: zodResolver(exampleSentenceSchema),
    defaultValues: {
      text: '',
      languageCode: defaultLanguageCode,
      audioUrl: '',
      note: '',
      level: '',
      vocabularyId: '',
      grammarId: '',
      topicId: '',
    },
  })

  useEffect(() => {
    if (open) {
      if (sentence) {
        form.reset({
          text: sentence.text,
          languageCode: defaultLanguageCode,
          audioUrl: sentence.audioUrl || '',
          note: sentence.note || '',
          level: sentence.level || '',
          vocabularyId: '',
          grammarId: '',
          topicId: '',
        })
        setTranslations(
          sentence.translations?.map(t => ({ languageCode: t.languageCode, translatedText: t.translatedText })) || []
        )
      } else {
        form.reset({
          text: '',
          languageCode: defaultLanguageCode,
          audioUrl: '',
          note: '',
          level: '',
          vocabularyId: '',
          grammarId: '',
          topicId: '',
        })
        setTranslations([])
      }
    }
  }, [open, sentence, defaultLanguageCode, form])

  const addTranslation = () => {
    // Find the first language not already used
    const usedCodes = new Set(translations.map(t => t.languageCode))
    const currentLang = form.getValues('languageCode')
    const available = languages?.find(l => l.code !== currentLang && !usedCodes.has(l.code))
    setTranslations(prev => [...prev, { languageCode: available?.code || '', translatedText: '' }])
  }

  const removeTranslation = (index: number) => {
    setTranslations(prev => prev.filter((_, i) => i !== index))
  }

  const updateTranslation = (index: number, field: keyof TranslationEntry, value: string) => {
    setTranslations(prev => prev.map((t, i) => i === index ? { ...t, [field]: value } : t))
  }

  const onSubmit = async (values: ExampleSentenceFormData) => {
    // Filter out empty translations and convert to dictionary format
    const validTranslations = translations.filter(t => t.languageCode && t.translatedText.trim())
    const translationsDict: Record<string, string> = {}
    validTranslations.forEach(t => {
      translationsDict[t.languageCode] = t.translatedText.trim()
    })

    const basePayload = {
      text: values.text,
      audioUrl: values.audioUrl || null,
      note: values.note || null,
      level: values.level === 'none' ? null : (values.level || null),
      vocabularyId: values.vocabularyId === 'none' ? null : (values.vocabularyId || null),
      grammarId: values.grammarId === 'none' ? null : (values.grammarId || null),
      topicId: values.topicId === 'none' ? null : (values.topicId || null),
      translations: Object.keys(translationsDict).length > 0 ? translationsDict : null,
    }

    if (isEditing && sentence) {
      await updateMutation.mutateAsync({ id: sentence.id, ...basePayload })
    } else {
      await createMutation.mutateAsync({ ...basePayload, languageCode: values.languageCode })
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[650px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Example Sentence' : 'Add Example Sentence'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update sentence details below.' : 'Add a new example sentence to the curriculum.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">

            <FormField
              control={form.control}
              name="text"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Sentence Text <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <textarea
                      placeholder="e.g. She goes to school every day."
                      {...field}
                      className="flex min-h-[80px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="note"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Note / Context</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Present Simple with third person" {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="audioUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Audio URL</FormLabel>
                  <FormControl>
                    <Input placeholder="https://..." {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="languageCode"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Language <span className="text-red-500">*</span></FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="Select language" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {languages?.map(lang => (
                          <SelectItem key={lang.id} value={lang.code}>{lang.name} ({lang.code})</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
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
            </div>

            <div className="grid grid-cols-3 gap-4">
              <FormField
                control={form.control}
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
                        <SelectItem value="none">None</SelectItem>
                        {topics.map(topic => (
                          <SelectItem key={topic.id} value={topic.id}>{topic.title}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="vocabularyId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Vocabulary</FormLabel>
                    <FormControl>
                      <VocabularyAsyncSelect
                        languageCode={form.watch('languageCode')}
                        value={field.value === 'none' ? null : field.value}
                        onValueChange={(val) => field.onChange(val || 'none')}
                        placeholder="Select vocab"
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="grammarId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Grammar</FormLabel>
                    <FormControl>
                      <GrammarAsyncSelect
                        languageCode={form.watch('languageCode')}
                        value={field.value === 'none' ? null : field.value}
                        onValueChange={(val) => field.onChange(val || 'none')}
                        placeholder="Select grammar"
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            {/* Translations Section */}
            <div className="space-y-3 pt-2">
              <div className="flex items-center justify-between">
                <FormLabel className="text-sm font-medium">Translations</FormLabel>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={addTranslation}
                  className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)] hover:text-[var(--color-primary)]"
                >
                  <Plus className="h-3.5 w-3.5 mr-1" />
                  Add Translation
                </Button>
              </div>

              {translations.length === 0 ? (
                <p className="text-sm text-[var(--color-text-muted)] italic">No translations added yet.</p>
              ) : (
                <div className="space-y-2">
                  {translations.map((t, index) => (
                    <div key={index} className="flex items-center gap-2">
                      <Select
                        value={t.languageCode}
                        onValueChange={(val) => updateTranslation(index, 'languageCode', val)}
                      >
                        <SelectTrigger className="w-[130px] bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="Lang" />
                        </SelectTrigger>
                        <SelectContent>
                          {languages?.filter(l => l.code !== form.getValues('languageCode')).map(lang => (
                            <SelectItem key={lang.id} value={lang.code}>{lang.name}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <Input
                        placeholder="Translated text..."
                        value={t.translatedText}
                        onChange={(e) => updateTranslation(index, 'translatedText', e.target.value)}
                        className="flex-1 bg-[var(--color-input-bg)] border-[var(--color-border)]"
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeTranslation(index)}
                        className="h-8 w-8 text-[var(--color-text-secondary)] hover:text-[var(--color-destructive)] hover:bg-[var(--color-destructive)]/10 flex-shrink-0"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  ))}
                </div>
              )}
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
                {isPending ? 'Saving...' : (isEditing ? 'Save Changes' : 'Create Sentence')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
