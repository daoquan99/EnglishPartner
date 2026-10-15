import React from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2 } from 'lucide-react';
import { Dialog } from './Dialog';
import { Button } from './Button';
import { WarningToastIcon } from './ToastIcons';

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  targetName: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isLoading?: boolean;
}

/**
 * Shared Confirmation Dialog for Delete / Destructive operations.
 * Displays the exact name of the object about to be deleted.
 */
export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  targetName,
  description,
  confirmLabel,
  cancelLabel,
  isLoading = false,
}) => {
  const { t } = useTranslation();

  const effectiveTitle = title ?? t('common.confirmDeleteTitle');
  const effectiveDescription = description ?? t('common.confirmDeleteDesc');
  const effectiveConfirmLabel = confirmLabel ?? t('common.confirmDeleteAction');
  const effectiveCancelLabel = cancelLabel ?? t('common.cancel');

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="md"
      title={effectiveTitle}
      icon={<WarningToastIcon className="w-5 h-5" />}
      footer={
        <div className="flex items-center justify-end gap-2.5 w-full">
          <Button variant="outline" size="sm" onClick={onClose} disabled={isLoading}>
            {effectiveCancelLabel}
          </Button>
          <Button variant="danger" size="sm" onClick={onConfirm} disabled={isLoading}>
            <Trash2 className="w-4 h-4" />
            {isLoading ? t('common.processing') : effectiveConfirmLabel}
          </Button>
        </div>
      }
    >
      <div className="space-y-3 text-sm text-slate-600">
        <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 text-rose-900">
          {t('common.targetToDelete')}{' '}
          <strong className="font-extrabold">{targetName}</strong>
        </div>
        <p className="text-xs leading-relaxed text-slate-600">{effectiveDescription}</p>
      </div>
    </Dialog>
  );
};
