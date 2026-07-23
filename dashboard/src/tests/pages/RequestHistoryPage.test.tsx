import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
        TenantId: 'tenant-a',
        HttpMethod: 'POST',
        RequestUrl: '/bucket/object',
        BucketName: 'content-bucket',
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
      {
        Id: 'entry-2',
        TenantId: 'tenant-b',
        HttpMethod: 'POST',
        RequestUrl: '/bucket/slow',
        BucketName: 'ops-bucket',
        SourceIp: '10.0.0.5',
        StatusCode: 503,
        Success: false,
        DurationMs: 220.5,
        RequestType: 'S3',
        UserId: 'user-2',
        AccessKey: 'AK456',
        RequestContentType: 'application/json',
        RequestBodyLength: 15,
        ResponseContentType: 'application/json',
        ResponseBodyLength: 16,
        RequestBody: '{"retry":true}',
        ResponseBody: '{"error":true}',
        CreatedUtc: '2026-05-15T12:01:00.000Z',
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
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: jest.fn().mockResolvedValue(undefined) },
      configurable: true,
    });
    global.URL.createObjectURL = jest.fn(() => 'blob:request-history');
    global.URL.revokeObjectURL = jest.fn();
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
    expect(within(dialog).getByText('Tenant ID')).toBeInTheDocument();
    expect(within(dialog).getByText('Bucket')).toBeInTheDocument();
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
  }, 15000);

  const selectFilterOption = async (ariaLabel: string, optionText: string) => {
    await userEvent.click(screen.getByLabelText(ariaLabel));

    const options = await screen.findAllByText(optionText);
    const option = options.find((element) => element.closest('.ant-select-item-option'));

    expect(option).toBeDefined();
    await userEvent.click(option as HTMLElement);
  };

  it('filters request history by tenant, user, credential, and bucket', async () => {
    renderWithRedux(<RequestHistoryPage />, false, undefined, true);

    await selectFilterOption('Bucket filter', 'ops-bucket');

    expect(screen.getByText('/bucket/slow')).toBeInTheDocument();
    expect(screen.queryByText('/bucket/object')).not.toBeInTheDocument();

    await selectFilterOption('User filter', 'user-1');
    expect(screen.getByText('No data available')).toBeInTheDocument();

    await selectFilterOption('User filter', 'user-2');
    expect(screen.getByText('/bucket/slow')).toBeInTheDocument();

    await selectFilterOption('Credential filter', 'AK123');
    expect(screen.getByText('No data available')).toBeInTheDocument();

    await selectFilterOption('Credential filter', 'AK456');
    expect(screen.getByText('/bucket/slow')).toBeInTheDocument();

    await selectFilterOption('Tenant filter', 'tenant-a');
    expect(screen.getByText('No data available')).toBeInTheDocument();

    await selectFilterOption('Tenant filter', 'tenant-b');
    expect(screen.getByText('/bucket/slow')).toBeInTheDocument();
  }, 15000);

  it('filters, groups, saves, exports, and copies request history entries', async () => {
    renderWithRedux(<RequestHistoryPage />, false, undefined, true);

    fireEvent.click(screen.getByLabelText('Failed only'));
    fireEvent.change(screen.getByPlaceholderText('Slow >= ms'), { target: { value: '100' } });

    expect(screen.getByText('/bucket/slow')).toBeInTheDocument();
    expect(screen.queryByText('/bucket/object')).not.toBeInTheDocument();

    fireEvent.click(screen.getByLabelText('Grouped'));
    expect(await screen.findByText('POST 5xx')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Save Filters/i }));
    expect(localStorage.getItem('less3_request_history_saved_filters')).toContain('"failedOnly":true');

    fireEvent.click(screen.getByRole('button', { name: /Export CSV/i }));
    expect(global.URL.createObjectURL).toHaveBeenCalled();

    const moreButton = screen.getAllByRole('button').find((button) => button.querySelector('.anticon-more'));
    expect(moreButton).toBeDefined();
    fireEvent.click(moreButton as HTMLElement);
    fireEvent.click(await screen.findByText('Copy as cURL'));

    await waitFor(() => {
      expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
        expect.stringContaining('curl -X POST "http://127.0.0.1:8000/bucket/slow"')
      );
    });

    const copiedCommand = (navigator.clipboard.writeText as jest.Mock).mock.calls[0][0] as string;
    expect(copiedCommand).toContain('-H "Content-Type: application/json"');
    expect(copiedCommand).toContain('--data-raw "{\\"retry\\":true}"');
    expect(copiedCommand).not.toContain("'http://127.0.0.1:8000/bucket/slow'");
    expect(copiedCommand).not.toContain('\\\n');
  }, 15000);
});
