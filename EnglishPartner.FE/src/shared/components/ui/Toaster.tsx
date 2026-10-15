import React, { useState } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';
import {
  SuccessToastIcon,
  ErrorToastIcon,
  WarningToastIcon,
  InfoToastIcon,
  LoadingToastIcon,
} from './ToastIcons';
import {
  useToastStore,
  toast,
  type ToastItem,
  type ToastType,
  type ToastOptions,
  type ToastAction,
} from '@/shared/lib/toast';
import { cn } from '@/shared/lib/cn';

export {
  toast,
  useToastStore,
  type ToastItem,
  type ToastType,
  type ToastOptions,
  type ToastAction,
};

export interface ToasterProps {
  position?:
    | 'top-right'
    | 'top-left'
    | 'bottom-right'
    | 'bottom-left'
    | 'top-center'
    | 'bottom-center';
  maxToasts?: number;
}

const POSITION_CLASSES: Record<NonNullable<ToasterProps['position']>, string> = {
  'top-right': 'top-4 right-4 items-end flex-col',
  'top-left': 'top-4 left-4 items-start flex-col',
  'top-center': 'top-4 left-1/2 -translate-x-1/2 items-center flex-col',
  'bottom-right': 'bottom-4 right-4 items-end flex-col-reverse',
  'bottom-left': 'bottom-4 left-4 items-start flex-col-reverse',
  'bottom-center': 'bottom-4 left-1/2 -translate-x-1/2 items-center flex-col-reverse',
};

const TOAST_THEMES: Record<
  ToastType,
  {
    icon: React.ComponentType<{ size?: number; className?: string }>;
    accentBar: string;
    progressBar: string;
    cardBorder: string;
    glowBg: string;
    titleColor: string;
    badgeLabel: string;
    badgeStyle: string;
  }
> = {
  success: {
    icon: SuccessToastIcon,
    accentBar: 'bg-gradient-to-b from-emerald-400 to-emerald-600',
    progressBar: 'bg-emerald-500',
    cardBorder: 'border-emerald-200/80 shadow-[0_10px_30px_-5px_rgba(16,185,129,0.15)]',
    glowBg: 'bg-gradient-to-br from-emerald-50/60 via-white to-white',
    titleColor: 'text-emerald-950',
    badgeLabel: 'Thành công',
    badgeStyle: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  },
  error: {
    icon: ErrorToastIcon,
    accentBar: 'bg-gradient-to-b from-rose-400 to-rose-600',
    progressBar: 'bg-rose-500',
    cardBorder: 'border-rose-200/80 shadow-[0_10px_30px_-5px_rgba(244,63,94,0.18)]',
    glowBg: 'bg-gradient-to-br from-rose-50/60 via-white to-white',
    titleColor: 'text-rose-950',
    badgeLabel: 'Lỗi',
    badgeStyle: 'bg-rose-50 text-rose-700 border-rose-200',
  },
  warning: {
    icon: WarningToastIcon,
    accentBar: 'bg-gradient-to-b from-amber-400 to-amber-600',
    progressBar: 'bg-amber-500',
    cardBorder: 'border-amber-200/80 shadow-[0_10px_30px_-5px_rgba(245,158,11,0.18)]',
    glowBg: 'bg-gradient-to-br from-amber-50/60 via-white to-white',
    titleColor: 'text-amber-950',
    badgeLabel: 'Cảnh báo',
    badgeStyle: 'bg-amber-50 text-amber-700 border-amber-200',
  },
  info: {
    icon: InfoToastIcon,
    accentBar: 'bg-gradient-to-b from-sky-400 to-blue-600',
    progressBar: 'bg-sky-500',
    cardBorder: 'border-sky-200/80 shadow-[0_10px_30px_-5px_rgba(2,132,199,0.15)]',
    glowBg: 'bg-gradient-to-br from-sky-50/60 via-white to-white',
    titleColor: 'text-sky-950',
    badgeLabel: 'Thông tin',
    badgeStyle: 'bg-sky-50 text-sky-700 border-sky-200',
  },
  loading: {
    icon: LoadingToastIcon,
    accentBar: 'bg-gradient-to-b from-emerald-400 to-teal-600',
    progressBar: 'bg-emerald-500',
    cardBorder: 'border-emerald-200/80 shadow-[0_10px_30px_-5px_rgba(16,185,129,0.15)]',
    glowBg: 'bg-gradient-to-br from-emerald-50/40 via-white to-white',
    titleColor: 'text-slate-900',
    badgeLabel: 'Đang xử lý',
    badgeStyle: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  },
};

