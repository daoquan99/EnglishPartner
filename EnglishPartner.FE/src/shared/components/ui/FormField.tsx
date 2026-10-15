import React from 'react';
import { cn } from '@/shared/lib/cn';

export interface FormFieldProps {
  label: string;
  required?: boolean;
  error?: string | null;
  helperText?: string;
  className?: string;
  children: React.ReactNode;
}

export const FormField: React.FC<FormFieldProps> = ({
  label,
  required,
  error,
  helperText,
  className,
  children,
}) => {
  return (
    <div className={cn('space-y-1.5', className)}>
      <label className="block text-xs font-bold text-slate-700">
        {label}
        {required && <span className="text-rose-600 ml-0.5">*</span>}
      </label>
      {children}
      {helperText && !error && <p className="text-[11px] text-slate-500">{helperText}</p>}
      {error && <p className="text-[11px] font-semibold text-rose-600">{error}</p>}
    </div>
  );
};
