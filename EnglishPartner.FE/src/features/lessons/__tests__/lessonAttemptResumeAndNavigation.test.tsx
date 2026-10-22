import React from 'react';
import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import {
  resolveExerciseResult,
  getExerciseNavigationStatus,
  extractComprehensionAnswers,
  getDeterministicScore,
} from '../components/player/lessonAttemptResolver';
import { ExerciseRenderer } from '../components/player/ExerciseRenderer';
import {
  ExerciseType,
  type ExerciseAnswerResultDto,
  type ExerciseLearnerDto,
  type GradingMode,
  type SubmittedAnswerSummaryDto,
} from '../types/lessons.types';

describe('Lesson Attempt Resume & Navigation State Resolution', () => {
  const makeExercise = (
    id: string,
    type: ExerciseType,
    gradingMode: any = 'Deterministic',
    content: Record<string, unknown> = {}
  ): ExerciseLearnerDto => ({
    id,
    key: `ex-${id}`,
    type,
    title: `Exercise ${id}`,
    instructions: `Instructions for ${id}`,
    order: 1,
    maxPoints: 10,
    gradingMode: gradingMode as GradingMode,
    content,
  });

  const makeSummary = (
    overrides: Partial<SubmittedAnswerSummaryDto> = {}
  ): SubmittedAnswerSummaryDto => ({
    answerId: 'ans-1',
    status: 'Completed',
    score100: 100,
    pointsEarned: 10,
    maxPoints: 10,
    feedback: 'Tốt lắm',
    ...overrides,
  });

  const makeResult = (
    overrides: Partial<ExerciseAnswerResultDto> = {}
  ): ExerciseAnswerResultDto => ({
    answerId: 'ans-1',
    attemptId: 'att-1',
    attemptRevision: 1,
    exerciseId: 'ex-1',
    subQuestionId: null,
    status: 'Completed',
    gradingMode: 'Deterministic',
    score100: 100,
    pointsEarned: 10,
    maxPoints: 10,
    rubricCode: null,
    rubricVersion: null,
    criteriaBreakdown: null,
    strengths: null,
    improvements: null,
    suggestedAnswer: null,
    attemptProgress: {
      completedExercisesCount: 1,
      totalExercisesCount: 5,
      pendingGradingCount: 0,
      failedGradingCount: 0,
      currentSubtotalPoints: 10,
      totalMaxPoints: 50,
      finalScore100: 20,
    },
    ...overrides,
  });

  describe('resolveExerciseResult lookup and reconciliation', () => {
    const ex = makeExercise('ex-1', ExerciseType.MultipleChoice);

    it('resolves by root exerciseId from submittedMap', () => {
      const summary = makeSummary({ answerId: 'ans-10' });
      const res = resolveExerciseResult(ex, {}, { 'ex-1': summary });
      expect(res).not.toBeNull();
      expect(res?.answerId).toBe('ans-10');
    });

    it('resolves by compound exerciseId_subQuestionId key when stored with subquestion', () => {
      const summary = makeSummary({ answerId: 'ans-sub-1' });
      const res = resolveExerciseResult(ex, {}, { 'ex-1_sub-q-1': summary });
      expect(res).not.toBeNull();
      expect(res?.answerId).toBe('ans-sub-1');
    });

    it('resolves by exerciseId property inside summary DTO', () => {
      const summary = makeSummary({ answerId: 'ans-prop-1', exerciseId: 'ex-1' });
      const res = resolveExerciseResult(ex, {}, { 'custom-key': summary });
      expect(res).not.toBeNull();
      expect(res?.answerId).toBe('ans-prop-1');
    });

    it('merges missing answerPayload and feedback into immediate submit result when matching same answerId', () => {
      const immediate = makeResult({
        answerId: 'ans-1',
        answerPayload: undefined,
        feedback: undefined,
      });
      const persisted = makeSummary({
        answerId: 'ans-1',
        answerPayload: { selectedOptionIds: ['opt-2'] },
        feedback: 'Persisted explanation',
      });

      const merged = resolveExerciseResult(ex, { 'ex-1': immediate }, { 'ex-1': persisted });
      expect((merged as any)?.answerPayload).toEqual({ selectedOptionIds: ['opt-2'] });
      expect(merged?.feedback).toBe('Persisted explanation');
    });
  });

  describe('Navigation status indicator & icons', () => {
    const exDeterministic = makeExercise('ex-1', ExerciseType.MultipleChoice, 'Deterministic');
    const exRubric = makeExercise('ex-2', ExerciseType.Translation, 'AiRubric');
    const exFlashcard = makeExercise('ex-3', ExerciseType.Flashcards, 'SelfAssessment');

    it('returns unsubmitted state with number icon when not answered', () => {
      const status = getExerciseNavigationStatus(null, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('unsubmitted');
      expect(status.tone).toBe('neutral');
      expect(status.iconType).toBe('number');
      expect(status.label).toBe('Chưa làm');
    });

    it('returns correct state with green icon for deterministic 100%', () => {
      const res = makeSummary({ score100: 100, pointsEarned: 10, maxPoints: 10 });
      const status = getExerciseNavigationStatus(res, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('correct');
      expect(status.tone).toBe('correct');
      expect(status.iconType).toBe('correct');
      expect(status.label).toBe('Chính xác');
    });

    it('returns incorrect state with red X icon for deterministic 0% (never green checkmark!)', () => {
      const res = makeSummary({ score100: 0, pointsEarned: 0, maxPoints: 10 });
      const status = getExerciseNavigationStatus(res, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('incorrect');
      expect(status.tone).toBe('incorrect');
      expect(status.iconType).toBe('incorrect');
      expect(status.label).toBe('Chưa đúng');
    });

    it('returns partial state with amber icon for deterministic 50%', () => {
      const res = makeSummary({ score100: 50, pointsEarned: 5, maxPoints: 10 });
      const status = getExerciseNavigationStatus(res, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('partial');
      expect(status.tone).toBe('partial');
      expect(status.iconType).toBe('partial');
      expect(status.label).toBe('Đúng một phần');
    });

    it('returns grading clock state for Pending or Grading status', () => {
      const res = makeSummary({ status: 'Grading', score100: null });
      const status = getExerciseNavigationStatus(res, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('grading');
      expect(status.tone).toBe('grading');
      expect(status.iconType).toBe('clock');
    });

    it('returns failed alert state for Failed or FailedGrading', () => {
      const res = makeSummary({ status: 'Failed', score100: null, errorMessage: 'Timeout' });
      const status = getExerciseNavigationStatus(res, exDeterministic.type, exDeterministic.gradingMode);
      expect(status.state).toBe('failed');
      expect(status.tone).toBe('failed');
      expect(status.iconType).toBe('alert');
    });

    it('treats Flashcard self-assessment as neutral check, not binary correct/incorrect', () => {
      const res = makeSummary({ score100: 100, pointsEarned: 0, maxPoints: 0 });
      const status = getExerciseNavigationStatus(res, exFlashcard.type, exFlashcard.gradingMode);
      expect(status.state).toBe('self_assessed');
      expect(status.tone).toBe('neutral');
      expect(status.iconType).toBe('check');
    });

    it('treats AI Rubric grading as neutral sparkles, not binary correct/incorrect', () => {
      const res = makeSummary({ score100: 60, pointsEarned: 6, maxPoints: 10 });
      const status = getExerciseNavigationStatus(res, exRubric.type, exRubric.gradingMode);
      expect(status.state).toBe('ai_evaluated');
      expect(status.tone).toBe('neutral');
      expect(status.iconType).toBe('sparkles');
    });
  });

  describe('Hydration of submitted answers after reload/navigation', () => {
    it('restores Reading/Listening comprehension answers including Single, Multiple, TrueFalse and ShortAnswer', () => {
      const subQuestions = [
        { id: 'q-1', kind: 'SingleChoice', options: [{ id: 'opt-a', text: 'A' }, { id: 'opt-b', text: 'B' }] },
        { id: 'q-2', kind: 'MultipleChoice', options: [{ id: 'opt-c', text: 'C' }, { id: 'opt-d', text: 'D' }] },
        { id: 'q-3', kind: 'TrueFalse' },
        { id: 'q-4', kind: 'ShortAnswer' },
      ];

      const rawPayload = {
        subQuestionAnswers: [
          { questionId: 'q-1', selectedOptionIds: ['opt-b'] },
          { questionId: 'q-2', selectedOptionIds: ['opt-c', 'opt-d'] },
          { questionId: 'q-3', answer: false },
          { questionId: 'q-4', textAnswer: 'Paris' },
        ],
      };

      const hydrated = extractComprehensionAnswers(rawPayload, subQuestions);
      expect(hydrated['q-1']).toBe('opt-b');
      expect(hydrated['q-2']).toEqual(['opt-c', 'opt-d']);
      expect(hydrated['q-3']).toBe(false); // boolean false must be preserved!
      expect(hydrated['q-4']).toBe('Paris');

      // Now verify rendering with hydrated payload
      const exReading = makeExercise('read-1', ExerciseType.ReadingComprehension, 'Composite', {
        passage: 'Sample reading text',
        subQuestions,
      });

      const summary = makeSummary({
        answerPayload: rawPayload,
      });

      const html = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exReading}
          onSubmit={() => {}}
          result={summary}
        />
      );

      expect(html).toContain('Sample reading text');
      expect(html).toContain('Paris');
    });

    it('restores Flashcards ratings on mount/reload', () => {
      const exCards = makeExercise('cards-1', ExerciseType.Flashcards, 'SelfAssessment', {
        cards: [
          { id: 'c-1', term: 'Hello', meaningVi: 'Xin chào' },
          { id: 'c-2', term: 'Goodbye', meaningVi: 'Tạm biệt' },
        ],
      });

      const summary = makeSummary({
        answerPayload: { ratings: { 'c-1': 4, 'c-2': 3 } },
      });

      const html = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exCards}
          onSubmit={() => {}}
          result={summary}
        />
      );

      expect(html).toContain('Hello');
      expect(html).toContain('Đã đánh giá: 2/2');
      // Level 4 button is selected (has emerald background)
      expect(html).toContain('bg-emerald-600 text-white');
    });

    it('restores TrueFalse boolean false answer without losing it to falsiness', () => {
      const exTF = makeExercise('tf-1', ExerciseType.TrueFalse, 'Deterministic', {
        statements: [
          { id: 's-1', text: 'The sky is green.' },
        ],
      });

      const summary = makeSummary({
        score100: 100,
        answerPayload: { answers: { 's-1': false } },
      });

      const html = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exTF}
          onSubmit={() => {}}
          result={summary}
        />
      );

      expect(html).toContain('The sky is green.');
      expect(html).toContain('bg-rose-600 text-white shadow-2xs');
    });

    it('restores Shadowing and Speaking Practice audio playback from saved audioAssetId', () => {
      const exShadow = makeExercise('sh-1', ExerciseType.Shadowing, 'AiRubric', {
        referenceText: 'Good morning ladies and gentlemen.',
      });

      const summary = makeSummary({
        answerPayload: { audioAssetId: 'asset-abc-123' },
      });

      const html = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exShadow}
          onSubmit={() => {}}
          result={summary}
        />
      );

      expect(html).toContain('Good morning ladies and gentlemen.');
      // Restores audio element with server stream URL and playback button
      expect(html).toContain('/api/lesson-media/asset-abc-123/stream');
      expect(html).toContain('Nghe lại');
    });

    it('restores Conversation Role Play dialogue turns from saved session', () => {
      const exRolePlay = makeExercise('rp-1', ExerciseType.ConversationRolePlay, 'AiRubric', {
        scenario: 'Ordering coffee at a cafe',
        aiRole: 'Barista',
      });

      const summary = makeSummary({
        answerPayload: {
          turns: [
            { role: 'learner', text: 'Can I have an iced latte please?' },
            { role: 'ai', text: 'Sure! What size would you like?' },
          ],
        },
      });

      const html = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exRolePlay}
          onSubmit={() => {}}
          result={summary}
        />
      );

      expect(html).toContain('Ordering coffee at a cafe');
      expect(html).toContain('Can I have an iced latte please?');
      expect(html).toContain('Sure! What size would you like?');
    });
  });

  describe('Workflow: Submit → Chuyển câu → Reload → Quay lại & kiểm tra icon đúng/sai', () => {
    const ex1 = makeExercise('ex-1', ExerciseType.MultipleChoice, 'Deterministic', {
      question: 'Capital of France?',
      options: [
        { id: 'opt-a', text: 'London' },
        { id: 'opt-b', text: 'Paris' },
      ],
    });

    const ex2 = makeExercise('ex-2', ExerciseType.SpellingPractice, 'Deterministic', {
      prompt: 'Word meaning schedule',
    });

    const exercises = [ex1, ex2];

    it('preserves exact correctness icons and inputs across submit, navigate, reload and return', () => {
      // Step 1: Submit Exercise 1 with incorrect answer (selected London)
      const ex1SubmitResult: ExerciseAnswerResultDto = makeResult({
        exerciseId: 'ex-1',
        score100: 0,
        pointsEarned: 0,
        maxPoints: 10,
        answerPayload: { selectedOptionIds: ['opt-a'] },
      });

      let localLastResults: Record<string, ExerciseAnswerResultDto> = {
        'ex-1': ex1SubmitResult,
      };

      // Server DB record state (simulates what DB stores)
      const serverSubmittedAnswers: Record<string, SubmittedAnswerSummaryDto> = {
        'ex-1': makeSummary({
          answerId: ex1SubmitResult.answerId,
          score100: 0,
          pointsEarned: 0,
          maxPoints: 10,
          feedback: 'Đáp án đúng là Paris.',
          answerPayload: { selectedOptionIds: ['opt-a'] },
          exerciseId: 'ex-1',
        }),
      };

      // 1.1 Before reload on Ex 1 (active): Verify icon is X đỏ and active
      const resEx1Before = resolveExerciseResult(ex1, localLastResults, {});
      const navEx1Before = getExerciseNavigationStatus(resEx1Before, ex1.type, ex1.gradingMode);
      expect(navEx1Before.state).toBe('incorrect');
      expect(navEx1Before.iconType).toBe('incorrect');
      expect(navEx1Before.tone).toBe('incorrect');

      // Step 2: Navigate / Chuyển sang Exercise 2
      // Submit Exercise 2 with correct answer
      const ex2SubmitResult: ExerciseAnswerResultDto = makeResult({
        answerId: 'ans-2',
        exerciseId: 'ex-2',
        score100: 100,
        pointsEarned: 10,
        maxPoints: 10,
        answerPayload: { spelledWord: 'schedule' },
      });

      localLastResults['ex-2'] = ex2SubmitResult;
      serverSubmittedAnswers['ex-2'] = makeSummary({
        answerId: 'ans-2',
        score100: 100,
        pointsEarned: 10,
        maxPoints: 10,
        feedback: 'Chính xác!',
        answerPayload: { spelledWord: 'schedule' },
        exerciseId: 'ex-2',
      });

      // 2.1 Before reload: Ex 2 (active) has checkmark, Ex 1 (inactive) has X đỏ
      const resEx2Before = resolveExerciseResult(ex2, localLastResults, {});
      const navEx2Before = getExerciseNavigationStatus(resEx2Before, ex2.type, ex2.gradingMode);
      expect(navEx2Before.state).toBe('correct');
      expect(navEx2Before.iconType).toBe('correct');

      // Step 3: RELOAD trang!
      // In a real page reload, localLastResults is reset to {}
      // All data is restored purely from server GET /api/lesson-attempts/:attemptId/resume
      const reloadedLastResults: Record<string, ExerciseAnswerResultDto> = {}; // Clean slate
      const reloadedServerAnswers = { ...serverSubmittedAnswers }; // From resume endpoint

      // 3.1 After reload: Check icons for both exercises
      const resEx1AfterReload = resolveExerciseResult(ex1, reloadedLastResults, reloadedServerAnswers);
      const navEx1AfterReload = getExerciseNavigationStatus(resEx1AfterReload, ex1.type, ex1.gradingMode);
      expect(navEx1AfterReload.state).toBe('incorrect');
      expect(navEx1AfterReload.iconType).toBe('incorrect');
      expect(navEx1AfterReload.tone).toBe('incorrect');

      const resEx2AfterReload = resolveExerciseResult(ex2, reloadedLastResults, reloadedServerAnswers);
      const navEx2AfterReload = getExerciseNavigationStatus(resEx2AfterReload, ex2.type, ex2.gradingMode);
      expect(navEx2AfterReload.state).toBe('correct');
      expect(navEx2AfterReload.iconType).toBe('correct');
      expect(navEx2AfterReload.tone).toBe('correct');

      // Step 4: Quay lại câu 1 (Ex 1) sau reload
      // Render Ex 1 from server resume data: verify learner answer 'opt-a' is preserved!
      const htmlEx1AfterReload = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={ex1}
          onSubmit={() => {}}
          result={resEx1AfterReload}
        />
      );

      expect(htmlEx1AfterReload).toContain('Capital of France?');
      // Opt-a was selected
      expect(htmlEx1AfterReload).toContain('border-emerald-500');
      // Feedback is preserved
      expect(htmlEx1AfterReload).toContain('Đáp án đúng là Paris.');
      expect(htmlEx1AfterReload).toContain('Chưa đúng');

      // Render Ex 2 from server resume data: verify spelledWord 'schedule' is preserved!
      const htmlEx2AfterReload = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={ex2}
          onSubmit={() => {}}
          result={resEx2AfterReload}
        />
      );

      expect(htmlEx2AfterReload).toContain('schedule');
      expect(htmlEx2AfterReload).toContain('Chính xác');
    });
  });

  describe('Redo Mode in ExerciseRenderer', () => {
    const exMcq = makeExercise('ex-mcq', ExerciseType.MultipleChoice, 'Deterministic', {
      question: 'Which is a primary color?',
      options: [
        { id: 'opt-red', text: 'Red' },
        { id: 'opt-green', text: 'Green' },
      ],
    });

    const savedResult = makeSummary({
      answerId: 'ans-mcq',
      score100: 100,
      pointsEarned: 10,
      maxPoints: 10,
      answerPayload: { selectedOptionIds: ['opt-red'] },
    });

    it('unlocks the editor and hides previous selections when isRedoMode is true', () => {
      // Normal submitted state
      const normalHtml = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exMcq}
          onSubmit={() => {}}
          result={savedResult}
          isRedoMode={false}
        />
      );
      // Saved selection is marked active
      expect(normalHtml).toContain('border-emerald-500');
      // Submit button is hidden because isSubmitted is true
      expect(normalHtml).not.toContain('Xác nhận nộp đáp án');
      expect(normalHtml).not.toContain('Chế độ làm lại câu hỏi');

      // Redo mode active
      const redoHtml = renderToStaticMarkup(
        <ExerciseRenderer
          exercise={exMcq}
          onSubmit={() => {}}
          result={savedResult}
          isRedoMode={true}
          onCancelRedo={() => {}}
        />
      );
      // Redo banner is shown
      expect(redoHtml).toContain('Chế độ làm lại câu hỏi');
      expect(redoHtml).toContain('Hủy làm lại');
      // Submit button is available for new submission!
      expect(redoHtml).toContain('Xác nhận nộp đáp án');
      // Selected option is NOT pre-selected (starts clean)
      expect(redoHtml).not.toContain('border-emerald-500');
    });
  });

  describe('Result Filtering and Counts', () => {
    const ex1 = makeExercise('ex-1', ExerciseType.MultipleChoice);
    const ex2 = makeExercise('ex-2', ExerciseType.SpellingPractice);
    const ex3 = makeExercise('ex-3', ExerciseType.FillInTheBlanks);
    const ex4 = makeExercise('ex-4', ExerciseType.Translation, 'AiRubric');
    const ex5 = makeExercise('ex-5', ExerciseType.TrueFalse);

    const exercises = [ex1, ex2, ex3, ex4, ex5];

    const submittedAnswers: Record<string, SubmittedAnswerSummaryDto> = {
      'ex-1': makeSummary({ answerId: 'a1', score100: 100, pointsEarned: 10, maxPoints: 10 }), // correct
      'ex-2': makeSummary({ answerId: 'a2', score100: 0, pointsEarned: 0, maxPoints: 10 }), // incorrect
      'ex-3': makeSummary({ answerId: 'a3', score100: 50, pointsEarned: 5, maxPoints: 10 }), // partial
      'ex-4': makeSummary({ answerId: 'a4', score100: 80, pointsEarned: 8, maxPoints: 10 }), // ai_evaluated
      // ex-5 is unsubmitted
    };

    it('computes status counts correctly across exercise list', () => {
      const counts = { all: exercises.length, correct: 0, incorrect: 0, partial: 0, unsubmitted: 0 };
      for (const ex of exercises) {
        const res = resolveExerciseResult(ex, {}, submittedAnswers);
        const nav = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
        if (nav.state === 'correct') counts.correct++;
        else if (nav.state === 'incorrect') counts.incorrect++;
        else if (nav.state === 'partial') counts.partial++;
        else if (nav.state === 'unsubmitted') counts.unsubmitted++;
      }

      expect(counts.all).toBe(5);
      expect(counts.correct).toBe(1); // ex-1
      expect(counts.incorrect).toBe(1); // ex-2
      expect(counts.partial).toBe(1); // ex-3
      expect(counts.unsubmitted).toBe(1); // ex-5
      // Note: ex-4 is ai_evaluated, so it is NOT counted in correct/incorrect/partial/unsubmitted!
    });

    it('filters exercises by status matching the AND condition', () => {
      const filterBy = (status: 'all' | 'correct' | 'incorrect' | 'partial' | 'unsubmitted') => {
        return exercises.filter((ex) => {
          if (status === 'all') return true;
          const res = resolveExerciseResult(ex, {}, submittedAnswers);
          const nav = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
          return nav.state === status;
        });
      };

      expect(filterBy('all').map((e) => e.id)).toEqual(['ex-1', 'ex-2', 'ex-3', 'ex-4', 'ex-5']);
      expect(filterBy('correct').map((e) => e.id)).toEqual(['ex-1']);
      expect(filterBy('incorrect').map((e) => e.id)).toEqual(['ex-2']);
      expect(filterBy('partial').map((e) => e.id)).toEqual(['ex-3']);
      expect(filterBy('unsubmitted').map((e) => e.id)).toEqual(['ex-5']);
    });

    it('updates visible exercises after redo resubmission from correct to incorrect', () => {
      // Simulate ex-1 resubmitted with incorrect answer
      const updatedSubmitted = {
        ...submittedAnswers,
        'ex-1': makeSummary({ answerId: 'a1-v2', score100: 0, pointsEarned: 0, maxPoints: 10 }),
      };

      const correctList = exercises.filter((ex) => {
        const res = resolveExerciseResult(ex, {}, updatedSubmitted);
        const nav = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
        return nav.state === 'correct';
      });

      // ex-1 is no longer in correct list!
      expect(correctList.map((e) => e.id)).toEqual([]);

      const incorrectList = exercises.filter((ex) => {
        const res = resolveExerciseResult(ex, {}, updatedSubmitted);
        const nav = getExerciseNavigationStatus(res, ex.type, ex.gradingMode, ex.content);
        return nav.state === 'incorrect';
      });

      // ex-1 is now in incorrect list along with ex-2!
      expect(incorrectList.map((e) => e.id)).toEqual(['ex-1', 'ex-2']);
    });
  });
});
