import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog } from '@/shared/components/ui/Dialog';
import { Button } from '@/shared/components/ui/Button';
import {
  getAdminGrammarErrorMessage,
  parseJsonObjectField,
  groupIssuesByField,
} from '../../utils/grammarHelpers';
import type {
  GrammarExerciseAdminDto,
  CreateOfficialGrammarExerciseRequestDto,
  GrammarExerciseType,
  GrammarGradingMode,
} from '../../types/grammar.types';

export interface OfficialExerciseFormDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (payload: CreateOfficialGrammarExerciseRequestDto) => Promise<void>;
  initialData?: GrammarExerciseAdminDto | null;
  isSaving?: boolean;
}

const GRAMMAR_EXERCISE_TYPES: readonly GrammarExerciseType[] = [
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

const GRADING_MODES: readonly GrammarGradingMode[] = [
  'Deterministic',
  'SelfAssessment',
  'AiRubric',
  'Composite',
] as const;

const EXERCISE_FIELDS = [
  'type',
  'key',
  'difficulty',
  'title',
  'instructions',
  'focus',
  'maxPoints',
  'gradingMode',
  'isCore',
  'content',
  'grading',
];

export const OfficialExerciseFormDialog: React.FC<OfficialExerciseFormDialogProps> = ({
  isOpen,
  onClose,
  onSave,
  initialData,
  isSaving = false,
}) => {
  const { t } = useTranslation();

  const isEditing = Boolean(initialData);

  const [type, setType] = useState<GrammarExerciseType>('MultipleChoice');
  const [key, setKey] = useState<string>('');
  const [difficulty, setDifficulty] = useState<number>(1);
  const [title, setTitle] = useState<string>('');
  const [instructions, setInstructions] = useState<string>('');
  const [focus, setFocus] = useState<string>('');
  const [maxPoints, setMaxPoints] = useState<number>(10);
  const [gradingMode, setGradingMode] = useState<GrammarGradingMode>('Deterministic');
  const [isCore, setIsCore] = useState<boolean>(true);
  const [contentJson, setContentJson] = useState<string>('{}');
  const [gradingJson, setGradingJson] = useState<string>('');

  const [clientErrors, setClientErrors] = useState<Record<string, string>>({});
  const [serverFieldErrors, setServerFieldErrors] = useState<Record<string, string[]>>({});
  const [unmatchedIssues, setUnmatchedIssues] = useState<Array<{ path: string; message: string }>>([]);
  const [generalError, setGeneralError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      if (initialData) {
        setType((initialData.type as GrammarExerciseType) || 'MultipleChoice');
        setKey(initialData.key || '');
        setDifficulty(initialData.difficulty || 1);
        setTitle(initialData.title || '');
        setInstructions(initialData.instructions || '');
        setFocus(initialData.focus || '');
        setMaxPoints(initialData.maxPoints ?? 10);
        setGradingMode((initialData.gradingMode as GrammarGradingMode) || 'Deterministic');
        setIsCore(Boolean(initialData.isCore));
        setContentJson(JSON.stringify(initialData.content || {}, null, 2));
        setGradingJson(
          initialData.grading ? JSON.stringify(initialData.grading, null, 2) : ''
        );
      } else {
        setType('MultipleChoice');
        setKey('');
        setDifficulty(1);
        setTitle('');
        setInstructions('');
        setFocus('');
        setMaxPoints(10);
        setGradingMode('Deterministic');
        setIsCore(true);
        setContentJson('{}');
        setGradingJson('');
      }
      setClientErrors({});
      setServerFieldErrors({});
      setUnmatchedIssues([]);
      setGeneralError(null);
    }
  }, [isOpen, initialData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setClientErrors({});
    setServerFieldErrors({});
    setUnmatchedIssues([]);
    setGeneralError(null);

    const errors: Record<string, string> = {};

    if (!title.trim()) {
      errors.title = t('admin.grammar.exerciseErrors.titleRequired');
    }
    if (!instructions.trim()) {
      errors.instructions = t('admin.grammar.exerciseErrors.instructionsRequired');
    }
    if (Number(maxPoints) < 1) {
      errors.maxPoints = t('admin.grammar.exerciseErrors.maxPointsMin1');
    }

    const contentParsed = parseJsonObjectField(contentJson, 'content', false);
    if (!contentParsed.ok) {
      errors.content = t(contentParsed.errorKey);
    }

    const gradingParsed = parseJsonObjectField(gradingJson, 'grading', true);
    if (!gradingParsed.ok) {
      errors.grading = t(gradingParsed.errorKey);
    }

    if (!contentParsed.ok || !gradingParsed.ok || Object.keys(errors).length > 0) {
      setClientErrors(errors);
      return;
    }

    try {
      const payload: CreateOfficialGrammarExerciseRequestDto = {
        key: isEditing ? undefined : key.trim() || null,
        type,
        difficulty,
        title: title.trim(),
        instructions: instructions.trim(),
        focus: focus.trim() || null,
        maxPoints: Number(maxPoints) || 1,
        gradingMode,
        isCore,
        content: contentParsed.value!,
        grading: gradingParsed.value,
      };

      await onSave(payload);
      onClose();
    } catch (err: any) {
      const grouped = groupIssuesByField(
        err?.response?.data?.errors ?? {},
        EXERCISE_FIELDS
      );
      setServerFieldErrors(grouped.fieldIssues);
      setUnmatchedIssues(grouped.otherIssues);

      const hasFieldErrors = Object.values(grouped.fieldIssues).some(
        (arr) => arr.length > 0
      );
      if (!hasFieldErrors && grouped.otherIssues.length === 0) {
        setGeneralError(getAdminGrammarErrorMessage(err, t));
      }
    }
  };

  return (
    <Dialog
      isOpen={isOpen}
      onClose={onClose}
      title={
        isEditing
          ? t('admin.grammar.editExerciseTitle')
          : t('admin.grammar.createExerciseTitle')
      }
      maxWidth="3xl"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {generalError && (
          <div className="p-3 rounded-xl bg-rose-50 border border-rose-200 text-xs text-rose-700 font-semibold">
            {generalError}
          </div>
        )}

        {unmatchedIssues.length > 0 && (
          <div className="p-3.5 rounded-xl bg-amber-50 border border-amber-200 text-xs text-amber-900 space-y-1">
            <div className="font-bold">{t('admin.grammar.otherIssuesHeading')}:</div>
            <ul className="list-disc list-inside space-y-0.5 text-[11px] text-amber-800">
              {unmatchedIssues.map((issue, idx) => (
                <li key={idx}>
                  <span className="font-mono font-bold">{issue.path}: </span>
                  <span>{issue.message}</span>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {/* Type */}
          <div>
            <label
              htmlFor="exercise-type-select"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.exerciseType')}
            </label>
            <select
              id="exercise-type-select"
              value={type}
              onChange={(e) => setType(e.target.value as GrammarExerciseType)}
              className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
            >
              {GRAMMAR_EXERCISE_TYPES.map((tVal) => (
                <option key={tVal} value={tVal}>
                  {t(`grammar.exerciseTypes.${tVal}` as any)}
                </option>
              ))}
            </select>
            {serverFieldErrors.type?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>

          {/* Key */}
          <div>
            <label
              htmlFor="exercise-key-input"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.exerciseKey')}
            </label>
            <input
              id="exercise-key-input"
              type="text"
              value={key}
              onChange={(e) => setKey(e.target.value)}
              disabled={isEditing}
              readOnly={isEditing}
              placeholder={t('admin.grammar.exerciseKeyPlaceholder')}
              className={`w-full p-2.5 rounded-xl border border-slate-300 text-xs ${
                isEditing
                  ? 'bg-slate-200 text-slate-500 cursor-not-allowed'
                  : 'bg-slate-50 focus:bg-white'
              }`}
            />
            {serverFieldErrors.key?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>

          {/* Difficulty */}
          <div>
            <label
              htmlFor="exercise-difficulty-select"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.difficulty')}
            </label>
            <select
              id="exercise-difficulty-select"
              value={difficulty}
              onChange={(e) => setDifficulty(Number(e.target.value))}
              className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
            >
              <option value={1}>1 — {t('grammar.difficultyBasic')}</option>
              <option value={2}>2 — {t('grammar.difficultyMedium')}</option>
              <option value={3}>3 — {t('grammar.difficultyAdvanced')}</option>
            </select>
            {serverFieldErrors.difficulty?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>
        </div>

        {/* Title */}
        <div>
          <label
            htmlFor="exercise-title-input"
            className="block text-xs font-bold text-slate-700 mb-1"
          >
            {t('admin.grammar.exerciseTitle')}
          </label>
          <input
            id="exercise-title-input"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t('admin.grammar.exerciseTitlePlaceholder')}
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
          />
          {clientErrors.title && (
            <p className="text-xs text-rose-600 mt-1">{clientErrors.title}</p>
          )}
          {serverFieldErrors.title?.map((err, i) => (
            <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
          ))}
        </div>

        {/* Instructions */}
        <div>
          <label
            htmlFor="exercise-instructions-input"
            className="block text-xs font-bold text-slate-700 mb-1"
          >
            {t('admin.grammar.exerciseInstructions')}
          </label>
          <textarea
            id="exercise-instructions-input"
            rows={2}
            value={instructions}
            onChange={(e) => setInstructions(e.target.value)}
            placeholder={t('admin.grammar.exerciseInstructionsPlaceholder')}
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
          />
          {clientErrors.instructions && (
            <p className="text-xs text-rose-600 mt-1">{clientErrors.instructions}</p>
          )}
          {serverFieldErrors.instructions?.map((err, i) => (
            <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
          ))}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {/* Max Points */}
          <div>
            <label
              htmlFor="exercise-maxpoints-input"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.maxPoints')}
            </label>
            <input
              id="exercise-maxpoints-input"
              type="number"
              min={1}
              max={100}
              value={maxPoints}
              onChange={(e) => setMaxPoints(Number(e.target.value))}
              className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
            />
            {clientErrors.maxPoints && (
              <p className="text-xs text-rose-600 mt-1">{clientErrors.maxPoints}</p>
            )}
            {serverFieldErrors.maxPoints?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>

          {/* Grading Mode */}
          <div>
            <label
              htmlFor="exercise-gradingmode-select"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.gradingMode')}
            </label>
            <select
              id="exercise-gradingmode-select"
              value={gradingMode}
              onChange={(e) => setGradingMode(e.target.value as GrammarGradingMode)}
              className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
            >
              {GRADING_MODES.map((mode) => (
                <option key={mode} value={mode}>
                  {mode}
                </option>
              ))}
            </select>
            {serverFieldErrors.gradingMode?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>

          {/* Focus */}
          <div>
            <label
              htmlFor="exercise-focus-input"
              className="block text-xs font-bold text-slate-700 mb-1"
            >
              {t('admin.grammar.focusLabel')}
            </label>
            <input
              id="exercise-focus-input"
              type="text"
              value={focus}
              onChange={(e) => setFocus(e.target.value)}
              placeholder={t('admin.grammar.focusPlaceholder')}
              className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
            />
            {serverFieldErrors.focus?.map((err, i) => (
              <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
            ))}
          </div>
        </div>

        {/* Is Core Checkbox */}
        <div className="p-3 rounded-xl border border-slate-200 bg-slate-50">
          <label htmlFor="exercise-iscore-checkbox" className="flex items-center gap-2 cursor-pointer">
            <input
              id="exercise-iscore-checkbox"
              type="checkbox"
              checked={isCore}
              onChange={(e) => setIsCore(e.target.checked)}
              className="rounded text-emerald-600 focus:ring-emerald-500 w-4 h-4 cursor-pointer"
            />
            <span className="font-bold text-slate-800 text-xs">
              {t('admin.grammar.markAsCoreExercise')}
            </span>
          </label>
          {serverFieldErrors.isCore?.map((err, i) => (
            <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
          ))}
        </div>

        {/* Content JSON Textarea */}
        <div>
          <label
            htmlFor="exercise-content-json"
            className="block text-xs font-bold text-slate-700 mb-1"
          >
            {t('admin.grammar.contentJsonLabel')}
          </label>
          <textarea
            id="exercise-content-json"
            rows={5}
            value={contentJson}
            onChange={(e) => setContentJson(e.target.value)}
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs font-mono bg-slate-50 focus:bg-white"
          />
          {clientErrors.content && (
            <p className="text-xs text-rose-600 mt-1">{clientErrors.content}</p>
          )}
          {serverFieldErrors.content?.map((err, i) => (
            <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
          ))}
        </div>

        {/* Grading JSON Textarea */}
        <div>
          <label
            htmlFor="exercise-grading-json"
            className="block text-xs font-bold text-slate-700 mb-1"
          >
            {t('admin.grammar.gradingJsonLabel')}
          </label>
          <textarea
            id="exercise-grading-json"
            rows={4}
            value={gradingJson}
            onChange={(e) => setGradingJson(e.target.value)}
            placeholder="{}"
            className="w-full p-2.5 rounded-xl border border-slate-300 text-xs font-mono bg-slate-50 focus:bg-white"
          />
          {clientErrors.grading && (
            <p className="text-xs text-rose-600 mt-1">{clientErrors.grading}</p>
          )}
          {serverFieldErrors.grading?.map((err, i) => (
            <p key={i} className="text-xs text-rose-600 mt-1">{err}</p>
          ))}
        </div>

        <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={onClose}
            disabled={isSaving}
          >
            {t('common.cancel')}
          </Button>
          <Button
            type="submit"
            variant="primary"
            size="sm"
            disabled={isSaving}
          >
            {isSaving ? t('common.saving') : t('common.save')}
          </Button>
        </div>
      </form>
    </Dialog>
  );
};
