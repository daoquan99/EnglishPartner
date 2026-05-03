'use client'

import Link from 'next/link'
import { ArrowLeft, Loader2 } from 'lucide-react'
import { Badge } from '@/ui/badge'
import { Button } from '@/ui/button'
import { Card } from '@/ui/card'
import { useGrammar } from '../api/useCurriculum'

interface GrammarLessonViewProps {
  grammarId: string
}

export function GrammarLessonView({ grammarId }: GrammarLessonViewProps) {
  const { data: grammar, isLoading } = useGrammar(grammarId)

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!grammar) {
    return <div className="text-center py-20 text-[var(--color-text-muted)]">Grammar lesson not found.</div>
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <Button asChild variant="ghost" size="sm" className="text-[var(--color-text-secondary)]">
        <Link href="/grammar"><ArrowLeft className="h-4 w-4 mr-1" /> Back</Link>
      </Button>

      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">{grammar.title}</h1>
        {grammar.level && (
          <Badge className="mt-2 bg-[var(--color-primary-dim)] text-[var(--color-primary)] border-0">{grammar.level}</Badge>
        )}
      </div>

      {/* Formula */}
      {grammar.formula && (
        <Card className="p-4 border-2 border-[var(--color-primary)]/20 bg-[var(--color-primary-dim)]">
          <p className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-1">Formula</p>
          <p className="text-lg font-mono font-semibold text-[var(--color-primary)]">{grammar.formula}</p>
        </Card>
      )}

      {/* Explanation */}
      {grammar.explanation && (
        <div>
          <h2 className="text-lg font-semibold text-[var(--color-text-primary)] mb-2">Explanation</h2>
          <div className="prose prose-sm max-w-none text-[var(--color-text-secondary)] leading-relaxed whitespace-pre-wrap">
            {grammar.explanation}
          </div>
        </div>
      )}

      {/* Examples */}
      {grammar.examples && grammar.examples.length > 0 && (
        <div>
          <h2 className="text-lg font-semibold text-[var(--color-text-primary)] mb-3">Examples</h2>
          <div className="space-y-2">
            {grammar.examples
              .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0))
              .map(ex => (
                <Card key={ex.id} className="p-4 border-[var(--color-border)] bg-[var(--color-surface)]">
                  <p className="text-[var(--color-text-primary)] font-medium">{ex.text}</p>
                  {ex.translations && ex.translations.length > 0 && (
                    <p className="text-sm text-[var(--color-text-muted)] mt-1 italic">{ex.translations[0].translatedText}</p>
                  )}
                  {ex.note && <p className="text-xs text-[var(--color-text-muted)] mt-1">💡 {ex.note}</p>}
                </Card>
              ))}
          </div>
        </div>
      )}
    </div>
  )
}
