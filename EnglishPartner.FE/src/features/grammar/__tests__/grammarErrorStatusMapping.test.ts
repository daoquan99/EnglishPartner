import { describe, it, expect } from 'vitest';
import { getGrammarErrorMessage, getAdminGrammarErrorMessage, groupIssuesByField } from '../utils/grammarHelpers';

const t = (key: string) => key;
const axiosError = (status: number, detail?: string, errors?: Record<string, string[]>) =>
  Object.assign(new Error('Request failed with status code ' + status), {
    name: 'AxiosError',
    code: 'ERR_BAD_REQUEST',
    response: { status, data: { detail, errors } },
  });

describe('grammar error mapping by status', () => {
  it('keeps learner lock messages when BE returns 403 with a grammar code', () => {
    expect(getGrammarErrorMessage(axiosError(403, 'grammar_step_locked'), t)).toBe('grammar.errors.stepLocked');
    expect(getGrammarErrorMessage(axiosError(403, 'grammar_level_test_locked'), t)).toBe('grammar.errors.levelTestLocked');
  });

  it('falls back to status only for unknown codes', () => {
    expect(getGrammarErrorMessage(axiosError(403, 'Bạn không có quyền'), t)).toBe('common.forbidden');
    expect(getGrammarErrorMessage(axiosError(404, 'Step không tồn tại'), t)).toBe('grammar.errors.generic');
    expect(getAdminGrammarErrorMessage(axiosError(404, 'Step không tồn tại'), t)).toBe('admin.grammar.stepNotFoundTitle');
  });

  it('does not treat axios error properties as field issues', () => {
    const err = axiosError(409, 'conflict');
    const grouped = groupIssuesByField((err as any)?.response?.data?.errors ?? {}, ['content', 'grading']);
    expect(grouped.otherIssues).toEqual([]);
    expect(Object.values(grouped.fieldIssues).every((a: any) => a.length === 0)).toBe(true);
  });
});

import { validateTheoryJson } from '../utils/grammarHelpers';

describe('validateTheoryJson nested shapes', () => {
  it('rejects non-array formulas/examples inside a section', () => {
    expect(validateTheoryJson(JSON.stringify({ sections: [{ heading: 'h', explanationVi: 'e', formulas: 'x' }] })).ok).toBe(false);
    expect(validateTheoryJson(JSON.stringify({ sections: [{ heading: 'h', explanationVi: 'e', examples: [null] }] })).ok).toBe(false);
  });
  it('accepts a well-formed section', () => {
    expect(validateTheoryJson(JSON.stringify({ sections: [{ heading: 'h', explanationVi: 'e', formulas: [{ label: 'a', pattern: 'b' }], examples: [{ en: 'I am', vi: 'Tôi là' }] }] })).ok).toBe(true);
  });
});

import { getAnswerIndicatorTone } from '../utils/grammarHelpers';

describe('getAnswerIndicatorTone', () => {
  it('colours graded answers by score and keeps ungraded answers neutral', () => {
    expect(getAnswerIndicatorTone(undefined)).toBe('unanswered');
    expect(getAnswerIndicatorTone({ status: 'Completed', score100: 100 })).toBe('correct');
    expect(getAnswerIndicatorTone({ status: 'Completed', score100: 60 })).toBe('partial');
    expect(getAnswerIndicatorTone({ status: 'Completed', score100: 0 })).toBe('wrong');
    expect(getAnswerIndicatorTone({ status: 'SelfAssessed', score100: null })).toBe('ungraded');
    expect(getAnswerIndicatorTone({ status: 'PendingGrading' })).toBe('ungraded');
    expect(getAnswerIndicatorTone({ status: 'FailedGrading', score100: null })).toBe('failed');
  });
});
