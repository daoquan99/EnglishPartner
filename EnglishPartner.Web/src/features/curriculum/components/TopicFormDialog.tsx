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
import { useCreateTopic, useUpdateTopic } from '../api/useCurriculum'
import type { TopicResponse } from '../types/curriculum.types'

const topicSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  languageCode: z.string().min(1, 'Language code is required'),
  description: z.string().nullable(),
  level: z.string().nullable(),
  imageUrl: z.string().url('Invalid URL').nullable().or(z.literal('')),
  sortOrder: z.coerce.number().int().min(0),
})

type TopicFormData = z.infer<typeof topicSchema>

interface TopicFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  topic?: TopicResponse | null
  defaultLanguageCode?: string
}

export function TopicFormDialog({ open, onOpenChange, topic, defaultLanguageCode = 'en' }: TopicFormDialogProps) {
  const isEditing = !!topic
  const createMutation = useCreateTopic()
  const updateMutation = useUpdateTopic()
  const isPending = createMutation.isPending || updateMutation.isPending

  const form = useForm<TopicFormData>({
    resolver: zodResolver(topicSchema),
    defaultValues: {
      title: '',
      languageCode: defaultLanguageCode,
      description: '',
      level: '',
      imageUrl: '',
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (open) {
      if (topic) {
        form.reset({
          title: topic.title,
          languageCode: defaultLanguageCode, // Topic API doesn't return languageCode, we infer from parent
          description: topic.description || '',
          level: topic.level || '',
          imageUrl: topic.imageUrl || '',
          sortOrder: topic.sortOrder,
        })
      } else {
        form.reset({
          title: '',
          languageCode: defaultLanguageCode,
          description: '',
          level: '',
          imageUrl: '',
          sortOrder: 0,
        })
      }
    }
  }, [open, topic, defaultLanguageCode, form])

  const onSubmit = async (values: TopicFormData) => {
    // Convert empty strings to null
    const payload = {
      ...values,
      description: values.description || null,
      level: values.level || null,
      imageUrl: values.imageUrl || null,
    }

    if (isEditing && topic) {
      await updateMutation.mutateAsync({ id: topic.id, ...payload })
    } else {
      await createMutation.mutateAsync(payload)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Topic' : 'Add New Topic'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update the details of the topic below.' : 'Create a new topic for the curriculum.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Daily Routines" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Description</FormLabel>
                  <FormControl>
                    <textarea 
                      placeholder="Learn about everyday activities..." 
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
                name="sortOrder"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Sort Order</FormLabel>
                    <FormControl>
                      <Input type="number" min="0" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

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
                {isPending ? 'Saving...' : 'Save Topic'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
