'use client'

import { useState } from 'react'
import { Zap, RotateCcw, Trash2, XCircle, Search } from 'lucide-react'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Card } from '@/ui/card'
import { Label } from '@/ui/label'
import { ConfirmActionDialog } from '@/shared/components/ConfirmActionDialog'
import { AdjustXpDialog } from './AdjustXpDialog'
import { useResetXp, useResetAllMasteries, useForceAbandonSession } from '../api/useLearning'

export function LearningAdminPanel() {
  const [userId, setUserId] = useState('')
  const [sessionId, setSessionId] = useState('')
  const [adjustXpOpen, setAdjustXpOpen] = useState(false)

  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean; title: string; description: string; action: (() => void) | null
  }>({ open: false, title: '', description: '', action: null })

  const { mutate: resetXp } = useResetXp()
  const { mutate: resetAllMasteries } = useResetAllMasteries()
  const { mutate: forceAbandon } = useForceAbandonSession()

  const showConfirm = (title: string, description: string, action: () => void) => {
    setConfirmDialog({ open: true, title, description, action })
  }

  return (
    <div className="flex flex-col h-full gap-6">
      <div>
        <h1 className="text-2xl font-bold text-[var(--color-text-primary)]">Learning Management</h1>
        <p className="text-sm text-[var(--color-text-secondary)] mt-1">Manage user XP, masteries, and sessions</p>
      </div>

      {/* User Actions */}
      <Card className="p-5 border-[var(--color-border)] bg-[var(--color-surface)] space-y-4">
        <Label className="text-sm font-semibold text-[var(--color-text-primary)]">User Actions</Label>
        <div className="flex items-center gap-3">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--color-text-muted)]" />
            <Input
              value={userId}
              onChange={e => setUserId(e.target.value)}
              placeholder="Enter User ID..."
              className="pl-9 bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            onClick={() => setAdjustXpOpen(true)}
            disabled={!userId}
            className="bg-gradient-to-r from-amber-500 to-orange-600 text-white border-0"
          >
            <Zap className="h-4 w-4 mr-2" /> Adjust XP
          </Button>
          <Button
            variant="outline"
            onClick={() => showConfirm('Reset XP', 'This will set the user\'s XP to 0. Continue?', () => resetXp(userId))}
            disabled={!userId}
            className="border-[var(--color-border)] text-[var(--color-text-primary)]"
          >
            <RotateCcw className="h-4 w-4 mr-2" /> Reset XP
          </Button>
          <Button
            variant="outline"
            onClick={() => showConfirm('Reset All Masteries', 'This will reset ALL learning progress for this user. This cannot be undone!', () => resetAllMasteries(userId))}
            disabled={!userId}
            className="border-[var(--color-destructive)] text-[var(--color-destructive)]"
          >
            <Trash2 className="h-4 w-4 mr-2" /> Reset All Masteries
          </Button>
        </div>
      </Card>

      {/* Session Actions */}
      <Card className="p-5 border-[var(--color-border)] bg-[var(--color-surface)] space-y-4">
        <Label className="text-sm font-semibold text-[var(--color-text-primary)]">Session Actions</Label>
        <div className="flex items-center gap-3">
          <div className="relative flex-1 max-w-sm">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[var(--color-text-muted)]" />
            <Input
              value={sessionId}
              onChange={e => setSessionId(e.target.value)}
              placeholder="Enter Session ID..."
              className="pl-9 bg-[var(--color-input-bg)] border-[var(--color-border)]"
            />
          </div>
          <Button
            variant="outline"
            onClick={() => showConfirm('Force Abandon Session', 'This will force-abandon the session. Continue?', () => forceAbandon(sessionId))}
            disabled={!sessionId}
            className="border-[var(--color-destructive)] text-[var(--color-destructive)]"
          >
            <XCircle className="h-4 w-4 mr-2" /> Force Abandon
          </Button>
        </div>
      </Card>

      {/* Dialogs */}
      {userId && (
        <AdjustXpDialog userId={userId} open={adjustXpOpen} onOpenChange={setAdjustXpOpen} />
      )}

      <ConfirmActionDialog
        open={confirmDialog.open}
        onOpenChange={(open) => setConfirmDialog(prev => ({ ...prev, open }))}
        title={confirmDialog.title}
        description={confirmDialog.description}
        onConfirm={() => { confirmDialog.action?.(); setConfirmDialog(prev => ({ ...prev, open: false })) }}
      />
    </div>
  )
}
