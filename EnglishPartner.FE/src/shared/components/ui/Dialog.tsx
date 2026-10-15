import React, { useEffect } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { IconButton } from './IconButton';

export interface DialogProps {
  isOpen?: boolean;
  open?: boolean;
  onClose: () => void;
  title: React.ReactNode;
  description?: React.ReactNode;
  icon?: React.ReactNode;
  maxWidth?: 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | '5xl';
  footer?: React.ReactNode;
  children: React.ReactNode;
  bodyClassName?: string;
}

const MAX_WIDTH_CLASSES: Record<NonNullable<DialogProps['maxWidth']>, string> = {
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-xl',
  '2xl': 'max-w-2xl',
  '3xl': 'max-w-3xl',
  '4xl': 'max-w-4xl',
  '5xl': 'max-w-5xl',
};

/**
 * Shared Accessible Dialog / Modal Primitive:
 * - Rendered via React Portal into `document.body` so parent overflow/stacking contexts never clip it
 * - Accepts both `isOpen` and `open` props for compatibility
 * - Supports ESC key and click-outside to close
 * - Fixed Header, independently scrollable Body (`max-h-[85vh] overflow-y-auto`), and sticky Footer
 */
export const Dialog: React.FC<DialogProps> = ({
  isOpen,
  open,
  onClose,
  title,
  description,
  icon,
  maxWidth = '2xl',
  footer,
  children,
  bodyClassName,
}) => {
  const visible = Boolean(isOpen ?? open);

  useEffect(() => {
    if (!visible) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [visible, onClose]);

  if (!visible) return null;

  const dialogContent = (
    <div
      className="fixed inset-0 z-[100] bg-slate-900/55 backdrop-blur-xs flex items-center justify-center p-3 sm:p-6 animate-fadeIn"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        className={cn(
          'bg-white rounded-2xl sm:rounded-3xl border border-slate-200 shadow-2xl w-full max-h-[85vh] flex flex-col overflow-hidden',
          MAX_WIDTH_CLASSES[maxWidth]
        )}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Sticky Header */}
        <div className="px-5 sm:px-6 py-4 border-b border-slate-200/80 flex items-center justify-between gap-4 bg-slate-50/70 shrink-0">
          <div className="flex items-center gap-3 min-w-0">
            {icon && (
              <div className="w-10 h-10 rounded-2xl bg-emerald-600 text-white flex items-center justify-center shadow-xs shrink-0">
                {icon}
              </div>
            )}
            <div className="min-w-0">
              <h2 className="text-base sm:text-lg font-extrabold text-slate-900 truncate flex items-center gap-2">
                {title}
              </h2>
              {description && (
                <p className="text-xs text-slate-500 mt-0.5 line-clamp-1">{description}</p>
              )}
            </div>
          </div>

          <IconButton
            variant="ghost"
            size="sm"
            label="Đóng hộp thoại (ESC)"
            icon={<X className="w-4 h-4" />}
            onClick={onClose}
          />
        </div>

        {/* Independently Scrollable Body */}
        <div className={cn('flex-1 overflow-y-auto p-5 sm:p-6', bodyClassName)}>{children}</div>

        {/* Sticky Footer */}
        {footer && (
          <div className="px-5 sm:px-6 py-3.5 bg-slate-50 border-t border-slate-200/80 flex flex-wrap items-center justify-between gap-3 shrink-0">
            {footer}
          </div>
        )}
      </div>
    </div>
  );

  if (typeof document !== 'undefined' && document.body) {
    return createPortal(dialogContent, document.body);
  }
  return dialogContent;
};
