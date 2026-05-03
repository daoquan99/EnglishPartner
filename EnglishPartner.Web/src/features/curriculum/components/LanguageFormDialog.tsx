'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/ui/form'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { useCreateLanguage, useUpdateLanguage } from '../api/useCurriculum'
import type { LanguageResponse } from '../types/curriculum.types'

const languageSchema = z.object({
  code: z.string().min(1, 'Code is required').max(10, 'Code is too long'),
  name: z.string().min(1, 'Name is required'),
  nativeName: z.string().min(1, 'Native name is required'),
  sortOrder: z.number().int().min(0),
})

type LanguageFormData = z.infer<typeof languageSchema>

interface LanguageFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  language?: LanguageResponse | null
}

export function LanguageFormDialog({ open, onOpenChange, language }: LanguageFormDialogProps) {
  const isEditing = !!language
  const createMutation = useCreateLanguage()
  const updateMutation = useUpdateLanguage()
  const isPending = createMutation.isPending || updateMutation.isPending

  const form = useForm<LanguageFormData>({
    resolver: zodResolver(languageSchema),
    defaultValues: {
      code: '',
      name: '',
      nativeName: '',
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (open) {
      if (language) {
        form.reset({
          code: language.code,
          name: language.name,
          nativeName: language.nativeName,
          sortOrder: language.sortOrder,
        })
      } else {
        form.reset({
          code: '',
          name: '',
          nativeName: '',
          sortOrder: 0,
        })
      }
    }
  }, [open, language, form])

  const onSubmit = async (values: LanguageFormData) => {
    if (isEditing && language) {
      // Code is typically not editable, but the API might allow it, or just use name/nativeName
      await updateMutation.mutateAsync({ id: language.id, ...values })
    } else {
      await createMutation.mutateAsync(values)
    }
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit Language' : 'Add Language'}</DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            {isEditing ? 'Update language details.' : 'Add a new language to the system.'}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            
            <FormField
              control={form.control}
              name="code"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Language Code <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. en, vi, ja" {...field} disabled={isEditing} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. English" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="nativeName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Native Name <span className="text-red-500">*</span></FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. English, Tiếng Việt" {...field} className="bg-[var(--color-input-bg)] border-[var(--color-border)]" />
                  </FormControl>
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
                {isPending ? 'Saving...' : (isEditing ? 'Save Changes' : 'Create Language')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
