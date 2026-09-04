import type { PageMetric } from '../api/models';

export interface MetricDay {
  /** Label shown on the x axis, e.g. "24.05" */
  label: string;
  /** Start of the day the metrics are aggregated for */
  date: Date;
}

/** Builds the last `count` days, oldest first, like the Blazor metrics page did. */
export const lastDays = (count: number, today: Date = new Date()): MetricDay[] =>
  Array.from({ length: count }, (_, index) => {
    const date = new Date(today);
    date.setDate(today.getDate() - index);
    date.setHours(0, 0, 0, 0);

    return {
      label: `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}`,
      date,
    };
  }).reverse();

export type ChartRow = { label: string } & Record<string, number | string>;

/** Merges the page view history of every selected post into rows for the chart. */
export const buildChartRows = (
  days: MetricDay[],
  history: Record<string, PageMetric[]>,
): ChartRow[] =>
  days.map((day) => {
    const row: ChartRow = { label: day.label };

    for (const [slug, metrics] of Object.entries(history)) {
      const match = metrics.find(
        (metric) => new Date(metric.timestamp).toDateString() === day.date.toDateString(),
      );

      row[slug] = match?.views ?? 0;
    }

    return row;
  });

const palette = ['#1976d2', '#2e7d32', '#ed6c02', '#9c27b0', '#d32f2f', '#0288d1'];

export const seriesColor = (index: number): string => palette[index % palette.length];
