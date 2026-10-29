import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { validateRejectReason, getAdminGrammarErrorMessage } from '../../utils/grammarHelpers';

export interface RejectReasonDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => Promise<void>;
  isRejecting?: boolean;
}

export const RejectReasonDialog: React.FC<RejectReasonDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  isRejecting = false,
}) => {
  const { t } = useTranslation();
  const [reason, setReason] = useState<string>('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setReason('');
      setError(null);
    }
  }, [isOpen]);

  const trimmed = reason.trim();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validation = validateRejectReason(trimmed);
    if (!validation.ok) {
      if (validation.errorKey) {
        setError(t(validation.errorKey));
      }
      return;
    }

    try {
      await onConfirm(trimmed);
      onClose();
    } catch (err: any) {
      setError(getAdminGrammarErrorMessage(err, t));
    }
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={isRejecting ? () => {} : onClose}
      title={t('admin.grammar.rejectDialogTitle')}
      maxWidth="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <div className="flex items-center justify-between mb-1">
            <label
              htmlFor="reject-reason-input"
              className="text-xs font-bold text-slate-700"
            >
              {t('admin.grammar.rejectReasonLabel')}
            </label>
            <span
              className={`text-[11px] font-mono ${
                trimmed.length > 500 ? 'text-rose-600 font-bold' : 'text-slate-400'
              }`}
            >
              {trimmed.length}/500
            </span>
          </div>
          <textarea
            id="reject-reason-input"
            rows={4}
            value={reason}
            onChange={(e) => {
              setReason(e.target.value);
              setError(null);
            }}
            placeholder={t('admin.grammar.rejectReasonPlaceholder')}
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-rose-600"
            autoFocus
          />
          {error && <p className="text-xs text-rose-600 mt-1 font-semibold">{error}</p>}
        </div>

        <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            disabled={isRejecting}
          >
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            variant="danger"
            size="sm"
            disabled={isRejecting}
          >
            {isRejecting ? t('common.saving') : t('admin.grammar.confirmReject')}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
