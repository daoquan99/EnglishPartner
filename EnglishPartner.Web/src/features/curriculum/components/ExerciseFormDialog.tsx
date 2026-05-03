'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/ui/form'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { useCreateExercise, useUpdateExercise, useLanguages, useExercise } from '../api/useCurriculum'
import type { TopicResponse } from '../types/curriculum.types'
import { EXERCISE_TYPES } from '../constants/exercise-types'
import { VocabularyAsyncSelect } from './VocabularyAsyncSelect'
import { GrammarAsyncSelect } from './GrammarAsyncSelect'

const exerciseSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  instruction: z.string().min(1, 'Instruction is required'),
  type: z.number().int(),
  correctAnswer: z.string().min(1, 'Correct answer is required'),
  languageCode: z.string().min(1, 'Language is required'),
  level: z.string().nullable(),
  topicId: z.string().nullable(),
  vocabularyId: z.string().nullable(),
  grammarId: z.string().nullable(),
  content: z.string().nullable(),
  explanation: z.string().nullable(),
  xpReward: z.number().int().min(0, 'XP must be positive'),
})

type ExerciseFormData = z.infer<typeof exerciseSchema>

interface ExerciseFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  exerciseId?: string | null
  defaultLanguageCode?: string
  topics: TopicResponse[]
}

export function ExerciseFormDialog({
  open,
  onOpenChange,
  exerciseId,
  defaultLanguageCode = 'en',
  topics,
}: ExerciseFormDialogProps) {
  const isEditing = !!exerciseId
  const { data: exercise, isLoading: isLoadingExercise } = useExercise(exerciseId || '')
  
  const createMutation = useCreateExercise()
  const updateMutation = useUpdateExercise()
  const { data: languages } = useLanguages()
  const isPending = createMutation.isPending || updateMutation.isPending || (isEditing && isLoadingExercise)

  const form = useForm<ExerciseFormData>({
    resolver: zodResolver(exerciseSchema),
    defaultValues: {
      title: '',
      instruction: '',
      type: 0,
      correctAnswer: '',
      languageCode: defaultLanguageCode,
      level: '',
      topicId: '',
      vocabularyId: '',
      grammarId: '',
      content: '',
      explanation: '',
      xpReward: 10,
    },
  })

  useEffect(() => {
    if (open) {
      if (exercise) {
        // Find numeric type value from string name returned by API
        const numericType = EXERCISE_TYPES.find(t => t.label.replace(/\s+/g, '').replace(/\//g, '') === exercise.type)?.value ?? 0
        
        form.reset({
          title: exercise.title,
          instruction: exercise.instruction,
          type: numericType,
          correctAnswer: exercise.correctAnswer,
          languageCode: exercise.languageCode,
          level: exercise.level || '',
          topicId: exercise.topicId || '',
          vocabularyId: exercise.vocabularyId || '',
          grammarId: exercise.grammarId || '',
          content: exercise.content || '',
          explanation: exercise.explanation || '',
          xpReward: exercise.xpReward,
        })
      } else if (!isEditing) {
        form.reset({
          title: '',
          instruction: '',
          type: 0,
          correctAnswer: '',
          languageCode: defaultLanguageCode,
          level: '',
          topicId: '',
          vocabularyId: '',
          grammarId: '',
          content: '',
          explanation: '',
          xpReward: 10,
        })
      }
    }
  }, [open, exercise, defaultLanguageCode, form])

  const onSubmit = async (values: ExerciseFormData) => {
    const payload = {
      ...values,
      level: values.level === 'none' ? null : (values.level || null),
      vocabularyId: values.vocabularyId === 'none' ? null : (values.vocabularyId || null),
      grammarId: values.grammarId === 'none' ? null : (values.grammarId || null),
      topicId: values.topicId === 'none' ? null : (values.topicId || null),
      content: values.content || null,
      explanation: values.explanation || null,
    }

    if (isEditing && exercise) {
      await updateMutation.mutateAsync({ id: exercise.id, ...payload })
    } else {
      await createMutation.mutateAsync(payload)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Exercise' : 'Add Exercise'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update exercise details below.' : 'Create a new exercise in the curriculum.'}
          </DialogDescription>
        </DialogHeader>

        {isEditing && isLoadingExercise ? (
          <div className="flex-1 flex justify-center items-center py-20">
            <Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" />
          </div>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Title <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Choose the correct verb form" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid grid-cols-2 gap-4">
              <FormField
                control={form.control}
                name="type"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Exercise Type <span className="text-red-500">*</span></FormLabel>
                    <Select 
                      onValueChange={(val) => field.onChange(parseInt(val, 10))} 
                      value={field.value.toString()}
                    >
                      <FormControl>
                        <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                          <SelectValue placeholder="Select type" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {EXERCISE_TYPES.map(type => (
                          <SelectItem key={type.value} value={type.value.toString()}>
                            {type.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="xpReward"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>XP Reward <span className="text-red-500">*</span></FormLabel>
                    <FormControl>
                      <Input 
                        type="number" 
                        {...field} 
                        onChange={e => field.onChange(parseInt(e.target.value, 10) || 0)}
                        className="bg-[var(--color-input-bg)] border-[var(--color-border)]" 
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="instruction"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Instruction <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <textarea
                      placeholder="e.g. Select the best answer for each question."
                      {...field}
                      className="flex min-h-[60px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="content"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Content (Question / Passage)</FormLabel>
                  <FormControl>
                    <textarea
                      placeholder="e.g. She ___ to school every day."
                      {...field}
                      value={field.value || ''}
                      className="flex min-h-[80px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="correctAnswer"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Correct Answer <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. goes" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
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
                      placeholder="e.g. 'Goes' is correct because the subject is third person singular."
                      {...field}
                      value={field.value || ''}
                      className="flex min-h-[60px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border border-[var(--color-border)] resize-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    />
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
                {isPending ? 'Saving...' : (isEditing ? 'Save Changes' : 'Create Exercise')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
        )}
      </DialogContent>
    </Dialog>
  )
}
