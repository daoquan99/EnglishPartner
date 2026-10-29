import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  XCircle,
  Lightbulb,
  AlertCircle,
  ArrowLeft,
  ArrowRight,
  BookOpen,
  Target,
  FileText,
} from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import type { GrammarTheoryDto, GrammarTheoryFormulaDto } from '../types/grammar.types';

interface TheoryRendererProps {
  theory: GrammarTheoryDto;
  prevCode?: string | null;
  nextCode?: string | null;
}

/**
 * Safely renders text with the first occurrence of highlight wrapped in <mark>,
 * without dangerouslySetInnerHTML to prevent XSS.
 */
export function SafeHighlightedText({
  text,
  highlight,
}: {
  text: string;
  highlight?: string;
}) {
  if (!highlight || !highlight.trim()) {
    return <>{text}</>;
  }

  const idx = text.indexOf(highlight);
  if (idx === -1) {
    return <>{text}</>;
  }

  const before = text.slice(0, idx);
  const matched = text.slice(idx, idx + highlight.length);
  const after = text.slice(idx + highlight.length);

  return (
    <>
      {before}
      <mark className="bg-amber-100 text-amber-900 font-semibold px-1 py-0.5 rounded">
        {matched}
      </mark>
      {after}
    </>
  );
}

export const TheoryRenderer: React.FC<TheoryRendererProps> = ({
  theory,
  prevCode,
  nextCode,
}) => {
  const navigate = useNavigate();
  const { t } = useTranslation();

  return (
    <div className="space-y-8 max-w-4xl mx-auto">
      {/* 1. Objectives */}
      {theory.objectives && theory.objectives.length > 0 && (
        <section className="bg-emerald-50/70 border border-emerald-200/80 rounded-2xl p-5 shadow-xs">
          <div className="flex items-center gap-2 mb-3">
            <Target className="w-5 h-5 text-emerald-700 shrink-0" />
            <h3 className="text-sm font-bold text-emerald-950 uppercase tracking-wider">
              {t('grammar.objectives')}
            </h3>
          </div>
          <ul className="space-y-2">
            {theory.objectives.map((obj, i) => (
              <li key={i} className="flex items-start gap-2 text-sm text-emerald-900">
                <span className="text-emerald-600 font-bold mt-0.5">•</span>
                <span>{obj}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* 2. Main Sections */}
      {theory.sections && theory.sections.length > 0 && (
        <div className="space-y-6">
          {theory.sections.map((section, sIdx) => (
            <section
              key={sIdx}
              className="bg-white border border-slate-200/90 rounded-2xl p-5 sm:p-6 shadow-xs space-y-4"
            >
              <h3 className="text-base sm:text-lg font-bold text-slate-900 flex items-center gap-2">
                <span className="flex items-center justify-center w-6 h-6 rounded-full bg-emerald-100 text-emerald-800 text-xs font-bold">
                  {sIdx + 1}
                </span>
                {section.heading}
              </h3>

              <p className="text-sm sm:text-base text-slate-700 whitespace-pre-line leading-relaxed">
                {section.explanationVi}
              </p>

              {/* Formulas table in a dedicated horizontally scrollable wrapper */}
              {section.formulas && section.formulas.length > 0 && (
                <div className="space-y-2 pt-2">
                  <div className="text-xs font-bold uppercase tracking-wider text-slate-500">
                    {t('grammar.formulas')}
                  </div>
                  <div className="overflow-x-auto rounded-xl border border-slate-200 bg-slate-50/70 p-1">
                    <table className="min-w-full text-left text-sm">
                      <thead className="border-b border-slate-200 text-xs font-semibold text-slate-600">
                        <tr>
                          <th className="px-4 py-2">{t('grammar.formLabel')}</th>
                          <th className="px-4 py-2">{t('grammar.grammarStructure')}</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-200/80">
                        {section.formulas.map((formula, fIdx) => {
                          const isObj = typeof formula === 'object' && formula !== null;
                          const label = isObj
                            ? (formula as GrammarTheoryFormulaDto).label || t('grammar.structureIndex', { index: fIdx + 1 })
                            : t('grammar.structureIndex', { index: fIdx + 1 });
                          const pattern = isObj
                            ? (formula as GrammarTheoryFormulaDto).pattern
                            : String(formula);

                          return (
                            <tr key={fIdx} className="hover:bg-slate-100/60 transition-colors">
                              <td className="px-4 py-2.5 font-medium text-slate-700 text-xs sm:text-sm whitespace-nowrap">
                                {label}
                              </td>
                              <td className="px-4 py-2.5 font-mono text-xs sm:text-sm text-emerald-800 font-semibold whitespace-nowrap">
                                {pattern}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Examples */}
              {section.examples && section.examples.length > 0 && (
                <div className="space-y-2 pt-2">
                  <div className="text-xs font-bold uppercase tracking-wider text-slate-500">
                    {t('grammar.examples')}
                  </div>
                  <div className="space-y-2">
                    {section.examples.map((ex, eIdx) => (
                      <div
                        key={eIdx}
                        className="p-3 rounded-xl bg-slate-50 border border-slate-100 space-y-1"
                      >
                        <div className="text-sm font-semibold text-slate-900">
                          <SafeHighlightedText text={ex.en} highlight={ex.highlight} />
                        </div>
                        <div className="text-xs text-slate-600">
                          {ex.vi}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Notes */}
              {section.notes && section.notes.length > 0 && (
                <div className="p-3.5 rounded-xl bg-amber-50/70 border border-amber-200/80 space-y-1 text-xs sm:text-sm text-amber-900">
                  <div className="font-bold flex items-center gap-1.5 text-amber-800">
                    <AlertCircle className="w-4 h-4" />
                    {t('grammar.importantNotes')}
                  </div>
                  <ul className="list-disc list-inside space-y-1 pl-1">
                    {section.notes.map((note, nIdx) => (
                      <li key={nIdx}>{note}</li>
                    ))}
                  </ul>
                </div>
              )}
            </section>
          ))}
        </div>
      )}

      {/* 3. Common Mistakes */}
      {theory.commonMistakes && theory.commonMistakes.length > 0 && (
        <section className="bg-rose-50/50 border border-rose-200/80 rounded-2xl p-5 sm:p-6 shadow-xs space-y-4">
          <div className="flex items-center gap-2">
            <XCircle className="w-5 h-5 text-rose-600 shrink-0" />
            <h3 className="text-sm font-bold text-rose-950 uppercase tracking-wider">
              {t('grammar.commonMistakes')}
            </h3>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {theory.commonMistakes.map((mistake, mIdx) => (
              <div
                key={mIdx}
                className="p-3.5 rounded-xl bg-white border border-rose-100 shadow-2xs space-y-2"
              >
                <div className="flex items-start gap-2 text-rose-700 text-xs sm:text-sm font-mono line-through">
                  <span className="font-bold text-rose-600">❌</span>
                  <span>{mistake.wrong}</span>
                </div>
                <div className="flex items-start gap-2 text-emerald-700 text-xs sm:text-sm font-mono font-semibold">
                  <span className="font-bold text-emerald-600">✅</span>
                  <span>{mistake.right}</span>
                </div>
                <div className="text-xs text-slate-600 pt-1 border-t border-slate-100">
                  {mistake.explanationVi}
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* 4. Tips */}
      {theory.tips && theory.tips.length > 0 && (
        <section className="bg-indigo-50/60 border border-indigo-200/80 rounded-2xl p-5 shadow-xs space-y-3">
          <div className="flex items-center gap-2">
            <Lightbulb className="w-5 h-5 text-indigo-600 shrink-0" />
            <h3 className="text-sm font-bold text-indigo-950 uppercase tracking-wider">
              {t('grammar.tips')}
            </h3>
          </div>
          <ul className="space-y-2">
            {theory.tips.map((tip, tIdx) => (
              <li key={tIdx} className="flex items-start gap-2 text-sm text-indigo-900">
                <span className="text-indigo-500 font-bold mt-0.5">•</span>
                <span>{tip}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* 5. Summary */}
      {theory.summary && theory.summary.length > 0 && (
        <section className="bg-slate-50 border border-slate-200 rounded-2xl p-5 shadow-xs space-y-3">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />
            <h3 className="text-sm font-bold text-slate-900 uppercase tracking-wider">
              {t('grammar.summary')}
            </h3>
          </div>
          <ul className="space-y-1.5">
            {theory.summary.map((sum, sIdx) => (
              <li key={sIdx} className="flex items-start gap-2 text-sm text-slate-700">
                <span className="text-emerald-600 font-bold mt-0.5">✔</span>
                <span>{sum}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* Navigation Buttons: Prev / Next Step */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-6 border-t border-slate-200">
        {prevCode ? (
          <Button
            variant="outline"
            onClick={() => navigate(`/student/grammar/${prevCode}`)}
            className="flex items-center gap-2 text-xs sm:text-sm"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('grammar.prevStepWithCode', { code: prevCode })}
          </Button>
        ) : (
          <div />
        )}

        {nextCode ? (
          <Button
            variant="primary"
            onClick={() => navigate(`/student/grammar/${nextCode}`)}
            className="flex items-center gap-2 text-xs sm:text-sm bg-emerald-600 hover:bg-emerald-700 text-white"
          >
            {t('grammar.nextStepWithCode', { code: nextCode })}
            <ArrowRight className="w-4 h-4" />
          </Button>
        ) : (
          <div />
        )}
      </div>
    </div>
  );
};
