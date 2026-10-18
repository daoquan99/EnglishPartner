import React from 'react';
import { cn } from '@/shared/lib/cn';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
}

export const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  className,
  children,
  ...props
}) => {
  const variants: Record<NonNullable<ButtonProps['variant']>, string> = {
    primary:
      'bg-emerald-600 text-white hover:bg-emerald-700 active:bg-emerald-800 shadow-xs border border-emerald-600 hover:border-emerald-700 disabled:bg-slate-100 disabled:text-slate-400 disabled:border-slate-200 disabled:shadow-none',
    secondary:
      'bg-amber-500 text-white hover:bg-amber-600 active:bg-amber-700 shadow-xs border border-amber-500 hover:border-amber-600 disabled:bg-slate-100 disabled:text-slate-400 disabled:border-slate-200 disabled:shadow-none',
    outline:
      'bg-white text-slate-700 hover:bg-slate-50 active:bg-slate-100 border border-slate-200 shadow-2xs disabled:bg-slate-50 disabled:text-slate-300 disabled:border-slate-100 disabled:shadow-none',
    ghost:
      'bg-transparent text-slate-600 hover:bg-slate-100 active:bg-slate-200 border border-transparent disabled:opacity-40 disabled:hover:bg-transparent',
    danger:
      'bg-rose-600 text-white hover:bg-rose-700 active:bg-rose-800 shadow-xs border border-rose-600 hover:border-rose-700 disabled:bg-slate-100 disabled:text-slate-400 disabled:border-slate-200 disabled:shadow-none',
  };

  const sizes: Record<NonNullable<ButtonProps['size']>, string> = {
    sm: 'px-3 py-1.5 text-xs rounded-xl font-semibold',
    md: 'px-4 py-2 text-sm rounded-xl font-semibold',
    lg: 'px-5 py-2.5 text-base rounded-2xl font-bold',
  };

  return (
    <button
      className={cn(
        'inline-flex items-center justify-center gap-2 transition-all cursor-pointer disabled:cursor-not-allowed',
        variants[variant],
        sizes[size],
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
};
