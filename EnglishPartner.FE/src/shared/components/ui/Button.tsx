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
      'bg-emerald-600 text-white hover:bg-emerald-700 shadow-sm border border-emerald-600 disabled:bg-emerald-300',
    secondary:
      'bg-amber-500 text-white hover:bg-amber-600 shadow-sm border border-amber-500 disabled:bg-amber-300',
    outline:
      'bg-white text-slate-700 hover:bg-slate-50 border border-slate-200 shadow-xs disabled:opacity-50',
    ghost:
      'bg-transparent text-slate-600 hover:bg-slate-100 border border-transparent disabled:opacity-50',
    danger:
      'bg-rose-600 text-white hover:bg-rose-700 shadow-sm border border-rose-600 disabled:bg-rose-300',
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