const ToastCard: React.FC<{
  item: ToastItem;
  onDismiss: (id: string) => void;
}> = ({ item, onDismiss }) => {
  const [isHovered, setIsHovered] = useState(false);
  const theme = TOAST_THEMES[item.type];
  const IconComponent = theme.icon;

  const handleMouseEnter = () => {
    setIsHovered(true);
    toast.pause(item.id);
  };

  const handleMouseLeave = () => {
    setIsHovered(false);
    toast.resume(item.id);
  };

  return (
    <div
      role="status"
      aria-live={item.type === 'error' ? 'assertive' : 'polite'}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      className={cn(
        'pointer-events-auto relative w-full sm:w-[380px] rounded-2xl border backdrop-blur-xl transition-all duration-200 overflow-hidden select-none',
        theme.cardBorder,
        theme.glowBg,
        isHovered ? 'scale-[1.01] shadow-2xl' : 'shadow-lg'
      )}
    >
      <div className="flex items-start gap-3 p-3.5">
        {/* Custom Handcrafted SVG Icon */}
        <div className="shrink-0 mt-0.5">
          <IconComponent size={30} />
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0 pr-1 space-y-1">
          <div className="flex items-center justify-between gap-2">
            <h4 className={cn('text-xs font-extrabold tracking-tight truncate', theme.titleColor)}>
              {item.title || theme.badgeLabel}
            </h4>
            <span
              className={cn(
                'text-[10px] font-bold px-2 py-0.5 rounded-full border shrink-0 uppercase tracking-wider',
                theme.badgeStyle
              )}
            >
              {theme.badgeLabel}
            </span>
          </div>

          <p className="text-xs text-slate-600 leading-relaxed font-medium break-words">
            {item.message}
          </p>

          {item.action && (
            <div className="pt-1.5">
              <button
                type="button"
                onClick={() => {
                  item.action?.onClick();
                  onDismiss(item.id);
                }}
                className="text-xs font-bold text-emerald-700 hover:text-emerald-800 hover:underline cursor-pointer transition-colors"
              >
                {item.action.label} →
              </button>
            </div>
          )}
        </div>

        {/* Close Button */}
        <button
          type="button"
          onClick={() => onDismiss(item.id)}
          aria-label="Đóng thông báo"
          className="p-1 rounded-xl text-slate-400 hover:text-slate-700 hover:bg-slate-100/80 transition-colors shrink-0 cursor-pointer"
        >
          <X className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Auto-dismiss Animated Progress Bar */}
      {item.duration > 0 && item.duration !== Infinity && (
        <div className="w-full h-1 bg-slate-100/90 overflow-hidden">
          <div
            key={`${item.id}-${item.duration}`}
            className={cn('h-full origin-left', theme.progressBar)}
            style={{
              width: '100%',
              animation: `toast-progress ${item.duration}ms linear forwards`,
              animationPlayState: isHovered ? 'paused' : 'running',
            }}
          />
        </div>
      )}
    </div>
  );
};

export const Toaster: React.FC<ToasterProps> = ({
  position = 'top-right',
  maxToasts = 3,
}) => {
  const { toasts, removeToast } = useToastStore();

  if (toasts.length === 0) return null;

  const displayedToasts = toasts.slice(0, maxToasts);

  const content = (
    <>
      <style>{`
        @keyframes toast-progress {
          from {
            width: 100%;
          }
          to {
            width: 0%;
          }
        }
      `}</style>
      <aside
        aria-label="Thông báo hệ thống"
        className={cn(
          'fixed z-[9999] p-4 pointer-events-none flex gap-3 max-w-[calc(100vw-2rem)]',
          POSITION_CLASSES[position]
        )}
      >
        {displayedToasts.map((item) => (
          <ToastCard key={item.id} item={item} onDismiss={removeToast} />
        ))}
      </aside>
    </>
  );

  if (typeof document !== 'undefined' && document.body) {
    return createPortal(content, document.body);
  }

  return content;
};
