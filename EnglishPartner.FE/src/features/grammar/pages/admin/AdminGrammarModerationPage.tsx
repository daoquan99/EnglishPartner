import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Clock,
  CheckCircle,
  XCircle,
  Award,
  Filter,
  Eye,
  Code2,
  User,
  Calendar,
  AlertCircle,
  ArrowLeft,
  ShieldAlert,
  AlertTriangle,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { useCurrentUser, hasPermission } from '@/features/auth';
import {
  useAdminGrammarModerationQuery,
  useApproveGrammarExerciseMutation,
  useRejectGrammarExerciseMutation,
  usePromoteGrammarExerciseMutation,
} from '../../hooks/useAdminGrammarQueries';
import { RejectReasonDialog } from '../../components/admin/RejectReasonDialog';
import { PromoteExerciseDialog } from '../../components/admin/PromoteExerciseDialog';
import { ExerciseRenderer } from '@/features/lessons/components/player/ExerciseRenderer';
import { mapGrammarExerciseToLearnerDto } from '../../utils/grammarExerciseAdapter';
import { getAdminGrammarErrorMessage, getModerationActionButtons } from '../../utils/grammarHelpers';
import { toast } from '@/shared/lib/toast';
import type {
  GrammarModerationItemDto,
  GrammarModerationStatus,
} from '../../types/grammar.types';

const CEFR_LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const;

