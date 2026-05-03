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
import { useCreateConversation, useUpdateConversation } from '../api/useCurriculum'
import type { ConversationResponse, TopicResponse } from '../types/curriculum.types'

const conversationSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  languageCode: z.string().min(1, 'Language code is required'),
  scenario: z.string().nullable(),
  level: z.string().nullable(),
  imageUrl: z.string().nullable(),
  topicId: z.string().nullable(),
  grammarId: z.string().nullable(),
})

type ConversationFormData = z.infer<typeof conversationSchema>

interface ConversationFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  conversation?: ConversationResponse | null
  defaultLanguageCode?: string
  topics: TopicResponse[]
}

export function ConversationFormDialog({ open, onOpenChange, conversation, defaultLanguageCode = 'en', topics }: ConversationFormDialogProps) {
  const isEditing = !!conversation
  const createMutation = useCreateConversation()
  const updateMutation = useUpdateConversation()
  const isPending = createMutation.isPending || updateMutation.isPending

  const form = useForm<ConversationFormData>({
    resolver: zodResolver(conversationSchema),
    defaultValues: {
      title: '',
      scenario: '',
      languageCode: defaultLanguageCode,
      level: '',
      imageUrl: '',
      topicId: '',
      grammarId: '',
    },
  })

  useEffect(() => {
    if (open) {
      if (conversation) {
        form.reset({
          title: conversation.title,
          scenario: conversation.scenario || '',
          languageCode: conversation.languageCode,
          level: conversation.level || '',
          imageUrl: conversation.imageUrl || '',
          topicId: conversation.topicId || '',
          grammarId: conversation.grammarId || '',
        })
      } else {
        form.reset({
          title: '',
          scenario: '',
          languageCode: defaultLanguageCode,
          level: '',
          imageUrl: '',
          topicId: '',
          grammarId: '',
        })
      }
    }
  }, [open, conversation, defaultLanguageCode, form])

  const onSubmit = async (values: ConversationFormData) => {
    const payload = {
      ...values,
      scenario: values.scenario || null,
      imageUrl: values.imageUrl || null,
      level: values.level === 'none' ? null : (values.level || null),
      topicId: values.topicId === 'none' ? null : (values.topicId || null),
      grammarId: values.grammarId === 'none' ? null : (values.grammarId || null),
    }

    if (isEditing && conversation) {
      await updateMutation.mutateAsync({ id: conversation.id, ...payload })
    } else {
      await createMutation.mutateAsync(payload)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[600px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Conversation' : 'Add New Conversation'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update conversation details.' : 'Create a new conversation scenario.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. At the Restaurant" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="scenario"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Scenario</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Ordering food at a restaurant" {...field} value={field.value || ''} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="imageUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Image URL</FormLabel>
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
                {isPending ? 'Saving...' : 'Save Conversation'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
