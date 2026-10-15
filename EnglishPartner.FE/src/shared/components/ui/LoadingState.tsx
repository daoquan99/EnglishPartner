import React from 'react';
import { Loader2 } from 'lucide-react';

export interface LoadingStateProps {
  message?: string;
}

export const LoadingState: React.FC<LoadingStateProps> = ({
  message = 'Đang tải dữ liệu từ hệ thống...',
}) => {
  return (
    <div className="py-12 px-6 flex flex-col items-center justify-center gap-2.5 text-slate-500">
      <Loader2 className="w-6 h-6 text-emerald-600 animate-spin" />
      <span className="text-xs font-semibold">{message}</span>
    </div>
  );
};
