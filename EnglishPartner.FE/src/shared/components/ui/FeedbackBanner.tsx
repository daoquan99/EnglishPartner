import React from 'react';
import { X } from 'lucide-react';
import { SuccessToastIcon, ErrorToastIcon } from './ToastIcons';
import { cn } from '@/shared/lib/cn';

export interface FeedbackBannerProps {
  type: 'success' | 'error';
  message: string;
  onClose?: () => void;
}

export const FeedbackBanner: React.FC<FeedbackBannerProps> = ({ type, message, onClose }) => {
  return (
    <div
      className={cn(
        'rounded-2xl px-4 py-3 border flex items-center justify-between gap-3 text-xs font-semibold',
        type === 'success'
          ? 'bg-emerald-50 border-emerald-200 text-emerald-900'
          : 'bg-rose-50 border-rose-200 text-rose-900'
      )}
    >
      <div className="flex items-center gap-2">
        {type === 'success' ? (
          <SuccessToastIcon className="w-4 h-4 shrink-0" />
        ) : (
          <ErrorToastIcon className="w-4 h-4 shrink-0" />
        )}
        <span>{message}</span>
      </div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          className="p-1 rounded-lg hover:bg-black/5 cursor-pointer"
          aria-label="Đóng thông báo"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      )}
    </div>
  );
};
