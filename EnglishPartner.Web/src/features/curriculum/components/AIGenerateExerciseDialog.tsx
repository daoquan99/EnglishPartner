'use client'

import { useState } from 'react'
import { Sparkles, Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Label } from '@/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { useGenerateExercise, useTopics, useLanguages } from '../api/useCurriculum'
import { Slider } from '@/ui/slider'
import { EXERCISE_TYPES } from '../constants/exercise-types'
import { VocabularyAsyncSelect } from './VocabularyAsyncSelect'
import { GrammarAsyncSelect } from './GrammarAsyncSelect'

// Allowed types for AI Generation according to API specs
const AI_SUPPORTED_EXERCISE_TYPES = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 30]

interface AIGenerateExerciseDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  languageCode: string
}

export function AIGenerateExerciseDialog({ open, onOpenChange, languageCode }: AIGenerateExerciseDialogProps) {
  const { data: topics } = useTopics({ languageCode })
  const { data: languages } = useLanguages()
  
  const generateMutation = useGenerateExercise()

  const [targetLanguage, setTargetLanguage] = useState(languageCode)
  const [context, setContext] = useState('')
  const [exerciseType, setExerciseType] = useState<number>(0)
  const [count, setCount] = useState(5)
  const [level, setLevel] = useState('none')
  const [topicId, setTopicId] = useState('none')
  const [vocabularyId, setVocabularyId] = useState('none')
  const [grammarId, setGrammarId] = useState('none')
  const [xpReward, setXpReward] = useState(10)

  const handleGenerate = async () => {
    if (!context.trim()) return

    await generateMutation.mutateAsync({
      targetLanguage: targetLanguage,
      context: context.trim(),
      exerciseType,
      count,
      level: level === 'none' ? undefined : level,
      topicId: topicId === 'none' ? undefined : topicId,
      vocabularyId: vocabularyId === 'none' ? undefined : vocabularyId,
      grammarId: grammarId === 'none' ? undefined : grammarId,
      xpReward,
    })
    
    onOpenChange(false)
    setContext('')
  }

  const supportedTypes = EXERCISE_TYPES.filter(t => AI_SUPPORTED_EXERCISE_TYPES.includes(t.value))

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[550px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-transparent bg-clip-text bg-gradient-to-r from-indigo-400 to-purple-500">
            <Sparkles className="h-5 w-5 text-indigo-400" />
            Generate Exercises with AI
          </DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Automatically generate text-based exercises like Multiple Choice, Fill in the Blanks, and more.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4 px-1">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Target Language <span className="text-red-500">*</span></Label>
              <Select value={targetLanguage} onValueChange={setTargetLanguage}>
                <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                  <SelectValue placeholder="Select language" />
                </SelectTrigger>
                <SelectContent>
                  {languages?.map(lang => (
                    <SelectItem key={lang.id} value={lang.code}>{lang.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Context / Topic Description <span className="text-red-500">*</span></Label>
              <Input 
                placeholder="e.g. Daily routines..." 
                value={context}
                onChange={e => setContext(e.target.value)}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Exercise Type <span className="text-red-500">*</span></Label>
              <Select value={String(exerciseType)} onValueChange={v => setExerciseType(Number(v))}>
                <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {supportedTypes.map(t => (
                    <SelectItem key={t.value} value={String(t.value)}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Target Level</Label>
              <Select value={level} onValueChange={setLevel}>
                <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                  <SelectValue placeholder="Any level" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Any Level</SelectItem>
                  <SelectItem value="A1">A1</SelectItem>
                  <SelectItem value="A2">A2</SelectItem>
                  <SelectItem value="B1">B1</SelectItem>
                  <SelectItem value="B2">B2</SelectItem>
                  <SelectItem value="C1">C1</SelectItem>
                  <SelectItem value="C2">C2</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex justify-between">
              <Label>Number of Exercises to Generate</Label>
              <span className="text-sm font-medium text-[var(--color-primary)]">{count}</span>
            </div>
            <Slider 
              value={[count]} 
              onValueChange={v => setCount(v[0])} 
              max={15} 
              min={1} 
              step={1} 
            />
          </div>

          <div className="space-y-4 p-4 rounded-lg bg-[var(--color-surface-2)] border border-[var(--color-border)]">
            <Label className="block text-sm font-medium border-b border-[var(--color-border)] pb-2 mb-2">
              Associations (Optional)
            </Label>
            
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label className="text-xs text-[var(--color-text-secondary)]">Assign to Topic</Label>
                <Select value={topicId} onValueChange={setTopicId}>
                  <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                    <SelectValue placeholder="No Topic" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">No Topic</SelectItem>
                    {topics?.map(t => (
                      <SelectItem key={t.id} value={t.id}>{t.title}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label className="text-xs text-[var(--color-text-secondary)]">Assign to Vocabulary</Label>
                <VocabularyAsyncSelect
                  languageCode={languageCode}
                  value={vocabularyId === 'none' ? null : vocabularyId}
                  onValueChange={(val) => setVocabularyId(val || 'none')}
                  placeholder="No Vocabulary"
                />
              </div>

              <div className="space-y-2 col-span-2">
                <Label className="text-xs text-[var(--color-text-secondary)]">Assign to Grammar</Label>
                <GrammarAsyncSelect
                  languageCode={languageCode}
                  value={grammarId === 'none' ? null : grammarId}
                  onValueChange={(val) => setGrammarId(val || 'none')}
                  placeholder="No Grammar Rule"
                />
              </div>
            </div>
            
            <div className="pt-2">
              <Label className="text-xs text-[var(--color-text-secondary)]">XP Reward per Exercise</Label>
              <Input 
                type="number"
                value={xpReward}
                onChange={e => setXpReward(parseInt(e.target.value) || 0)}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)] mt-1"
              />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="bg-transparent border-[var(--color-border)] text-[var(--color-text-secondary)]"
            disabled={generateMutation.isPending}
          >
            Cancel
          </Button>
          <Button
            onClick={handleGenerate}
            disabled={!context.trim() || generateMutation.isPending}
            className="bg-gradient-to-r from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white border-0"
          >
            {generateMutation.isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Generating...
              </>
            ) : (
              'Generate & Save'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
