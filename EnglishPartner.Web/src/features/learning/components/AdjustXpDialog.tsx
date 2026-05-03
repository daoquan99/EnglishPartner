'use client'

import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/ui/dialog'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { Loader2 } from 'lucide-react'
import { useAdjustXp } from '../api/useLearning'

interface AdjustXpDialogProps {
  userId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface FormValues {
  amount: number
  reason: string
}

export function AdjustXpDialog({ userId, open, onOpenChange }: AdjustXpDialogProps) {
  const { mutate: adjustXp, isPending } = useAdjustXp()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
    defaultValues: { amount: 0, reason: '' },
  })

  const onSubmit = (values: FormValues) => {
    adjustXp(
      { userId, amount: values.amount, reason: values.reason },
      { onSuccess: () => { reset(); onOpenChange(false) } }
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[400px] bg-[var(--color-surface)] border-[var(--color-border)]">
        <DialogHeader>
          <DialogTitle className="text-[var(--color-text-primary)]">Adjust XP</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label className="text-[var(--color-text-secondary)]">Amount (positive to add, negative to subtract)</Label>
            <Input
              type="number"
              {...register('amount', { required: true, valueAsNumber: true })}
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          <div className="space-y-2">
            <Label className="text-[var(--color-text-secondary)]">Reason</Label>
            <Input
              {...register('reason', { required: 'Reason is required' })}
              placeholder="e.g. Event bonus"
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
            {errors.reason && <p className="text-xs text-[var(--color-destructive)]">{errors.reason.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} className="border-[var(--color-border)]">Cancel</Button>
            <Button type="submit" disabled={isPending} className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]">
              {isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              Apply
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
