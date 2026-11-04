import React from 'react';
import { describe, it, expect } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import '@/shared/i18n';
import { ActivityChart } from '../components/ActivityChart';
import type { ProgressDailyActivityDto } from '@/shared/api/openapi.types';

const generateMockActivity = (count: number): ProgressDailyActivityDto[] => {
  const result: ProgressDailyActivityDto[] = [];
  const baseDate = new Date('2026-11-02');
  for (let i = count - 1; i >= 0; i--) {
    const d = new Date(baseDate);
    d.setDate(d.getDate() - i);
    const dateStr = d.toISOString().split('T')[0];
    result.push({
      date: dateStr,
      vocabularyReviews: (i % 3) + 1,
      lessonAttempts: i % 2,
      grammarAttempts: i % 4,
      pronunciationAssessments: i % 3,
      tutorSessions: i === 0 ? 1 : 0,
    });
  }
  return result;
};

describe('ActivityChart Component', () => {
  it('renders SVG with role="img" and aria-label', () => {
    const activity7 = generateMockActivity(7);
    const html = renderToStaticMarkup(<ActivityChart activity={activity7} days={7} />);

    expect(html).toContain('role="img"');
    expect(html).toContain('aria-label=');
    expect(html).toContain('Biểu đồ hoạt động');
  });

  it('renders the exact number of activity columns matching days', () => {
    const activity7 = generateMockActivity(7);
    const html7 = renderToStaticMarkup(<ActivityChart activity={activity7} days={7} />);
    const colMatches7 = html7.match(/data-testid="activity-column"/g);
    expect(colMatches7).toHaveLength(7);

    const activity30 = generateMockActivity(30);
    const html30 = renderToStaticMarkup(<ActivityChart activity={activity30} days={30} />);
    const colMatches30 = html30.match(/data-testid="activity-column"/g);
    expect(colMatches30).toHaveLength(30);

    const activity90 = generateMockActivity(90);
    const html90 = renderToStaticMarkup(<ActivityChart activity={activity90} days={90} />);
    const colMatches90 = html90.match(/data-testid="activity-column"/g);
    expect(colMatches90).toHaveLength(90);
  });

  it('renders a hidden table with class sr-only for screen readers', () => {
    const activity7 = generateMockActivity(7);
    const html = renderToStaticMarkup(<ActivityChart activity={activity7} days={7} />);

    expect(html).toContain('class="sr-only"');
    expect(html).toContain('<table>');
    expect(html).toContain('<caption>');
    expect(html).toContain('<th scope="col">Ngày</th>');
    expect(html).toContain('<th scope="col">Từ vựng</th>');
    expect(html).toContain('<th scope="col">Bài học</th>');
    expect(html).toContain('<th scope="col">Ngữ pháp</th>');
    expect(html).toContain('<th scope="col">Phát âm</th>');
    expect(html).toContain('<th scope="col">Live Tutor</th>');

    for (const item of activity7) {
      expect(html).toContain(`<td>${item.date}</td>`);
    }
  });

  it('renders tooltip title in each column group', () => {
    const activity = [
      {
        date: '2026-11-02',
        vocabularyReviews: 5,
        lessonAttempts: 2,
        grammarAttempts: 3,
        pronunciationAssessments: 4,
        tutorSessions: 1,
      },
    ];
    const html = renderToStaticMarkup(<ActivityChart activity={activity} days={1} />);

    expect(html).toContain('<title>');
    expect(html).toContain('2026-11-02');
    expect(html).toContain('Tổng cộng: 15');
    expect(html).toContain('Từ vựng: 5');
    expect(html).toContain('Bài học: 2');
    expect(html).toContain('Ngữ pháp: 3');
    expect(html).toContain('Phát âm: 4');
    expect(html).toContain('Live Tutor: 1');
  });

  it('includes responsive viewBox and preserveAspectRatio', () => {
    const activity7 = generateMockActivity(7);
    const html = renderToStaticMarkup(<ActivityChart activity={activity7} days={7} />);

    expect(html).toContain('viewBox="0 0 900 280"');
    expect(html).toContain('preserveAspectRatio="xMidYMid meet"');
  });
});
