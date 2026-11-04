import React from 'react';
import { useTranslation } from 'react-i18next';
import type { ProgressDailyActivityDto } from '@/shared/api/openapi.types';

export interface ActivityChartProps {
  activity: ProgressDailyActivityDto[];
  days: number;
}

interface ActivityCategory {
  key: keyof Omit<ProgressDailyActivityDto, 'date'>;
  labelKey: string;
  color: string;
}

const CATEGORIES: ActivityCategory[] = [
  { key: 'vocabularyReviews', labelKey: 'progress.vocabulary', color: '#10b981' },
  { key: 'lessonAttempts', labelKey: 'progress.lessons', color: '#6366f1' },
  { key: 'grammarAttempts', labelKey: 'progress.grammar', color: '#f59e0b' },
  { key: 'pronunciationAssessments', labelKey: 'progress.pronunciation', color: '#f43f5e' },
  { key: 'tutorSessions', labelKey: 'progress.liveTutor', color: '#8b5cf6' },
];

export const ActivityChart: React.FC<ActivityChartProps> = ({ activity, days }) => {
  const { t } = useTranslation();

  const totalActivity = activity.reduce(
    (sum, d) =>
      sum +
      d.vocabularyReviews +
      d.lessonAttempts +
      d.grammarAttempts +
      d.pronunciationAssessments +
      d.tutorSessions,
    0
  );

  // SVG Chart Geometry
  const svgWidth = 900;
  const svgHeight = 280;
  const padding = { top: 25, right: 25, bottom: 45, left: 45 };
  const chartWidth = svgWidth - padding.left - padding.right;
  const chartHeight = svgHeight - padding.top - padding.bottom;
  const zeroY = padding.top + chartHeight;

  // Max daily total for Y-axis scale
  const maxDayTotal = Math.max(
    1,
    ...activity.map(
      (d) =>
        d.vocabularyReviews +
        d.lessonAttempts +
        d.grammarAttempts +
        d.pronunciationAssessments +
        d.tutorSessions
    )
  );

  // Round up tickMax to a multiple of 4
  let tickMax = Math.ceil(maxDayTotal);
  if (tickMax < 4) {
    tickMax = 4;
  } else if (tickMax % 4 !== 0) {
    tickMax += 4 - (tickMax % 4);
  }

  const yTicks = [0, tickMax * 0.25, tickMax * 0.5, tickMax * 0.75, tickMax];

  const numDays = activity.length || 1;
  const slotWidth = chartWidth / numDays;
  const barWidth =
    numDays <= 7
      ? Math.min(48, slotWidth * 0.55)
      : numDays <= 30
      ? Math.min(20, slotWidth * 0.7)
      : Math.max(3, slotWidth * 0.75);

  // Step for showing date labels on X axis
  const labelInterval = numDays <= 7 ? 1 : numDays <= 30 ? 5 : 15;

  const formatDateLabel = (dateStr: string) => {
    // "yyyy-MM-dd" -> "dd/MM"
    const parts = dateStr.split('-');
    if (parts.length === 3) {
      return `${parts[2]}/${parts[1]}`;
    }
    return dateStr;
  };

  return (
    <div className="p-4 sm:p-6 rounded-3xl bg-white border border-slate-200/90 shadow-2xs space-y-4">
      {/* Chart Header + Legend */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h3 className="text-base font-black text-slate-900 tracking-tight">
            {t('progress.activityChartTitle')}
          </h3>
          <p className="text-xs text-slate-500 mt-0.5">
            {t('progress.activityChartSubtitle', { days })}
          </p>
        </div>

        {/* Legend */}
        <div className="flex flex-wrap items-center gap-3 text-xs font-semibold text-slate-600">
          {CATEGORIES.map((cat) => (
            <div key={cat.key} className="flex items-center gap-1.5">
              <span
                className="w-2.5 h-2.5 rounded-full shrink-0"
                style={{ backgroundColor: cat.color }}
              />
              <span>{t(cat.labelKey)}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Screen Reader Table */}
      <div className="sr-only">
        <table>
          <caption>{t('progress.chartTableCaption')}</caption>
          <thead>
            <tr>
              <th scope="col">{t('progress.chartDate')}</th>
              <th scope="col">{t('progress.vocabulary')}</th>
              <th scope="col">{t('progress.lessons')}</th>
              <th scope="col">{t('progress.grammar')}</th>
              <th scope="col">{t('progress.pronunciation')}</th>
              <th scope="col">{t('progress.liveTutor')}</th>
              <th scope="col">{t('progress.chartTotal')}</th>
            </tr>
          </thead>
          <tbody>
            {activity.map((item) => {
              const dayTotal =
                item.vocabularyReviews +
                item.lessonAttempts +
                item.grammarAttempts +
                item.pronunciationAssessments +
                item.tutorSessions;
              return (
                <tr key={item.date}>
                  <td>{item.date}</td>
                  <td>{item.vocabularyReviews}</td>
                  <td>{item.lessonAttempts}</td>
                  <td>{item.grammarAttempts}</td>
                  <td>{item.pronunciationAssessments}</td>
                  <td>{item.tutorSessions}</td>
                  <td>{dayTotal}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pure SVG Stacked Bar Chart */}
      <div className="w-full overflow-hidden">
        <svg
          role="img"
          aria-label={t('progress.chartAriaLabel', { days, total: totalActivity })}
          viewBox={`0 0 ${svgWidth} ${svgHeight}`}
          preserveAspectRatio="xMidYMid meet"
          className="w-full h-auto max-h-[340px] select-none block"
        >
          {/* Y Axis Grid lines and labels */}
          {yTicks.map((val) => {
            const y = zeroY - (val / tickMax) * chartHeight;
            return (
              <g key={`ytick-${val}`}>
                <line
                  x1={padding.left}
                  y1={y}
                  x2={padding.left + chartWidth}
                  y2={y}
                  stroke="#e2e8f0"
                  strokeDasharray="3 3"
                  strokeWidth="1"
                />
                <text
                  x={padding.left - 8}
                  y={y + 3.5}
                  textAnchor="end"
                  fontSize="10"
                  fill="#94a3b8"
                  className="font-mono font-medium select-none"
                >
                  {val}
                </text>
              </g>
            );
          })}

          {/* Activity Columns */}
          {activity.map((item, i) => {
            const x = padding.left + i * slotWidth + (slotWidth - barWidth) / 2;
            const slotCenterX = padding.left + i * slotWidth + slotWidth / 2;
            const dayTotal =
              item.vocabularyReviews +
              item.lessonAttempts +
              item.grammarAttempts +
              item.pronunciationAssessments +
              item.tutorSessions;

            let currentY = zeroY;

            const shouldShowLabel =
              i % labelInterval === 0 || i === activity.length - 1;

            return (
              <g
                key={item.date}
                data-testid="activity-column"
                className="group cursor-pointer"
              >
                {/* Accessible Tooltip for Hover / Keyboard Focus */}
                <title>
                  {`${item.date} — ${t('progress.chartTotal')}: ${dayTotal}\n` +
                    `• ${t('progress.vocabulary')}: ${item.vocabularyReviews}\n` +
                    `• ${t('progress.lessons')}: ${item.lessonAttempts}\n` +
                    `• ${t('progress.grammar')}: ${item.grammarAttempts}\n` +
                    `• ${t('progress.pronunciation')}: ${item.pronunciationAssessments}\n` +
                    `• ${t('progress.liveTutor')}: ${item.tutorSessions}`}
                </title>

                {/* Subtle Hover / Focus Background Column */}
                <rect
                  x={padding.left + i * slotWidth}
                  y={padding.top}
                  width={slotWidth}
                  height={chartHeight}
                  fill="transparent"
                  className="group-hover:fill-slate-100 transition-colors"
                />

                {/* Stacked Segments */}
                {CATEGORIES.map((cat, catIdx) => {
                  const count = item[cat.key] as number;
                  if (count <= 0) return null;
                  const segHeight = Math.max(1, (count / tickMax) * chartHeight);
                  const segY = currentY - segHeight;
                  currentY = segY;

                  return (
                    <rect
                      key={`${item.date}-${cat.key}`}
                      x={x}
                      y={segY}
                      width={barWidth}
                      height={segHeight}
                      fill={cat.color}
                      rx={catIdx === CATEGORIES.length - 1 || currentY <= zeroY - chartHeight ? 2 : 0}
                      className="transition-all"
                    />
                  );
                })}

                {/* X-axis date label */}
                {shouldShowLabel && (
                  <text
                    x={slotCenterX}
                    y={zeroY + 18}
                    textAnchor="middle"
                    fontSize="10"
                    fill="#64748b"
                    className="font-medium select-none"
                  >
                    {formatDateLabel(item.date)}
                  </text>
                )}
              </g>
            );
          })}

          {/* Baseline X Axis */}
          <line
            x1={padding.left}
            y1={zeroY}
            x2={padding.left + chartWidth}
            y2={zeroY}
            stroke="#cbd5e1"
            strokeWidth="1.5"
          />
        </svg>
      </div>
    </div>
  );
};
