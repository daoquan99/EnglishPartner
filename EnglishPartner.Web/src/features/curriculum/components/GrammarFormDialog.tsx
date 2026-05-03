'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/ui/form'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Loader2 } from 'lucide-react'
import { useCreateGrammar, useUpdateGrammar, useGrammar } from '../api/useCurriculum'
import type { TopicResponse } from '../types/curriculum.types'

const grammarSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  explanation: z.string().nullable(),
  formula: z.string().nullable(),
  languageCode: z.string().min(1, 'Language code is required'),
  level: z.string().nullable(),
  topicId: z.string().nullable(),
})

type GrammarFormData = z.infer<typeof grammarSchema>

interface GrammarFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  grammarId?: string | null
  defaultLanguageCode?: string
  topics: TopicResponse[]
}

export function GrammarFormDialog({ open, onOpenChange, grammarId, defaultLanguageCode = 'en', topics }: GrammarFormDialogProps) {
  const isEditing = !!grammarId
  const { data: grammar, isLoading: isLoadingGrammar } = useGrammar(grammarId && open ? grammarId : '')
  const createMutation = useCreateGrammar()
  const updateMutation = useUpdateGrammar()
  const isPending = createMutation.isPending || updateMutation.isPending

  const form = useForm<GrammarFormData>({
    resolver: zodResolver(grammarSchema),
    defaultValues: {
      title: '',
      explanation: '',
      formula: '',
      languageCode: defaultLanguageCode,
      level: '',
      topicId: '',
    },
  })

  useEffect(() => {
    if (open) {
      if (grammar && isEditing) {
        form.reset({
          title: grammar.title,
          explanation: grammar.explanation || '',
          formula: grammar.formula || '',
          languageCode: grammar.languageCode,
          level: grammar.level || '',
          topicId: grammar.topicId || '',
        })
      } else if (!isEditing) {
        form.reset({
          title: '',
          explanation: '',
          formula: '',
          languageCode: defaultLanguageCode,
          level: '',
          topicId: '',
        })
      }
    }
  }, [open, grammar, isEditing, defaultLanguageCode, form])

  const onSubmit = async (values: GrammarFormData) => {
    const payload = {
      ...values,
      explanation: values.explanation || null,
      formula: values.formula || null,
      level: values.level === 'none' ? null : (values.level || null),
      topicId: values.topicId === 'none' ? null : (values.topicId || null),
    }

    if (isEditing && grammarId) {
      await updateMutation.mutateAsync({ id: grammarId, ...payload })
    } else {
      await createMutation.mutateAsync(payload)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Grammar' : 'Add New Grammar'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update grammar rule details below.' : 'Add a new grammar rule to the curriculum.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            {isEditing && isLoadingGrammar ? (
              <div className="flex justify-center py-8">
                <Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" />
              </div>
            ) : (
            <>
            
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title / Rule Name <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Present Simple" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="formula"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Formula</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. S + V(s/es) + O" {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)] font-mono text-sm" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="explanation"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Explanation</FormLabel>
                  <FormControl>
                    <textarea 
                      placeholder="Explain when and how to use this grammar..." 
                      {...field} 
                      value={field.value || ''}
                      className="flex min-h-[80px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring" 
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
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
            </>
            )}

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
                {isPending ? 'Saving...' : 'Save Grammar'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
