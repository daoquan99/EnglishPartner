import React from 'react';
import { Inbox } from 'lucide-react';

export interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: React.ReactNode;
  action?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  icon,
  action,
}) => {
  return (
    <div className="py-12 px-6 text-center flex flex-col items-center justify-center space-y-3">
      <div className="w-12 h-12 rounded-2xl bg-slate-100 border border-slate-200 text-slate-500 flex items-center justify-center">
        {icon ?? <Inbox className="w-6 h-6" />}
      </div>
      <div className="space-y-1 max-w-md">
        <h3 className="text-sm font-extrabold text-slate-800">{title}</h3>
        {description && <p className="text-xs text-slate-500 leading-relaxed">{description}</p>}
      </div>
      {action && <div className="pt-1">{action}</div>}
    </div>
  );
};
