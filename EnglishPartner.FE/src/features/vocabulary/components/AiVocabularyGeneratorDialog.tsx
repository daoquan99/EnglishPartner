import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, Copy, Cpu, FileText, Loader2, Sparkles, AlertCircle } from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { TopicCreatablePicker } from '@/shared/components/ui/TopicCreatablePicker';
import { cn } from '@/shared/lib/cn';
import { isAgyLocalEnabled } from '@/shared/config/features';
import {
  useTopicsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import { useTutorSessionStore } from '@/features/tutor';
import {
  useBuildVocabularyGenerationPromptMutation,
  useGenerateVocabularyMutation,
  useImportVocabularyJsonMutation,
} from '../hooks/useDailyVocabulary';
import type { VocabularyFlashcardDto } from '../types/vocabulary.types';
import type { CefrLevel } from '@/shared/api/openapi.types';

export interface AiVocabularyGeneratorDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (generatedCards: VocabularyFlashcardDto[]) => void;
  onSetApiKeyWarning: (msg: string | null) => void;
  initialMode?: 'direct' | 'manual';
}

export const AiVocabularyGeneratorDialog: React.FC<AiVocabularyGeneratorDialogProps> = React.memo(
  ({
    isOpen,
    onClose,
    onSuccess,
    onSetApiKeyWarning,
    initialMode,
  }) => {
    const { t } = useTranslation();
    const { apiKey, requestFocusLeftMenuApiKey, aiProvider, agyModelCode } =
      useTutorSessionStore();

    const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
    const { data: vocabTopics = [] } = useTopicsQuery({
      onlyActive: true,
      supportsVocabulary: true,
    });
    const isAgyActive = isAgyLocalEnabled && aiProvider === 'agy';

    const generateMutation = useGenerateVocabularyMutation();
    const buildPromptMutation = useBuildVocabularyGenerationPromptMutation();
    const importJsonMutation = useImportVocabularyJsonMutation();

    const [genMode, setGenMode] = useState<'direct' | 'manual'>(initialMode ?? 'direct');
    const [genCefr, setGenCefr] = useState<CefrLevel>('B2');
    const [genTopic, setGenTopic] = useState<string>(
      'Essential Workplace Phrasal Verbs & Executive Idioms'
    );
    const [genCount, setGenCount] = useState<number>(3);
    const [selectedVocabModel, setSelectedVocabModel] = useState<string>('gemini-3.8-flash');
    const [manualPrompt, setManualPrompt] = useState<string>('');
    const [isPromptCopied, setIsPromptCopied] = useState(false);
    const [manualJsonInput, setManualJsonInput] = useState<string>('');
    const [assignToTodayPlan, setAssignToTodayPlan] = useState(true);
    const [examplesPerSense, setExamplesPerSense] = useState<number>(2);
    const [manualError, setManualError] = useState<string | null>(null);

    useEffect(() => {
      if (isOpen && initialMode) {
        setGenMode(initialMode);
      }
    }, [isOpen, initialMode]);

    useEffect(() => {
      if (vocabModels.length > 0 && !vocabModels.some((m) => m.modelCode === selectedVocabModel)) {
        setSelectedVocabModel(vocabModels[0].modelCode);
      }
    }, [vocabModels, selectedVocabModel]);

    const handleGenerateMoreWords = async () => {
      const isAgy = isAgyActive;
      if (!isAgy && !apiKey.trim()) {
        onClose();
        onSetApiKeyWarning(t('vocab.apiKeyMissingNotice'));
        requestFocusLeftMenuApiKey();
        return;
      }
      onSetApiKeyWarning(null);
      try {
        const generated = await generateMutation.mutateAsync({
          apiKey: isAgy ? undefined : apiKey.trim(),
          cefrLevel: genCefr,
          topic: genTopic || 'Software Engineering & Daily Workplace',
          requestedCount: genCount,
          modelCode: isAgy ? agyModelCode : selectedVocabModel,
          provider: isAgy ? 'agy' : 'gemini',
          assignToTodayPlan,
          examplesPerSense,
        });
        if (generated && generated.length > 0) {
          onSuccess(generated);
          onClose();
        }
      } catch (err: any) {
        onSetApiKeyWarning(
          err?.response?.data?.error ||
            err?.message ||
            t('vocab.genAiFailed')
        );
      }
    };

    const handleBuildPrompt = async () => {
      setManualError(null);
      try {
        const res = await buildPromptMutation.mutateAsync({
          requestedCount: genCount,
          cefrLevel: genCefr,
          topic: genTopic || 'General English',
          examplesPerSense,
        });
        setManualPrompt(res.prompt);
      } catch (err: any) {
        setManualError(
          err?.response?.data?.error ||
            err?.message ||
            t('vocab.buildPromptFailed')
        );
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
        setManualError(t('vocab.enterJsonNotice'));
        return;
      }
      setManualError(null);
      try {
        const imported = await importJsonMutation.mutateAsync({
          jsonText: manualJsonInput.trim(),
          cefrLevel: genCefr,
          topic: genTopic || 'General English',
          modelCode: 'manual-web-ai',
          assignToTodayPlan,
          examplesPerSense,
        });
        if (imported && imported.length > 0) {
          onSuccess(imported);
          onClose();
          setManualPrompt('');
          setManualJsonInput('');
          setManualError(null);
        }
      } catch (err: any) {
        setManualError(
          err?.response?.data?.error ||
            err?.message ||
            'Không thể nhập từ vựng từ JSON. Hãy kiểm tra định dạng JSON từ AI.'
        );
      }
    };

    return (
      <Dialog
        isOpen={isOpen}
        onClose={onClose}
        maxWidth="2xl"
        icon={
          <img
            src="/assets/icons/navigation/nav-study.svg"
            alt=""
            className="w-5 h-5 object-contain"
          />
        }
        title={t('vocab.aiGeneratorTitle')}
        description={t('vocab.aiGeneratorDesc')}
        footer={
          <div className="flex items-center justify-end gap-2.5 w-full">
            <Button
              variant="outline"
              size="sm"
              onClick={onClose}
              disabled={generateMutation.isPending || importJsonMutation.isPending}
            >
              {t('vocab.cancelBtn')}
            </Button>
            {genMode === 'direct' ? (
              <Button
                variant="primary"
                size="sm"
                disabled={generateMutation.isPending}
                onClick={() => void handleGenerateMoreWords()}
              >
                <Sparkles className="w-4 h-4" />
                {generateMutation.isPending
                  ? t('vocab.creatingWithModel', { model: selectedVocabModel })
                  : t('vocab.addWordsToDeck')}
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
          {/* Mode Switch Tabs */}
          <div className="flex border-b border-slate-200 mb-2 gap-2 overflow-x-auto pb-0.5 scrollbar-none">
            <button
              type="button"
              onClick={() => setGenMode('direct')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer shrink-0 whitespace-nowrap',
                genMode === 'direct'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <Cpu className="w-3.5 h-3.5 shrink-0" />
              <span>{t('vocab.genModeDirect')}</span>
            </button>
            <button
              type="button"
              onClick={() => setGenMode('manual')}
              className={cn(
                'flex items-center gap-1.5 pb-2.5 px-3 font-bold border-b-2 transition-all cursor-pointer shrink-0 whitespace-nowrap',
                genMode === 'manual'
                  ? 'border-emerald-600 text-emerald-700'
                  : 'border-transparent text-slate-500 hover:text-slate-800'
              )}
            >
              <FileText className="w-3.5 h-3.5 shrink-0" />
              <span>{t('vocab.genModeManual')}</span>
              <Badge variant="emerald" className="text-[10px] py-0 px-1.5 ml-1 shrink-0 whitespace-nowrap">
                {t('vocab.freeBadge')}
              </Badge>
            </button>
          </div>

          {genMode === 'direct' ? (
            <>
              {isAgyActive && (
                <div className="p-2.5 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-[11px] font-medium flex items-center justify-between">
                  <span>Đang dùng cổng Antigravity CLI Local (Model: <strong>{agyModelCode}</strong>)</span>
                  <span className="text-[10px] bg-emerald-200 text-emerald-900 px-1.5 py-0.5 rounded font-bold">Không cần API Key</span>
                </div>
              )}
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
                {vocabModels.find((m) => m.modelCode === selectedVocabModel) && (
                  <p className="text-[11px] text-slate-500">
                    {vocabModels.find((m) => m.modelCode === selectedVocabModel)?.capabilityNote}
                  </p>
                )}
              </div>

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
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.wordsCountLabel')}
                  </label>
                  <select
                    value={genCount}
                    onChange={(e) => setGenCount(Number(e.target.value))}
                    className="w-full px-3 py-2.5 rounded-xl border border-slate-200 bg-white font-semibold"
                  >
                    <option value={2}>{t('vocab.intensiveWords', { count: 2 })}</option>
                    <option value={3}>{t('vocab.intensiveWords', { count: 3 })}</option>
                    <option value={5}>{t('vocab.intensiveWords', { count: 5 })}</option>
                    <option value={10}>{t('vocab.intensiveWords', { count: 10 })}</option>
                  </select>
                </div>
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

              <label className="flex items-center gap-2 cursor-pointer select-none text-[11px] font-semibold text-slate-700">
                <input
                  type="checkbox"
                  checked={assignToTodayPlan}
                  onChange={(e) => setAssignToTodayPlan(e.target.checked)}
                  className="w-4 h-4 rounded-sm border-slate-300 text-emerald-600 focus:ring-emerald-500"
                />
                {t('vocab.assignToTodayCheckbox')}
              </label>
            </>
          ) : (
            <div className="space-y-3.5">
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.cefrLevelLabel')}
                  </label>
                  <select
                    value={genCefr}
                    onChange={(e) => setGenCefr(e.target.value as CefrLevel)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-xs"
                  >
                    {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                      <option key={l} value={l}>
                        CEFR {l}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.wordsCountDesired')}
                  </label>
                  <select
                    value={genCount}
                    onChange={(e) => setGenCount(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-xs"
                  >
                    <option value={2}>{t('vocab.wordsSimple', { count: 2 })}</option>
                    <option value={3}>{t('vocab.wordsSimple', { count: 3 })}</option>
                    <option value={5}>{t('vocab.wordsSimple', { count: 5 })}</option>
                    <option value={10}>{t('vocab.wordsSimple', { count: 10 })}</option>
                  </select>
                </div>
                <div>
                  <label className="block font-bold uppercase tracking-wider text-slate-600 mb-1">
                    {t('vocab.examplesPerSenseLabel', 'Số câu ví dụ mỗi nghĩa')}
                  </label>
                  <select
                    value={examplesPerSense}
                    onChange={(e) => setExamplesPerSense(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-white font-semibold text-xs"
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
                    disabled={buildPromptMutation.isPending}
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
                  <Badge variant="sky">Nguồn: manual-web-ai</Badge>
                </div>

                <textarea
                  value={manualJsonInput}
                  onChange={(e) => setManualJsonInput(e.target.value)}
                  rows={4}
                  placeholder={t('vocab.jsonInputPlaceholder')}
                  className="w-full p-2.5 rounded-xl border border-slate-300 bg-white font-mono text-[11px] placeholder:text-slate-400 focus:outline-hidden focus:ring-2 focus:ring-emerald-500/20 focus:border-emerald-500"
                />

                <label className="flex items-center gap-2 cursor-pointer select-none text-[11px] font-semibold text-slate-700">
                  <input
                    type="checkbox"
                    checked={assignToTodayPlan}
                    onChange={(e) => setAssignToTodayPlan(e.target.checked)}
                    className="w-4 h-4 rounded-sm border-slate-300 text-emerald-600 focus:ring-emerald-500"
                  />
                  {t('vocab.assignToTodayCheckbox')}
                </label>
              </div>

              {manualError && (
                <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 text-rose-500" />
                  <span>{manualError}</span>
                </div>
              )}
            </div>
          )}
        </div>
      </Dialog>
    );
  }
);

AiVocabularyGeneratorDialog.displayName = 'AiVocabularyGeneratorDialog';
