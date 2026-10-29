import React, { useEffect, useId, useRef } from 'react';
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
  /** Pressing Enter confirms. Opt-in: keep it off for destructive confirmations. */
  confirmOnEnter?: boolean;
}

/**
 * Whether an Enter keydown should trigger the confirm action. Ignores repeats, IME composition and
 * Enter on focusable controls (e.g. Cancel), which keep their native behaviour.
 */
export function shouldConfirmOnEnter(event: {
  key: string;
  repeat?: boolean;
  isComposing?: boolean;
  defaultPrevented?: boolean;
  target?: EventTarget | null;
}): boolean {
  if (event.key !== 'Enter' || event.repeat || event.isComposing || event.defaultPrevented) return false;
  const tag = (event.target as { tagName?: string } | null)?.tagName?.toUpperCase();
  return !tag || !['BUTTON', 'A', 'INPUT', 'TEXTAREA', 'SELECT'].includes(tag);
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
  confirmOnEnter = false,
}) => {
  const { t } = useTranslation();
  const onConfirmRef = useRef(onConfirm);
  onConfirmRef.current = onConfirm;
  const confirmButtonId = useId();

  // Move focus into the dialog so Enter is not delivered to the control that opened it.
  useEffect(() => {
    if (!isOpen || !confirmOnEnter) return;
    const timer = window.setTimeout(() => document.getElementById(confirmButtonId)?.focus(), 0);
    return () => window.clearTimeout(timer);
  }, [isOpen, confirmOnEnter, confirmButtonId]);

  useEffect(() => {
    if (!isOpen || !confirmOnEnter || isLoading) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!shouldConfirmOnEnter(event)) return;
      event.preventDefault();
      onConfirmRef.current();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, confirmOnEnter, isLoading]);

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
          <Button id={confirmButtonId} variant={buttonVariant} size="sm" onClick={onConfirm} disabled={isLoading}>
            {defaultIcon}
            {isLoading ? t('common.processing') : effectiveConfirmLabel}
            {confirmOnEnter && !isLoading && (
              <kbd className="ml-1 rounded-md bg-white/20 px-1.5 py-0.5 font-mono text-[10px] font-semibold">Enter</kbd>
            )}
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
