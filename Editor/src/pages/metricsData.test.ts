import { describe, expect, it } from 'vitest';
import { buildChartRows, lastDays } from './metricsData';

describe('lastDays', () => {
  it('returns the requested amount of days, oldest first', () => {
    const days = lastDays(31, new Date('2024-05-31T12:00:00'));

    expect(days).toHaveLength(31);
    expect(days[0].label).toBe('01.05');
    expect(days[30].label).toBe('31.05');
  });
});

describe('buildChartRows', () => {
  it('maps the views of every post onto the days and defaults to zero', () => {
    const days = lastDays(2, new Date('2024-05-31T12:00:00'));

    const rows = buildChartRows(days, {
      'my-post': [{ slug: 'my-post', views: 42, timestamp: '2024-05-31T00:00:00' }],
    });

    expect(rows).toEqual([
      { label: '30.05', 'my-post': 0 },
      { label: '31.05', 'my-post': 42 },
    ]);
  });
});
