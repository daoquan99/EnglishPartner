import React from 'react';
import { Inbox } from 'lucide-react';

export type EmptyStateIllustration =
  | 'welcome'
  | 'calm-rest'
  | 'celebration'
  | 'load-error'
  | 'no-plan'
  | 'searching';

export interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: React.ReactNode;
  illustration?: EmptyStateIllustration | string;
  action?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  icon,
  illustration,
  action,
}) => {
  const illustrationSrc = illustration
    ? illustration.startsWith('/') || illustration.startsWith('http')
      ? illustration
      : `/assets/illustrations/illustration-${illustration}.svg`
    : null;

  return (
    <div className="py-10 px-6 text-center flex flex-col items-center justify-center space-y-3">
      {illustrationSrc ? (
        <div className="mb-1">
          <img
            src={illustrationSrc}
            alt=""
            className="w-36 h-28 object-contain mx-auto drop-shadow-xs select-none"
            loading="lazy"
          />
        </div>
      ) : (
        <div className="w-12 h-12 rounded-2xl bg-slate-100 border border-slate-200 text-slate-500 flex items-center justify-center">
          {icon ?? <Inbox className="w-6 h-6" />}
        </div>
      )}

      <div className="space-y-1 max-w-md">
        <h3 className="text-sm font-extrabold text-slate-800">{title}</h3>
        {description && <p className="text-xs text-slate-500 leading-relaxed">{description}</p>}
      </div>

      {action && <div className="pt-1">{action}</div>}
    </div>
  );
};
