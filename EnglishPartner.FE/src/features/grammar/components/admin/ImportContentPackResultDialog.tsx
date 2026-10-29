import React from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { CheckCircle2, AlertTriangle, Layers } from 'lucide-react';
import type { GrammarContentPackImportResultDto } from '../../types/grammar.types';

export interface ImportContentPackResultDialogProps {
  isOpen: boolean;
  onClose: () => void;
  result: GrammarContentPackImportResultDto | null;
}

export const ImportContentPackResultDialog: React.FC<ImportContentPackResultDialogProps> = ({
  isOpen,
  onClose,
  result,
}) => {
  const { t } = useTranslation();

  if (!result) return null;

  const hasErrors = result.errors && result.errors.length > 0;

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      title={t('admin.grammar.importResultTitle')}
      maxWidth="md"
    >
      <div className="space-y-4">
        <div className="flex items-center gap-3 p-3 rounded-xl bg-emerald-50 border border-emerald-200">
          <CheckCircle2 className="w-6 h-6 text-emerald-600 shrink-0" />
          <div>
            <h4 className="text-xs font-bold text-emerald-950">
              {t('admin.grammar.importCompletedTitle')}
            </h4>
            <p className="text-[11px] text-emerald-800">
              {t('admin.grammar.importCompletedDesc')}
            </p>
          </div>
        </div>

        {/* Stats breakdown */}
        <div className="grid grid-cols-2 gap-2 text-xs">
          <div className="p-2.5 rounded-xl border border-slate-200 bg-slate-50">
            <span className="text-slate-500 block text-[11px]">{t('admin.grammar.stepsCreated')}</span>
            <span className="text-base font-extrabold text-slate-900">{result.stepsCreated}</span>
          </div>
          <div className="p-2.5 rounded-xl border border-slate-200 bg-slate-50">
            <span className="text-slate-500 block text-[11px]">{t('admin.grammar.stepsUpdated')}</span>
            <span className="text-base font-extrabold text-slate-900">{result.stepsUpdated}</span>
          </div>
          <div className="p-2.5 rounded-xl border border-slate-200 bg-slate-50">
            <span className="text-slate-500 block text-[11px]">{t('admin.grammar.stepsSkipped')}</span>
            <span className="text-base font-extrabold text-slate-900">{result.stepsSkipped}</span>
          </div>
          <div className="p-2.5 rounded-xl border border-slate-200 bg-slate-50">
            <span className="text-slate-500 block text-[11px]">{t('admin.grammar.exercisesCreated')}</span>
            <span className="text-base font-extrabold text-slate-900">{result.exercisesCreated}</span>
          </div>
          <div className="p-2.5 rounded-xl border border-slate-200 bg-slate-50 col-span-2">
            <span className="text-slate-500 block text-[11px]">{t('admin.grammar.exercisesSkipped')}</span>
            <span className="text-base font-extrabold text-slate-900">{result.exercisesSkipped}</span>
          </div>
        </div>

        {/* Errors / Issues */}
        {hasErrors && (
          <div className="space-y-2 p-3 rounded-xl bg-amber-50 border border-amber-200">
            <div className="flex items-center gap-1.5 text-xs font-bold text-amber-900">
              <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
              <span>{t('admin.grammar.importErrorsTitle', { count: result.errors.length })}</span>
            </div>
            <div className="max-h-36 overflow-y-auto space-y-1">
              {result.errors.map((err, i) => (
                <div key={i} className="text-[11px] text-amber-800">
                  <span className="font-mono font-semibold">
                    {err.code ? `[${err.code}] ` : ''}
                    {err.key ? `(${err.key}) ` : ''}
                  </span>
                  <span>{err.message}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex justify-end pt-2 border-t border-slate-100">
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={onClose}
          >
            {t('common.close')}
          </Button>
        </div>
      </div>
    </Dialog>
  );
};
