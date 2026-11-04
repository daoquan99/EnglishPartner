import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Eye,
  Edit2,
  Trash2,
  Globe,
  Lock,
  AlertTriangle,
  AlertCircle,
  X,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { ExerciseRenderer } from '@/features/lessons/components/player/ExerciseRenderer';
import {
  useMyGrammarExercisesQuery,
  useUpdateMyGrammarExerciseMutation,
  useChangeMyGrammarExerciseVisibilityMutation,
  useDeleteMyGrammarExerciseMutation,
} from '../hooks/useGrammarQueries';
import { mapGrammarExerciseToLearnerDto } from '../utils/grammarExerciseAdapter';
import {
  getGrammarErrorMessage,
  parseJsonObjectField,
} from '../utils/grammarHelpers';
import type {
  GrammarMyExerciseDto,
  GrammarModerationStatus,
  GrammarExerciseVisibility,
} from '../types/grammar.types';

export const MyGrammarExercisesPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();

  // Filters & Pagination with 300ms debounce for step code filter per Item 22
  const [filterStepCodeInput, setFilterStepCodeInput] = useState<string>('');
  const [debouncedStepCode, setDebouncedStepCode] = useState<string>('');
  const [filterModeration, setFilterModeration] = useState<GrammarModerationStatus | 'ALL'>('ALL');
  const [page, setPage] = useState<number>(1);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedStepCode(filterStepCodeInput.trim().toUpperCase());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [filterStepCodeInput]);

  const queryParams = {
    stepCode: debouncedStepCode || undefined,
    moderation: filterModeration === 'ALL' ? undefined : filterModeration,
    page,
    pageSize: 10,
    paged: true,
  };

  const { data: pagedData, isLoading, error, refetch } = useMyGrammarExercisesQuery(queryParams);

  const updateMutation = useUpdateMyGrammarExerciseMutation();
  const visibilityMutation = useChangeMyGrammarExerciseVisibilityMutation();
  const deleteMutation = useDeleteMyGrammarExerciseMutation();

  // Dialogs state
  const [previewExercise, setPreviewExercise] = useState<GrammarMyExerciseDto | null>(null);
  const [editExercise, setEditExercise] = useState<GrammarMyExerciseDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<GrammarMyExerciseDto | null>(null);

  // Edit form state
  const [editTitle, setEditTitle] = useState<string>('');
  const [editInstructions, setEditInstructions] = useState<string>('');
  const [editDifficulty, setEditDifficulty] = useState<number>(2);
  const [editContentJson, setEditContentJson] = useState<string>('');
  const [editGradingJson, setEditGradingJson] = useState<string>('');
  const [editError, setEditError] = useState<string | null>(null);
  const [contentError, setContentError] = useState<string | null>(null);
  const [gradingError, setGradingError] = useState<string | null>(null);
  const [cardErrors, setCardErrors] = useState<Record<string, string>>({});

  const handleOpenEdit = (ex: GrammarMyExerciseDto) => {
    setEditExercise(ex);
    setEditTitle(ex.title || '');
    setEditInstructions(ex.instructions || '');
    setEditDifficulty(ex.difficulty || 2);
    setEditContentJson(JSON.stringify(ex.content || {}, null, 2));
    setEditGradingJson(ex.grading ? JSON.stringify(ex.grading, null, 2) : '');
    setEditError(null);
    setContentError(null);
    setGradingError(null);
  };

  const handleSaveEdit = async () => {
    if (!editExercise) return;
    setEditError(null);
    setContentError(null);
    setGradingError(null);

    // Validate content JSON using pure helper per Item 15
    const contentResult = parseJsonObjectField(editContentJson, 'content', false);
    if (!contentResult.ok) {
      const msg = t(contentResult.errorKey);
      setContentError(msg);
      setEditError(msg);
      return;
    }

    // Validate grading JSON using pure helper per Item 15
    const gradingResult = parseJsonObjectField(editGradingJson, 'grading', true);
    if (!gradingResult.ok) {
      const msg = t(gradingResult.errorKey);
      setGradingError(msg);
      setEditError(msg);
      return;
    }

    try {
      await updateMutation.mutateAsync({
        id: editExercise.id,
        payload: {
          title: editTitle.trim(),
          instructions: editInstructions.trim(),
          difficulty: editDifficulty,
          content: contentResult.value!,
          grading: gradingResult.value,
          focus: editExercise.focus || null, // preserve existing focus per Item 15!
        },
      });
      setEditExercise(null);
    } catch (err: any) {
      setEditError(getGrammarErrorMessage(err, t));
    }
  };

  const handleChangeVisibility = async (ex: GrammarMyExerciseDto) => {
    const nextVisibility: GrammarExerciseVisibility =
      ex.visibility === 'Public' ? 'Private' : 'Public';
    setActionError(null);
    setCardErrors((prev) => {
      const next = { ...prev };
      delete next[ex.id];
      return next;
    });

    try {
      await visibilityMutation.mutateAsync({
        id: ex.id,
        payload: { visibility: nextVisibility },
      });
    } catch (err: any) {
      const msg = getGrammarErrorMessage(err, t);
      setActionError(msg);
      setCardErrors((prev) => ({ ...prev, [ex.id]: msg }));
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setActionError(null);
    const targetId = deleteTarget.id;
    try {
      await deleteMutation.mutateAsync(targetId);
      setDeleteTarget(null);
    } catch (err: any) {
      const msg = getGrammarErrorMessage(err, t);
      setActionError(msg);
      setCardErrors((prev) => ({ ...prev, [targetId]: msg }));
      setDeleteTarget(null);
    }
  };

  const items = pagedData?.items || [];

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate('/student/grammar')}
            className="flex items-center gap-1.5 text-xs text-slate-600 hover:text-slate-900"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('grammar.backToRoadmap')}
          </Button>

          <div>
            <h1 className="text-xl sm:text-2xl font-extrabold text-slate-900">
              {t('grammar.myExercisesTitle')}
            </h1>
            <p className="text-xs sm:text-sm text-slate-500">
              {t('grammar.myExercisesSubtitle')}
            </p>
          </div>
        </div>
      </div>

      {/* Action Error Banner (e.g. 409 grammar_pending_limit when toggling Public per Item 9) */}
      {actionError && (
        <div className="p-4 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start justify-between gap-3 shadow-xs">
          <div className="flex items-start gap-2.5">
            <AlertCircle className="w-4 h-4 shrink-0 mt-0.5 text-rose-600" />
            <span className="font-medium">{actionError}</span>
          </div>
          <button
            type="button"
            onClick={() => setActionError(null)}
            className="text-rose-500 hover:text-rose-800 p-0.5 rounded cursor-pointer"
            aria-label={t('grammar.closeNotification')}
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Filter Bar with flex-wrap per Item 21 */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 flex-wrap p-3 rounded-2xl bg-slate-50 border border-slate-200">
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="text"
            value={filterStepCodeInput}
            onChange={(e) => setFilterStepCodeInput(e.target.value)}
            placeholder={t('grammar.filterStepCodePlaceholder')}
            className="p-2 rounded-xl border border-slate-300 text-xs font-mono uppercase bg-white w-48 focus:outline-emerald-600"
          />

          <div className="flex items-center gap-1.5 text-xs">
            <span className="text-slate-500">{t('grammar.moderationStatus')}</span>
            <select
              value={filterModeration}
              onChange={(e) => {
                setFilterModeration(e.target.value as any);
                setPage(1);
              }}
              className="p-1.5 rounded-xl border border-slate-300 text-xs bg-white"
            >
              <option value="ALL">{t('grammar.filterAll')}</option>
              <option value="None">{t('grammar.modDraft')}</option>
              <option value="Pending">{t('grammar.modPending')}</option>
              <option value="Approved">{t('grammar.modApproved')}</option>
              <option value="Rejected">{t('grammar.modRejected')}</option>
            </select>
          </div>
        </div>

        <div className="text-xs text-slate-500">
          {t('grammar.totalExercisesFiltered', { count: pagedData?.totalCount || 0 })}
        </div>
      </div>

      {/* Content List / Loading / Error */}
      {isLoading ? (
        <LoadingState message={t('grammar.loadingMyExercises')} />
      ) : error ? (
        <div className="p-4 sm:p-6 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs space-y-2">
          <div className="font-bold text-sm">{t('grammar.loadMyExercisesError')}</div>
          <div>{getGrammarErrorMessage(error, t)}</div>
          <Button variant="outline" size="sm" onClick={() => void refetch()}>
            {t('grammar.retry')}
          </Button>
        </div>
      ) : items.length === 0 ? (
        <EmptyState
          title={t('grammar.noMyExercisesTitle')}
          description={t('grammar.noMyExercisesFilterDesc')}
        />
      ) : (
        <div className="space-y-3">
          <div className="grid gap-3 sm:grid-cols-2">
            {items.map((ex) => {
              const isApproved = ex.moderation === 'Approved';
              const isPending = ex.moderation === 'Pending';
              const isRejected = ex.moderation === 'Rejected';
              const isPublic = ex.visibility === 'Public';

              return (
                <div
                  key={ex.id}
                  className="p-4 rounded-2xl border border-slate-200 bg-white shadow-2xs space-y-3 flex flex-col justify-between"
                >
                  <div className="space-y-2">
                    <div className="flex items-center justify-between gap-2 flex-wrap">
                      <div className="flex items-center gap-1.5">
                        <span className="font-mono text-[11px] font-bold px-2 py-0.5 rounded bg-emerald-50 text-emerald-800 border border-emerald-200">
                          {ex.stepCode}
                        </span>
                        <span className="font-mono text-[11px] font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-700">
                          {t(`grammar.exerciseTypes.${ex.type}` as any)}
                        </span>
                      </div>

                      <div className="flex items-center gap-1.5">
                        {/* Visibility Badge */}
                        <button
                          type="button"
                          onClick={() => void handleChangeVisibility(ex)}
                          disabled={visibilityMutation.isPending}
                          title={isPublic ? t('grammar.changeVisibilityToPrivate') : t('grammar.changeVisibilityToPublic')}
                          className={`flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full border transition-colors cursor-pointer ${
                            isPublic
                              ? 'bg-sky-50 text-sky-800 border-sky-200 hover:bg-sky-100'
                              : 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200'
                          }`}
                        >
                          {isPublic ? <Globe className="w-3 h-3 text-sky-600" /> : <Lock className="w-3 h-3" />}
                          {isPublic ? t('grammar.publicOption') : t('grammar.privateOption')}
                        </button>

                        {/* Moderation Badge */}
                        <Badge
                          variant={
                            isApproved
                              ? 'emerald'
                              : isPending
                              ? 'sky'
                              : isRejected
                              ? 'rose'
                              : 'default'
                          }
                          className="text-[10px]"
                        >
                          {isApproved
                            ? t('grammar.modApproved')
                            : isPending
                            ? t('grammar.modPending')
                            : isRejected
                            ? t('grammar.modRejected')
                            : t('grammar.modDraft')}
                        </Badge>
                      </div>
                    </div>

                    <div className="font-bold text-slate-900 text-sm line-clamp-1">
                      {ex.title || t('grammar.exerciseFallbackTitle', { type: t(`grammar.exerciseTypes.${ex.type}` as any) })}
                    </div>

                    {ex.instructions && (
                      <div className="text-slate-600 text-xs line-clamp-2">
                        {ex.instructions}
                      </div>
                    )}

                    {/* Rejection notice with reason if rejected */}
                    {isRejected && ex.rejectReason && (
                      <div className="p-2 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-[11px] flex items-start gap-1.5">
                        <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5 text-rose-600" />
                        <span>{t('grammar.rejectionReason', { reason: ex.rejectReason })}</span>
                      </div>
                    )}

                    {/* Card-level error for visibility or delete per Item 7 */}
                    {cardErrors[ex.id] && (
                      <div className="p-2 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-[11px] flex items-start justify-between gap-1.5">
                        <div className="flex items-start gap-1">
                          <AlertCircle className="w-3.5 h-3.5 shrink-0 mt-0.5 text-rose-600" />
                          <span>{cardErrors[ex.id]}</span>
                        </div>
                        <button
                          type="button"
                          onClick={() =>
                            setCardErrors((prev) => {
                              const next = { ...prev };
                              delete next[ex.id];
                              return next;
                            })
                          }
                          className="text-rose-500 hover:text-rose-700 cursor-pointer"
                          aria-label={t('grammar.closeNotification')}
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </div>
                    )}
                  </div>

                  <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs">
                    <span className="text-slate-400 text-[11px]">{t('grammar.difficultyValue', { difficulty: ex.difficulty })}</span>

                    <div className="flex items-center gap-1.5">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setPreviewExercise(ex)}
                        className="text-xs p-1.5 text-slate-600 hover:text-slate-900"
                        title={t('grammar.previewBtn')}
                      >
                        <Eye className="w-3.5 h-3.5 mr-1" />
                        {t('grammar.previewBtn')}
                      </Button>

                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleOpenEdit(ex)}
                        className="text-xs p-1.5"
                        title={t('grammar.editBtn')}
                      >
                        <Edit2 className="w-3.5 h-3.5 mr-1" />
                        {t('grammar.editBtn')}
                      </Button>

                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setDeleteTarget(ex)}
                        className="text-xs p-1.5 text-rose-600 hover:bg-rose-50"
                        title={t('grammar.deleteBtn')}
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </Button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Pagination controls with flex-wrap per Item 21 */}
          {pagedData && pagedData.totalPages > 1 && (
            <div className="flex flex-wrap items-center justify-between gap-3 pt-4 border-t border-slate-200 text-xs">
              <span className="text-slate-500">
                {t('grammar.paginationSummary', {
                  page: pagedData.page,
                  totalPages: pagedData.totalPages,
                  totalCount: pagedData.totalCount,
                })}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={pagedData.page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <ChevronLeft className="w-4 h-4 mr-1" />
                  {t('grammar.prevPage')}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={pagedData.page >= pagedData.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  {t('grammar.nextPage')}
                  <ChevronRight className="w-4 h-4 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Preview Dialog */}
      {previewExercise && (
        <Dialog
          isOpen={Boolean(previewExercise)}
          onClose={() => setPreviewExercise(null)}
          title={t('grammar.previewTitle', {
            title:
              previewExercise.title ||
              t(`grammar.exerciseTypes.${previewExercise.type}` as any),
          })}
          description={t('grammar.previewDesc', {
            stepCode: previewExercise.stepCode,
            difficulty: previewExercise.difficulty,
          })}
          footer={
            <Button variant="outline" size="sm" onClick={() => setPreviewExercise(null)}>
              {t('common.close')}
            </Button>
          }
        >
          <div className="space-y-4 text-xs pr-1">
            <ExerciseRenderer
              attemptId="preview"
              exercise={mapGrammarExerciseToLearnerDto(previewExercise as any)}
              onSubmit={() => {}}
              isSubmitting={false}
              isAttemptInProgress={false}
            />
          </div>
        </Dialog>
      )}

      {/* Edit Exercise Dialog - Removed nested max-h-[70vh] overflow-y-auto per Item 21 */}
      {editExercise && (
        <Dialog
          isOpen={Boolean(editExercise)}
          onClose={() => setEditExercise(null)}
          title={t('grammar.editExerciseTitle')}
          description={t('grammar.editExerciseSubtitle')}
          footer={
            <div className="flex items-center justify-end gap-2 w-full">
              <Button variant="outline" size="sm" onClick={() => setEditExercise(null)}>
                {t('common.cancel')}
              </Button>
              <Button
                variant="primary"
                size="sm"
                disabled={updateMutation.isPending}
                onClick={() => void handleSaveEdit()}
                className="bg-emerald-600 hover:bg-emerald-700 text-white"
              >
                {updateMutation.isPending ? t('grammar.savingChanges') : t('grammar.saveChanges')}
              </Button>
            </div>
          }
        >
          <div className="space-y-4 text-xs pr-1">
            {editExercise.moderation === 'Approved' && (
              <div className="p-3 bg-amber-50 border border-amber-200 rounded-xl text-amber-900 flex items-start gap-2">
                <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5 text-amber-600" />
                <span>{t('grammar.editWarningApproved')}</span>
              </div>
            )}

            {editError && (
              <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 flex items-start gap-2">
                <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                <span>{editError}</span>
              </div>
            )}

            {/* Form controls with connected htmlFor and id per Item 15 */}
            <div className="space-y-1">
              <label htmlFor="edit-exercise-title" className="font-semibold text-slate-700 block">
                {t('grammar.fieldTitle')}
              </label>
              <input
                id="edit-exercise-title"
                type="text"
                value={editTitle}
                onChange={(e) => setEditTitle(e.target.value)}
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-white focus:outline-emerald-600"
              />
            </div>

            <div className="space-y-1">
              <label htmlFor="edit-exercise-instructions" className="font-semibold text-slate-700 block">
                {t('grammar.fieldInstructions')}
              </label>
              <input
                id="edit-exercise-instructions"
                type="text"
                value={editInstructions}
                onChange={(e) => setEditInstructions(e.target.value)}
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-white focus:outline-emerald-600"
              />
            </div>

            <div className="space-y-1">
              <label htmlFor="edit-exercise-difficulty" className="font-semibold text-slate-700 block">
                {t('grammar.fieldDifficulty')}
              </label>
              <select
                id="edit-exercise-difficulty"
                value={editDifficulty}
                onChange={(e) => setEditDifficulty(parseInt(e.target.value, 10) || 2)}
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-white"
              >
                <option value={1}>{t('grammar.diffEasy')}</option>
                <option value={2}>{t('grammar.diffMedium')}</option>
                <option value={3}>{t('grammar.diffHard')}</option>
              </select>
            </div>

            <div className="space-y-1">
              <label htmlFor="edit-exercise-content" className="font-semibold text-slate-700 block">
                {t('grammar.fieldContentJson')}
              </label>
              <textarea
                id="edit-exercise-content"
                rows={5}
                value={editContentJson}
                onChange={(e) => {
                  setEditContentJson(e.target.value);
                  if (contentError) setContentError(null);
                }}
                aria-invalid={Boolean(contentError)}
                aria-describedby={contentError ? 'edit-exercise-content-error' : undefined}
                className={`w-full p-2.5 rounded-xl border font-mono text-[11px] bg-white focus:outline-emerald-600 ${
                  contentError ? 'border-rose-400 focus:outline-rose-500' : 'border-slate-300'
                }`}
              />
              {contentError && (
                <p id="edit-exercise-content-error" className="text-xs text-rose-600 font-medium mt-1">
                  {contentError}
                </p>
              )}
            </div>

            <div className="space-y-1">
              <label htmlFor="edit-exercise-grading" className="font-semibold text-slate-700 block">
                {t('grammar.fieldGradingJson')}
              </label>
              <textarea
                id="edit-exercise-grading"
                rows={4}
                value={editGradingJson}
                onChange={(e) => {
                  setEditGradingJson(e.target.value);
                  if (gradingError) setGradingError(null);
                }}
                aria-invalid={Boolean(gradingError)}
                aria-describedby={gradingError ? 'edit-exercise-grading-error' : undefined}
                className={`w-full p-2.5 rounded-xl border font-mono text-[11px] bg-white focus:outline-emerald-600 ${
                  gradingError ? 'border-rose-400 focus:outline-rose-500' : 'border-slate-300'
                }`}
              />
              {gradingError && (
                <p id="edit-exercise-grading-error" className="text-xs text-rose-600 font-medium mt-1">
                  {gradingError}
                </p>
              )}
            </div>
          </div>
        </Dialog>
      )}

      {/* Delete Confirmation Dialog using shared ConfirmDialog per Item 15 */}
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        variant="danger"
        title={t('grammar.confirmDeleteTitle')}
        targetName={deleteTarget?.title || deleteTarget?.type || ''}
        targetLabel={t('grammar.exerciseLabel')}
        description={deleteTarget ? t('grammar.confirmDeleteDesc', { title: deleteTarget.title || deleteTarget.type }) : ''}
        confirmLabel={t('grammar.deleteBtn')}
        cancelLabel={t('common.cancel')}
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
};
