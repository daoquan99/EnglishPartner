import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  Save,
  Sparkles,
  BookOpen,
  Info,
  CheckCircle2,
  FileCode,
  Eye,
  Plus,
  Trash2,
  Edit3,
  Layers,
  AlertTriangle,
  Code2,
  RotateCcw,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { LoadingState } from '@/shared/components/ui/LoadingState';
import { useTutorSessionStore } from '@/features/tutor';
import { useVocabularyGenerationModelsQuery } from '@/features/catalog';
import {
  useAdminGrammarStepQuery,
  useUpdateAdminGrammarStepMutation,
  useUpdateAdminGrammarTheoryMutation,
  useDraftAdminGrammarTheoryMutation,
  useCreateOfficialGrammarExerciseMutation,
  useUpdateOfficialGrammarExerciseMutation,
  useDeleteOfficialGrammarExerciseMutation,
} from '../../hooks/useAdminGrammarQueries';
import { TheoryStructuredEditor } from '../../components/admin/TheoryStructuredEditor';
import { OfficialExerciseFormDialog } from '../../components/admin/OfficialExerciseFormDialog';
import { GenerateGrammarExercisesDialog } from '../../components/GenerateGrammarExercisesDialog';
import { TheoryRenderer } from '../../components/TheoryRenderer';
import { ExerciseRenderer } from '@/features/lessons/components/player/ExerciseRenderer';
import { mapGrammarExerciseToLearnerDto } from '../../utils/grammarExerciseAdapter';
import {
  convertTheoryBetweenModes,
  extractValidationErrors,
  getAdminGrammarErrorMessage,
  createEmptyTheory,
  validateTheoryJson,
  hasAnyTheoryContent,
} from '../../utils/grammarHelpers';
import { toast } from '@/shared/lib/toast';
import type {
  GrammarTheoryDto,
  GrammarExerciseAdminDto,
  ValidationIssueDto,
  CreateOfficialGrammarExerciseRequestDto,
  UpdateOfficialGrammarExerciseRequestDto,
} from '../../types/grammar.types';

type EditorTab = 'info' | 'theory' | 'exercises';

