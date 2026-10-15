import React from 'react';
import { AlertTriangle, Trash2 } from 'lucide-react';
import { Dialog } from './Dialog';
import { Button } from './Button';

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title?: string;
  targetName: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isLoading?: boolean;
}

/**
 * Shared Confirmation Dialog for Delete / Destructive operations.
 * Displays the exact name of the object about to be deleted.
 */
export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title = 'Xác nhận xóa dữ liệu',
  targetName,
  description = 'Hành động này sẽ xóa bản ghi khỏi danh sách hoạt động. Bạn có chắc chắn muốn tiếp tục?',
  confirmLabel = 'Xác nhận xóa',
  cancelLabel = 'Hủy bỏ',
  isLoading = false,
}) => {
  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="md"
      title={title}
      icon={<AlertTriangle className="w-5 h-5" />}
      footer={
        <div className="flex items-center justify-end gap-2.5 w-full">
          <Button variant="outline" size="sm" onClick={onClose} disabled={isLoading}>
            {cancelLabel}
          </Button>
          <Button variant="danger" size="sm" onClick={onConfirm} disabled={isLoading}>
            <Trash2 className="w-4 h-4" />
            {isLoading ? 'Đang xử lý...' : confirmLabel}
          </Button>
        </div>
      }
    >
      <div className="space-y-3 text-sm text-slate-600">
        <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 text-rose-900">
          Đối tượng sắp xóa: <strong className="font-extrabold">{targetName}</strong>
        </div>
        <p className="text-xs leading-relaxed text-slate-600">{description}</p>
      </div>
    </Dialog>
  );
};
