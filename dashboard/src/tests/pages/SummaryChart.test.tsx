import { render } from '@testing-library/react';
import SummaryChart from '#/page/request-history/SummaryChart';
import type { RequestHistorySummaryResult } from '#/store/slice/requestHistoryTypes';

describe('SummaryChart', () => {
  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('renders axis labels as zoom-scalable HTML text', () => {
    jest.spyOn(Date, 'now').mockReturnValue(new Date('2026-05-15T12:00:00.000Z').getTime());

    const summary: RequestHistorySummaryResult = {
      Data: [],
      StartUtc: '2026-05-15T11:00:00.000Z',
      EndUtc: '2026-05-15T12:00:00.000Z',
      Interval: 'minute',
      TotalSuccess: 0,
      TotalFailure: 0,
    };

    const { container } = render(
      <SummaryChart
        summary={summary}
        timeRange="hour"
        onTimeRangeChange={jest.fn()}
      />
    );

    expect(container.querySelectorAll('svg text')).toHaveLength(0);

    const yAxisLabels = container.querySelectorAll('[data-chart-axis-label="y"]');
    const xAxisLabels = container.querySelectorAll('[data-chart-axis-label="x"]');

    expect(yAxisLabels.length).toBeGreaterThan(0);
    expect(xAxisLabels.length).toBeGreaterThan(0);
    expect(yAxisLabels[0]).toHaveStyle({ fontSize: '0.625rem' });
    expect(xAxisLabels[0]).toHaveStyle({ fontSize: '0.5625rem' });
  });
});
