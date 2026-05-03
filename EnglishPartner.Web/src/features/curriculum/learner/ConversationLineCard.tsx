'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'motion/react'
import { Volume2, Loader2, Headphones } from 'lucide-react'
import { Button } from '@/ui/button'
import { AudioRecorder } from '@/shared/components/AudioRecorder'
import { StarRating } from '@/shared/components/StarRating'
import type { EvaluateConversationLineResponse } from '@/features/learning/types/learning.types'
import type { SttResponse } from '@/features/ai/types/ai.types'

interface ConversationLineCardProps {
  speaker: string
  text: string
  translation?: string
  audioUrl?: string | null
  isAiTurn: boolean
  evaluation?: EvaluateConversationLineResponse | null
  sttResult?: SttResponse | null
  onRecord?: (blob: Blob) => void
  onRecordingStart?: () => void
  onDataAvailable?: (blob: Blob) => void
  onPlaySample?: () => void
  isEvaluating?: boolean
  isTranscribing?: boolean
  isLoadingTts?: boolean
  showTranslation?: boolean
  isActive?: boolean
}

export function ConversationLineCard({
  speaker,
  text,
  translation,
  audioUrl,
  isAiTurn,
  evaluation,
  sttResult,
  onRecord,
  onRecordingStart,
  onDataAvailable,
  onPlaySample,
  isEvaluating,
  isTranscribing,
  isLoadingTts,
  showTranslation,
  isActive = true,
}: ConversationLineCardProps) {
  const playAudio = (url: string) => { new Audio(url).play() }
  const qualityScore = evaluation ? Math.round(evaluation.score / 20) : 0

  const scoreColor = evaluation
    ? evaluation.score >= 80 ? 'from-emerald-500 to-emerald-600'
    : evaluation.score >= 60 ? 'from-amber-500 to-amber-600'
    : 'from-red-500 to-red-600'
    : ''

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 10, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ type: 'spring', stiffness: 400, damping: 30 }}
      className={`flex ${isAiTurn ? 'justify-start' : 'justify-end'}`}
    >
      <div className={`max-w-[85%] ${isAiTurn ? 'items-start' : 'items-end'} flex flex-col gap-1.5`}>
        {/* Speaker label */}
        <div className={`flex items-center gap-2 px-1 ${isAiTurn ? '' : 'flex-row-reverse'}`}>
          <span className={`flex h-6 w-6 items-center justify-center rounded-full text-xs shadow-sm ${
            isAiTurn
              ? 'bg-gradient-to-br from-violet-500 to-indigo-500 text-white'
              : 'bg-gradient-to-br from-emerald-500 to-teal-500 text-white'
          }`}>
            {isAiTurn ? '🤖' : '👤'}
          </span>
          <span className="text-xs font-medium text-[var(--color-text-secondary)]">
            {speaker}{!isAiTurn && ' (You)'}
          </span>
        </div>

        {/* Message bubble */}
        <motion.div 
          layout
          className={`relative px-4 py-3 shadow-sm transition-all duration-200 border ${
            isAiTurn
              ? 'bg-white border-[var(--color-border)] rounded-2xl rounded-tl-sm'
              : 'bg-emerald-50 dark:bg-emerald-950/30 border-emerald-100 dark:border-emerald-900/50 rounded-2xl rounded-tr-sm'
          } ${isActive ? '' : 'opacity-90'}`}
        >
          {/* Text */}
          <p className={`text-[15px] leading-relaxed text-[var(--color-text-primary)]`}>
            {text}
          </p>

          {/* Translation */}
          {showTranslation && translation && (
            <p className={`text-sm mt-2 italic pt-2 border-t ${
              isAiTurn
                ? 'text-[var(--color-text-muted)] border-[var(--color-border)]'
                : 'text-emerald-700/70 dark:text-emerald-400/70 border-emerald-200/50 dark:border-emerald-800/50'
            }`}>
              {translation}
            </p>
          )}

          {/* Action row — both AI and User get audio controls */}
          <div className={`mt-3 pt-2 border-t flex items-center gap-2 flex-wrap ${
            isAiTurn ? 'border-[var(--color-border)]' : 'border-emerald-200/50 dark:border-emerald-800/50'
          }`}>
            {/* AI turn: play audio (audioUrl or TTS fallback) */}
            {isAiTurn && (audioUrl || onPlaySample) && (
              <Button
                variant="ghost"
                size="sm"
                onClick={audioUrl ? () => playAudio(audioUrl) : onPlaySample}
                disabled={isLoadingTts}
                className="h-8 px-2.5 text-xs font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--color-primary-dim)]"
              >
                {isLoadingTts
                  ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                  : <Volume2 className="h-4 w-4 mr-1.5" />
                }
                Play
              </Button>
            )}

            {/* User turn: listen to sample */}
            {!isAiTurn && onPlaySample && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onPlaySample}
                disabled={isLoadingTts}
                className="h-8 px-2.5 text-xs font-medium text-emerald-700 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 hover:bg-emerald-100/50 dark:hover:bg-emerald-900/30"
              >
                {isLoadingTts
                  ? <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                  : <Headphones className="h-4 w-4 mr-1.5" />
                }
                Listen sample
              </Button>
            )}

            {/* User turn: record */}
            {!isAiTurn && onRecord && (
              <>
                <div className="flex-1" />
                <AudioRecorder
                  onRecordingComplete={onRecord}
                  onRecordingStart={onRecordingStart}
                  onDataAvailable={onDataAvailable}
                  disabled={isEvaluating}
                  size="sm"
                  maxDuration={60}
                  className="bg-white text-emerald-600 shadow-sm border border-emerald-100 hover:bg-emerald-50 dark:bg-emerald-950 dark:border-emerald-800 dark:text-emerald-400 dark:hover:bg-emerald-900"
                />
                {isTranscribing && !sttResult && (
                  <motion.span 
                    initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                    className="flex items-center gap-1.5 text-xs font-medium text-emerald-600/70 dark:text-emerald-400/70"
                  >
                    <span className="relative flex h-2 w-2 mr-1">
                      <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                      <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                    </span>
                    Listening...
                  </motion.span>
                )}
                {isEvaluating && (
                  <motion.span 
                    initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                    className="flex items-center gap-1.5 text-xs font-medium text-emerald-600/70 dark:text-emerald-400/70"
                  >
                    <Loader2 className="h-3.5 w-3.5 animate-spin" /> Scoring...
                  </motion.span>
                )}
              </>
            )}
          </div>
        </motion.div>

        <AnimatePresence>
          {/* STT result — shown below the bubble, before evaluation */}
          {!isAiTurn && sttResult && (
            <motion.div 
              layout
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: 'auto' }}
              className="w-full rounded-xl p-3 bg-white dark:bg-[var(--color-surface-2)] shadow-sm border border-[var(--color-border)] overflow-hidden"
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-1.5 mb-1">
                    <span className="relative flex h-2 w-2">
                      {!sttResult.isFinal && <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>}
                      <span className={`relative inline-flex rounded-full h-2 w-2 ${sttResult.isFinal ? 'bg-gray-400' : 'bg-emerald-500'}`}></span>
                    </span>
                    <p className="text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wider">
                      {!isTranscribing ? 'You said' : 'Listening...'}
                    </p>
                  </div>
                  <p className="text-[14px] text-[var(--color-text-primary)] leading-relaxed italic">
                    &quot;{(sttResult.accumulated || '') + (sttResult.transcript || '')}&quot;
                  </p>
                </div>
                {!isTranscribing && sttResult.confidence > 0 && (
                  <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded-md shrink-0 ${
                    sttResult.confidence >= 0.9 ? 'bg-emerald-50 text-emerald-600 border border-emerald-100'
                    : sttResult.confidence >= 0.7 ? 'bg-amber-50 text-amber-600 border border-amber-100'
                    : 'bg-red-50 text-red-600 border border-red-100'
                  }`}>
                    {Math.round(sttResult.confidence * 100)}%
                  </span>
                )}
              </div>
              {isEvaluating && (
                <div className="flex items-center gap-1.5 mt-3 pt-2 border-t border-[var(--color-border)] text-emerald-600 dark:text-emerald-400">
                  <Loader2 className="h-3.5 w-3.5 animate-spin" />
                  <span className="text-[11px] font-medium">Scoring pronunciation...</span>
                </div>
              )}
            </motion.div>
          )}

          {/* Evaluation feedback — shown below STT */}
          {!isAiTurn && evaluation && (
            <motion.div 
              layout
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className={`w-full rounded-xl p-3.5 bg-gradient-to-r ${scoreColor} text-white shadow-md`}
            >
              <div className="flex items-center justify-between mb-2">
                <StarRating score={qualityScore} className="[&_svg]:h-3.5 [&_svg]:w-3.5 [&_svg]:text-white/90 [&_svg]:fill-white/90" />
                <span className="text-xl font-bold">{evaluation.score}<span className="text-xs font-normal opacity-70">/100</span></span>
              </div>
              <div className="space-y-1.5">
                {evaluation.pronunciationFeedback ? (
                  <p className="text-xs opacity-95 bg-white/15 backdrop-blur-sm rounded-lg px-3 py-2.5 leading-relaxed shadow-sm">
                    💡 {evaluation.pronunciationFeedback}
                  </p>
                ) : (
                  <p className="text-xs opacity-95 bg-white/15 backdrop-blur-sm rounded-lg px-3 py-2.5 leading-relaxed shadow-sm">
                    ✨ Perfect pronunciation!
                  </p>
                )}
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </motion.div>
  )
}
