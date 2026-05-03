'use client'

import { useState } from 'react'
import { Sparkles, Loader2, ChevronDown } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/ui/dialog'
import { Input } from '@/ui/input'
import { Button } from '@/ui/button'
import { Label } from '@/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'
import { DropdownMenu, DropdownMenuTrigger, DropdownMenuContent, DropdownMenuCheckboxItem } from '@/ui/dropdown-menu'
import { useGenerateExampleSentences, useLanguages } from '../api/useCurriculum'
import { Slider } from '@/ui/slider'

interface AIGenerateExampleSentenceDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  contextTitle: string
  contextFilters: {
    vocabularyId?: string
    grammarId?: string
    topicId?: string
  }
  languageCode: string
}

export function AIGenerateExampleSentenceDialog({ open, onOpenChange, contextTitle, contextFilters, languageCode }: AIGenerateExampleSentenceDialogProps) {
  const { data: languages } = useLanguages()
  const generateMutation = useGenerateExampleSentences()

  const [context, setContext] = useState(contextTitle)
  const [count, setCount] = useState(3)
  const [level, setLevel] = useState('none')
  
  // Choose target language for the sentence itself (usually the language of the topic/vocab)
  const [targetLanguage, setTargetLanguage] = useState(languageCode)
  // Translations
  const [targetLangCodes, setTargetLangCodes] = useState<string[]>(['vi'])

  const handleGenerate = async () => {
    if (!context.trim()) return

    await generateMutation.mutateAsync({
      targetLanguage,
      context: context.trim(),
      count,
      level: level === 'none' ? undefined : level,
      ...contextFilters,
      targetLanguageCodes: targetLangCodes,
    })
    
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)] sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-transparent bg-clip-text bg-gradient-to-r from-indigo-400 to-purple-500">
            <Sparkles className="h-5 w-5 text-indigo-400" />
            Generate Sentences with AI
          </DialogTitle>
          <DialogDescription className="text-[var(--color-text-secondary)]">
            Automatically generate example sentences and translations based on the provided context.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          <div className="space-y-2">
            <Label>Context / Prompt <span className="text-red-500">*</span></Label>
            <Input 
              placeholder="e.g. Using 'Present Simple' or 'Apple'..." 
              value={context}
              onChange={e => setContext(e.target.value)}
              className="bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>

          <div className="space-y-4">
            <div className="flex justify-between">
              <Label>Number of Sentences</Label>
              <span className="text-sm font-medium text-[var(--color-primary)]">{count}</span>
            </div>
            <Slider 
              value={[count]} 
              onValueChange={v => setCount(v[0])} 
              max={10} 
              min={1} 
              step={1} 
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label>Target Language <span className="text-red-500">*</span></Label>
              <Select value={targetLanguage} onValueChange={setTargetLanguage}>
                <SelectTrigger className="bg-[var(--color-input-bg)] border-[var(--color-border)]">
                  <SelectValue placeholder="Language" />
                </SelectTrigger>
                <SelectContent>
                  {languages?.map(lang => (
                    <SelectItem key={lang.id} value={lang.code}>{lang.name}</SelectItem>
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

          <div className="space-y-2">
            <Label>Translation Languages</Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" className="w-full justify-between font-normal bg-[var(--color-input-bg)] border-[var(--color-border)] min-h-[40px] h-auto px-3 py-2 text-sm">
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
                      <span className="text-muted-foreground">None</span>
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
              </DropdownMenuContent>
            </DropdownMenu>
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
