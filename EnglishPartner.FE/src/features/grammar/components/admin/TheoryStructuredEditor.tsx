import React from 'react';
import { useTranslation } from 'react-i18next';
import {
  Plus,
  Trash2,
  ChevronUp,
  ChevronDown,
  Target,
  BookOpen,
  AlertTriangle,
  Lightbulb,
  FileText,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { moveItem } from '../../utils/grammarHelpers';
import type {
  GrammarTheoryDto,
  GrammarTheorySectionDto,
  GrammarTheoryFormulaDto,
  GrammarTheoryExampleDto,
  GrammarTheoryMistakeDto,
} from '../../types/grammar.types';

export interface TheoryStructuredEditorProps {
  theory: GrammarTheoryDto;
  onChange: (updated: GrammarTheoryDto) => void;
  errorsByPath?: Record<string, string[]>;
}

export const TheoryStructuredEditor: React.FC<TheoryStructuredEditorProps> = ({
  theory,
  onChange,
  errorsByPath = {},
}) => {
  const { t } = useTranslation();

  const displayedPaths = new Set<string>([
    'objectives',
    '$.objectives',
    'sections',
    '$.sections',
    'summary',
    '$.summary',
  ]);

  (theory.sections || []).forEach((_, sIdx) => {
    displayedPaths.add(`sections[${sIdx}].heading`.toLowerCase());
    displayedPaths.add(`$.sections[${sIdx}].heading`.toLowerCase());
    displayedPaths.add(`sections[${sIdx}].explanationvi`.toLowerCase());
    displayedPaths.add(`$.sections[${sIdx}].explanationvi`.toLowerCase());
  });

  const unhandledErrors = Object.entries(errorsByPath).filter(
    ([path]) =>
      !displayedPaths.has(path.toLowerCase()) &&
      !displayedPaths.has(path.replace(/^\$\.?/, '').toLowerCase())
  );

  // Helper to get errors for a specific JSON path (e.g. "$.sections[0].heading")
  const getFieldErrors = (path: string): string[] => {
    return errorsByPath[path] || errorsByPath[`$.${path}`] || [];
  };

  // Objectives handlers
  const handleAddObjective = () => {
    const objectives = theory.objectives ? [...theory.objectives, ''] : [''];
    onChange({ ...theory, objectives });
  };

  const handleUpdateObjective = (index: number, val: string) => {
    const objectives = [...(theory.objectives || [])];
    objectives[index] = val;
    onChange({ ...theory, objectives });
  };

  const handleRemoveObjective = (index: number) => {
    const objectives = [...(theory.objectives || [])];
    objectives.splice(index, 1);
    onChange({ ...theory, objectives });
  };

  const handleMoveObjective = (index: number, direction: 'up' | 'down') => {
    const objectives = [...(theory.objectives || [])];
    const targetIdx = direction === 'up' ? index - 1 : index + 1;
    if (targetIdx < 0 || targetIdx >= objectives.length) return;
    const [item] = objectives.splice(index, 1);
    objectives.splice(targetIdx, 0, item);
    onChange({ ...theory, objectives });
  };

  // Sections handlers
  const handleAddSection = () => {
    const newSection: GrammarTheorySectionDto = {
      heading: '',
      explanationVi: '',
      formulas: [],
      examples: [],
      notes: [],
    };
    onChange({
      ...theory,
      sections: [...(theory.sections || []), newSection],
    });
  };

  const handleUpdateSectionField = <K extends keyof GrammarTheorySectionDto>(
    sIdx: number,
    field: K,
    val: GrammarTheorySectionDto[K]
  ) => {
    const sections = [...(theory.sections || [])];
    sections[sIdx] = { ...sections[sIdx], [field]: val };
    onChange({ ...theory, sections });
  };

  const handleRemoveSection = (sIdx: number) => {
    const sections = [...(theory.sections || [])];
    sections.splice(sIdx, 1);
    onChange({ ...theory, sections });
  };

  const handleMoveSection = (sIdx: number, direction: 'up' | 'down') => {
    const sections = [...(theory.sections || [])];
    const targetIdx = direction === 'up' ? sIdx - 1 : sIdx + 1;
    if (targetIdx < 0 || targetIdx >= sections.length) return;
    const [item] = sections.splice(sIdx, 1);
    sections.splice(targetIdx, 0, item);
    onChange({ ...theory, sections });
  };

  // Section Formula handlers
  const handleAddFormula = (sIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const formulas = [...(section.formulas || []), { label: '', pattern: '' }];
    handleUpdateSectionField(sIdx, 'formulas', formulas);
  };

  const handleUpdateFormula = (
    sIdx: number,
    fIdx: number,
    field: 'label' | 'pattern',
    val: string
  ) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const formulas = [...(section.formulas || [])];
    const cur = formulas[fIdx];
    if (typeof cur === 'string') {
      formulas[fIdx] = field === 'label' ? { label: val, pattern: cur } : { label: '', pattern: val };
    } else {
      formulas[fIdx] = { ...(cur as GrammarTheoryFormulaDto), [field]: val };
    }
    handleUpdateSectionField(sIdx, 'formulas', formulas);
  };

  const handleRemoveFormula = (sIdx: number, fIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const formulas = [...(section.formulas || [])];
    formulas.splice(fIdx, 1);
    handleUpdateSectionField(sIdx, 'formulas', formulas);
  };

  const handleMoveFormula = (sIdx: number, fIdx: number, direction: 'up' | 'down') => {
    const section = (theory.sections || [])[sIdx];
    if (!section || !section.formulas) return;
    const targetIdx = direction === 'up' ? fIdx - 1 : fIdx + 1;
    const reordered = moveItem(section.formulas, fIdx, targetIdx);
    handleUpdateSectionField(sIdx, 'formulas', reordered);
  };

  // Section Example handlers
  const handleAddExample = (sIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const examples = [...(section.examples || []), { en: '', vi: '', highlight: '' }];
    handleUpdateSectionField(sIdx, 'examples', examples);
  };

  const handleUpdateExample = (
    sIdx: number,
    eIdx: number,
    field: keyof GrammarTheoryExampleDto,
    val: string
  ) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const examples = [...(section.examples || [])];
    examples[eIdx] = { ...examples[eIdx], [field]: val };
    handleUpdateSectionField(sIdx, 'examples', examples);
  };

  const handleRemoveExample = (sIdx: number, eIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const examples = [...(section.examples || [])];
    examples.splice(eIdx, 1);
    handleUpdateSectionField(sIdx, 'examples', examples);
  };

  const handleMoveExample = (sIdx: number, eIdx: number, direction: 'up' | 'down') => {
    const section = (theory.sections || [])[sIdx];
    if (!section || !section.examples) return;
    const targetIdx = direction === 'up' ? eIdx - 1 : eIdx + 1;
    const reordered = moveItem(section.examples, eIdx, targetIdx);
    handleUpdateSectionField(sIdx, 'examples', reordered);
  };

  // Section Notes handlers
  const handleAddNote = (sIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const notes = [...(section.notes || []), ''];
    handleUpdateSectionField(sIdx, 'notes', notes);
  };

  const handleUpdateNote = (sIdx: number, nIdx: number, val: string) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const notes = [...(section.notes || [])];
    notes[nIdx] = val;
    handleUpdateSectionField(sIdx, 'notes', notes);
  };

  const handleRemoveNote = (sIdx: number, nIdx: number) => {
    const section = (theory.sections || [])[sIdx];
    if (!section) return;
    const notes = [...(section.notes || [])];
    notes.splice(nIdx, 1);
    handleUpdateSectionField(sIdx, 'notes', notes);
  };

  const handleMoveNote = (sIdx: number, nIdx: number, direction: 'up' | 'down') => {
    const section = (theory.sections || [])[sIdx];
    if (!section || !section.notes) return;
    const targetIdx = direction === 'up' ? nIdx - 1 : nIdx + 1;
    const reordered = moveItem(section.notes, nIdx, targetIdx);
    handleUpdateSectionField(sIdx, 'notes', reordered);
  };

  // Common Mistakes handlers
  const handleAddMistake = () => {
    const commonMistakes = [
      ...(theory.commonMistakes || []),
      { wrong: '', right: '', explanationVi: '' },
    ];
    onChange({ ...theory, commonMistakes });
  };

  const handleUpdateMistake = (
    mIdx: number,
    field: keyof GrammarTheoryMistakeDto,
    val: string
  ) => {
    const commonMistakes = [...(theory.commonMistakes || [])];
    commonMistakes[mIdx] = { ...commonMistakes[mIdx], [field]: val };
    onChange({ ...theory, commonMistakes });
  };

  const handleRemoveMistake = (mIdx: number) => {
    const commonMistakes = [...(theory.commonMistakes || [])];
    commonMistakes.splice(mIdx, 1);
    onChange({ ...theory, commonMistakes });
  };

  const handleMoveMistake = (mIdx: number, direction: 'up' | 'down') => {
    if (!theory.commonMistakes) return;
    const targetIdx = direction === 'up' ? mIdx - 1 : mIdx + 1;
    const reordered = moveItem(theory.commonMistakes, mIdx, targetIdx);
    onChange({ ...theory, commonMistakes: reordered });
  };

  // Tips handlers
  const handleAddTip = () => {
    const tips = theory.tips ? [...theory.tips, ''] : [''];
    onChange({ ...theory, tips });
  };

  const handleUpdateTip = (tIdx: number, val: string) => {
    const tips = [...(theory.tips || [])];
    tips[tIdx] = val;
    onChange({ ...theory, tips });
  };

  const handleRemoveTip = (tIdx: number) => {
    const tips = [...(theory.tips || [])];
    tips.splice(tIdx, 1);
    onChange({ ...theory, tips });
  };

  const handleMoveTip = (tIdx: number, direction: 'up' | 'down') => {
    if (!theory.tips) return;
    const targetIdx = direction === 'up' ? tIdx - 1 : tIdx + 1;
    const reordered = moveItem(theory.tips, tIdx, targetIdx);
    onChange({ ...theory, tips: reordered });
  };

  // Summary handlers
  const handleAddSummary = () => {
    const summary = theory.summary ? [...theory.summary, ''] : [''];
    onChange({ ...theory, summary });
  };

  const handleUpdateSummary = (sIdx: number, val: string) => {
    const summary = [...(theory.summary || [])];
    summary[sIdx] = val;
    onChange({ ...theory, summary });
  };

  const handleRemoveSummary = (sIdx: number) => {
    const summary = [...(theory.summary || [])];
    summary.splice(sIdx, 1);
    onChange({ ...theory, summary });
  };

  const handleMoveSummary = (sIdx: number, direction: 'up' | 'down') => {
    if (!theory.summary) return;
    const targetIdx = direction === 'up' ? sIdx - 1 : sIdx + 1;
    const reordered = moveItem(theory.summary, sIdx, targetIdx);
    onChange({ ...theory, summary: reordered });
  };

  return (
    <div className="space-y-6">
      {unhandledErrors.length > 0 && (
        <div className="p-4 rounded-xl border border-rose-200 bg-rose-50 text-rose-800 space-y-2">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-rose-600 flex-shrink-0" />
            <h4 className="text-xs font-bold text-rose-900">
              {t('admin.grammar.otherIssuesHeading')}
            </h4>
          </div>
          <ul className="list-disc pl-5 text-xs space-y-1">
            {unhandledErrors.map(([path, msgs]) => (
              <li key={path}>
                <span className="font-mono font-semibold">{path}</span>: {msgs.join(', ')}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* 1. Objectives */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Target className="w-5 h-5 text-emerald-600" />
            <h3 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.objectivesTitle')}
            </h3>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddObjective}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{t('admin.grammar.addObjective')}</span>
          </Button>
        </div>

        {getFieldErrors('objectives').map((err, idx) => (
          <p key={idx} className="text-xs text-rose-600 font-semibold">{err}</p>
        ))}

        <div className="space-y-2">
          {(theory.objectives || []).map((obj, idx) => (
            <div key={idx} className="flex items-center gap-2">
              <span className="text-xs font-bold text-slate-400 w-5 text-right">{idx + 1}.</span>
              <input
                type="text"
                value={obj}
                aria-label={t('admin.grammar.objectiveItemLabel', { index: idx + 1 })}
                onChange={(e) => handleUpdateObjective(idx, e.target.value)}
                placeholder={t('admin.grammar.objectivePlaceholder')}
                className="flex-1 p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />
              <button
                type="button"
                onClick={() => handleMoveObjective(idx, 'up')}
                disabled={idx === 0}
                aria-label={t('admin.grammar.moveUp')}
                className="p-1.5 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronUp className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleMoveObjective(idx, 'down')}
                disabled={idx === (theory.objectives?.length || 0) - 1}
                aria-label={t('admin.grammar.moveDown')}
                className="p-1.5 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronDown className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleRemoveObjective(idx)}
                aria-label={t('admin.grammar.removeObjective')}
                className="p-1.5 text-slate-400 hover:text-rose-600 cursor-pointer"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
          {(!theory.objectives || theory.objectives.length === 0) && (
            <p className="text-xs text-slate-400 italic">{t('admin.grammar.noObjectives')}</p>
          )}
        </div>
      </div>

      {/* 2. Sections */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <BookOpen className="w-5 h-5 text-emerald-600" />
            <h3 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.sectionsTitle')}
            </h3>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddSection}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{t('admin.grammar.addSection')}</span>
          </Button>
        </div>

        {getFieldErrors('sections').map((err, idx) => (
          <p key={idx} className="text-xs text-rose-600 font-semibold">{err}</p>
        ))}

        <div className="space-y-5">
          {(theory.sections || []).map((sec, sIdx) => {
            const secPath = `sections[${sIdx}]`;
            const headingErrors = getFieldErrors(`${secPath}.heading`);
            const explErrors = getFieldErrors(`${secPath}.explanationVi`);

            return (
              <div
                key={sIdx}
                className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4"
              >
                {/* Section header */}
                <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                  <div className="flex items-center gap-2">
                    <span className="w-6 h-6 rounded-full bg-emerald-100 text-emerald-800 font-bold text-xs flex items-center justify-center">
                      {sIdx + 1}
                    </span>
                    <span className="font-bold text-xs text-slate-700">
                      {t('admin.grammar.sectionIndex', { index: sIdx + 1 })}
                    </span>
                  </div>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => handleMoveSection(sIdx, 'up')}
                      disabled={sIdx === 0}
                      aria-label={t('admin.grammar.moveUp')}
                      className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                    >
                      <ChevronUp className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleMoveSection(sIdx, 'down')}
                      disabled={sIdx === (theory.sections?.length || 0) - 1}
                      aria-label={t('admin.grammar.moveDown')}
                      className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                    >
                      <ChevronDown className="w-4 h-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleRemoveSection(sIdx)}
                      aria-label={t('admin.grammar.removeSection')}
                      className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>

                {/* Section Heading & Explanation */}
                <div className="space-y-3">
                  <div>
                    <label
                      htmlFor={`section-${sIdx}-heading`}
                      className="block text-xs font-semibold text-slate-700 mb-1"
                    >
                      {t('admin.grammar.sectionHeading')}
                    </label>
                    <input
                      id={`section-${sIdx}-heading`}
                      type="text"
                      value={sec.heading}
                      onChange={(e) => handleUpdateSectionField(sIdx, 'heading', e.target.value)}
                      placeholder={t('admin.grammar.sectionHeadingPlaceholder')}
                      className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
                    />
                    {headingErrors.map((err, idx) => (
                      <p key={idx} className="text-xs text-rose-600 mt-1">{err}</p>
                    ))}
                  </div>

                  <div>
                    <label
                      htmlFor={`section-${sIdx}-explanation`}
                      className="block text-xs font-semibold text-slate-700 mb-1"
                    >
                      {t('admin.grammar.sectionExplanation')}
                    </label>
                    <textarea
                      id={`section-${sIdx}-explanation`}
                      rows={3}
                      value={sec.explanationVi}
                      onChange={(e) =>
                        handleUpdateSectionField(sIdx, 'explanationVi', e.target.value)
                      }
                      placeholder={t('admin.grammar.sectionExplanationPlaceholder')}
                      className="w-full p-2.5 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
                    />
                    {explErrors.map((err, idx) => (
                      <p key={idx} className="text-xs text-rose-600 mt-1">{err}</p>
                    ))}
                  </div>
                </div>

                {/* Formulas */}
                <div className="space-y-2 pt-2 border-t border-slate-100">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-slate-600">
                      {t('admin.grammar.formulasTitle')}
                    </span>
                    <button
                      type="button"
                      onClick={() => handleAddFormula(sIdx)}
                      className="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 cursor-pointer"
                    >
                      <Plus className="w-3 h-3" />
                      <span>{t('admin.grammar.addFormula')}</span>
                    </button>
                  </div>
                  {(sec.formulas || []).map((f, fIdx) => {
                    const label = typeof f === 'string' ? '' : f.label;
                    const pattern = typeof f === 'string' ? f : f.pattern;

                    return (
                      <div key={fIdx} className="flex items-center gap-2">
                        <input
                          type="text"
                          value={label}
                          aria-label={t('admin.grammar.formulaItemLabel', { index: fIdx + 1 })}
                          onChange={(e) =>
                            handleUpdateFormula(sIdx, fIdx, 'label', e.target.value)
                          }
                          placeholder={t('admin.grammar.formulaLabelPlaceholder')}
                          className="w-1/3 p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
                        />
                        <input
                          type="text"
                          value={pattern}
                          aria-label={t('admin.grammar.formulaItemLabel', { index: fIdx + 1 })}
                          onChange={(e) =>
                            handleUpdateFormula(sIdx, fIdx, 'pattern', e.target.value)
                          }
                          placeholder={t('admin.grammar.formulaPatternPlaceholder')}
                          className="flex-1 p-2 rounded-xl border border-slate-300 text-xs font-mono bg-slate-50 focus:bg-white"
                        />
                        <button
                          type="button"
                          onClick={() => handleMoveFormula(sIdx, fIdx, 'up')}
                          disabled={fIdx === 0}
                          aria-label={t('admin.grammar.moveUp')}
                          className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                        >
                          <ChevronUp className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleMoveFormula(sIdx, fIdx, 'down')}
                          disabled={fIdx === (sec.formulas?.length || 0) - 1}
                          aria-label={t('admin.grammar.moveDown')}
                          className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                        >
                          <ChevronDown className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleRemoveFormula(sIdx, fIdx)}
                          aria-label={t('admin.grammar.removeFormula')}
                          className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    );
                  })}
                </div>

                {/* Examples */}
                <div className="space-y-2 pt-2 border-t border-slate-100">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-slate-600">
                      {t('admin.grammar.examplesTitle')}
                    </span>
                    <button
                      type="button"
                      onClick={() => handleAddExample(sIdx)}
                      className="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 cursor-pointer"
                    >
                      <Plus className="w-3 h-3" />
                      <span>{t('admin.grammar.addExample')}</span>
                    </button>
                  </div>
                  {(sec.examples || []).map((ex, eIdx) => (
                    <div key={eIdx} className="p-2.5 rounded-xl border border-slate-200 bg-slate-50/70 space-y-2">
                      <div className="flex items-center gap-2">
                        <input
                          type="text"
                          value={ex.en}
                          aria-label={t('admin.grammar.exampleItemLabel', { index: eIdx + 1 })}
                          onChange={(e) =>
                            handleUpdateExample(sIdx, eIdx, 'en', e.target.value)
                          }
                          placeholder={t('admin.grammar.exampleEnPlaceholder')}
                          className="flex-1 p-2 rounded-xl border border-slate-300 text-xs bg-white"
                        />
                        <input
                          type="text"
                          value={ex.highlight || ''}
                          aria-label={t('admin.grammar.exampleItemLabel', { index: eIdx + 1 })}
                          onChange={(e) =>
                            handleUpdateExample(sIdx, eIdx, 'highlight', e.target.value)
                          }
                          placeholder={t('admin.grammar.exampleHighlightPlaceholder')}
                          className="w-1/3 p-2 rounded-xl border border-slate-300 text-xs bg-white font-mono"
                        />
                        <button
                          type="button"
                          onClick={() => handleMoveExample(sIdx, eIdx, 'up')}
                          disabled={eIdx === 0}
                          aria-label={t('admin.grammar.moveUp')}
                          className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                        >
                          <ChevronUp className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleMoveExample(sIdx, eIdx, 'down')}
                          disabled={eIdx === (sec.examples?.length || 0) - 1}
                          aria-label={t('admin.grammar.moveDown')}
                          className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                        >
                          <ChevronDown className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleRemoveExample(sIdx, eIdx)}
                          aria-label={t('admin.grammar.removeExample')}
                          className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                      <input
                        type="text"
                        value={ex.vi}
                        aria-label={t('admin.grammar.exampleItemLabel', { index: eIdx + 1 })}
                        onChange={(e) =>
                          handleUpdateExample(sIdx, eIdx, 'vi', e.target.value)
                        }
                        placeholder={t('admin.grammar.exampleViPlaceholder')}
                        className="w-full p-2 rounded-xl border border-slate-300 text-xs bg-white"
                      />
                    </div>
                  ))}
                </div>

                {/* Notes */}
                <div className="space-y-2 pt-2 border-t border-slate-100">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-slate-600">
                      {t('admin.grammar.notesTitle')}
                    </span>
                    <button
                      type="button"
                      onClick={() => handleAddNote(sIdx)}
                      className="text-xs text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 cursor-pointer"
                    >
                      <Plus className="w-3 h-3" />
                      <span>{t('admin.grammar.addNote')}</span>
                    </button>
                  </div>
                  {(sec.notes || []).map((note, nIdx) => (
                    <div key={nIdx} className="flex items-center gap-2">
                      <input
                        type="text"
                        value={note}
                        aria-label={t('admin.grammar.noteItemLabel', { index: nIdx + 1 })}
                        onChange={(e) => handleUpdateNote(sIdx, nIdx, e.target.value)}
                        placeholder={t('admin.grammar.notePlaceholder')}
                        className="flex-1 p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
                      />
                      <button
                        type="button"
                        onClick={() => handleMoveNote(sIdx, nIdx, 'up')}
                        disabled={nIdx === 0}
                        aria-label={t('admin.grammar.moveUp')}
                        className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                      >
                        <ChevronUp className="w-4 h-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => handleMoveNote(sIdx, nIdx, 'down')}
                        disabled={nIdx === (sec.notes?.length || 0) - 1}
                        aria-label={t('admin.grammar.moveDown')}
                        className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                      >
                        <ChevronDown className="w-4 h-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => handleRemoveNote(sIdx, nIdx)}
                        aria-label={t('admin.grammar.removeNote')}
                        className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* 3. Common Mistakes */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-amber-600" />
            <h3 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.commonMistakesTitle')}
            </h3>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddMistake}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{t('admin.grammar.addMistake')}</span>
          </Button>
        </div>

        <div className="space-y-3">
          {(theory.commonMistakes || []).map((m, mIdx) => (
            <div key={mIdx} className="p-3 rounded-xl border border-amber-200 bg-amber-50/50 space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-amber-900">
                  {t('admin.grammar.mistakeIndex', { index: mIdx + 1 })}
                </span>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => handleMoveMistake(mIdx, 'up')}
                    disabled={mIdx === 0}
                    aria-label={t('admin.grammar.moveUp')}
                    className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronUp className="w-4 h-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => handleMoveMistake(mIdx, 'down')}
                    disabled={mIdx === (theory.commonMistakes?.length || 0) - 1}
                    aria-label={t('admin.grammar.moveDown')}
                    className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
                  >
                    <ChevronDown className="w-4 h-4" />
                  </button>
                  <button
                    type="button"
                    onClick={() => handleRemoveMistake(mIdx)}
                    aria-label={t('admin.grammar.removeMistake')}
                    className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                <input
                  type="text"
                  value={m.wrong}
                  aria-label={t('admin.grammar.mistakeWrongLabel', { index: mIdx + 1 })}
                  onChange={(e) => handleUpdateMistake(mIdx, 'wrong', e.target.value)}
                  placeholder={t('admin.grammar.mistakeWrongPlaceholder')}
                  className="p-2 rounded-xl border border-slate-300 text-xs bg-white text-rose-700"
                />
                <input
                  type="text"
                  value={m.right}
                  aria-label={t('admin.grammar.mistakeRightLabel', { index: mIdx + 1 })}
                  onChange={(e) => handleUpdateMistake(mIdx, 'right', e.target.value)}
                  placeholder={t('admin.grammar.mistakeRightPlaceholder')}
                  className="p-2 rounded-xl border border-slate-300 text-xs bg-white text-emerald-700"
                />
              </div>
              <input
                type="text"
                value={m.explanationVi}
                aria-label={t('admin.grammar.mistakeExplanationLabel', { index: mIdx + 1 })}
                onChange={(e) => handleUpdateMistake(mIdx, 'explanationVi', e.target.value)}
                placeholder={t('admin.grammar.mistakeExplanationPlaceholder')}
                className="w-full p-2 rounded-xl border border-slate-300 text-xs bg-white"
              />
            </div>
          ))}
          {(!theory.commonMistakes || theory.commonMistakes.length === 0) && (
            <p className="text-xs text-slate-400 italic">{t('admin.grammar.noMistakes')}</p>
          )}
        </div>
      </div>

      {/* 4. Tips */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Lightbulb className="w-5 h-5 text-amber-500" />
            <h3 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.tipsTitle')}
            </h3>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddTip}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{t('admin.grammar.addTip')}</span>
          </Button>
        </div>

        <div className="space-y-2">
          {(theory.tips || []).map((tip, tIdx) => (
            <div key={tIdx} className="flex items-center gap-2">
              <input
                type="text"
                value={tip}
                aria-label={t('admin.grammar.tipItemLabel', { index: tIdx + 1 })}
                onChange={(e) => handleUpdateTip(tIdx, e.target.value)}
                placeholder={t('admin.grammar.tipPlaceholder')}
                className="flex-1 p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white"
              />
              <button
                type="button"
                onClick={() => handleMoveTip(tIdx, 'up')}
                disabled={tIdx === 0}
                aria-label={t('admin.grammar.moveUp')}
                className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronUp className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleMoveTip(tIdx, 'down')}
                disabled={tIdx === (theory.tips?.length || 0) - 1}
                aria-label={t('admin.grammar.moveDown')}
                className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronDown className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleRemoveTip(tIdx)}
                aria-label={t('admin.grammar.removeTip')}
                className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
          {(!theory.tips || theory.tips.length === 0) && (
            <p className="text-xs text-slate-400 italic">{t('admin.grammar.noTips')}</p>
          )}
        </div>
      </div>

      {/* 5. Summary */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <FileText className="w-5 h-5 text-emerald-600" />
            <h3 className="text-sm font-bold text-slate-900">
              {t('admin.grammar.summaryTitle')}
            </h3>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddSummary}
            className="flex items-center gap-1.5"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>{t('admin.grammar.addSummaryItem')}</span>
          </Button>
        </div>

        {getFieldErrors('summary').map((err, idx) => (
          <p key={idx} className="text-xs text-rose-600 font-semibold">{err}</p>
        ))}

        <div className="space-y-2">
          {(theory.summary || []).map((sum, idx) => (
            <div key={idx} className="flex items-center gap-2">
              <span className="text-xs font-bold text-slate-400 w-5 text-right">{idx + 1}.</span>
              <input
                type="text"
                value={sum}
                aria-label={t('admin.grammar.summaryItemLabel', { index: idx + 1 })}
                onChange={(e) => handleUpdateSummary(idx, e.target.value)}
                placeholder={t('admin.grammar.summaryPlaceholder')}
                className="flex-1 p-2 rounded-xl border border-slate-300 text-xs bg-slate-50 focus:bg-white focus:outline-emerald-600"
              />
              <button
                type="button"
                onClick={() => handleMoveSummary(idx, 'up')}
                disabled={idx === 0}
                aria-label={t('admin.grammar.moveUp')}
                className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronUp className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleMoveSummary(idx, 'down')}
                disabled={idx === (theory.summary?.length || 0) - 1}
                aria-label={t('admin.grammar.moveDown')}
                className="p-1 text-slate-400 hover:text-slate-600 disabled:opacity-30 cursor-pointer"
              >
                <ChevronDown className="w-4 h-4" />
              </button>
              <button
                type="button"
                onClick={() => handleRemoveSummary(idx)}
                aria-label={t('admin.grammar.removeSummaryItem')}
                className="p-1 text-slate-400 hover:text-rose-600 cursor-pointer"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
          {(!theory.summary || theory.summary.length === 0) && (
            <p className="text-xs text-slate-400 italic">
              {t('admin.grammar.noSummaryYet')}
            </p>
          )}
        </div>
      </div>
    </div>
  );
};
