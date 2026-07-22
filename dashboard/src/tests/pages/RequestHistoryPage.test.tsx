import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import RequestHistoryPage from '#/page/request-history/RequestHistoryPage';
import { renderWithRedux } from '../store/utils';

const mockRefetch = jest.fn();
const mockRefetchSummary = jest.fn();
const mockDeleteRequestHistory = jest.fn();

jest.mock('#/store/slice/requestHistorySlice', () => ({
  useGetRequestHistoryQuery: () => ({
    data: [
      {
        Id: 'entry-1',
        HttpMethod: 'POST',
        RequestUrl: '/bucket/object',
        SourceIp: '127.0.0.1',
        StatusCode: 200,
        Success: true,
        DurationMs: 12.65,
        RequestType: 'S3',
        UserId: 'user-1',
        AccessKey: 'AK123',
        RequestContentType: 'application/json',
        RequestBodyLength: 16,
        ResponseContentType: 'application/json',
        ResponseBodyLength: 17,
        RequestBody: '{"request":true}',
        ResponseBody: '{"response":true}',
        CreatedUtc: '2026-05-15T12:00:00.000Z',
      },
    ],
    isLoading: false,
    refetch: mockRefetch,
  }),
  useGetRequestHistorySummaryQuery: () => ({
    data: null,
    isLoading: false,
    refetch: mockRefetchSummary,
  }),
  useDeleteRequestHistoryMutation: () => [
    mockDeleteRequestHistory,
    { isLoading: false },
  ],
}));

jest.mock('#/page/request-history/SummaryChart', () => ({
  __esModule: true,
  default: () => <div>Summary Chart</div>,
  getQuickRange: () => ({
    startUtc: new Date('2026-05-15T11:00:00.000Z'),
    endUtc: new Date('2026-05-15T12:00:00.000Z'),
    interval: 'minute',
  }),
}));

describe('RequestHistoryPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockDeleteRequestHistory.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({ success: true }),
    });
  });

  it('lets the user pretty-print both request and response bodies in the detail modal', async () => {
    renderWithRedux(<RequestHistoryPage />, false, undefined, true);

    fireEvent.click(screen.getByText('/bucket/object'));

    const dialog = await screen.findByRole('dialog');

    expect(dialog).toBeInTheDocument();
    expect(within(dialog).getByText('Entry ID')).toBeInTheDocument();
    expect(within(dialog).getByText('Route')).toBeInTheDocument();
    expect(within(dialog).getByText('Source IP')).toBeInTheDocument();
    expect(within(dialog).getByText('Status')).toBeInTheDocument();
    expect(within(dialog).getByText('Response Time')).toBeInTheDocument();
    expect(within(dialog).getByText('POST /bucket/object')).toBeInTheDocument();
    expect(within(dialog).getByText('12.65ms')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Pretty Print' })).toHaveLength(2);

    fireEvent.click(screen.getAllByRole('button', { name: 'Pretty Print' })[0]);

    await waitFor(() => {
      expect(
        screen.getByText((_, element) => element?.textContent === '{\n  "request": true\n}')
      ).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: 'Pretty Print' }));

    await waitFor(() => {
      expect(
        screen.getByText((_, element) => element?.textContent === '{\n  "response": true\n}')
      ).toBeInTheDocument();
    });

    expect(screen.getAllByRole('button', { name: 'Show Raw' })).toHaveLength(2);
  });
});
