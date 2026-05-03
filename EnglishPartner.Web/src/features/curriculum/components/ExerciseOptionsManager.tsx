'use client'

import { useState } from 'react'
import { Plus, ListChecks, CheckCircle2, XCircle, Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { Checkbox } from '@/ui/checkbox'
import { useExercise, useAddExerciseOption } from '../api/useCurriculum'

interface ExerciseOptionsManagerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  exerciseId: string | null
}

export function ExerciseOptionsManager({ open, onOpenChange, exerciseId }: ExerciseOptionsManagerProps) {
  const { data: exercise, isLoading } = useExercise(exerciseId || '')
  
  const [text, setText] = useState('')
  const [isCorrect, setIsCorrect] = useState(false)

  const addOptionMutation = useAddExerciseOption()

  const resetForm = () => {
    setText('')
    setIsCorrect(false)
  }

  const handleSave = async () => {
    if (!text.trim() || !exerciseId) return

    await addOptionMutation.mutateAsync({ 
      exerciseId, 
      text: text.trim(),
      isCorrect,
    })
    
    resetForm()
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) resetForm() }}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[700px] h-[80vh] flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="text-xl flex items-center gap-2">
            <ListChecks className="h-5 w-5 text-[var(--color-primary)]" />
            Exercise Options Manager
          </DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Manage options for exercise: <span className="font-semibold text-[var(--color-text-primary)]">{exercise?.title}</span>
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col md:flex-row gap-6 flex-1 min-h-0 py-4">
          
          {/* Left Side: Options List */}
          <div className="flex-1 border border-[var(--color-border)] rounded-lg bg-[var(--color-surface-2)] overflow-y-auto flex flex-col">
            <div className="p-3 border-b border-[var(--color-border)] bg-[var(--color-surface)] font-medium sticky top-0 z-10 flex justify-between items-center">
              <span>Options ({exercise?.options?.length || 0})</span>
            </div>
            
            {isLoading ? (
              <div className="flex-1 flex items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" />
              </div>
            ) : !exercise?.options || exercise.options.length === 0 ? (
              <div className="flex-1 flex items-center justify-center text-[var(--color-text-muted)] text-sm italic">
                No options found. Add your first option on the right.
              </div>
            ) : (
              <ul className="flex-1 p-2 space-y-2">
                {[...exercise.options].sort((a, b) => a.sortOrder - b.sortOrder).map((option) => (
                  <li
                    key={option.id}
                    className={`
                      flex gap-3 p-3 rounded-md bg-[var(--color-surface)] border shadow-sm
                      ${option.isCorrect ? 'border-green-500/50 bg-green-500/5 dark:bg-green-500/10' : 'border-[var(--color-border)]'}
                    `}
                  >
                    <div className="mt-0.5 flex-shrink-0">
                      {option.isCorrect ? (
                        <CheckCircle2 className="h-5 w-5 text-green-500" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-500 opacity-50" />
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-[var(--color-text-primary)] font-medium">{option.text}</p>
                      {option.isCorrect && (
                        <span className="inline-block mt-1 text-xs font-semibold text-green-600 dark:text-green-400">
                          Correct Answer
                        </span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {/* Right Side: Form */}
          <div className="w-full md:w-[280px] flex-shrink-0 bg-[var(--color-surface-2)] border border-[var(--color-border)] rounded-lg p-4 flex flex-col gap-4">
            <h3 className="font-semibold text-[var(--color-text-primary)] pb-2 border-b border-[var(--color-border)]">
              Add New Option
            </h3>
            
            <div className="space-y-4">
              <div className="space-y-1.5">
                <Label>Option Text <span className="text-red-500">*</span></Label>
                <textarea 
                  placeholder="e.g. goes" 
                  value={text}
                  onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setText(e.target.value)}
                  className="flex min-h-[80px] w-full rounded-md px-3 py-2 text-sm bg-[var(--color-input-bg)] border-[var(--color-border)] border ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 resize-none"
                />
              </div>

              <div className="flex items-center space-x-2 bg-[var(--color-surface)] p-3 rounded-md border border-[var(--color-border)]">
                <Checkbox 
                  id="isCorrect" 
                  checked={isCorrect}
                  onCheckedChange={(checked) => setIsCorrect(checked as boolean)}
                />
                <label
                  htmlFor="isCorrect"
                  className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
                >
                  This is the correct answer
                </label>
              </div>
            </div>

            <div className="mt-auto pt-4 flex gap-2">
              <Button 
                onClick={handleSave} 
                disabled={!text.trim() || addOptionMutation.isPending}
                className="w-full bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-white"
              >
                {addOptionMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Option
                  </>
                )}
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
