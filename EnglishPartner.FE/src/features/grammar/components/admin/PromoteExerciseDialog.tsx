import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { getAdminGrammarErrorMessage } from '../../utils/grammarHelpers';

export interface PromoteExerciseDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (isCore: boolean) => Promise<void>;
  isPromoting?: boolean;
}

export const PromoteExerciseDialog: React.FC<PromoteExerciseDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  isPromoting = false,
}) => {
  const { t } = useTranslation();
  const [isCore, setIsCore] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setIsCore(true);
      setError(null);
    }
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await onConfirm(isCore);
      onClose();
    } catch (err: any) {
      setError(getAdminGrammarErrorMessage(err, t));
    }
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={isPromoting ? () => {} : onClose}
      title={t('admin.grammar.promoteDialogTitle')}
      maxWidth="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-xs text-slate-600">
          {t('admin.grammar.promoteDialogDescription')}
        </p>

        <div className="p-3 rounded-xl border border-emerald-200 bg-emerald-50/60">
          <label htmlFor="promote-iscore-checkbox" className="flex items-center gap-2 cursor-pointer">
            <input
              id="promote-iscore-checkbox"
              type="checkbox"
              checked={isCore}
              onChange={(e) => setIsCore(e.target.checked)}
              className="rounded text-emerald-600 focus:ring-emerald-500 w-4 h-4 cursor-pointer"
            />
            <span className="font-bold text-emerald-950 text-xs">
              {t('admin.grammar.markAsCoreExercise')}
            </span>
          </label>
          <p className="text-[11px] text-emerald-800 mt-1">
            {t('admin.grammar.coreExerciseExplanation')}
          </p>
        </div>

        {error && <p className="text-xs text-rose-600 font-semibold">{error}</p>}

        <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            disabled={isPromoting}
          >
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            variant="primary"
            size="sm"
            disabled={isPromoting}
          >
            {isPromoting ? t('common.saving') : t('admin.grammar.confirmPromote')}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
