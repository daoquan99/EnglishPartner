import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { EmptyState } from './EmptyState';
import { LoadingState } from './LoadingState';

export interface DataTableColumn<T> {
  key: string;
  header: React.ReactNode;
  className?: string;
  render: (row: T, index: number) => React.ReactNode;
}

export interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  data: T[];
  rowKey: (row: T, index: number) => string;
  isLoading?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: React.ReactNode;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (newPage: number) => void;
  onPageSizeChange?: (newSize: number) => void;
}

/**
 * Modern SaaS DataTable with standardized header, loading/empty states, and Pagination bar.
 */
export function DataTable<T>({
  columns,
  data,
  rowKey,
  isLoading = false,
  emptyTitle = 'Chưa có dữ liệu phù hợp',
  emptyDescription = 'Hãy thử thay đổi bộ lọc tìm kiếm hoặc nhấn nút Thêm mới để tạo bản ghi đầu tiên.',
  emptyAction,
  page = 1,
  pageSize = 10,
  totalCount,
  onPageChange,
  onPageSizeChange,
}: DataTableProps<T>) {
  const effectiveTotal = totalCount ?? data.length;
  const totalPages = Math.max(1, Math.ceil(effectiveTotal / pageSize));
  const safePage = Math.min(Math.max(1, page), totalPages);

  // If caller passes un-sliced data without totalCount override, slice client-side automatically
  const rows =
    totalCount !== undefined && totalCount > data.length
      ? data
      : data.slice((safePage - 1) * pageSize, safePage * pageSize);

  const startIdx = effectiveTotal === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIdx = Math.min(effectiveTotal, safePage * pageSize);

  return (
    <div className="bg-white rounded-2xl border border-slate-200 shadow-2xs overflow-hidden">
      {isLoading ? (
        <LoadingState />
      ) : rows.length === 0 ? (
        <EmptyState
          title={emptyTitle}
          description={emptyDescription}
          action={emptyAction}
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50/90 border-b border-slate-200 text-slate-600 font-bold uppercase tracking-wider text-[11px]">
                {columns.map((col) => (
                  <th key={col.key} className={cn('py-3.5 px-4', col.className)}>
                    {col.header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rows.map((row, idx) => (
                <tr
                  key={rowKey(row, idx)}
                  className="hover:bg-slate-50/80 transition-colors"
                >
                  {columns.map((col) => (
                    <td key={col.key} className={cn('py-3.5 px-4 align-middle', col.className)}>
                      {col.render(row, idx)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Standardized SaaS Pagination Footer */}
      {effectiveTotal > 0 && onPageChange && (
        <div className="px-4 py-3 bg-slate-50/70 border-t border-slate-200/80 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-600">
          <div className="flex items-center gap-2">
            <span>
              Hiển thị <strong className="text-slate-900">{startIdx}</strong>–
              <strong className="text-slate-900">{endIdx}</strong> trên tổng{' '}
              <strong className="text-slate-900">{effectiveTotal}</strong> bản ghi
            </span>
            {onPageSizeChange && (
              <select
                value={pageSize}
                onChange={(e) => {
                  onPageSizeChange(Number(e.target.value));
                  onPageChange(1);
                }}
                aria-label="Số dòng mỗi trang"
                className="ml-2 px-2 py-1 rounded-lg border border-slate-200 bg-white text-xs font-semibold text-slate-700"
              >
                <option value={6}>6 / trang</option>
                <option value={10}>10 / trang</option>
                <option value={20}>20 / trang</option>
                <option value={50}>50 / trang</option>
              </select>
            )}
          </div>

          <div className="flex items-center gap-1.5">
            <button
              type="button"
              disabled={safePage <= 1}
              onClick={() => onPageChange(safePage - 1)}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 font-semibold hover:bg-slate-100 disabled:opacity-40 disabled:pointer-events-none cursor-pointer"
            >
              <ChevronLeft className="w-3.5 h-3.5" />
              Trước
            </button>
            <span className="px-2.5 py-1 font-bold text-slate-800">
              Trang {safePage} / {totalPages}
            </span>
            <button
              type="button"
              disabled={safePage >= totalPages}
              onClick={() => onPageChange(safePage + 1)}
              className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 font-semibold hover:bg-slate-100 disabled:opacity-40 disabled:pointer-events-none cursor-pointer"
            >
              Sau
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
