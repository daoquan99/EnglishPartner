import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Sparkles,
  AlertCircle,
  Copy,
  Check,
  Cpu,
  FileText,
  Trash2,
  CheckCircle2,
  AlertTriangle,
} from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { useTutorSessionStore } from '@/features/tutor';
import { useVocabularyGenerationModelsQuery } from '@/features/catalog';
import { isAgyLocalEnabled } from '@/shared/config/features';
import {
  useGenerateGrammarExercisesMutation,
  useBuildGrammarExercisePromptMutation,
  useImportGrammarExercisesMutation,
} from '../hooks/useGrammarQueries';
import {
  useGenerateOfficialGrammarExercisesMutation,
  useImportOfficialGrammarExercisesMutation,
  useBuildOfficialGrammarPromptMutation,
} from '../hooks/useAdminGrammarQueries';
import {
  getGrammarErrorMessage,
  validateGenerateItems,
  extractGrammarRejectionsFromError,
} from '../utils/grammarHelpers';
import type {
  GrammarExercisePromptRequestedItemDto,
  GrammarExerciseVisibility,
  GrammarExerciseRejectionDto,
} from '../types/grammar.types';

export interface GenerateGrammarExercisesDialogProps {
  isOpen: boolean;
  onClose: () => void;
  stepCode: string;
  stepCefrLevel?: string;
  defaultVisibility?: GrammarExerciseVisibility;
  onSuccess?: (createdCount: number) => void;
  isAdminMode?: boolean;
  defaultIsCore?: boolean;
}

const ALL_EXERCISE_TYPES = [
  'MultipleChoice',
  'TrueFalse',
  'FillInTheBlanks',
  'Matching',
  'SentenceUnscramble',
  'ErrorCorrection',
  'Translation',
  'WritingPractice',
  'ReadingComprehension',
] as const;

