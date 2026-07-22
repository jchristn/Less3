import { screen } from '@testing-library/react';
import DashboardPage from '#/page/dashboard/DashboardPage';
import { renderWithRedux } from '../store/utils';

jest.mock('#/store/slice/dashboardStatsSlice', () => ({
  useGetAdminHealthQuery: () => ({
    data: {
      ServerVersion: '3.0.0-test',
      UptimeSeconds: 120,
      DatabaseType: 'Sqlite',
      DatabaseReachable: true,
      StoragePathWritable: true,
      FreeDiskBytes: 1073741824,
      TempUploadCount: 0,
      RequestHistoryRetentionDays: 30,
    },
    isLoading: false,
  }),
  useGetDashboardStatsQuery: () => ({
    data: {
      BucketCount: 12,
      TotalBytes: 1048576,
      TotalObjectCount: 25,
      GeneratedUtc: '2026-05-15T12:00:00.000Z',
      Buckets: [],
    },
    isLoading: false,
  }),
  useGetRequestReportQuery: () => ({
    data: {
      RequestsPerMinute: 1.25,
      P95LatencyMs: 42,
    },
    isLoading: false,
  }),
}));

jest.mock('#/store/slice/credentialsSlice', () => ({
  useGetCredentialsQuery: () => ({
    data: [
      { Id: 'crd_active', Active: true },
      { Id: 'crd_disabled', Active: false },
    ],
    isLoading: false,
  }),
}));

jest.mock('#/store/slice/requestHistorySlice', () => ({
  useGetRequestHistorySummaryQuery: () => ({
    data: {
      TotalSuccess: 8,
      TotalFailure: 2,
      Points: [],
    },
    isLoading: false,
    refetch: jest.fn(),
  }),
}));

jest.mock('#/page/request-history/SummaryChart', () => ({
  __esModule: true,
  default: () => (
    <div>
      <span>Request Summary</span>
      <span>Last Day</span>
    </div>
  ),
  getQuickRange: () => ({
    startUtc: new Date('2026-05-15T00:00:00.000Z'),
    endUtc: new Date('2026-05-16T00:00:00.000Z'),
    interval: 'hour',
  }),
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => '/dashboard',
}));

describe('DashboardPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering', () => {
    it('renders the quick actions section', () => {
      renderWithRedux(<DashboardPage />);

      expect(screen.getByText('Quick Actions')).toBeInTheDocument();
      expect(screen.getByText('Create a Bucket')).toBeInTheDocument();
      expect(screen.getByText('Manage Objects')).toBeInTheDocument();
    });

    it('renders the request summary controls', () => {
      renderWithRedux(<DashboardPage />);

      expect(screen.getByText('Total Buckets')).toBeInTheDocument();
      expect(screen.getByText('12')).toBeInTheDocument();
      expect(screen.getByText('Total Objects')).toBeInTheDocument();
      expect(screen.getByText('25')).toBeInTheDocument();
      expect(screen.getByText('Storage Used')).toBeInTheDocument();
      expect(screen.getByText('1.0 MB')).toBeInTheDocument();
      expect(screen.getByText('Request Summary')).toBeInTheDocument();
      expect(screen.getByText('Last Day')).toBeInTheDocument();
    });

    it('renders the database quick action icon', () => {
      const { container } = renderWithRedux(<DashboardPage />);

      expect(container.querySelector('.anticon-database')).toBeInTheDocument();
    });
  });

  describe('Snapshots', () => {
    it('matches the default render', () => {
      const { container } = renderWithRedux(<DashboardPage />);

      expect(container.firstChild).toMatchSnapshot();
    });
  });
});
