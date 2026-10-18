import React from 'react';
import { X, Award, ShieldCheck } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { useLessonRubricDetailQuery } from '../../hooks/useLessonQueries';

interface LessonRubricDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  rubricId: string | null;
}

export const LessonRubricDetailModal: React.FC<LessonRubricDetailModalProps> = ({
  isOpen,
  onClose,
  rubricId,
}) => {
  const { data: rubric, isLoading } = useLessonRubricDetailQuery(rubricId ?? undefined);

  if (!isOpen || !rubricId) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 overflow-y-auto">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-2xl w-full max-w-3xl max-h-[90vh] flex flex-col my-8">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-bold text-slate-900">
                {rubric?.name ?? 'Chi tiết Rubric đánh giá'}
              </h3>
              {rubric && (
                <Badge variant={rubric.isActive ? 'emerald' : 'default'}>
                  {rubric.isActive ? 'Đang hoạt động' : 'Đã lưu trữ'}
                </Badge>
              )}
            </div>
            {rubric && (
              <p className="text-xs text-slate-500 font-mono mt-0.5">
                Mã: {rubric.code} • Phiên bản: v{rubric.version} • CEFR: {rubric.cefrLevel}
              </p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 rounded-xl hover:bg-slate-100 cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-5 space-y-4">
          {isLoading ? (
            <div className="text-xs text-slate-500 text-center py-8">Đang tải chi tiết rubric...</div>
          ) : rubric ? (
            <div className="space-y-4">
              <div className="bg-slate-50 p-4 rounded-2xl border border-slate-100 text-xs text-slate-700 leading-relaxed">
                {rubric.description}
              </div>

              <div className="space-y-3">
                <span className="text-xs font-bold text-slate-700 uppercase tracking-wider block">
                  Danh sách tiêu chí đánh giá ({rubric.criteria.length} tiêu chí):
                </span>

                {rubric.criteria.map((c) => (
                  <div
                    key={c.id}
                    className="p-4 rounded-2xl border border-slate-200/90 bg-white space-y-2.5 shadow-2xs"
                  >
                    <div className="flex items-center justify-between font-bold text-slate-800 text-xs sm:text-sm">
                      <span className="flex items-center gap-1.5">
                        <Award className="w-4 h-4 text-emerald-600" />
                        {c.name}
                      </span>
                      <span className="text-emerald-700 font-extrabold bg-emerald-50 px-2 py-0.5 rounded-lg border border-emerald-100">
                        Trọng số: {c.weight}%
                      </span>
                    </div>

                    {c.description && (
                      <p className="text-xs text-slate-600 italic">{c.description}</p>
                    )}

                    {/* Descriptors 0-4 */}
                    <div className="grid grid-cols-1 sm:grid-cols-5 gap-1.5 pt-1 text-[11px]">
                      {[
                        { level: 0, desc: c.descriptorLevel0, label: '0: Chưa đạt' },
                        { level: 1, desc: c.descriptorLevel1, label: '1: Dưới TB' },
                        { level: 2, desc: c.descriptorLevel2, label: '2: Trung bình' },
                        { level: 3, desc: c.descriptorLevel3, label: '3: Tốt' },
                        { level: 4, desc: c.descriptorLevel4, label: '4: Xuất sắc' },
                      ].map((d) => (
                        <div
                          key={d.level}
                          className="p-2 rounded-xl bg-slate-50 border border-slate-100 space-y-0.5"
                        >
                          <span className="font-bold text-slate-800 block text-[10px]">
                            {d.label}
                          </span>
                          <p className="text-slate-600 leading-snug line-clamp-3" title={d.desc}>
                            {d.desc}
                          </p>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="text-xs text-rose-500 text-center py-8">Không tìm thấy thông tin rubric.</div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end p-5 border-t border-slate-100">
          <Button type="button" variant="primary" onClick={onClose}>
            Đóng
          </Button>
        </div>
      </div>
    </div>
  );
};
