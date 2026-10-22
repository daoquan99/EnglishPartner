import { describe, expect, it } from 'vitest';
import { isStudentFallingWordsGroupRoute } from '@/app/layouts/StudentLayout';

describe('Student Left Menu "Từ vựng rơi" Route Matching', () => {
  it.each([
    ['/student/vocabulary-games/falling-words'],
    ['/student/vocabulary-games/falling-words/'],
    ['/student/vocabulary-games/falling-words/sessions/0f024ecc-581e-4290-adec-0f3c53d51ef2'],
    ['/student/vocabulary-games/falling-words/sessions/0f024ecc-581e-4290-adec-0f3c53d51ef2/'],
    ['/student/vocabulary-games/falling-words/history'],
    ['/student/vocabulary-games/falling-words/history/'],
  ])('matches falling words game route: %s', (path) => {
    expect(isStudentFallingWordsGroupRoute(path)).toBe(true);
  });

  it.each([
    ['/student'],
    ['/student/'],
    ['/student/tutor'],
    ['/student/vocabulary'],
    ['/student/lessons'],
    ['/student/vocabulary-other'],
    ['/admin/vocabulary-games/falling-words'],
    ['/vocabulary-games/falling-words'],
  ])('does NOT match unrelated or non-student routes: %s', (path) => {
    expect(isStudentFallingWordsGroupRoute(path)).toBe(false);
  });
});
