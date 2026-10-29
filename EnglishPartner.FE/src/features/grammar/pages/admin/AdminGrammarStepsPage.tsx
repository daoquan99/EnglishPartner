import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  DownloadCloud,
  CheckCircle2,
  Clock,
  Layers,
  Edit3,
  BookOpen,
  ArrowRight,
  Sparkles,
  AlertTriangle,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { DataTable, type DataTableColumn } from '@/shared/components/ui/DataTable';
import { useCurrentUser, hasPermission } from '@/features/auth';
import {
  useAdminGrammarStepsQuery,
  useImportAdminContentPackMutation,
} from '../../hooks/useAdminGrammarQueries';
import { ImportContentPackResultDialog } from '../../components/admin/ImportContentPackResultDialog';
import { toast } from '@/shared/lib/toast';
import { getAdminGrammarErrorMessage } from '../../utils/grammarHelpers';
import type {
  GrammarStepAdminDto,
  GrammarContentPackImportResultDto,
} from '../../types/grammar.types';

const CEFR_LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const;

export const AdminGrammarStepsPage: React.FC = () => {
  const { t } = useTranslation();
  const { user } = useCurrentUser();
  const canModerate = hasPermission(user, 'grammar.moderate');

  const [activeLevel, setActiveLevel] = useState<string>('A1');
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(20);

  const [isImportConfirmOpen, setIsImportConfirmOpen] = useState<boolean>(false);
  const [importResult, setImportResult] = useState<GrammarContentPackImportResultDto | null>(null);

  const {
    data: stepsData,
    isLoading,
    error,
    refetch,
  } = useAdminGrammarStepsQuery({
    level: activeLevel,
    page,
    pageSize,
    paged: true,
  });

  // Adjust page if page > totalPages
  useEffect(() => {
    if (stepsData && stepsData.totalPages > 0 && page > stepsData.totalPages) {
      setPage(stepsData.totalPages);
    }
  }, [stepsData?.totalPages, page]);

  const importMutation = useImportAdminContentPackMutation();

  const handleLevelChange = (level: string) => {
    setActiveLevel(level);
    setPage(1);
  };

  const handleConfirmImport = async () => {
    try {
      const result = await importMutation.mutateAsync();
      setIsImportConfirmOpen(false);
      setImportResult(result);
      toast.success(t('admin.grammar.importSuccess'));
    } catch (err: any) {
      toast.error(getAdminGrammarErrorMessage(err, t));
    }
  };

  const columns: DataTableColumn<GrammarStepAdminDto>[] = [
    {
      key: 'code',
      header: t('admin.grammar.codeHeader'),
      className: 'w-28 font-mono font-bold text-slate-900',
      render: (step) => (
        <div className="flex items-center gap-1.5">
          <span className="px-2 py-0.5 rounded-md bg-slate-100 text-slate-800 text-xs font-mono font-bold border border-slate-200">
            {step.code}
          </span>
          {step.isLevelReview && (
            <Badge variant="amber">
              {t('grammar.levelReview')}
            </Badge>
          )}
        </div>
      ),
    },
    {
      key: 'title',
      header: t('admin.grammar.titleHeader'),
      render: (step) => (
        <div className="space-y-0.5 min-w-[200px]">
          <div className="text-xs font-bold text-slate-900 line-clamp-1">{step.titleVi}</div>
          <div className="text-[11px] text-slate-500 italic line-clamp-1">{step.titleEn}</div>
        </div>
      ),
    },
    {
      key: 'exercises',
      header: t('admin.grammar.exercisesCountHeader'),
      render: (step) => (
        <div className="flex items-center gap-2 text-xs">
          <span
            title={t('admin.grammar.coreCountTooltip')}
            className="px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-800 font-semibold border border-emerald-200"
          >
            {step.coreExerciseCount} {t('admin.grammar.coreLabel')}
          </span>
          <span
            title={t('admin.grammar.officialCountTooltip')}
            className="px-2 py-0.5 rounded-full bg-blue-50 text-blue-800 font-semibold border border-blue-200"
          >
            {step.officialExerciseCount} {t('admin.grammar.officialLabel')}
          </span>
          {step.pendingExerciseCount > 0 && (
            <span
              title={t('admin.grammar.pendingCountTooltip')}
              className="px-2 py-0.5 rounded-full bg-amber-50 text-amber-800 font-semibold border border-amber-200"
            >
              {step.pendingExerciseCount} {t('admin.grammar.pendingLabel')}
            </span>
          )}
        </div>
      ),
    },
    {
      key: 'published',
      header: t('admin.grammar.statusHeader'),
      className: 'w-24',
      render: (step) => (
        step.isPublished ? (
          <Badge variant="emerald">
            {t('admin.grammar.published')}
          </Badge>
        ) : (
          <Badge variant="default">
            {t('admin.grammar.draft')}
          </Badge>
        )
      ),
    },
    {
      key: 'theoryEdited',
      header: t('admin.grammar.theoryStatusHeader'),
      render: (step) => (
        step.theoryEditedByAdmin ? (
          <span className="text-[11px] font-semibold text-purple-700 bg-purple-50 px-2 py-0.5 rounded-md border border-purple-200">
            {t('admin.grammar.theoryEdited')} (v{step.theoryVersion})
          </span>
        ) : (
          <span className="text-[11px] font-semibold text-slate-600 bg-slate-50 px-2 py-0.5 rounded-md border border-slate-200">
            {t('admin.grammar.theoryOriginal')} (v{step.theoryVersion})
          </span>
        )
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      className: 'w-28 text-right',
      render: (step) => (
        <Link
          to={`/admin/grammar/${encodeURIComponent(step.code)}`}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-100 hover:bg-emerald-50 text-slate-700 hover:text-emerald-700 font-bold text-xs transition-colors"
        >
          <Edit3 className="w-3.5 h-3.5" />
          <span>{t('admin.grammar.editStep')}</span>
        </Link>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header bar */}
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center shrink-0">
            <BookOpen className="w-6 h-6 text-emerald-600" />
          </div>
          <div>
            <h1 className="text-xl font-extrabold text-slate-900">
              {t('admin.grammar.stepsPageTitle')}
            </h1>
            <p className="text-xs text-slate-500 mt-0.5">
              {t('admin.grammar.stepsPageSubtitle')}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {canModerate && (
            <Link
              to="/admin/grammar/moderation"
              className="inline-flex items-center gap-2 px-3.5 py-2 rounded-2xl bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold transition-colors"
            >
              <Clock className="w-4 h-4 text-amber-600" />
              <span>{t('admin.grammar.moderationQueueLink')}</span>
            </Link>
          )}
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={() => setIsImportConfirmOpen(true)}
            disabled={importMutation.isPending}
            className="flex items-center gap-2"
          >
            <DownloadCloud className="w-4 h-4" />
            <span>{t('admin.grammar.importContentPack')}</span>
          </Button>
        </div>
      </div>

      {/* Level Tabs */}
      <div
        role="tablist"
        aria-label={t('common.level')}
        className="flex items-center gap-1.5 border-b border-slate-200 pb-2 overflow-x-auto"
      >
        {CEFR_LEVELS.map((level) => {
          const isActive = activeLevel === level;
          return (
            <button
              key={level}
              type="button"
              role="tab"
              id={`tab-${level}`}
              aria-selected={isActive}
              aria-controls={`panel-${level}`}
              onClick={() => handleLevelChange(level)}
              className={`px-4 py-2 rounded-2xl text-xs font-extrabold transition-all cursor-pointer whitespace-nowrap ${
                isActive
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'bg-white text-slate-600 hover:bg-slate-100 border border-slate-200'
              }`}
            >
              {t('grammar.levelTitle', { level })}
            </button>
          );
        })}
      </div>

      {/* Error Banner */}
      {error && (
        <div className="bg-white rounded-3xl border border-slate-200 p-8 text-center space-y-4">
          <AlertTriangle className="w-10 h-10 text-rose-500 mx-auto" />
          <h2 className="text-lg font-bold text-slate-900">
            {(error as any)?.response?.status === 403
              ? t('common.forbidden')
              : t('common.error')}
          </h2>
          <p className="text-xs text-slate-500">{getAdminGrammarErrorMessage(error, t)}</p>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            className="inline-flex items-center gap-1.5"
          >
            <RotateCcw className="w-4 h-4" />
            <span>{t('common.retry')}</span>
          </Button>
        </div>
      )}

      {/* Steps Table */}
      {!error && (
        <div
          role="tabpanel"
          id={`panel-${activeLevel}`}
          aria-labelledby={`tab-${activeLevel}`}
        >
          <DataTable
            columns={columns}
            data={stepsData?.items || []}
            rowKey={(step) => step.id}
            isLoading={isLoading}
            page={stepsData?.page ?? page}
            pageSize={stepsData?.pageSize ?? pageSize}
            totalCount={stepsData?.totalCount}
            onPageChange={setPage}
            onPageSizeChange={(newSize) => {
              setPageSize(newSize);
              setPage(1);
            }}
            emptyTitle={t('admin.grammar.noStepsForLevel', { level: activeLevel })}
            emptyDescription={t('admin.grammar.noStepsHint')}
          />
        </div>
      )}

      {/* Confirm Import Dialog */}
      <ConfirmDialog
        isOpen={isImportConfirmOpen}
        onClose={() => setIsImportConfirmOpen(false)}
        onConfirm={handleConfirmImport}
        title={t('admin.grammar.importConfirmTitle')}
        targetName={t('admin.grammar.importContentPack')}
        description={t('admin.grammar.importConfirmDesc')}
        confirmLabel={t('admin.grammar.confirmImportButton')}
        variant="primary"
        isLoading={importMutation.isPending}
      />

      {/* Result Dialog */}
      <ImportContentPackResultDialog
        isOpen={Boolean(importResult)}
        onClose={() => setImportResult(null)}
        result={importResult}
      />
    </div>
  );
};
