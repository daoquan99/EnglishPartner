'use client'

import { useState } from 'react'
import { Sparkles, Loader2 } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Label } from '@/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { Checkbox } from '@/ui/checkbox'
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuCheckboxItem } from '@/ui/dropdown-menu'
import { ChevronDown } from 'lucide-react'
import { useGenerateVocabulary, useLanguages } from '../api/useCurriculum'
import type { TopicResponse } from '../types/curriculum.types'
import { Slider } from '@/ui/slider'

interface AIGenerateVocabularyDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  languageCode: string
  topics: TopicResponse[]
}

export function AIGenerateVocabularyDialog({ open, onOpenChange, languageCode, topics }: AIGenerateVocabularyDialogProps) {
  const { data: languages } = useLanguages()
  const generateMutation = useGenerateVocabulary()

  const [targetLanguage, setTargetLanguage] = useState(languageCode)
  const [wordHint, setWordHint] = useState('')
  const [count, setCount] = useState(5)
  const [level, setLevel] = useState('none')
  const [topicId, setTopicId] = useState('none')
  const [autoGenExamples, setAutoGenExamples] = useState(true)
  const [exampleCount, setExampleCount] = useState(3)
  const [targetLangCodes, setTargetLangCodes] = useState<string[]>(['vi'])

  const handleGenerate = async () => {
    if (!wordHint.trim()) return

    await generateMutation.mutateAsync({
      targetLanguage: targetLanguage,
      wordHint: wordHint.trim(),
      count,
      level: level === 'none' ? undefined : level,
      topicId: topicId === 'none' ? undefined : topicId,
      autoGenerateExamples: autoGenExamples,
      exampleCount: autoGenExamples ? exampleCount : undefined,
      exampleTargetLanguageCodes: autoGenExamples ? targetLangCodes : [],
    })
    
    // Close on success
    onOpenChange(false)
    setWordHint('')
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-transparent bg-clip-text bg-gradient-to-r from-indigo-400 to-purple-500">
            <Sparkles className="h-5 w-5 text-indigo-400" />
            Generate Vocabulary with AI
          </DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Let the AI brainstorm and generate vocabulary based on a theme or hint. The generated words will be saved automatically.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
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
              <Label>Theme / Word Hint <span className="text-red-500">*</span></Label>
              <Input 
                placeholder="e.g. airport, emotions..." 
                value={wordHint}
                onChange={e => setWordHint(e.target.value)}
                className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
              />
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex justify-between">
              <Label>Number of Words to Generate</Label>
              <span className="text-sm font-medium text-[var(--color-primary)]">{count} words</span>
            </div>
            <Slider 
              value={[count]} 
              onValueChange={v => setCount(v[0])} 
              max={20} 
              min={1} 
              step={1} 
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
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
            <div className="space-y-2">
              <Label>Assign to Topic</Label>
              <Select value={topicId} onValueChange={setTopicId}>
                <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                  <SelectValue placeholder="No Topic" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">No Topic</SelectItem>
                  {topics.map(t => (
                    <SelectItem key={t.id} value={t.id}>{t.title}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="p-4 rounded-lg bg-[var(--color-surface-2)] border border-[var(--color-border)] space-y-4">
            <div className="flex items-center space-x-2">
              <Checkbox 
                id="autoGen" 
                checked={autoGenExamples} 
                onCheckedChange={(c) => setAutoGenExamples(c as boolean)} 
                className="border-[var(--color-border)] data-[state=checked]:bg-[var(--color-primary)]"
              />
              <label htmlFor="autoGen" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer">
                Auto-generate Example Sentences
              </label>
            </div>

            {autoGenExamples && (
              <div className="grid grid-cols-2 gap-4 pt-2 border-t border-[var(--color-border)]">
                <div className="space-y-2">
                  <Label className="text-xs text-[var(--color-text-secondary)]">Sentences per word</Label>
                  <Select value={String(exampleCount)} onValueChange={v => setExampleCount(Number(v))}>
                    <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)] h-8">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">1</SelectItem>
                      <SelectItem value="2">2</SelectItem>
                      <SelectItem value="3">3</SelectItem>
                      <SelectItem value="5">5</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label className="text-xs text-[var(--color-text-secondary)]">Translation Languages</Label>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="outline" className="w-full justify-between font-normal bg-[var(--color-input-bg)] border-[var(--color-border)] min-h-[32px] h-auto px-3 py-1.5 text-sm">
                        <div className="flex flex-wrap gap-1 items-center">
                          {targetLangCodes.length > 0 ? (
                            targetLangCodes.map(code => {
                              const langName = languages?.find(l => l.code === code)?.name || code
                              return (
                                <span key={code} className="bg-[var(--color-primary-dim)] text-[var(--color-primary)] px-2 py-0.5 rounded text-xs border border-[var(--color-primary)]/20">
                                  {langName}
                                </span>
                              )
                            })
                          ) : (
                            <span className="text-muted-foreground">Select languages...</span>
                          )}
                        </div>
                        <ChevronDown className="h-4 w-4 opacity-50 shrink-0 ml-2" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-56 bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                      {languages?.filter(l => l.code !== targetLanguage).map(lang => (
                        <DropdownMenuCheckboxItem
                          key={lang.id}
                          checked={targetLangCodes.includes(lang.code)}
                          onSelect={(e) => e.preventDefault()}
                          onCheckedChange={(checked) => {
                            if (checked) {
                              setTargetLangCodes(prev => [...prev, lang.code])
                            } else {
                              setTargetLangCodes(prev => prev.filter(c => c !== lang.code))
                            }
                          }}
                          className="focus:bg-[var(--color-surface-2)] focus:text-[var(--color-text-primary)] cursor-pointer"
                        >
                          {lang.name}
                        </DropdownMenuCheckboxItem>
                      ))}
                      {languages?.filter(l => l.code !== targetLanguage).length === 0 && (
                        <div className="px-2 py-1.5 text-xs text-[var(--color-text-muted)] italic">No other languages</div>
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            )}
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
            disabled={!wordHint.trim() || generateMutation.isPending}
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
