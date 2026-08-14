import { screen } from '@testing-library/react';
import LocksPage from '#/page/locks/LocksPage';
import { renderWithRedux } from '../store/utils';

const mockRefetch = jest.fn();

let locksResult: any;

jest.mock('#/store/slice/clusterSlice', () => ({
  useGetLocksQuery: () => locksResult,
}));

describe('LocksPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    locksResult = {
      data: [
        {
          LockKey: 'bucket/report.csv',
          Mode: 'Exclusive',
          HolderId: 'holder-1234567890abcdef',
          FencingToken: 42,
          NodeId: 'node-alpha-1234567890',
          AcquiredUtc: '2026-08-13T12:00:00.000Z',
          LeaseExpiresUtc: '2026-08-13T12:05:00.000Z',
        },
      ],
      isLoading: false,
      isFetching: false,
      refetch: mockRefetch,
    };
  });

  it('renders active locks with fencing tokens and the Grafana data-integrity reassurance', () => {
    renderWithRedux(<LocksPage />, false, undefined, true);

    expect(screen.getByText('bucket/report.csv')).toBeInTheDocument();
    expect(screen.getByText('Exclusive')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();

    // Fencing/Grafana reassurance
    expect(screen.getByText('Data-integrity guard is active')).toBeInTheDocument();
    expect(screen.getByText('Monitored in Grafana')).toBeInTheDocument();

    const grafanaLink = screen.getByRole('link', { name: /Open Grafana/i });
    expect(grafanaLink).toHaveAttribute('href', 'http://localhost:3001');

    // No empty state while a lock exists
    expect(screen.queryByText('No active locks')).not.toBeInTheDocument();
  });

  it('shows a reassuring single-node empty state when no locks are held', () => {
    locksResult = {
      data: [],
      isLoading: false,
      isFetching: false,
      refetch: mockRefetch,
    };

    renderWithRedux(<LocksPage />, false, undefined, true);

    expect(screen.getByText('No active locks')).toBeInTheDocument();
    // The reassurance panel is still present regardless of lock count
    expect(screen.getByText('Data-integrity guard is active')).toBeInTheDocument();
  });
});