export const AdminGrammarStepEditorPage: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const { t } = useTranslation();
  const normalizedCode = (code || '').toUpperCase();

  const [activeTab, setActiveTab] = useState<EditorTab>('info');

  // Metadata form state
  const [titleVi, setTitleVi] = useState<string>('');
  const [titleEn, setTitleEn] = useState<string>('');
  const [summaryVi, setSummaryVi] = useState<string>('');
  const [isPublished, setIsPublished] = useState<boolean>(true);

  // Dirty tracking state
  const [isInfoDirty, setIsInfoDirty] = useState<boolean>(false);
  const [isTheoryDirty, setIsTheoryDirty] = useState<boolean>(false);
  const [lastLoadedCode, setLastLoadedCode] = useState<string>('');
  const syncedDetailRef = useRef<unknown>(undefined);

  // Theory editor state
  const [theory, setTheory] = useState<GrammarTheoryDto>(createEmptyTheory());
  const [theoryRawJson, setTheoryRawJson] = useState<string>('{}');
  const [theoryMode, setTheoryMode] = useState<'structured' | 'raw'>('structured');
  const [isPreviewTheory, setIsPreviewTheory] = useState<boolean>(false);
  const [theoryErrorsByPath, setTheoryErrorsByPath] = useState<Record<string, string[]>>({});
  const [theoryGeneralError, setTheoryGeneralError] = useState<string | null>(null);
  const [aiDraftIssues, setAiDraftIssues] = useState<ValidationIssueDto[]>([]);
  const [isDraftOverwriteConfirmOpen, setIsDraftOverwriteConfirmOpen] = useState<boolean>(false);

  // Exercises state
  const [previewingExerciseId, setPreviewingExerciseId] = useState<string | null>(null);
  const [viewAnswersExerciseId, setViewAnswersExerciseId] = useState<string | null>(null);
  const [isExerciseFormOpen, setIsExerciseFormOpen] = useState<boolean>(false);
  const [editingExercise, setEditingExercise] = useState<GrammarExerciseAdminDto | null>(null);
  const [deletingExerciseId, setDeletingExerciseId] = useState<string | null>(null);
  const [isAiGenerateDialogOpen, setIsAiGenerateDialogOpen] = useState<boolean>(false);

  const { data: stepDetail, isLoading, error, refetch } = useAdminGrammarStepQuery(normalizedCode);

  const updateStepMutation = useUpdateAdminGrammarStepMutation(normalizedCode);
  const updateTheoryMutation = useUpdateAdminGrammarTheoryMutation(normalizedCode);
  const draftTheoryMutation = useDraftAdminGrammarTheoryMutation(normalizedCode);
  const createExerciseMutation = useCreateOfficialGrammarExerciseMutation(normalizedCode);
  const updateExerciseMutation = useUpdateOfficialGrammarExerciseMutation(normalizedCode);
  const deleteExerciseMutation = useDeleteOfficialGrammarExerciseMutation(normalizedCode);

  const { apiKey, aiProvider, agyModelCode } = useTutorSessionStore();
  const { data: models = [] } = useVocabularyGenerationModelsQuery();
  const isAgy = aiProvider === 'agy';
  const hasKey = Boolean(apiKey.trim()) || isAgy;
  const hasModels = isAgy || models.length > 0;
  const effectiveModel = isAgy
    ? agyModelCode
    : (models[0]?.modelCode || '');
  const canDraftWithAi = hasKey && hasModels && !draftTheoryMutation.isPending;

  // Populate data when stepDetail loads
  useEffect(() => {
    if (!stepDetail) return;

    if (lastLoadedCode !== normalizedCode) {
      setTitleVi(stepDetail.titleVi || '');
      setTitleEn(stepDetail.titleEn || '');
      setSummaryVi(stepDetail.summaryVi || '');
      setIsPublished(Boolean(stepDetail.isPublished));
      setIsInfoDirty(false);

      const parsedTheory = stepDetail.theory || createEmptyTheory();
      setTheory(parsedTheory);
      setTheoryRawJson(JSON.stringify(parsedTheory, null, 2));
      setIsTheoryDirty(false);

      setLastLoadedCode(normalizedCode);
    } else if (syncedDetailRef.current !== stepDetail) {
      // Sync only when fresh data arrives, so clearing a dirty flag after save does not reapply stale data.
      if (!isInfoDirty) {
        setTitleVi(stepDetail.titleVi || '');
        setTitleEn(stepDetail.titleEn || '');
        setSummaryVi(stepDetail.summaryVi || '');
        setIsPublished(Boolean(stepDetail.isPublished));
      }
      if (!isTheoryDirty) {
        const parsedTheory = stepDetail.theory || createEmptyTheory();
        setTheory(parsedTheory);
        setTheoryRawJson(JSON.stringify(parsedTheory, null, 2));
      }
    }
    syncedDetailRef.current = stepDetail;
  }, [stepDetail, normalizedCode, lastLoadedCode, isInfoDirty, isTheoryDirty]);

  if (isLoading) {
    return <LoadingState />;
  }

  const isNotFound = (error as any)?.response?.status === 404 || (!isLoading && !error && !stepDetail);
  const isForbidden = (error as any)?.response?.status === 403;

  if (error || !stepDetail) {
    return (
      <div className="bg-white rounded-3xl border border-slate-200 p-8 text-center space-y-4">
        <AlertTriangle className="w-10 h-10 text-rose-500 mx-auto" />
        <h2 className="text-lg font-bold text-slate-900">
          {isNotFound
            ? t('admin.grammar.stepNotFoundTitle')
            : isForbidden
            ? t('common.forbidden')
            : t('common.error')}
        </h2>
        <p className="text-xs text-slate-500">
          {getAdminGrammarErrorMessage(error, t)}
        </p>
        <div className="flex items-center justify-center gap-2">
          {!isNotFound && !isForbidden && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => refetch()}
              className="flex items-center gap-1.5"
            >
              <RotateCcw className="w-4 h-4" />
              <span>{t('common.retry')}</span>
            </Button>
          )}
          <Link
            to="/admin/grammar"
            className="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-slate-900 text-white text-xs font-bold"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>{t('admin.grammar.backToSteps')}</span>
          </Link>
        </div>
      </div>
    );
  }

  // --- Handlers Tab 1: Info ---
  const handleSaveInfo = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await updateStepMutation.mutateAsync({
        titleVi: titleVi.trim(),
        titleEn: titleEn.trim(),
        summaryVi: summaryVi.trim(),
        isPublished,
      });
      setIsInfoDirty(false);
      toast.success(t('admin.grammar.infoSavedSuccess'));
    } catch (err: any) {
      toast.error(getAdminGrammarErrorMessage(err, t));
    }
  };

  // --- Handlers Tab 2: Theory ---
  const handleToggleTheoryMode = (targetMode: 'structured' | 'raw') => {
    if (targetMode === theoryMode) return;
    const conversion = convertTheoryBetweenModes(theory, targetMode, theoryRawJson);
    if (conversion.errorKey) {
      setTheoryGeneralError(t(conversion.errorKey));
      return;
    }
    setTheoryMode(targetMode);
    setTheory(conversion.theory);
    setTheoryRawJson(conversion.raw);
    setTheoryGeneralError(null);
  };

  const handleSaveTheory = async () => {
    setTheoryGeneralError(null);
    setTheoryErrorsByPath({});

    let payloadTheory = theory;
    if (theoryMode === 'raw') {
      const parsed = validateTheoryJson(theoryRawJson);
      if (!parsed.ok) {
        setTheoryGeneralError(t(parsed.errorKey));
        return;
      }
      payloadTheory = parsed.value;
      setTheory(payloadTheory);
    }

    try {
      await updateTheoryMutation.mutateAsync({ theory: payloadTheory });
      setIsTheoryDirty(false);
      toast.success(t('admin.grammar.theorySavedSuccess'));
    } catch (err: any) {
      const fieldErrors = extractValidationErrors(err);
      if (Object.keys(fieldErrors).length > 0) {
        setTheoryErrorsByPath(fieldErrors);
      }
      setTheoryGeneralError(getAdminGrammarErrorMessage(err, t));
    }
  };

  const handleRequestDraftWithAi = () => {
    if (!canDraftWithAi) return;

    if (hasAnyTheoryContent(theory)) {
      setIsDraftOverwriteConfirmOpen(true);
    } else {
      executeDraftWithAi();
    }
  };

  const executeDraftWithAi = async () => {
    setIsDraftOverwriteConfirmOpen(false);
    setTheoryGeneralError(null);
    setTheoryErrorsByPath({});
    setAiDraftIssues([]);

    try {
      const res = await draftTheoryMutation.mutateAsync({
        aiConfig: {
          provider: isAgy ? 'agy' : 'gemini',
          modelCode: effectiveModel,
          apiKey: isAgy ? undefined : apiKey.trim(),
        },
      });

      // Load draft into editor - NO auto-save!
      setTheory(res.theory);
      setTheoryRawJson(JSON.stringify(res.theory, null, 2));
      setIsTheoryDirty(true);
      if (res.issues && res.issues.length > 0) {
        setAiDraftIssues(res.issues);
      }
      toast.info(t('admin.grammar.aiDraftLoadedNotice'));
    } catch (err: any) {
      setTheoryGeneralError(getAdminGrammarErrorMessage(err, t));
    }
  };

  // --- Handlers Tab 3: Official Exercises ---
  const handleSaveExercise = async (payload: CreateOfficialGrammarExerciseRequestDto) => {
    if (editingExercise) {
      await updateExerciseMutation.mutateAsync({
        id: editingExercise.id,
        payload: payload as UpdateOfficialGrammarExerciseRequestDto,
      });
      toast.success(t('admin.grammar.exerciseUpdatedSuccess'));
    } else {
      await createExerciseMutation.mutateAsync(payload);
      toast.success(t('admin.grammar.exerciseCreatedSuccess'));
    }
  };

  const handleDeleteExercise = async () => {
    if (!deletingExerciseId) return;
    try {
      await deleteExerciseMutation.mutateAsync(deletingExerciseId);
      toast.success(t('admin.grammar.exerciseDeletedSuccess'));
      setDeletingExerciseId(null);
    } catch (err: any) {
      toast.error(getAdminGrammarErrorMessage(err, t));
    }
  };

  return (
    <div className="space-y-6">
      {/* Header with Step details */}
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link
            to="/admin/grammar"
            className="p-2.5 rounded-2xl bg-slate-100 hover:bg-slate-200 text-slate-700 transition-colors"
            title={t('admin.grammar.backToSteps')}
            aria-label={t('admin.grammar.backToSteps')}
          >
            <ArrowLeft className="w-5 h-5" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <span className="px-2.5 py-0.5 rounded-md bg-slate-900 text-white font-mono font-bold text-xs">
                {stepDetail.code}
              </span>
              <span className="text-xs font-bold text-emerald-700">
                {t('grammar.levelTitle', { level: stepDetail.cefrLevel })}
              </span>
              {stepDetail.isPublished ? (
                <Badge variant="emerald">
                  {t('admin.grammar.published')}
                </Badge>
              ) : (
                <Badge variant="default">
                  {t('admin.grammar.draft')}
                </Badge>
              )}
            </div>
            <h1 className="text-lg font-extrabold text-slate-900 mt-1">
              {stepDetail.titleVi}
            </h1>
          </div>
        </div>

        {/* Tab Switcher */}
        <div
          role="tablist"
          aria-label={t('admin.grammar.metadataTitle')}
          className="flex items-center p-1 rounded-2xl bg-slate-100 border border-slate-200 gap-1"
        >
          <button
            type="button"
            role="tab"
            id="tab-info"
            aria-selected={activeTab === 'info'}
            aria-controls="panel-info"
            onClick={() => setActiveTab('info')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'info'
                ? 'bg-white text-slate-900 shadow-2xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Info className="w-4 h-4" />
            <span>{t('admin.grammar.tabInfo')}</span>
          </button>
          <button
            type="button"
            role="tab"
            id="tab-theory"
            aria-selected={activeTab === 'theory'}
            aria-controls="panel-theory"
            onClick={() => setActiveTab('theory')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'theory'
                ? 'bg-white text-slate-900 shadow-2xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <BookOpen className="w-4 h-4" />
            <span>{t('admin.grammar.tabTheory')}</span>
          </button>
          <button
            type="button"
            role="tab"
            id="tab-exercises"
            aria-selected={activeTab === 'exercises'}
            aria-controls="panel-exercises"
            onClick={() => setActiveTab('exercises')}
            className={`px-3.5 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'exercises'
                ? 'bg-white text-slate-900 shadow-2xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Layers className="w-4 h-4" />
            <span>
              {t('admin.grammar.tabOfficialExercises', {
                count: stepDetail.officialExercises?.length || 0,
              })}
            </span>
          </button>
        </div>
      </div>

      {/* Unsaved changes banner */}
      {(isInfoDirty || isTheoryDirty) && (
        <div className="p-3.5 rounded-2xl bg-amber-50 border border-amber-200 text-xs text-amber-900 font-semibold flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
          <span>{t('admin.grammar.unsavedChangesBanner')}</span>
        </div>
      )}

      {/* TAB 1: INFO */}
      {activeTab === 'info' && (
        <div
          role="tabpanel"
          id="panel-info"
          aria-labelledby="tab-info"
          className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs max-w-3xl"
        >
          <form onSubmit={handleSaveInfo} className="space-y-4">
            <h2 className="text-sm font-bold text-slate-900 border-b border-slate-100 pb-2">
              {t('admin.grammar.metadataTitle')}
            </h2>

            <div>
              <label
                htmlFor="step-title-vi"
                className="block text-xs font-bold text-slate-700 mb-1"
              >
                {t('admin.grammar.titleViLabel')}
              </label>
              <input
                id="step-title-vi"
                type="text"
                value={titleVi}
                onChange={(e) => {
                  setTitleVi(e.target.value);
                  setIsInfoDirty(true);
                }}
                required
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />
            </div>

            <div>
              <label
                htmlFor="step-title-en"
                className="block text-xs font-bold text-slate-700 mb-1"
              >
                {t('admin.grammar.titleEnLabel')}
              </label>
              <input
                id="step-title-en"
                type="text"
                value={titleEn}
                onChange={(e) => {
                  setTitleEn(e.target.value);
                  setIsInfoDirty(true);
                }}
                required
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />
            </div>

            <div>
              <label
                htmlFor="step-summary-vi"
                className="block text-xs font-bold text-slate-700 mb-1"
              >
                {t('admin.grammar.summaryViLabel')}
              </label>
              <textarea
                id="step-summary-vi"
                rows={3}
                value={summaryVi}
                onChange={(e) => {
                  setSummaryVi(e.target.value);
                  setIsInfoDirty(true);
                }}
                required
                className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />
            </div>

            <div className="p-3 rounded-xl border border-slate-200 bg-slate-50">
              <label htmlFor="step-ispublished-toggle" className="flex items-center gap-2 cursor-pointer">
                <input
                  id="step-ispublished-toggle"
                  type="checkbox"
                  checked={isPublished}
                  onChange={(e) => {
                    setIsPublished(e.target.checked);
                    setIsInfoDirty(true);
                  }}
                  className="rounded text-emerald-600 focus:ring-emerald-500 w-4 h-4 cursor-pointer"
                />
                <span className="font-bold text-slate-800 text-xs">
                  {t('admin.grammar.isPublishedLabel')}
                </span>
              </label>
            </div>

            <div className="flex justify-end pt-3 border-t border-slate-100">
              <Button
                type="submit"
                variant="primary"
                size="sm"
                disabled={updateStepMutation.isPending}
                className="flex items-center gap-1.5"
              >
                <Save className="w-4 h-4" />
                <span>
                  {updateStepMutation.isPending
                    ? t('common.saving')
                    : t('admin.grammar.saveInfoButton')}
                </span>
              </Button>
            </div>
          </form>
        </div>
      )}

      {/* TAB 2: THEORY */}
      {activeTab === 'theory' && (
        <div
          role="tabpanel"
          id="panel-theory"
          aria-labelledby="tab-theory"
          className="space-y-4"
        >
          {/* Action bar for theory editor */}
          <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-2xs flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <div className="flex items-center p-1 rounded-xl bg-slate-100 border border-slate-200">
                <button
                  type="button"
                  aria-pressed={theoryMode === 'structured'}
                  onClick={() => handleToggleTheoryMode('structured')}
                  className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                    theoryMode === 'structured'
                      ? 'bg-white text-slate-900 shadow-2xs'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  {t('admin.grammar.structuredMode')}
                </button>
                <button
                  type="button"
                  aria-pressed={theoryMode === 'raw'}
                  onClick={() => handleToggleTheoryMode('raw')}
                  className={`px-3 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5 ${
                    theoryMode === 'raw'
                      ? 'bg-white text-slate-900 shadow-2xs'
                      : 'text-slate-600 hover:text-slate-900'
                  }`}
                >
                  <FileCode className="w-3.5 h-3.5" />
                  <span>{t('admin.grammar.rawJsonMode')}</span>
                </button>
              </div>

              <Button
                type="button"
                variant={isPreviewTheory ? 'primary' : 'outline'}
                size="sm"
                onClick={() => setIsPreviewTheory(!isPreviewTheory)}
                className="flex items-center gap-1.5"
              >
                <Eye className="w-4 h-4" />
                <span>{t('admin.grammar.previewTheory')}</span>
              </Button>
            </div>

            <div className="flex items-center gap-2">
              {!hasKey && (
                <span className="text-[11px] text-amber-700 font-semibold bg-amber-50 px-2.5 py-1 rounded-lg border border-amber-200">
                  {t('grammar.noApiKeyWarning')}
                </span>
              )}
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleRequestDraftWithAi}
                disabled={!canDraftWithAi}
                className="flex items-center gap-1.5 text-purple-700 border-purple-200 hover:bg-purple-50 disabled:opacity-40"
              >
                <Sparkles className="w-4 h-4 text-purple-600" />
                <span>
                  {draftTheoryMutation.isPending
                    ? t('admin.grammar.draftingAi')
                    : t('admin.grammar.draftWithAi')}
                </span>
              </Button>
              <Button
                type="button"
                variant="primary"
                size="sm"
                onClick={handleSaveTheory}
                disabled={updateTheoryMutation.isPending}
                className="flex items-center gap-1.5"
              >
                <Save className="w-4 h-4" />
                <span>
                  {updateTheoryMutation.isPending
                    ? t('common.saving')
                    : t('admin.grammar.saveTheoryButton')}
                </span>
              </Button>
            </div>
          </div>

          {/* Error & Issue Banners */}
          {theoryGeneralError && (
            <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 text-xs text-rose-800 font-semibold flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
              <span>{theoryGeneralError}</span>
            </div>
          )}

          {aiDraftIssues.length > 0 && (
            <div className="p-3.5 rounded-2xl bg-amber-50 border border-amber-200 text-xs text-amber-900 space-y-1">
              <div className="flex items-center gap-1.5 font-bold">
                <AlertTriangle className="w-4 h-4 text-amber-600 shrink-0" />
                <span>{t('admin.grammar.aiDraftIssuesNotice')}</span>
              </div>
              <ul className="list-disc list-inside space-y-0.5 text-[11px] text-amber-800">
                {aiDraftIssues.map((issue, idx) => (
                  <li key={idx}>
                    <span className="font-mono font-semibold">{issue.path}: </span>
                    <span>{issue.message}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Theory Editor Body: Preview / Structured / Raw */}
          {isPreviewTheory ? (
            <div className="p-6 bg-white rounded-3xl border border-slate-200 shadow-xs">
              <div className="mb-4 pb-2 border-b border-slate-100 flex items-center justify-between">
                <span className="text-xs font-bold text-slate-500 uppercase tracking-wider">
                  {t('admin.grammar.previewModeHeading')}
                </span>
              </div>
              <TheoryRenderer theory={theory} />
            </div>
          ) : theoryMode === 'structured' ? (
            <TheoryStructuredEditor
              theory={theory}
              onChange={(updated) => {
                setTheory(updated);
                setTheoryRawJson(JSON.stringify(updated, null, 2));
                setIsTheoryDirty(true);
              }}
              errorsByPath={theoryErrorsByPath}
            />
          ) : (
            <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-3">
              <div className="flex items-center justify-between">
                <label
                  htmlFor="raw-theory-textarea"
                  className="text-xs font-bold text-slate-700"
                >
                  {t('admin.grammar.rawJsonEditorLabel')}
                </label>
                <span className="text-[11px] text-slate-400 font-mono">
                  {t('admin.grammar.rfcJsonFormat')}
                </span>
              </div>
              <textarea
                id="raw-theory-textarea"
                rows={20}
                value={theoryRawJson}
                onChange={(e) => {
                  setTheoryRawJson(e.target.value);
                  setIsTheoryDirty(true);
                }}
                className="w-full p-3 rounded-xl border border-slate-300 font-mono text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />

              {Object.keys(theoryErrorsByPath).length > 0 && (
                <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 text-xs text-rose-800 space-y-1">
                  <div className="flex items-center gap-1.5 font-bold">
                    <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
                    <span>{t('admin.grammar.otherIssuesHeading')}</span>
                  </div>
                  <ul className="list-disc list-inside space-y-0.5 text-[11px]">
                    {Object.entries(theoryErrorsByPath).map(([path, msgs]) => (
                      <li key={path}>
                        <span className="font-mono font-semibold">{path}: </span>
                        <span>{msgs.join(', ')}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}

          {/* Confirm overwrite with AI Draft Dialog */}
          <ConfirmDialog
            isOpen={isDraftOverwriteConfirmOpen}
            onClose={() => setIsDraftOverwriteConfirmOpen(false)}
            onConfirm={executeDraftWithAi}
            title={t('admin.grammar.draftOverwriteConfirmTitle')}
            targetName={stepDetail.code}
            description={t('admin.grammar.draftOverwriteConfirmMessage')}
            confirmLabel={t('admin.grammar.confirmDraftOverwrite')}
            variant="warning"
            isLoading={draftTheoryMutation.isPending}
          />
        </div>
      )}

      {/* TAB 3: OFFICIAL EXERCISES */}
      {activeTab === 'exercises' && (
        <div
          role="tabpanel"
          id="panel-exercises"
          aria-labelledby="tab-exercises"
          className="space-y-4"
        >
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.officialExercisesListTitle')}
            </h2>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsAiGenerateDialogOpen(true)}
                className="flex items-center gap-1.5 text-purple-700 border-purple-200 hover:bg-purple-50"
              >
                <Sparkles className="w-4 h-4 text-purple-600" />
                <span>{t('admin.grammar.generateWithAi')}</span>
              </Button>
              <Button
                type="button"
                variant="primary"
                size="sm"
                onClick={() => {
                  setEditingExercise(null);
                  setIsExerciseFormOpen(true);
                }}
                className="flex items-center gap-1.5"
              >
                <Plus className="w-4 h-4" />
                <span>{t('admin.grammar.addExerciseManual')}</span>
              </Button>
            </div>
          </div>

          <div className="space-y-3">
            {stepDetail.officialExercises && stepDetail.officialExercises.length > 0 ? (
              stepDetail.officialExercises.map((ex, idx) => {
                const isPreviewing = previewingExerciseId === ex.id;
                const isViewingAnswers = viewAnswersExerciseId === ex.id;
                const learnerEx = mapGrammarExerciseToLearnerDto({
                  id: ex.id,
                  key: ex.key,
                  type: ex.type,
                  title: ex.title,
                  instructions: ex.instructions,
                  order: idx + 1,
                  maxPoints: ex.maxPoints,
                  gradingMode: ex.gradingMode,
                  content: ex.content,
                  difficulty: ex.difficulty,
                  source: ex.source,
                  visibility: ex.visibility,
                  moderation: ex.moderation,
                });

                return (
                  <div
                    key={ex.id}
                    className="bg-white rounded-2xl border border-slate-200 p-4 shadow-2xs space-y-3"
                  >
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <div className="space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="w-6 h-6 rounded-full bg-slate-100 text-slate-800 font-bold text-xs flex items-center justify-center font-mono">
                            {idx + 1}
                          </span>
                          <span className="font-bold text-xs text-slate-900">
                            {ex.title}
                          </span>
                          <span className="px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 text-[10px] font-semibold border border-slate-200">
                            {t(`grammar.exerciseTypes.${ex.type}` as any)}
                          </span>
                          {ex.isCore && (
                            <Badge variant="emerald">
                              {t('admin.grammar.coreLabel')}
                            </Badge>
                          )}
                          <span className="text-[11px] text-slate-400">
                            ★ {ex.difficulty} · {ex.maxPoints} {t('admin.grammar.pts')}
                          </span>
                        </div>
                        {ex.instructions && (
                          <p className="text-[11px] text-slate-500 line-clamp-1 ml-8">
                            {ex.instructions}
                          </p>
                        )}
                      </div>

                      <div className="flex items-center gap-1.5 self-end sm:self-auto">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() =>
                            setPreviewingExerciseId(isPreviewing ? null : ex.id)
                          }
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
                          onClick={() =>
                            setViewAnswersExerciseId(isViewingAnswers ? null : ex.id)
                          }
                          className="flex items-center gap-1"
                        >
                          <Code2 className="w-3.5 h-3.5" />
                          <span>
                            {isViewingAnswers
                              ? t('admin.grammar.hideAnswers')
                              : t('admin.grammar.showAnswers')}
                          </span>
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setEditingExercise(ex);
                            setIsExerciseFormOpen(true);
                          }}
                          className="flex items-center gap-1 text-slate-700 hover:text-emerald-700"
                        >
                          <Edit3 className="w-3.5 h-3.5" />
                          <span>{t('common.edit')}</span>
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => setDeletingExerciseId(ex.id)}
                          className="p-1.5 text-slate-400 hover:text-rose-600"
                          title={t('common.delete')}
                          aria-label={t('common.delete')}
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>

                    {/* Preview box */}
                    {isPreviewing && (
                      <div className="pt-3 border-t border-slate-100 bg-slate-50/60 p-4 rounded-xl">
                        <ExerciseRenderer
                          exercise={learnerEx}
                          onSubmit={() => {}}
                          isAttemptInProgress={false}
                        />
                      </div>
                    )}

                    {/* Answers/Grading Viewer */}
                    {isViewingAnswers && (
                      <div className="pt-3 border-t border-slate-100 bg-slate-900 text-slate-100 p-3.5 rounded-xl text-xs font-mono">
                        <span className="text-slate-400 block text-[10px] uppercase font-bold mb-1">
                          {t('admin.grammar.gradingDefinitionJson')}
                        </span>
                        <pre className="overflow-x-auto whitespace-pre-wrap">
                          {JSON.stringify(ex.grading || {}, null, 2)}
                        </pre>
                      </div>
                    )}
                  </div>
                );
              })
            ) : (
              <div className="p-8 text-center bg-white rounded-2xl border border-slate-200">
                <p className="text-xs text-slate-400 italic">
                  {t('admin.grammar.noOfficialExercises')}
                </p>
              </div>
            )}
          </div>

          {/* Exercise Form Dialog */}
          <OfficialExerciseFormDialog
            isOpen={isExerciseFormOpen}
            onClose={() => {
              setIsExerciseFormOpen(false);
              setEditingExercise(null);
            }}
            onSave={handleSaveExercise}
            initialData={editingExercise}
            isSaving={
              createExerciseMutation.isPending || updateExerciseMutation.isPending
            }
          />

          {/* AI Generator Dialog in Admin Mode */}
          <GenerateGrammarExercisesDialog
            isOpen={isAiGenerateDialogOpen}
            onClose={() => setIsAiGenerateDialogOpen(false)}
            stepCode={normalizedCode}
            stepCefrLevel={stepDetail.cefrLevel}
            isAdminMode={true}
            defaultIsCore={true}
            onSuccess={() => {
              setIsAiGenerateDialogOpen(false);
              toast.success(t('admin.grammar.aiGenerateSuccess'));
            }}
          />

          {/* Confirm Delete Exercise Dialog */}
          <ConfirmDialog
            isOpen={Boolean(deletingExerciseId)}
            onClose={() => setDeletingExerciseId(null)}
            onConfirm={handleDeleteExercise}
            title={t('admin.grammar.deleteExerciseConfirmTitle')}
            targetName={
              stepDetail.officialExercises.find((e) => e.id === deletingExerciseId)?.title ||
              stepDetail.officialExercises.find((e) => e.id === deletingExerciseId)?.key ||
              t('admin.grammar.exercise')
            }
            description={t('admin.grammar.deleteExerciseConfirmMessage')}
            confirmLabel={t('common.delete')}
            variant="danger"
            isLoading={deleteExerciseMutation.isPending}
          />
        </div>
      )}
    </div>
  );
};
