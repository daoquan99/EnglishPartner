'use client'

import Link from 'next/link'
import { ArrowLeft, Loader2, Volume2 } from 'lucide-react'
import { Badge } from '@/ui/badge'
import { Button } from '@/ui/button'
import { Card } from '@/ui/card'
import { Mic } from 'lucide-react'
import { useVocabulary, useExampleSentences } from '../api/useCurriculum'
import { PronunciationPractice } from './PronunciationPractice'
import { useState } from 'react'
import { useTts } from '@/features/ai/api/useAi'
import { useAuthStore } from '@/lib/zustand/auth.store'

interface VocabularyDetailViewProps {
  vocabularyId: string
}

export function VocabularyDetailView({ vocabularyId }: VocabularyDetailViewProps) {
  const { user } = useAuthStore()
  const languageCode = user?.targetLanguage || 'en'
  const [practiceItem, setPracticeItem] = useState<{ text: string; audioUrl?: string | null; subtitle?: string | null } | null>(null)
  const [playingId, setPlayingId] = useState<string | null>(null)
  const { data: vocab, isLoading } = useVocabulary(vocabularyId)
  const { data: examples } = useExampleSentences({ languageCode, vocabularyId, page: 1, pageSize: 20 })
  const { mutateAsync: generateTts } = useTts()

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-[var(--color-primary)]" /></div>
  }

  if (!vocab) {
    return <div className="text-center py-20 text-[var(--color-text-muted)]">Vocabulary not found.</div>
  }

  const playAudio = async (text: string, id: string, audioUrl?: string | null) => {
    if (audioUrl) {
      new Audio(audioUrl).play()
      return
    }

    setPlayingId(id)
    try {
      const blob = await generateTts({ text })
      const url = URL.createObjectURL(blob)
      const audio = new Audio(url)
      audio.onended = () => URL.revokeObjectURL(url)
      audio.play()
    } catch {
      console.error('Failed to generate TTS')
    } finally {
      setPlayingId(null)
    }
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <Button asChild variant="ghost" size="sm" className="text-[var(--color-text-secondary)]">
        <Link href="/vocabulary"><ArrowLeft className="h-4 w-4 mr-1" /> Back</Link>
      </Button>

      {/* Word Header */}
      <Card className="p-6 border-[var(--color-border)] bg-[var(--color-surface)]">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">{vocab.word}</h1>
            <div className="flex items-center gap-2 mt-2">
              {vocab.partOfSpeech && (
                <Badge className="bg-blue-500/10 text-blue-500 border-0">{vocab.partOfSpeech}</Badge>
              )}
              {vocab.level && (
                <Badge variant="outline" className="border-[var(--color-primary)] text-[var(--color-primary)]">{vocab.level}</Badge>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button 
              variant="outline" 
              size="icon" 
              onClick={() => playAudio(vocab.word, vocab.id, vocab.audioUrl)} 
              disabled={playingId === vocab.id}
              className="rounded-full border-[var(--color-border)]"
            >
              {playingId === vocab.id ? <Loader2 className="h-5 w-5 animate-spin" /> : <Volume2 className="h-5 w-5" />}
            </Button>
            <Button
              variant="default"
              size="sm"
              onClick={() => setPracticeItem({ text: vocab.word, audioUrl: vocab.audioUrl, subtitle: vocab.definition })}
              className="gap-1.5 bg-gradient-to-r from-indigo-500 to-blue-500 hover:from-indigo-600 hover:to-blue-600 shadow-md shadow-indigo-500/20"
            >
              <Mic className="h-4 w-4" /> Practice Word
            </Button>
          </div>
        </div>

        <p className="mt-4 text-lg text-[var(--color-text-secondary)]">{vocab.definition}</p>

        {vocab.pronunciation && (
          <p className="mt-2 text-sm text-[var(--color-text-muted)] font-mono">{vocab.pronunciation}</p>
        )}
        {vocab.phoneticTranscription && (
          <p className="text-sm text-[var(--color-text-muted)]">Phonetic: {vocab.phoneticTranscription}</p>
        )}

        {vocab.synonyms && vocab.synonyms.length > 0 && (
          <div className="mt-4">
            <p className="text-xs font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-1">Synonyms</p>
            <div className="flex flex-wrap gap-1.5">
              {vocab.synonyms.map(s => (
                <Badge key={s} variant="outline" className="border-[var(--color-border)] text-[var(--color-text-secondary)]">{s}</Badge>
              ))}
            </div>
          </div>
        )}
      </Card>

      {/* Example Sentences */}
      {examples?.items && examples.items.length > 0 && (
        <div>
          <h2 className="text-lg font-semibold text-[var(--color-text-primary)] mb-3">Example Sentences</h2>
          <div className="space-y-2">
            {examples.items.map(ex => (
              <Card key={ex.id} className="p-4 border-[var(--color-border)] bg-[var(--color-surface)]">
                <div className="flex items-start gap-2">
                  <div className="flex flex-col gap-1">
                    <Button 
                      variant="ghost" 
                      size="icon" 
                      className="shrink-0 h-7 w-7" 
                      onClick={() => playAudio(ex.text, ex.id, ex.audioUrl)}
                      disabled={playingId === ex.id}
                    >
                      {playingId === ex.id ? <Loader2 className="h-4 w-4 animate-spin text-[var(--color-text-secondary)]" /> : <Volume2 className="h-4 w-4 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]" />}
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="shrink-0 h-7 w-7 text-[var(--color-text-secondary)] hover:text-emerald-500 hover:bg-emerald-500/10"
                      onClick={() => setPracticeItem({ 
                        text: ex.text, 
                        audioUrl: ex.audioUrl, 
                        subtitle: ex.translations?.find(t => t.languageCode === user?.nativeLanguage)?.translatedText || ex.translations?.[0]?.translatedText 
                      })}
                      title="Practice pronunciation"
                    >
                      <Mic className="h-4 w-4" />
                    </Button>
                  </div>
                  <div>
                    <p className="text-[var(--color-text-primary)]">{ex.text}</p>
                    {ex.translations && ex.translations.length > 0 && (
                      <p className="text-sm text-[var(--color-text-muted)] mt-1 italic">
                        {ex.translations.find(t => t.languageCode === user?.nativeLanguage)?.translatedText || ex.translations[0].translatedText}
                      </p>
                    )}
                    {ex.note && <p className="text-xs text-[var(--color-text-muted)] mt-1">💡 {ex.note}</p>}
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      <PronunciationPractice
        open={!!practiceItem}
        onOpenChange={(v) => { if (!v) setPracticeItem(null) }}
        expectedText={practiceItem?.text ?? ''}
        audioUrl={practiceItem?.audioUrl}
        subtitle={practiceItem?.subtitle}
      />
    </div>
  )
}
