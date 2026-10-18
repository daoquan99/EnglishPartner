import React from 'react';
import { ArrowUp, ArrowDown, Edit2, Trash2, Award, Layers } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { EXERCISE_TYPE_CATALOG, type ExerciseAuthorDto } from '../../types/lessons.types';

interface ExerciseCardProps {
  exercise: ExerciseAuthorDto;
  index: number;
  totalCount: number;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onEdit: () => void;
  onDelete: () => void;
  isMoving?: boolean;
}

export const ExerciseCard: React.FC<ExerciseCardProps> = ({
  exercise,
  index,
  totalCount,
  onMoveUp,
  onMoveDown,
  onEdit,
  onDelete,
  isMoving = false,
}) => {
  const meta = EXERCISE_TYPE_CATALOG[exercise.type];

  return (
    <div className="bg-white rounded-2xl border border-slate-200/90 p-4 sm:p-5 shadow-2xs hover:shadow-xs transition-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      {/* Exercise Information */}
      <div className="space-y-1.5 min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs font-mono font-bold text-slate-400 bg-slate-100 px-2 py-0.5 rounded-lg">
            #{index + 1}
          </span>

          <Badge variant={meta?.group === 'Learn' ? 'default' : meta?.group === 'Practice' ? 'sky' : 'amber'}>
            {meta?.labelVi ?? exercise.type}
          </Badge>

          <Badge variant="outline" className="text-slate-600">
            {exercise.gradingMode}
          </Badge>

          <span className="text-xs font-bold text-emerald-700 flex items-center gap-1">
            <Award className="w-3.5 h-3.5" />
            {exercise.maxPoints} điểm
          </span>
        </div>

        <div className="text-sm font-bold text-slate-900 truncate">
          {exercise.title}
        </div>

        {exercise.instructions && (
          <p className="text-xs text-slate-500 line-clamp-1">{exercise.instructions}</p>
        )}
      </div>

      {/* Actions: Reorder + Edit + Delete */}
      <div className="flex items-center gap-1.5 shrink-0 self-end sm:self-center">
        {/* Keyboard/Touch accessible Reorder buttons */}
        <div className="flex items-center bg-slate-50 rounded-xl border border-slate-200 p-0.5 mr-1">
          <button
            type="button"
            onClick={onMoveUp}
            disabled={index === 0 || isMoving}
            title="Di chuyển lên trên"
            className="p-1.5 text-slate-600 hover:text-slate-900 hover:bg-slate-200/70 rounded-lg transition-colors disabled:opacity-30 cursor-pointer"
          >
            <ArrowUp className="w-4 h-4" />
          </button>
          <button
            type="button"
            onClick={onMoveDown}
            disabled={index === totalCount - 1 || isMoving}
            title="Di chuyển xuống dưới"
            className="p-1.5 text-slate-600 hover:text-slate-900 hover:bg-slate-200/70 rounded-lg transition-colors disabled:opacity-30 cursor-pointer"
          >
            <ArrowDown className="w-4 h-4" />
          </button>
        </div>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onEdit}
          className="gap-1.5 text-xs text-slate-700"
        >
          <Edit2 className="w-3.5 h-3.5" /> Sửa
        </Button>

        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={onDelete}
          className="gap-1.5 text-xs text-rose-600 border-rose-200 hover:bg-rose-50"
        >
          <Trash2 className="w-3.5 h-3.5" /> Xóa
        </Button>
      </div>
    </div>
  );
};
