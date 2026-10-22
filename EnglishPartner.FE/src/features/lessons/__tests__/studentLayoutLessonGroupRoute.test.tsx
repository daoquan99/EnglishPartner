import React from 'react';
import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Link } from 'react-router-dom';
import { isStudentLessonGroupRoute } from '@/app/layouts/StudentLayout';
import { cn } from '@/shared/lib/cn';

describe('Left Menu "Bài học & Luyện tập" Route Matching', () => {
  describe('isStudentLessonGroupRoute helper function', () => {
    it.each([
      ['/student/lessons'],
      ['/student/lessons/'],
      ['/student/lessons/3fa85f64-5717-4562-b3fc-2c963f66afa6'],
      ['/student/lessons/3fa85f64-5717-4562-b3fc-2c963f66afa6/'],
      ['/student/lesson-attempts/5e6225b4-d213-4981-b895-862f540b1eb1'],
      ['/student/lesson-attempts/5e6225b4-d213-4981-b895-862f540b1eb1/'],
      ['/student/lesson-history'],
      ['/student/lesson-history/'],
    ])('matches lesson group route: %s', (path) => {
      expect(isStudentLessonGroupRoute(path)).toBe(true);
    });

    it.each([
      ['/student'],
      ['/student/'],
      ['/student/tutor'],
      ['/student/vocabulary'],
      ['/student/lessons-other'],
      ['/student/lessons-archived'],
      ['/admin/lessons'],
      ['/admin/lesson-attempts'],
      ['/lessons'],
    ])('does NOT match unrelated or sibling prefix routes: %s', (path) => {
      expect(isStudentLessonGroupRoute(path)).toBe(false);
    });
  });

  describe('Integration active status & aria-current rendering', () => {
    const renderLessonNavLink = (initialPath: string) => {
      const isLessonGroupActive = isStudentLessonGroupRoute(initialPath);

      return renderToStaticMarkup(
        <MemoryRouter initialEntries={[initialPath]}>
          <Link
            to="/student/lessons"
            title="Bài học & Luyện tập"
            aria-current={isLessonGroupActive ? 'page' : undefined}
            className={cn(
              'flex items-center rounded-2xl transition-all',
              isLessonGroupActive
                ? 'bg-emerald-600 text-white shadow-xs'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
            )}
          >
            Bài học & Luyện tập
          </Link>
        </MemoryRouter>
      );
    };

    it.each([
      ['/student/lessons'],
      ['/student/lessons/f1234567-89ab-cdef-0123-456789abcdef'],
      ['/student/lesson-attempts/5e6225b4-d213-4981-b895-862f540b1eb1'],
      ['/student/lesson-history'],
    ])('renders active background and aria-current="page" on %s', (path) => {
      const html = renderLessonNavLink(path);
      expect(html).toContain('aria-current="page"');
      expect(html).toContain('bg-emerald-600 text-white');
    });

    it.each([
      ['/student/tutor'],
      ['/student/vocabulary'],
      ['/admin/lessons'],
    ])('renders inactive styling and no aria-current on %s', (path) => {
      const html = renderLessonNavLink(path);
      expect(html).not.toContain('aria-current="page"');
      expect(html).toContain('text-slate-600');
      expect(html).not.toContain('bg-emerald-600');
    });
  });
});
