import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { AlertCircle, Check, Copy, Cpu, FileText, Loader2, Sparkles } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { cn } from '@/shared/lib/cn';
import { isAgyLocalEnabled } from '@/shared/config/features';
import { toast } from '@/shared/lib/toast';
import { extractApiErrorMessage } from '@/shared/api/httpClient';
import {
  useTopicsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useBuildVocabularyGenerationPromptMutation,
  useGenerateVocabularyMutation,
  useImportVocabularyJsonMutation,
  useLearningSettingsQuery,
  VOCAB_QUERY_KEYS,
} from '../hooks/useDailyVocabulary';
import { normalizeTargetText, isSentenceMode } from '../lib/targetedVocabulary';
import type { CefrLevel } from '@/shared/api/openapi.types';

export interface TargetedVocabularyDialogProps {
  isOpen: boolean;
  onClose: () => void;
  rawText: string;
  initialAssignToDaily?: boolean;
  onSuccess?: (createdCount: number) => void;
}

export const TargetedVocabularyDialog: React.FC<TargetedVocabularyDialogProps> = React.memo(
  ({
    isOpen,
    onClose,
    rawText,
    initialAssignToDaily = true,
    onSuccess,
  }) => {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const { apiKey, requestFocusLeftMenuApiKey, aiProvider, agyModelCode } =
      useTutorSessionStore();

    const { data: learningSettings } = useLearningSettingsQuery();
    const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
    const { data: vocabTopics = [] } = useTopicsQuery({
      onlyActive: true,
      supportsVocabulary: true,
    });

    // Skip global error toasts so rate limit 429 and errors show solely in dialog banner without duplicate toast
    const generateMutation = useGenerateVocabularyMutation({ skipErrorToast: true });
    const buildPromptMutation = useBuildVocabularyGenerationPromptMutation({ skipErrorToast: true });
    const importJsonMutation = useImportVocabularyJsonMutation({ skipErrorToast: true });

    const normalizedText = useMemo(() => normalizeTargetText(rawText), [rawText]);
    const isSentence = useMemo(() => isSentenceMode(normalizedText), [normalizedText]);
    const isTermEmpty = !normalizedText || normalizedText.trim().length === 0;
    const isTermTooLong = !isSentence && normalizedText.length > 80;
    const isTermInvalid = isTermEmpty || isTermTooLong;

    const isAgyActive = isAgyLocalEnabled && aiProvider === 'agy';
    const hasDirectAi = isAgyActive || Boolean(apiKey.trim());

    const [genMode, setGenMode] = useState<'direct' | 'manual'>('direct');
    const [genCefr, setGenCefr] = useState<CefrLevel>('B1');
    const [genTopic, setGenTopic] = useState<string>('');
    const [requestedCount, setRequestedCount] = useState<number>(3);
    const [examplesPerSense, setExamplesPerSense] = useState<number>(2);
    const [selectedVocabModel, setSelectedVocabModel] = useState<string>('gemini-3.8-flash');
    const [assignToTodayPlan, setAssignToTodayPlan] = useState<boolean>(initialAssignToDaily);

    const [manualPrompt, setManualPrompt] = useState<string>('');
    const [isPromptCopied, setIsPromptCopied] = useState(false);
    const [manualJsonInput, setManualJsonInput] = useState<string>('');
    const [errorMsg, setErrorMsg] = useState<string | null>(null);

    // Reset effect ONLY runs when dialog transitions from closed to open (false -> true).
    // It does not wipe pasted JSON or manual prompt when learningSettings query completes later.
    const prevIsOpenRef = useRef(false);
    useEffect(() => {
      const wasOpen = prevIsOpenRef.current;
      prevIsOpenRef.current = isOpen;

      if (!wasOpen && isOpen) {
        if (learningSettings?.cefrLevel) {
          setGenCefr(learningSettings.cefrLevel as CefrLevel);
        }
        setAssignToTodayPlan(initialAssignToDaily);
        setErrorMsg(null);
        setManualPrompt('');
        setManualJsonInput('');
        setGenMode(hasDirectAi ? 'direct' : 'manual');
      }
    }, [isOpen, learningSettings?.cefrLevel, initialAssignToDaily, hasDirectAi]);

    useEffect(() => {
      if (vocabModels.length > 0 && !vocabModels.some((m) => m.modelCode === selectedVocabModel)) {
        setSelectedVocabModel(vocabModels[0].modelCode);
      }
    }, [vocabModels, selectedVocabModel]);

    const handleSuccessNotification = (createdCount: number) => {
      const count = createdCount > 0 ? createdCount : isSentence ? requestedCount : 1;
      const msg = assignToTodayPlan
        ? t('vocab.createdAndAddedSuccess', {
            count,
            defaultValue: `Đã tạo ${count} từ và thêm vào Daily`,
          })
        : t('vocab.createdSuccess', {
            count,
            defaultValue: `Đã tạo ${count} từ`,
          });
      toast.success(msg);
      void queryClient.invalidateQueries({ queryKey: VOCAB_QUERY_KEYS.todayPlan() });
      void queryClient.invalidateQueries({ queryKey: ['vocabulary'] });
      void queryClient.invalidateQueries({ queryKey: ['admin', 'vocabularies'] });
      onSuccess?.(count);
      onClose();
    };

    const handleGenerateDirect = async () => {
      if (isTermInvalid) return;
      const isAgy = isAgyActive;
      if (!isAgy && !apiKey.trim()) {
        setGenMode('manual');
        requestFocusLeftMenuApiKey();
        return;
      }
      setErrorMsg(null);

      try {
        const payload = {
          apiKey: isAgy ? undefined : apiKey.trim(),
          cefrLevel: genCefr,
          topic: genTopic.trim() ? genTopic.trim() : undefined,
          requestedCount: isSentence ? requestedCount : 1,
          modelCode: isAgy ? agyModelCode : selectedVocabModel,
          provider: isAgy ? 'agy' : 'gemini',
          assignToTodayPlan,
          examplesPerSense,
          targetTerms: isSentence ? undefined : [normalizedText],
          sourceSentence: isSentence ? normalizedText : undefined,
        };

        const generated = await generateMutation.mutateAsync(payload);
        handleSuccessNotification(generated?.length ?? 1);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 429) {
          setErrorMsg(t('vocab.rateLimit429', 'Bạn đã tạo quá nhiều, thử lại sau ít phút'));
        } else {
          setErrorMsg(extractApiErrorMessage(err));
        }
      }
    };

    const handleBuildPrompt = async () => {
      if (isTermInvalid) return;
      setErrorMsg(null);
      try {
        const res = await buildPromptMutation.mutateAsync({
          requestedCount: isSentence ? requestedCount : 1,
          cefrLevel: genCefr,
          topic: genTopic.trim() ? genTopic.trim() : undefined,
          examplesPerSense,
          targetTerms: isSentence ? undefined : [normalizedText],
          sourceSentence: isSentence ? normalizedText : undefined,
        });
        setManualPrompt(res.prompt);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 429) {
          setErrorMsg(t('vocab.rateLimit429', 'Bạn đã tạo quá nhiều, thử lại sau ít phút'));
        } else {
          setErrorMsg(extractApiErrorMessage(err));
        }
      }
    };

    const handleCopyPrompt = async () => {
      if (!manualPrompt) return;
      try {
        await navigator.clipboard.writeText(manualPrompt);
        setIsPromptCopied(true);
        setTimeout(() => setIsPromptCopied(false), 2000);
      } catch {
        // Fallback if clipboard API is not available
      }
    };

    const handleImportManualJson = async () => {
      if (!manualJsonInput.trim()) {
        setErrorMsg(t('vocab.enterJsonNotice'));
        return;
      }
      setErrorMsg(null);
      try {
        const imported = await importJsonMutation.mutateAsync({
          jsonText: manualJsonInput.trim(),
          cefrLevel: genCefr,
          topic: genTopic.trim() ? genTopic.trim() : undefined,
          modelCode: 'manual-web-ai',
          assignToTodayPlan,
          examplesPerSense,
        });
        handleSuccessNotification(imported?.length ?? 1);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 429) {
          setErrorMsg(t('vocab.rateLimit429', 'Bạn đã tạo quá nhiều, thử lại sau ít phút'));
        } else {
          setErrorMsg(extractApiErrorMessage(err));
        }
      }
    };

    const isSubmitting =
      generateMutation.isPending || buildPromptMutation.isPending || importJsonMutation.isPending;

    return (
      <Dialog
        isOpen={isOpen}
        onClose={onClose}
        maxWidth="2xl"
        icon={<Sparkles className="w-5 h-5 text-emerald-600" />}
        title={t('vocab.targetedVocabTitle', 'Tạo Từ Vựng Từ Chữ Đã Chọn')}
        description={t(
          'vocab.targetedVocabDesc',
          'Trích xuất và tạo từ vựng thông minh bằng AI từ văn bản bạn đã bôi đen'
        )}
        footer={
          <div className="flex items-center justify-end gap-2.5 w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={onClose}
              disabled={isSubmitting}
            >
              {t('vocab.cancelBtn')}
            </Button>
            {genMode === 'direct' ? (
              <Button
                variant="primary"
                size="sm"
                disabled={generateMutation.isPending || isTermInvalid}
                onClick={() => void handleGenerateDirect()}
              >
                <Sparkles className="w-4 h-4" />
                {generateMutation.isPending
                  ? t('vocab.creatingWithModel', { model: isAgyActive ? agyModelCode : selectedVocabModel })
                  : assignToTodayPlan
                  ? t('vocab.createAndAddToDaily', 'Tạo và thêm vào Daily hôm nay…')
                  : t('vocab.createVocabulary', 'Tạo từ vựng…')}
              </Button>
            ) : (
              <Button
                variant="primary"
                size="sm"
                disabled={importJsonMutation.isPending || !manualJsonInput.trim()}
                onClick={() => void handleImportManualJson()}
              >
                <Sparkles className="w-4 h-4" />
                {importJsonMutation.isPending ? t('vocab.savingToDb') : t('vocab.importWordsToDeck')}
              </Button>
            )}
          </div>
        }
      >
        <div className="space-y-4 text-xs">
          {/* Highlighted text preview block (Rendered safely without dangerouslySetInnerHTML) */}
          <div className="p-3 bg-slate-50 border border-slate-200 rounded-2xl space-y-1">
            <div className="flex items-center justify-between">
              <span className="text-[10px] uppercase font-bold tracking-wider text-slate-500">
                {isSentence
                  ? t('vocab.sourceSentenceModeTitle', 'Câu gốc (AI trích từ vựng):')
                  : t('vocab.targetTermModeTitle', 'Từ / cụm từ mục tiêu:')}
              </span>
              <Badge variant={isSentence ? 'sky' : 'emerald'}>
                {isSentence
                  ? t('vocab.sourceSentenceBadge', 'Chế độ: Trích từ câu')
                  : t('vocab.targetTermBadge', 'Chế độ: Từ / Cụm từ')}
              </Badge>
            </div>
            <p className="font-semibold text-slate-800 text-sm italic break-words selection-preview">
              "{normalizedText}"
            </p>
          </div>

          {/* Validation warning if single term exceeds 80 characters */}
          {isTermTooLong && (
            <div className="p-2.5 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-xs flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 text-amber-600" />
              <span>{t('vocab.termTooLongError', 'Từ/cụm quá dài (tối đa 80 ký tự). Hãy bôi đen ngắn hơn.')}</span>
            </div>
          )}

          {/* Mode Switch Tabs */}
          <div
            role="tablist"
            aria-label={t('vocab.generationModes', 'Chế độ tạo từ vựng')}
            className="flex border-b border-slate-200 mb-2 gap-2"
          >
            <button
              type="button"
              role="tab"
              id="tab-direct"
              aria-selected={genMode === 'direct'}
              aria-controls="panel-direct"
              disabled={isSubmitting}
              onClick={() => !isSubmitting && setGenMode('direct')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer',
                isSubmitting && 'opacity-60 cursor-not-allowed',
                genMode === 'direct'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <Cpu className="w-3.5 h-3.5" />
              {t('vocab.genModeDirect')}
            </button>
            <button
              type="button"
              role="tab"
              id="tab-manual"
              aria-selected={genMode === 'manual'}
              aria-controls="panel-manual"
              disabled={isSubmitting}
              onClick={() => !isSubmitting && setGenMode('manual')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer',
                isSubmitting && 'opacity-60 cursor-not-allowed',
                genMode === 'manual'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <FileText className="w-3.5 h-3.5" />
              {t('vocab.genModeManual')}
              <Badge variant="emerald" className="text-[10px] py-0 px-1.5 ml-1">
                {t('vocab.freeBadge')}
              </Badge>
            </button>
          </div>

          {!hasDirectAi && genMode === 'direct' && (
            <div className="p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-900 text-xs flex items-center justify-between gap-2">
              <span>{t('vocab.missingApiKeyHint', 'Chưa có Google AI Studio API Key. Bạn có thể nhập Key ở Menu trái hoặc dùng chế độ Web AI miễn phí bên dưới.')}</span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => {
                  requestFocusLeftMenuApiKey();
                }}
              >
                {t('vocab.enterApiKeyInLeftMenu', 'Nhập API Key ở Menu trái')}
              </Button>
            </div>
          )}

          {/* Options Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                {t('vocab.cefrLevelLabel')}
              </label>
              <select
                value={genCefr}
                onChange={(e) => setGenCefr(e.target.value as CefrLevel)}
                className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    CEFR {l}
                  </option>
                ))}
              </select>
            </div>

            {isSentence ? (
              <div>
                <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                  {t('vocab.sourceSentenceCountLabel', 'Số từ cần trích (1–5)')}
                </label>
                <select
                  value={requestedCount}
                  onChange={(e) => setRequestedCount(Number(e.target.value))}
                  className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
                >
                  {[1, 2, 3, 4, 5].map((num) => (
                    <option key={num} value={num}>
                      {t('vocab.wordsSimple', { count: num })}
                    </option>
                  ))}
                </select>
              </div>
            ) : (
              <div>
                <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                  {t('vocab.wordsCountLabel')}
                </label>
                <div className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-slate-50 font-semibold text-slate-600">
                  {t('vocab.wordsSimple', { count: 1 })}
                </div>
              </div>
            )}

            <div>
              <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                {t('vocab.examplesPerSenseLabel', 'Số câu ví dụ mỗi nghĩa')}
              </label>
              <select
                value={examplesPerSense}
                onChange={(e) => setExamplesPerSense(Number(e.target.value))}
                className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
              >
                {[1, 2, 3, 4, 5].map((num) => (
                  <option key={num} value={num}>
                    {t('vocab.examplesCountOption', { count: num, defaultValue: `${num} câu ví dụ` })}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <TopicCreatablePicker
            label={t('vocab.genTopicLabel')}
            value={genTopic}
            onChange={setGenTopic}
            topics={vocabTopics}
            placeholder={t('vocab.genTopicPlaceholder')}
            helperText={t('vocab.genTopicHelper')}
          />

          {genMode === 'direct' ? (
            <div id="panel-direct" role="tabpanel" aria-labelledby="tab-direct" className="space-y-3 pt-1">
              {isAgyActive ? (
                <div className="p-2.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-[11px] font-medium flex items-center justify-between">
                  <span>{t('vocab.agyPortActive', { defaultValue: 'Đang dùng cổng Antigravity CLI Local (Model: {{model}})', model: agyModelCode })}</span>
                  <span className="text-[10px] bg-emerald-200 text-emerald-900 px-1.5 py-0.5 rounded font-bold">{t('vocab.noApiKeyNeeded', 'Không cần API Key')}</span>
                </div>
              ) : (
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between">
                    <label className="font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
                      <Cpu className="w-3.5 h-3.5 text-emerald-600" />
                      {t('vocab.vocabModelLabel')}
                    </label>
                    <Badge variant="emerald">DB ({vocabModels.length})</Badge>
                  </div>
                  <select
                    value={selectedVocabModel}
                    onChange={(e) => setSelectedVocabModel(e.target.value)}
                    className="w-full px-3.5 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold text-slate-800"
                  >
                    {vocabModels.map((m) => (
                      <option key={m.modelCode} value={m.modelCode}>
                        {m.displayName} —{' '}
                        {m.supportsAudioInput ? t('vocab.multimodal') : t('vocab.textOnly')}
                      </option>
                    ))}
                  </select>
                </div>
              )}
            </div>
          ) : (
            <div id="panel-manual" role="tabpanel" aria-labelledby="tab-manual" className="space-y-3.5 pt-1">
              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 space-y-2.5">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-700 flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-emerald-600" />
                    {t('vocab.step1PromptTitle')}
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={buildPromptMutation.isPending || isTermInvalid}
                    onClick={() => void handleBuildPrompt()}
                  >
                    {buildPromptMutation.isPending ? (
                      <>
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        {t('vocab.buildingPrompt')}
                      </>
                    ) : (
                      t('vocab.buildPromptBtn')
                    )}
                  </Button>
                </div>

                {manualPrompt && (
                  <div className="space-y-2 pt-1">
                    <div className="relative">
                      <textarea
                        readOnly
                        value={manualPrompt}
                        rows={4}
                        className="w-full p-2.5 rounded-xl bg-slate-900 text-emerald-300 font-mono text-[11px] leading-relaxed resize-none focus:outline-hidden"
                      />
                      <button
                        type="button"
                        onClick={() => void handleCopyPrompt()}
                        className="absolute top-2 right-2 flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-[11px] shadow-xs cursor-pointer transition-colors"
                      >
                        {isPromptCopied ? (
                          <>
                            <Check className="w-3.5 h-3.5" />
                            {t('vocab.copiedPrompt')}
                          </>
                        ) : (
                          <>
                            <Copy className="w-3.5 h-3.5" />
                            {t('vocab.copyPrompt')}
                          </>
                        )}
                      </button>
                    </div>
                    <p className="text-[11px] text-slate-500">
                      {t('vocab.copyPromptHint')}
                    </p>
                  </div>
                )}
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-50 border border-slate-200 space-y-2.5">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-slate-700 flex items-center gap-1.5">
                    <FileText className="w-3.5 h-3.5 text-emerald-600" />
                    {t('vocab.step2JsonTitle')}
                  </span>
                  <Badge variant="sky">{t('vocab.sourceManualWebAi', 'Nguồn: manual-web-ai')}</Badge>
                </div>

                <textarea
                  value={manualJsonInput}
                  onChange={(e) => setManualJsonInput(e.target.value)}
                  rows={4}
                  placeholder={t('vocab.jsonInputPlaceholder')}
                  className="w-full p-2.5 rounded-xl border border-slate-300 bg-white font-mono text-[11px] placeholder:text-slate-400 focus:outline-hidden focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500"
                />
              </div>
            </div>
          )}

          {/* Daily Plan Checkbox */}
          <label className="flex items-center gap-2 cursor-pointer select-none text-[11px] font-semibold text-slate-700">
            <input
              type="checkbox"
              checked={assignToTodayPlan}
              onChange={(e) => setAssignToTodayPlan(e.target.checked)}
              className="w-4 h-4 rounded-sm border-slate-300 text-emerald-600 focus:ring-emerald-500"
            />
            {t('vocab.assignToTodayCheckbox')}
          </label>

          {/* Error Banner */}
          {errorMsg && (
            <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 text-rose-500" />
              <span>{errorMsg}</span>
            </div>
          )}
        </div>
      </Dialog>
    );
  }
);

TargetedVocabularyDialog.displayName = 'TargetedVocabularyDialog';
