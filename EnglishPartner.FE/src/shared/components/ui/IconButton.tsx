import React from 'react';
import { Pencil, Trash2, Eye, Plus, Settings } from 'lucide-react';
import { cn } from '@/shared/lib/cn';

export type IconButtonVariant = 'default' | 'primary' | 'danger' | 'ghost';
export type IconButtonAction = 'edit' | 'delete' | 'view' | 'add' | 'settings';

export interface IconButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: IconButtonVariant;
  action?: IconButtonAction;
  icon?: React.ReactNode;
  label: string;
  size?: 'sm' | 'md';
}

function resolveActionIcon(action?: IconButtonAction): React.ReactNode {
  switch (action) {
    case 'edit':
      return <Pencil className="w-4 h-4" />;
    case 'delete':
      return <Trash2 className="w-4 h-4" />;
    case 'view':
      return <Eye className="w-4 h-4" />;
    case 'add':
      return <Plus className="w-4 h-4" />;
    case 'settings':
      return <Settings className="w-4 h-4" />;
    default:
      return null;
  }
}

/**
 * Standardized Reusable IconButton (36-40px hit target, 16-18px Lucide icon):
 * - Enforces consistent Tooltip (`title`) & accessibility (`aria-label`)
 * - Standard icons: Edit=Pencil, Delete=Trash2, View=Eye, Add=Plus, Settings=Settings
 */
export const IconButton: React.FC<IconButtonProps> = ({
  variant,
  action,
  icon,
  label,
  size = 'md',
  className,
  disabled,
  ...props
}) => {
  const effectiveVariant: IconButtonVariant =
    variant ?? (action === 'delete' ? 'danger' : action === 'add' ? 'primary' : 'default');

  const variants: Record<IconButtonVariant, string> = {
    default:
      'bg-white text-slate-600 border border-slate-200 hover:bg-slate-100 hover:text-slate-900 hover:border-slate-300 active:bg-slate-200 focus-visible:ring-2 focus-visible:ring-slate-400/40',
    primary:
      'bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-600 hover:text-white hover:border-emerald-600 active:bg-emerald-700 focus-visible:ring-2 focus-visible:ring-emerald-500/40',
    danger:
      'bg-rose-50/70 text-rose-600 border border-rose-200 hover:bg-rose-600 hover:text-white hover:border-rose-600 active:bg-rose-700 focus-visible:ring-2 focus-visible:ring-rose-500/40',
    ghost:
      'bg-transparent text-slate-500 border border-transparent hover:bg-slate-100 hover:text-slate-900 active:bg-slate-200 focus-visible:ring-2 focus-visible:ring-slate-400/40',
  };

  const sizes = {
    sm: 'w-9 h-9 rounded-xl',
    md: 'w-10 h-10 rounded-xl',
  };

  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      disabled={disabled}
      className={cn(
        'inline-flex items-center justify-center shrink-0 transition-all cursor-pointer focus:outline-none disabled:opacity-45 disabled:pointer-events-none',
        variants[effectiveVariant],
        sizes[size],
        className
      )}
      {...props}
    >
      {icon ?? resolveActionIcon(action)}
    </button>
  );
};