export const AdminGrammarModerationPage: React.FC = () => {
  const { t } = useTranslation();
  const { user } = useCurrentUser();

  const canModerate = hasPermission(user, 'grammar.moderate');
  const canManage = hasPermission(user, 'grammar.manage');

  const [selectedStatus, setSelectedStatus] = useState<GrammarModerationStatus>('Pending');
  const [selectedLevel, setSelectedLevel] = useState<string>('');
  const [stepCodeInput, setStepCodeInput] = useState<string>('');
  const [debouncedStepCode, setDebouncedStepCode] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);

  const [previewingId, setPreviewingId] = useState<string | null>(null);
  const [viewAnswersId, setViewAnswersId] = useState<string | null>(null);

  // Dialog states
  const [rejectingItem, setRejectingItem] = useState<GrammarModerationItemDto | null>(null);
  const [promotingItem, setPromotingItem] = useState<GrammarModerationItemDto | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedStepCode(stepCodeInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [stepCodeInput]);

  const {
    data: queueData,
    isLoading,
    error,
    refetch,
  } = useAdminGrammarModerationQuery({
    status: selectedStatus,
    level: selectedLevel || undefined,
    stepCode: debouncedStepCode || undefined,
    page,
    pageSize,
    paged: true,
  });

  // Adjust page if page > totalPages
  useEffect(() => {
    if (queueData && queueData.totalPages > 0 && page > queueData.totalPages) {
      setPage(queueData.totalPages);
    }
  }, [queueData?.totalPages, page]);

  const approveMutation = useApproveGrammarExerciseMutation();
  const rejectMutation = useRejectGrammarExerciseMutation();
  const promoteMutation = usePromoteGrammarExerciseMutation();

  const handleApprove = async (id: string) => {
    try {
      await approveMutation.mutateAsync(id);
      toast.success(t('admin.grammar.approveSuccess'));
    } catch (err: any) {
      toast.error(getAdminGrammarErrorMessage(err, t));
    }
  };

  const handleConfirmReject = async (reason: string) => {
    if (!rejectingItem) return;
    await rejectMutation.mutateAsync({
      id: rejectingItem.id,
      payload: { reason },
    });
    toast.success(t('admin.grammar.rejectSuccess'));
    setRejectingItem(null);
  };

  const handleConfirmPromote = async (isCore: boolean) => {
    if (!promotingItem) return;
    await promoteMutation.mutateAsync({
      id: promotingItem.id,
      stepCode: promotingItem.stepCode,
      payload: { isCore },
    });
    toast.success(t('admin.grammar.promoteSuccess'));
    setPromotingItem(null);
  };

  return (
    <div className="space-y-6">
      {/* Header bar */}
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/grammar"
            className="p-2.5 rounded-2xl bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors"
            title={t('admin.grammar.backToSteps')}
            aria-label={t('admin.grammar.backToSteps')}
          >
            <ArrowLeft className="w-5 h-5" />
          </Link>
          <div className="w-12 h-12 rounded-2xl bg-amber-50 border border-amber-200 flex items-center justify-center shrink-0">
            <Clock className="w-6 h-6 text-amber-600" />
          </div>
          <div>
            <h1 className="text-xl font-extrabold text-slate-900">
              {t('admin.grammar.moderationPageTitle')}
            </h1>
            <p className="text-xs text-slate-500 mt-0.5">
              {t('admin.grammar.moderationPageSubtitle')}
            </p>
          </div>
        </div>

        {/* Permission status badge */}
        <div className="flex items-center gap-2">
          {canModerate && (
            <Badge variant="emerald">
              {t('admin.grammar.moderatorRoleBadge')}
            </Badge>
          )}
          {canManage && (
            <Badge variant="amber">
              {t('admin.grammar.managerRoleBadge')}
            </Badge>
          )}
        </div>
      </div>

      {/* Filter toolbar */}
      <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-2xs flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-slate-400" />
            <span className="text-xs font-bold text-slate-700">
              {t('common.filters')}:
            </span>
          </div>

          {/* Status Filter */}
          <div>
            <label htmlFor="moderation-status-filter" className="sr-only">
              {t('admin.grammar.statusFilter.label')}
            </label>
            <select
              id="moderation-status-filter"
              value={selectedStatus}
              onChange={(e) => {
                setSelectedStatus(e.target.value as GrammarModerationStatus);
                setPage(1);
              }}
              className="p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 font-bold"
            >
              <option value="Pending">{t('admin.grammar.statusFilter.pending')}</option>
              <option value="Approved">{t('admin.grammar.statusFilter.approved')}</option>
              <option value="Rejected">{t('admin.grammar.statusFilter.rejected')}</option>
            </select>
          </div>

          {/* Level Filter */}
          <div>
            <label htmlFor="moderation-level-filter" className="sr-only">
              {t('admin.grammar.allLevels')}
            </label>
            <select
              id="moderation-level-filter"
              value={selectedLevel}
              onChange={(e) => {
                setSelectedLevel(e.target.value);
                setPage(1);
              }}
              className="p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 font-medium"
            >
              <option value="">{t('admin.grammar.allLevels')}</option>
              {CEFR_LEVELS.map((lvl) => (
                <option key={lvl} value={lvl}>
                  {t('grammar.levelTitle', { level: lvl })}
                </option>
              ))}
            </select>
          </div>

          {/* Step Code Filter */}
          <div>
            <label htmlFor="moderation-stepcode-filter" className="sr-only">
              {t('admin.grammar.filterStepCodePlaceholder')}
            </label>
            <input
              id="moderation-stepcode-filter"
              type="text"
              value={stepCodeInput}
              onChange={(e) => setStepCodeInput(e.target.value)}
              placeholder={t('admin.grammar.filterStepCodePlaceholder')}
              className="p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 w-36 uppercase font-mono"
            />
          </div>
        </div>

        <span className="text-xs font-semibold text-slate-500">
          {t('admin.grammar.totalPendingItems', {
            count: queueData?.totalCount ?? 0,
          })}
        </span>
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

      {/* Queue items list */}
      {!error && (isLoading ? (
        <LoadingState />
      ) : queueData?.items && queueData.items.length > 0 ? (
        <div className="space-y-4">
          {queueData.items.map((item) => {
            const isPreviewing = previewingId === item.id;
            const isViewingAnswers = viewAnswersId === item.id;
            const actionButtons = getModerationActionButtons(item.moderation, {
              canModerate,
              canManage,
            });
            const learnerDto = mapGrammarExerciseToLearnerDto({
              id: item.id,
              key: item.id,
              type: item.type,
              title: item.title,
              instructions: item.instructions,
              order: 1,
              maxPoints: item.maxPoints,
              gradingMode: item.gradingMode,
              content: item.content,
              difficulty: item.difficulty,
              source: 'UserAi',
              visibility: item.visibility,
              moderation: item.moderation,
            });

            return (
              <div
                key={item.id}
                className="bg-white rounded-3xl border border-slate-200 p-5 sm:p-6 shadow-xs space-y-4"
              >
                {/* Item Meta Header */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="px-2.5 py-1 rounded-md bg-slate-900 text-white font-mono font-bold text-xs">
                      {item.stepCode}
                    </span>
                    <span className="text-xs font-bold text-emerald-700">
                      {item.cefrLevel}
                    </span>
                    <span className="px-2.5 py-0.5 rounded-full bg-slate-100 text-slate-700 text-xs font-semibold border border-slate-200">
                      {t(`grammar.exerciseTypes.${item.type}` as any)}
                    </span>
                    <span className="text-xs text-slate-400">
                      ★ {item.difficulty} · {item.maxPoints} {t('admin.grammar.pts')}
                    </span>
                    <Badge
                      variant={
                        item.moderation === 'Approved'
                          ? 'emerald'
                          : item.moderation === 'Rejected'
                          ? 'rose'
                          : 'amber'
                      }
                    >
                      {t(`grammar.mod${item.moderation}`)}
                    </Badge>
                  </div>

                  <div className="flex items-center gap-3 text-xs text-slate-500">
                    <div className="flex items-center gap-1">
                      <User className="w-3.5 h-3.5 text-slate-400" />
                      <span>{item.authorName || t('admin.grammar.anonymousAuthor')}</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <Calendar className="w-3.5 h-3.5 text-slate-400" />
                      <span>{new Date(item.createdAtUtc).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>

                {/* Title & Instructions */}
                <div className="space-y-1">
                  <h3 className="text-sm font-bold text-slate-900">{item.title}</h3>
                  {item.instructions && (
                    <p className="text-xs text-slate-600">{item.instructions}</p>
                  )}
                  {item.focus && (
                    <p className="text-[11px] text-emerald-800 font-medium">
                      {t('admin.grammar.focusLabel')}: {item.focus}
                    </p>
                  )}
                </div>

                {/* Rejected Reason info if rejected */}
                {item.moderation === 'Rejected' && (
                  <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-800 space-y-0.5">
                    <span className="font-bold">{t('admin.grammar.rejectedReasonLabel')}: </span>
                    <span>{item.rejectReason || t('admin.grammar.noRejectReason')}</span>
                  </div>
                )}

                {/* Previews & Answers Toggle */}
                <div className="flex items-center gap-2">
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setPreviewingId(isPreviewing ? null : item.id)}
                    className="flex items-center gap-1"
                  >
                    <Eye className="w-3.5 h-3.5" />
                    <span>
                      {isPreviewing
                        ? t('admin.grammar.hidePreview')
                        : t('admin.grammar.showPreview')}
                    </span>
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setViewAnswersId(isViewingAnswers ? null : item.id)}
                    className="flex items-center gap-1"
                  >
                    <Code2 className="w-3.5 h-3.5" />
                    <span>
                      {isViewingAnswers
                        ? t('admin.grammar.hideAnswers')
                        : t('admin.grammar.showAnswers')}
                    </span>
                  </Button>
                </div>

                {/* Preview Box */}
                {isPreviewing && (
                  <div className="p-4 rounded-2xl bg-slate-50/80 border border-slate-200">
                    <ExerciseRenderer
                      exercise={learnerDto}
                      onSubmit={() => {}}
                      isAttemptInProgress={false}
                    />
                  </div>
                )}

                {/* Answers Box */}
                {isViewingAnswers && (
                  <div className="p-3.5 rounded-2xl bg-slate-900 text-slate-100 font-mono text-xs overflow-x-auto">
                    <span className="text-[10px] uppercase font-bold text-slate-400 block mb-1">
                      {t('admin.grammar.gradingDefinitionJson')}
                    </span>
                    <pre className="whitespace-pre-wrap">
                      {JSON.stringify(item.grading || {}, null, 2)}
                    </pre>
                  </div>
                )}

                {/* Action Buttons (Strictly filtered by permissions & status) */}
                <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
                  {actionButtons.canReject && (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setRejectingItem(item)}
                      disabled={rejectMutation.isPending}
                      className="text-rose-600 border-rose-200 hover:bg-rose-50 flex items-center gap-1.5"
                    >
                      <XCircle className="w-4 h-4" />
                      <span>{t('admin.grammar.rejectButton')}</span>
                    </Button>
                  )}

                  {actionButtons.canApprove && (
                    <Button
                      type="button"
                      variant="primary"
                      size="sm"
                      onClick={() => handleApprove(item.id)}
                      disabled={approveMutation.isPending}
                      className="flex items-center gap-1.5"
                    >
                      <CheckCircle className="w-4 h-4" />
                      <span>{t('admin.grammar.approveButton')}</span>
                    </Button>
                  )}

                  {actionButtons.canPromote && (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setPromotingItem(item)}
                      disabled={promoteMutation.isPending}
                      className="text-purple-700 border-purple-200 hover:bg-purple-50 flex items-center gap-1.5"
                    >
                      <Award className="w-4 h-4 text-purple-600" />
                      <span>{t('admin.grammar.promoteButton')}</span>
                    </Button>
                  )}

                  {!actionButtons.canApprove &&
                    !actionButtons.canReject &&
                    !actionButtons.canPromote && (
                      <span className="text-xs text-slate-400 italic">
                        {item.moderation === 'Rejected'
                          ? t('admin.grammar.rejectedStatusNotice')
                          : t('admin.grammar.noPermissionToModerate')}
                      </span>
                    )}
                </div>
              </div>
            );
          })}

          {/* Simple Pagination */}
          {queueData.totalPages > 1 && (
            <div className="flex items-center justify-between p-4 bg-white rounded-2xl border border-slate-200">
              <span className="text-xs text-slate-500">
                {t('common.pageOf', { page: queueData.page, total: queueData.totalPages })}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((prev) => Math.max(1, prev - 1))}
                  disabled={queueData.page <= 1}
                >
                  {t('common.prev')}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((prev) => Math.min(queueData.totalPages, prev + 1))}
                  disabled={queueData.page >= queueData.totalPages}
                >
                  {t('common.next')}
                </Button>
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="p-12 text-center bg-white rounded-3xl border border-slate-200 space-y-3">
          <CheckCircle className="w-12 h-12 text-emerald-500 mx-auto" />
          <h3 className="text-base font-bold text-slate-800">
            {t('admin.grammar.noPendingExercisesTitle')}
          </h3>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            {t('admin.grammar.noPendingExercisesDesc')}
          </p>
        </div>
      ))}

      {/* Reject Modal */}
      <RejectReasonDialog
        isOpen={Boolean(rejectingItem)}
        onClose={() => setRejectingItem(null)}
        onConfirm={handleConfirmReject}
        isRejecting={rejectMutation.isPending}
      />

      {/* Promote Modal */}
      <PromoteExerciseDialog
        isOpen={Boolean(promotingItem)}
        onClose={() => setPromotingItem(null)}
        onConfirm={handleConfirmPromote}
        isPromoting={promoteMutation.isPending}
      />
    </div>
  );
};
