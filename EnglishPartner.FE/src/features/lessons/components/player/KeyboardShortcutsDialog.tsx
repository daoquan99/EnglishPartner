import React from 'react';
import { Keyboard, ArrowRight, CornerDownLeft, Volume2, Mic, Sparkles, SlidersHorizontal } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';

export interface KeyboardShortcutsDialogProps {
  isOpen: boolean;
  onClose: () => void;
}

interface ShortcutItem {
  keys: string[];
  description: string;
  icon?: React.ReactNode;
}

interface ShortcutGroup {
  groupName: string;
  items: ShortcutItem[];
}

const SHORTCUT_GROUPS: ShortcutGroup[] = [
  {
    groupName: 'Làm bài & Nộp bài',
    items: [
      {
        keys: ['Ctrl', 'Enter'],
        description: 'Xác nhận nộp bài (áp dụng cho mọi dạng bài tập, kể cả khi con trỏ đang ở trong ô nhập văn bản)',
        icon: <CornerDownLeft className="w-4 h-4 text-emerald-600" />,
      },
      {
        keys: ['Ctrl', 'Space'],
        description: 'Bật / Dừng phát âm thanh mẫu (Listening, Dictation, Shadowing, Flashcard, v.v.)',
        icon: <Volume2 className="w-4 h-4 text-blue-600" />,
      },
      {
        keys: ['Alt', 'R'],
        description: 'Bật / Dừng ghi âm microphone (Speaking Practice, Shadowing)',
        icon: <Mic className="w-4 h-4 text-rose-600" />,
      },
      {
        keys: ['Alt', 'I'],
        description: 'Gửi bài chép cho AI nhận xét chi tiết (Dictation)',
        icon: <Sparkles className="w-4 h-4 text-amber-600" />,
      },
    ],
  },
  {
    groupName: 'Điều hướng bài học',
    items: [
      {
        keys: ['Alt', '→'],
        description: 'Chuyển sang bài tập tiếp theo (hoặc Ctrl + →)',
        icon: <ArrowRight className="w-4 h-4 text-indigo-600" />,
      },
      {
        keys: ['Alt', '←'],
        description: 'Quay lại bài tập trước (hoặc Ctrl + ←)',
        icon: <ArrowRight className="w-4 h-4 text-indigo-600 rotate-180" />,
      },
    ],
  },
  {
    groupName: 'Cấu hình & Trợ giúp',
    items: [
      {
        keys: ['Alt', 'S'],
        description: 'Mở cửa sổ Cấu hình AI Speech & Luyện âm',
        icon: <SlidersHorizontal className="w-4 h-4 text-slate-600" />,
      },
      {
        keys: ['?'],
        description: 'Hiển thị danh sách phím tắt này (khi không ở trong ô nhập văn bản)',
        icon: <Keyboard className="w-4 h-4 text-slate-600" />,
      },
      {
        keys: ['Esc'],
        description: 'Đóng hộp thoại đang mở',
      },
    ],
  },
];

export const KeyboardShortcutsDialog: React.FC<KeyboardShortcutsDialogProps> = ({
  isOpen,
  onClose,
}) => {
  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      title="Phím tắt luyện bài học (Hotkeys)"
      description="Sử dụng phím tắt giúp bạn làm bài tập nhanh chóng và liền mạch mà không cần dùng chuột."
      icon={<Keyboard className="w-5 h-5 text-emerald-600" />}
      maxWidth="lg"
      footer={
        <div className="flex justify-end w-full">
          <Button type="button" variant="primary" size="sm" onClick={onClose} className="rounded-xl px-4">
            Đã hiểu
          </Button>
        </div>
      }
    >
      <div className="space-y-6 py-1">
        {SHORTCUT_GROUPS.map((group) => (
          <div key={group.groupName} className="space-y-2.5">
            <h4 className="text-xs font-bold text-slate-500 uppercase tracking-wider">
              {group.groupName}
            </h4>
            <div className="bg-slate-50/80 rounded-2xl border border-slate-200/80 divide-y divide-slate-100 overflow-hidden">
              {group.items.map((item, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-3 gap-3 hover:bg-slate-100/60 transition-colors"
                >
                  <div className="flex items-center gap-2.5 min-w-0">
                    {item.icon && <div className="shrink-0">{item.icon}</div>}
                    <span className="text-xs text-slate-700 leading-snug font-medium">
                      {item.description}
                    </span>
                  </div>
                  <div className="flex items-center gap-1 shrink-0 font-mono">
                    {item.keys.map((k, kIdx) => (
                      <React.Fragment key={kIdx}>
                        <kbd className="px-2 py-1 text-[11px] font-bold bg-white text-slate-800 border border-slate-300 rounded-md shadow-2xs">
                          {k}
                        </kbd>
                        {kIdx < item.keys.length - 1 && (
                          <span className="text-slate-400 text-xs font-bold">+</span>
                        )}
                      </React.Fragment>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </Dialog>
  );
};
