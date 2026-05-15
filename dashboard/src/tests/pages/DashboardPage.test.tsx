import { screen } from '@testing-library/react';
import DashboardPage from '#/page/dashboard/DashboardPage';
import { renderWithRedux } from '../store/utils';

jest.mock('#/store/slice/dashboardStatsSlice', () => ({
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
      expect(screen.getByText('Total Storage')).toBeInTheDocument();
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