export const GenerateGrammarExercisesDialog: React.FC<GenerateGrammarExercisesDialogProps> = ({
  isOpen,
  onClose,
  stepCode,
  stepCefrLevel = 'A1',
  defaultVisibility = 'Private',
  onSuccess,
  isAdminMode = false,
  defaultIsCore = false,
}) => {
  const { t } = useTranslation();
  const { apiKey, requestFocusLeftMenuApiKey, aiProvider, agyModelCode } = useTutorSessionStore();
  const { data: models = [] } = useVocabularyGenerationModelsQuery();

  const isAgy = isAgyLocalEnabled && aiProvider === 'agy';
  const hasKey = Boolean(apiKey.trim()) || isAgy;
  const maxCount = isAdminMode ? 20 : 10;

  const [mode, setMode] = useState<'direct' | 'manual'>(hasKey ? 'direct' : 'manual');
  const [selectedModel, setSelectedModel] = useState<string>('');

  // Filter allowed types: ReadingComprehension only for B1+
  const isReadingAllowed = !['A1', 'A2'].includes(stepCefrLevel.toUpperCase());
  const availableTypes = ALL_EXERCISE_TYPES.filter(
    (type) => type !== 'ReadingComprehension' || isReadingAllowed
  );

  const [requestedItems, setRequestedItems] = useState<GrammarExercisePromptRequestedItemDto[]>([
    { type: 'MultipleChoice', count: 2, difficulty: 1 },
    { type: 'FillInTheBlanks', count: 1, difficulty: 2 },
  ]);

  const [learnerNote, setLearnerNote] = useState<string>('');
  const [visibility, setVisibility] = useState<GrammarExerciseVisibility>(defaultVisibility);
  const [isCore, setIsCore] = useState<boolean>(defaultIsCore);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Manual mode state
  const [promptText, setPromptText] = useState<string>('');
  const [isPromptCopied, setIsPromptCopied] = useState<boolean>(false);
  const [jsonText, setJsonText] = useState<string>('');

  // Generation response state
  const [rejections, setRejections] = useState<GrammarExerciseRejectionDto[]>([]);
  const [successCount, setSuccessCount] = useState<number | null>(null);

  const learnerGenerateMutation = useGenerateGrammarExercisesMutation(stepCode);
  const learnerBuildPromptMutation = useBuildGrammarExercisePromptMutation(stepCode);
  const learnerImportMutation = useImportGrammarExercisesMutation(stepCode);

  const adminGenerateMutation = useGenerateOfficialGrammarExercisesMutation(stepCode);
  const adminBuildPromptMutation = useBuildOfficialGrammarPromptMutation(stepCode);
  const adminImportMutation = useImportOfficialGrammarExercisesMutation(stepCode);

  const isGenerating = isAdminMode
    ? adminGenerateMutation.isPending
    : learnerGenerateMutation.isPending;
  const isImporting = isAdminMode
    ? adminImportMutation.isPending
    : learnerImportMutation.isPending;
  const isBuildingPrompt = isAdminMode
    ? adminBuildPromptMutation.isPending
    : learnerBuildPromptMutation.isPending;

  useEffect(() => {
    if (isOpen) {
      setMode(hasKey ? 'direct' : 'manual');
      setErrorMessage(null);
      setRejections([]);
      setSuccessCount(null);
      setJsonText('');
      setPromptText('');
      setIsCore(defaultIsCore);
    }
  }, [isOpen, hasKey, defaultIsCore]);

  useEffect(() => {
    if (models.length > 0) {
      if (!selectedModel || !models.some((m) => m.modelCode === selectedModel)) {
        setSelectedModel(models[0].modelCode);
      }
    }
  }, [models, selectedModel]);

  const validation = validateGenerateItems(requestedItems, maxCount);
  const totalCount = validation.totalCount;

  const handleAddItem = (type: string) => {
    if (requestedItems.some((i) => i.type === type)) return;
    setRequestedItems((prev) => [...prev, { type, count: 1, difficulty: 2 }]);
    setErrorMessage(null);
  };

  const handleRemoveItem = (index: number) => {
    setRequestedItems((prev) => prev.filter((_, idx) => idx !== index));
    setErrorMessage(null);
  };

  const handleUpdateItem = (
    index: number,
    field: 'count' | 'difficulty',
    value: number
  ) => {
    setRequestedItems((prev) =>
      prev.map((item, idx) => (idx === index ? { ...item, [field]: value } : item))
    );
    setErrorMessage(null);
  };

  const handleGenerate = async () => {
    setErrorMessage(null);
    setRejections([]);
    setSuccessCount(null);

    if (!validation.valid) {
      setErrorMessage(t(validation.errorKey!));
      return;
    }

    if (!hasKey) {
      setMode('manual');
      setErrorMessage(t('grammar.noApiKeyWarning'));
      return;
    }

    if (!isAgy && models.length === 0) {
      setErrorMessage(t('grammar.noModelsAvailable'));
      return;
    }

    const effectiveModel = isAgy ? agyModelCode : (selectedModel || models[0]?.modelCode || '');

    try {
      const res = isAdminMode
        ? await adminGenerateMutation.mutateAsync({
            items: requestedItems,
            isCore,
            aiConfig: {
              provider: isAgy ? 'agy' : 'gemini',
              modelCode: effectiveModel,
              apiKey: isAgy ? undefined : apiKey.trim(),
            },
          })
        : await learnerGenerateMutation.mutateAsync({
            items: requestedItems,
            learnerNote: learnerNote.trim() || null,
            visibility,
            aiConfig: {
              provider: isAgy ? 'agy' : 'gemini',
              modelCode: effectiveModel,
              apiKey: isAgy ? undefined : apiKey.trim(),
            },
          });

      setSuccessCount(res.created.length);
      setRejections(res.rejected || []);

      if (res.created.length > 0 && (!isAdminMode || !res.rejected || res.rejected.length === 0)) {
        onSuccess?.(res.created.length);
      }
    } catch (err: any) {
      const rejectedList = extractGrammarRejectionsFromError(err);
      if (rejectedList.length > 0) {
        setRejections(rejectedList);
      }
      setErrorMessage(getGrammarErrorMessage(err, t));
    }
  };

  const handleBuildPrompt = async () => {
    setErrorMessage(null);
    if (!validation.valid) {
      setErrorMessage(t(validation.errorKey!));
      return;
    }

    try {
      const res = isAdminMode
        ? await adminBuildPromptMutation.mutateAsync({
            items: requestedItems,
            learnerNote: learnerNote.trim() || null,
          })
        : await learnerBuildPromptMutation.mutateAsync({
            items: requestedItems,
            learnerNote: learnerNote.trim() || null,
          });
      setPromptText(res.prompt);
    } catch (err: any) {
      setErrorMessage(getGrammarErrorMessage(err, t));
    }
  };

  const handleCopyPrompt = async () => {
    if (!promptText) return;
    try {
      await navigator.clipboard.writeText(promptText);
      setIsPromptCopied(true);
      setTimeout(() => setIsPromptCopied(false), 2000);
    } catch {
      // Fallback
    }
  };

  const handleImportJson = async () => {
    setErrorMessage(null);
    setRejections([]);
    setSuccessCount(null);

    if (!jsonText.trim()) {
      setErrorMessage(t('grammar.pasteJsonRequired'));
      return;
    }

    try {
      const res = isAdminMode
        ? await adminImportMutation.mutateAsync({
            jsonText: jsonText.trim(),
            isCore,
            expectedItems: requestedItems,
          })
        : await learnerImportMutation.mutateAsync({
            jsonText: jsonText.trim(),
            visibility,
            expectedItems: requestedItems,
          });

      setSuccessCount(res.created.length);
      setRejections(res.rejected || []);

      if (res.created.length > 0 && (!isAdminMode || !res.rejected || res.rejected.length === 0)) {
        onSuccess?.(res.created.length);
      }
    } catch (err: any) {
      const rejectedList = extractGrammarRejectionsFromError(err);
      if (rejectedList.length > 0) {
        setRejections(rejectedList);
      }
      setErrorMessage(getGrammarErrorMessage(err, t));
    }
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      title={t('grammar.dialogTitle')}
      description={t('grammar.dialogSubtitle', { code: stepCode })}
      footer={
        <div className="flex items-center justify-between w-full">
          <Button variant="outline" size="sm" onClick={onClose} disabled={isAdminMode && (isGenerating || isImporting)}>
            {t('common.cancel')}
          </Button>

          {mode === 'direct' ? (
            <Button
              variant="primary"
              size="sm"
              disabled={
                isGenerating ||
                !hasKey ||
                !validation.valid ||
                (!isAgy && models.length === 0)
              }
              onClick={() => void handleGenerate()}
              className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold"
            >
              {isGenerating ? t('grammar.generatingAction') : t('grammar.generateAction')}
            </Button>
          ) : (
            <Button
              variant="primary"
              size="sm"
              disabled={isImporting || !jsonText.trim()}
              onClick={() => void handleImportJson()}
              className="bg-emerald-600 hover:bg-emerald-700 text-white font-bold"
            >
              {isImporting ? t('grammar.importingAction') : t('grammar.importJsonAction')}
            </Button>
          )}
        </div>
      }
    >
      <div className="space-y-4 text-xs pr-1">
        {/* Semantic Mode Switch Tabs (role="tablist" & role="tab" with aria-selected) */}
        <div role="tablist" aria-label={t('grammar.dialogTitle')} className="flex border-b border-slate-200 gap-2">
          <button
            type="button"
            role="tab"
            id="dialog-tab-direct"
            aria-controls="dialog-tabpanel-direct"
            aria-selected={mode === 'direct'}
            onClick={() => setMode('direct')}
            className={`flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer ${
              mode === 'direct'
                ? 'border-emerald-600 text-emerald-700'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <Cpu className="w-3.5 h-3.5" />
            {t('grammar.directAiMode')}
          </button>
          <button
            type="button"
            role="tab"
            id="dialog-tab-manual"
            aria-controls="dialog-tabpanel-manual"
            aria-selected={mode === 'manual'}
            onClick={() => setMode('manual')}
            className={`flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer ${
              mode === 'manual'
                ? 'border-emerald-600 text-emerald-700'
                : 'border-transparent text-slate-500 hover:text-slate-800'
            }`}
          >
            <FileText className="w-3.5 h-3.5" />
            {t('grammar.manualMode')}
          </button>
        </div>

        {/* Direct tabpanel: Notice if no key, or model selection if hasKey */}
        <div
          role="tabpanel"
          id="dialog-tabpanel-direct"
          aria-labelledby="dialog-tab-direct"
          hidden={mode !== 'direct'}
          className={mode !== 'direct' ? 'hidden' : 'space-y-4'}
        >
          {!hasKey && (
            <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 flex items-start gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <div className="space-y-1">
                <div>{t('grammar.noApiKeyWarning')}</div>
                <button
                  type="button"
                  onClick={() => {
                    onClose();
                    requestFocusLeftMenuApiKey();
                  }}
                  className="text-xs text-amber-900 font-bold underline cursor-pointer"
                >
                  {t('grammar.configureApiKeyNow')}
                </button>
              </div>
            </div>
          )}

          {hasKey && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 rounded-xl bg-slate-50 border border-slate-200">
              <div>
                <span className="block text-slate-700 font-semibold mb-1">
                  {t('grammar.aiProviderLabel')}
                </span>
                <div className="text-slate-600">
                  {isAgy ? `Antigravity CLI (${agyModelCode})` : 'Google Gemini API'}
                </div>
              </div>
              {!isAgy && models.length > 0 && (
                <div>
                  <label htmlFor="gemini-model-select" className="block text-slate-700 font-semibold mb-1">
                    {t('grammar.geminiModelLabel')}
                  </label>
                  <select
                    id="gemini-model-select"
                    value={selectedModel}
                    onChange={(e) => setSelectedModel(e.target.value)}
                    className="w-full text-xs p-1.5 rounded-lg border border-slate-300 bg-white"
                  >
                    {models.map((m) => (
                      <option key={m.modelCode} value={m.modelCode}>
                        {m.displayName || m.modelCode}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Exercise Types & Count Selection */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <span className="font-bold text-slate-800">
              {t('grammar.selectTypesCount', { count: totalCount })}
            </span>
            {totalCount > maxCount && (
              <span className="text-rose-600 font-semibold text-[11px]">
                {t('grammar.overMaxCount')}
              </span>
            )}
          </div>

          <div className="space-y-2">
            {requestedItems.map((item, idx) => (
              <div
                key={idx}
                className="flex flex-wrap items-center justify-between gap-2 p-2.5 rounded-xl border border-slate-200 bg-white shadow-2xs"
              >
                <div className="font-semibold text-slate-800 text-xs min-w-[140px]">
                  {t(`grammar.exerciseTypes.${item.type}` as any)}
                </div>

                <div className="flex items-center gap-3">
                  {/* Count with htmlFor / id */}
                  <div className="flex items-center gap-1.5">
                    <label htmlFor={`exercise-count-${idx}`} className="text-slate-500 text-[11px]">
                      {t('grammar.exerciseCountLabel')}
                    </label>
                    <input
                      id={`exercise-count-${idx}`}
                      type="number"
                      min={1}
                      max={maxCount}
                      value={item.count}
                      onChange={(e) =>
                        handleUpdateItem(idx, 'count', Math.max(1, parseInt(e.target.value, 10) || 1))
                      }
                      className="w-12 text-center p-1 rounded-md border border-slate-300 text-xs font-semibold"
                    />
                  </div>

                  {/* Difficulty with htmlFor / id */}
                  <div className="flex items-center gap-1.5">
                    <label htmlFor={`exercise-diff-${idx}`} className="text-slate-500 text-[11px]">
                      {t('grammar.difficultyLabel')}
                    </label>
                    <select
                      id={`exercise-diff-${idx}`}
                      value={item.difficulty}
                      onChange={(e) =>
                        handleUpdateItem(idx, 'difficulty', parseInt(e.target.value, 10) || 2)
                      }
                      className="p-1 rounded-md border border-slate-300 text-xs"
                    >
                      <option value={1}>{t('grammar.diffEasy')}</option>
                      <option value={2}>{t('grammar.diffMedium')}</option>
                      <option value={3}>{t('grammar.diffHard')}</option>
                    </select>
                  </div>

                  <button
                    type="button"
                    onClick={() => handleRemoveItem(idx)}
                    className="p-1 text-slate-400 hover:text-rose-600 transition-colors cursor-pointer"
                    title={t('common.delete')}
                    aria-label={t('grammar.removeExerciseType', {
                      type: t(`grammar.exerciseTypes.${item.type}` as any),
                    })}
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>

          {/* Add exercise type dropdown */}
          <div className="flex items-center gap-2">
            <label htmlFor="add-exercise-type-select" className="sr-only">
              {t('grammar.addExerciseType')}
            </label>
            <select
              id="add-exercise-type-select"
              defaultValue=""
              onChange={(e) => {
                if (e.target.value) {
                  handleAddItem(e.target.value);
                  e.target.value = '';
                }
              }}
              className="text-xs p-2 rounded-xl border border-slate-300 bg-slate-50 font-medium text-slate-700"
            >
              <option value="" disabled>
                {t('grammar.addOtherExerciseType')}
              </option>
              {availableTypes
                .filter((type) => !requestedItems.some((i) => i.type === type))
                .map((type) => (
                  <option key={type} value={type}>
                    {t(`grammar.exerciseTypes.${type}` as any)}
                  </option>
                ))}
            </select>
          </div>
        </div>

        {/* Learner Note (max 300 chars) */}
        <div className="space-y-1">
          <div className="flex items-center justify-between">
            <label htmlFor="grammar-learner-note" className="font-semibold text-slate-700">
              {t('grammar.learnerNoteLabel')}
            </label>
            <span
              className={`text-[11px] font-mono ${
                learnerNote.length > 300 ? 'text-rose-600 font-bold' : 'text-slate-400'
              }`}
            >
              {learnerNote.length}/300
            </span>
          </div>
          <input
            id="grammar-learner-note"
            type="text"
            maxLength={300}
            value={learnerNote}
            onChange={(e) => setLearnerNote(e.target.value)}
            placeholder={t('grammar.learnerNotePlaceholder')}
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-white focus:outline-emerald-600"
          />
        </div>

        {/* Visibility Selection (Learner) or IsCore Toggle (Admin) */}
        {isAdminMode ? (
          <div className="space-y-1.5 p-3 rounded-xl border border-emerald-200 bg-emerald-50/60">
            <label htmlFor="admin-is-core-checkbox" className="flex items-center gap-2 cursor-pointer">
              <input
                id="admin-is-core-checkbox"
                type="checkbox"
                checked={isCore}
                onChange={(e) => setIsCore(e.target.checked)}
                className="rounded text-emerald-600 focus:ring-emerald-500 w-4 h-4 cursor-pointer"
              />
              <span className="font-bold text-emerald-950 text-xs">
                {t('admin.grammar.markAsCoreExercise')}
              </span>
            </label>
            <p className="text-[11px] text-emerald-800">
              {t('admin.grammar.coreExerciseExplanation')}
            </p>
          </div>
        ) : (
          <div className="space-y-1.5 p-3 rounded-xl border border-slate-200 bg-slate-50/60">
            <span className="font-bold text-slate-800 block">{t('grammar.shareCommunityLabel')}</span>
            <div className="flex items-center gap-4">
              <label htmlFor="visibility-private" className="flex items-center gap-1.5 cursor-pointer">
                <input
                  id="visibility-private"
                  type="radio"
                  name="visibility"
                  checked={visibility === 'Private'}
                  onChange={() => setVisibility('Private')}
                />
                <span className="font-medium text-slate-700">{t('grammar.privateOption')}</span>
              </label>
              <label htmlFor="visibility-public" className="flex items-center gap-1.5 cursor-pointer">
                <input
                  id="visibility-public"
                  type="radio"
                  name="visibility"
                  checked={visibility === 'Public'}
                  onChange={() => setVisibility('Public')}
                />
                <span className="font-medium text-slate-700">{t('grammar.publicOption')}</span>
              </label>
            </div>
            {visibility === 'Public' ? (
              <div className="text-[11px] text-amber-800 bg-amber-50 p-2 rounded-lg border border-amber-200 flex items-start gap-1.5">
                <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5 text-amber-600" />
                <span>{t('grammar.publicNotice')}</span>
              </div>
            ) : null}
          </div>
        )}

        {/* Manual Mode Prompt Generation & JSON Input */}
        <div
          role="tabpanel"
          id="dialog-tabpanel-manual"
          aria-labelledby="dialog-tab-manual"
          hidden={mode !== 'manual'}
          className={mode !== 'manual' ? 'hidden' : 'space-y-3 pt-2 border-t border-slate-200'}
        >
          <div className="flex items-center justify-between">
            <span className="font-bold text-slate-800">{t('grammar.step1BuildPrompt')}</span>
            <Button
              variant="outline"
              size="sm"
              disabled={isBuildingPrompt || !validation.valid}
              onClick={() => void handleBuildPrompt()}
              className="text-xs"
            >
              {isBuildingPrompt ? t('grammar.gettingPromptBtn') : t('grammar.getPromptBtn')}
            </Button>
          </div>

          {promptText && (
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label htmlFor="grammar-prompt-text" className="text-[11px] text-slate-500">
                  {t('grammar.pastePromptHint')}
                </label>
                <button
                  type="button"
                  onClick={() => void handleCopyPrompt()}
                  className="flex items-center gap-1 text-[11px] font-bold text-emerald-700 hover:text-emerald-800 cursor-pointer"
                >
                  {isPromptCopied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                  {isPromptCopied ? t('grammar.copiedPrompt') : t('grammar.copyPrompt')}
                </button>
              </div>
              <textarea
                id="grammar-prompt-text"
                readOnly
                rows={4}
                value={promptText}
                className="w-full p-2 rounded-lg border border-slate-200 font-mono text-[11px] bg-slate-100"
              />
            </div>
          )}

          <div className="space-y-1">
            <label htmlFor="grammar-json-input" className="font-bold text-slate-800 block">
              {t('grammar.step2PasteJson')}
            </label>
            <textarea
              id="grammar-json-input"
              rows={5}
              value={jsonText}
              onChange={(e) => setJsonText(e.target.value)}
              placeholder={t('grammar.jsonPlaceholder')}
              className="w-full p-2.5 rounded-xl border border-slate-300 font-mono text-[11px] bg-white focus:outline-emerald-600"
            />
          </div>
        </div>

        {/* Error message */}
        {errorMessage && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-start gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Results summary (created) */}
        {successCount !== null && (
          <div className="space-y-2 p-3 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-950">
            <div className="flex items-center gap-2 font-bold text-xs">
              <CheckCircle2 className="w-4 h-4 text-emerald-600" />
              <span>{t('grammar.successCreatedCount', { count: successCount })}</span>
            </div>
          </div>
        )}

        {/* 422 rejected list: Render whenever rejections is non-empty */}
        {rejections.length > 0 && (
          <div className="space-y-1.5 p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs">
            <div className="font-semibold text-rose-800">
              {t('grammar.rejectedNotice', { count: rejections.length })}
            </div>
            <ul className="space-y-1 list-disc list-inside text-rose-700 text-[11px]">
              {rejections.map((rej, rIdx) => (
                <li key={rIdx}>
                  {t('grammar.exerciseIndexLabel', { index: rej.index + 1 })}: {rej.issues.join('; ')}
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </Dialog>
  );
};
