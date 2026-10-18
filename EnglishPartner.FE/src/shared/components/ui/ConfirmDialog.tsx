import React from 'react';
import { useTranslation } from 'react-i18next';
import { Trash2, CheckCircle2 } from 'lucide-react';
import { Dialog } from './Dialog';
import { Button } from './Button';
import { WarningToastIcon } from './ToastIcons';

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  targetName: string;
  targetLabel?: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isLoading?: boolean;
  variant?: 'danger' | 'primary' | 'warning';
  confirmIcon?: React.ReactNode;
}

/**
 * Shared Confirmation Dialog for Action / Delete / Destructive operations.
 * Displays the exact name of the object about to be affected.
 */
export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  targetName,
  targetLabel,
  description,
  confirmLabel,
  cancelLabel,
  isLoading = false,
  variant = 'danger',
  confirmIcon,
}) => {
  const { t } = useTranslation();

  const effectiveTitle = title ?? t('common.confirmDeleteTitle');
  const effectiveDescription = description ?? t('common.confirmDeleteDesc');
  const effectiveConfirmLabel = confirmLabel ?? t('common.confirmDeleteAction');
  const effectiveCancelLabel = cancelLabel ?? t('common.cancel');

  const bannerColor =
    variant === 'primary'
      ? 'bg-emerald-50 border-emerald-200 text-emerald-950'
      : variant === 'warning'
      ? 'bg-amber-50 border-amber-200 text-amber-950'
      : 'bg-rose-50 border-rose-200 text-rose-950';

  const defaultTargetLabel =
    targetLabel ?? (variant === 'danger' ? t('common.targetToDelete') : 'Đối tượng:');

  const defaultIcon =
    confirmIcon ?? (variant === 'danger' ? <Trash2 className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />);

  const buttonVariant = variant === 'danger' ? 'danger' : 'primary';

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
          <Button variant={buttonVariant} size="sm" onClick={onConfirm} disabled={isLoading}>
            {defaultIcon}
            {isLoading ? t('common.processing') : effectiveConfirmLabel}
          </Button>
        </div>
      }
    >
      <div className="space-y-3 text-sm text-slate-600">
        <div className={`p-3.5 rounded-2xl border ${bannerColor}`}>
          {defaultTargetLabel}{' '}
          <strong className="font-extrabold">{targetName}</strong>
        </div>
        <p className="text-xs leading-relaxed text-slate-600">{effectiveDescription}</p>
      </div>
    </Dialog>
  );
};
