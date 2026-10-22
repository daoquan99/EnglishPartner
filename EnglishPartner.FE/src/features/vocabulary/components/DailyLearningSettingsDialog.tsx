import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Settings, Target, Layers, AlertCircle, Loader2 } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import type { CefrLevel } from '@/shared/api/openapi.types';
import type {
  DailyVocabularyPlanResponseDto,
  UserLearningSettingsDto,
} from '../types/vocabulary.types';

interface DailyLearningSettingsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  settings?: UserLearningSettingsDto;
  todayPlan?: DailyVocabularyPlanResponseDto;
  onSave: (target: number, level: CefrLevel) => Promise<void>;
  isSaving: boolean;
}

const CEFR_LEVELS: { level: CefrLevel; label: string; desc: string }[] = [
  { level: 'A1', label: 'A1 - Beginner', desc: 'Từ vựng cơ bản nhập môn' },
  { level: 'A2', label: 'A2 - Elementary', desc: 'Giao tiếp tình huống hàng ngày' },
  { level: 'B1', label: 'B1 - Intermediate', desc: 'Công việc & học tập thông dụng' },
  { level: 'B2', label: 'B2 - Upper Intermediate', desc: 'Chuyên môn & ngữ cảnh phức tạp' },
  { level: 'C1', label: 'C1 - Advanced', desc: 'Học thuật & thảo luận chuyên sâu' },
  { level: 'C2', label: 'C2 - Proficiency', desc: 'Thành thạo gần như người bản xứ' },
];

export const DailyLearningSettingsDialog: React.FC<DailyLearningSettingsDialogProps> = ({
  isOpen,
  onClose,
  settings,
  todayPlan,
  onSave,
  isSaving,
}) => {
  const { t } = useTranslation();
  const [target, setTarget] = useState<number>(20);
  const [level, setLevel] = useState<CefrLevel>('B1');
  const [validationError, setValidationError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setTarget(settings?.dailyVocabularyTarget ?? 20);
      setLevel((settings?.cefrLevel as CefrLevel) ?? 'B1');
      setValidationError(null);
    }
  }, [isOpen, settings]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (target < 1 || target > 100 || isNaN(target)) {
      setValidationError('Mục tiêu từ vựng mỗi ngày phải từ 1 đến 100 từ.');
      return;
    }

    setValidationError(null);
    try {
      await onSave(target, level);
      onClose();
    } catch (err: any) {
      setValidationError(err?.response?.data?.error || err?.message || 'Không thể lưu cài đặt.');
    }
  };

  const hasPlanToday = Boolean(todayPlan);

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      maxWidth="md"
      icon={<Settings className="w-5 h-5 text-white" />}
      title="Mục tiêu học từ vựng mỗi ngày"
      description="Tùy chỉnh số lượng từ vựng học lần đầu mỗi ngày và trình độ CEFR để bổ sung từ vựng mới."
    >
      <form onSubmit={handleSubmit} className="space-y-5 pt-2">
        {/* Target Words Input */}
        <div className="space-y-1.5">
          <label htmlFor="daily-target-input" className="flex items-center gap-1.5 text-xs font-bold text-slate-700">
            <Target className="w-3.5 h-3.5 text-emerald-600" />
            Số từ học lần đầu mỗi ngày (1 – 100 từ)
          </label>
          <div className="relative">
            <input
              id="daily-target-input"
              type="number"
              min={1}
              max={100}
              value={target}
              onChange={(e) => {
                const val = parseInt(e.target.value, 10);
                setTarget(isNaN(val) ? 0 : val);
                if (validationError) setValidationError(null);
              }}
              className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm font-semibold text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent"
              placeholder="Nhập số từ (ví dụ: 20)"
              required
            />
            <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-xs font-medium text-slate-400">
              từ / ngày
            </span>
          </div>
          <p className="text-[11px] text-slate-500">
            Mục tiêu học lần đầu; các bài ôn tập đến hạn sẽ được phân nhóm riêng và không trừ vào chỉ tiêu này.
          </p>
        </div>

        {/* CEFR Level Selection */}
        <div className="space-y-1.5">
          <label htmlFor="cefr-level-select" className="flex items-center gap-1.5 text-xs font-bold text-slate-700">
            <Layers className="w-3.5 h-3.5 text-emerald-600" />
            Trình độ CEFR lấy từ bổ sung
          </label>
          <select
            id="cefr-level-select"
            value={level}
            onChange={(e) => setLevel(e.target.value as CefrLevel)}
            className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 text-sm font-semibold text-slate-900 bg-white focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent"
          >
            {CEFR_LEVELS.map((item) => (
              <option key={item.level} value={item.level}>
                {item.label} — {item.desc}
              </option>
            ))}
          </select>
          <p className="text-[11px] text-slate-500">
            Khi thay đổi trình độ, các từ cũ chưa học xong vẫn được bảo toàn chuyển tiếp, chỉ từ mới bổ sung được lấy theo trình độ này.
          </p>
        </div>

        {/* Informational Context Banner */}
        <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 text-xs space-y-1">
          {hasPlanToday ? (
            <>
              <div className="font-bold text-slate-800">
                Kế hoạch hôm nay: {todayPlan?.targetWordCount ?? 20} từ · Trình độ {todayPlan?.targetCefrLevel ?? 'B1'}
              </div>
              <div className="text-slate-600">
                Thay đổi mục tiêu này sẽ áp dụng từ ngày kế tiếp{' '}
                <span className="font-semibold text-emerald-700">({settings?.effectiveFromLearningDate})</span>.
              </div>
            </>
          ) : (
            <div className="text-slate-600">
              Bạn chưa bắt đầu kế hoạch học hôm nay. Cấu hình mục tiêu này sẽ được{' '}
              <span className="font-semibold text-emerald-700">áp dụng ngay</span> khi bạn tạo kế hoạch.
            </div>
          )}
        </div>

        {/* Error Feedback */}
        {validationError && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 flex items-start gap-2 text-xs text-rose-700 font-medium">
            <AlertCircle className="w-4 h-4 text-rose-600 shrink-0 mt-0.5" />
            <span>{validationError}</span>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-end gap-3 pt-2">
          <Button type="button" variant="outline" size="sm" onClick={onClose} disabled={isSaving}>
            Hủy
          </Button>
          <Button type="submit" variant="primary" size="sm" disabled={isSaving}>
            {isSaving ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Đang lưu...
              </>
            ) : (
              'Lưu mục tiêu'
            )}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
