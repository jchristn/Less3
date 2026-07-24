import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import BucketDetailPage from '#/page/buckets/BucketDetailPage';
import { renderWithRedux } from '../store/utils';

const mockPush = jest.fn();
const mockRefetchBucket = jest.fn();
const mockRefetchObjects = jest.fn();
const mockRefetchTags = jest.fn();
const mockRefetchAcl = jest.fn();
const mockRefetchHistory = jest.fn();

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useParams: () => ({ id: 'bkt_test' }),
  useSearchParams: () => new URLSearchParams('name=test-bucket'),
}));

jest.mock('#/store/slice/bucketsSlice', () => ({
  useGetBucketsQuery: () => ({
    data: [
      {
        Id: 'bkt_test',
        TenantId: 'default',
        Name: 'test-bucket',
        OwnerId: 'usr_owner',
        RegionString: 'us-east-1',
        EnableVersioning: true,
        EnablePublicRead: false,
        EnablePublicWrite: false,
        CreatedUtc: '2026-07-22T00:00:00.000Z',
      },
    ],
  }),
  useGetBucketByIdQuery: () => ({
    data: {
      Id: 'bkt_test',
      TenantId: 'default',
      Name: 'test-bucket',
      OwnerId: 'usr_owner',
      RegionString: 'us-east-1',
      EnableVersioning: true,
      EnablePublicRead: false,
      EnablePublicWrite: false,
      CreatedUtc: '2026-07-22T00:00:00.000Z',
    },
    isLoading: false,
    refetch: mockRefetchBucket,
  }),
  useListBucketObjectsQuery: () => ({
    data: {
      Contents: [
        {
          Key: 'folder/file.txt',
          Size: 12,
          ContentType: 'text/plain',
          LastModified: '2026-07-22T00:01:00.000Z',
        },
      ],
    },
    isLoading: false,
    refetch: mockRefetchObjects,
  }),
  useGetBucketTagsQuery: () => ({
    data: {
      tags: [
        { Key: 'env', Value: 'test' },
      ],
    },
    isLoading: false,
    refetch: mockRefetchTags,
  }),
  useGetBucketACLQuery: () => ({
    data: { acl: { Owner: { ID: 'usr_owner' } } },
    isLoading: false,
    refetch: mockRefetchAcl,
  }),
}));

jest.mock('#/store/slice/requestHistorySlice', () => ({
  useGetRequestHistoryQuery: () => ({
    data: [
      {
        Id: 'req_test',
        HttpMethod: 'GET',
        RequestUrl: '/test-bucket/folder/file.txt',
        StatusCode: 200,
        CreatedUtc: '2026-07-22T00:02:00.000Z',
      },
    ],
    isLoading: false,
    refetch: mockRefetchHistory,
  }),
}));

describe('BucketDetailPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders all bucket detail tabs and bucket context', async () => {
    renderWithRedux(<BucketDetailPage />, false, undefined, true);

    expect(screen.getByRole('heading', { name: 'Bucket: test-bucket' })).toBeInTheDocument();
    expect(screen.getByText('bkt_test')).toBeInTheDocument();
    expect(screen.getByText('usr_owner')).toBeInTheDocument();

    for (const tabName of ['Overview', 'Objects', 'Activity', 'Tags', 'ACL', 'Versioning', 'Settings']) {
      expect(screen.getByRole('tab', { name: tabName })).toBeInTheDocument();
    }
  });

  it('shows objects, activity, tags, acl, and versioning detail', async () => {
    renderWithRedux(<BucketDetailPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('tab', { name: 'Objects' }));
    expect(screen.getByText('folder/file.txt')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('tab', { name: 'Activity' }));
    expect(screen.getByText('/test-bucket/folder/file.txt')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('tab', { name: 'Tags' }));
    expect(screen.getByText('env')).toBeInTheDocument();
    expect(screen.getByText('test')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('tab', { name: 'ACL' }));
    expect(screen.getAllByText(/usr_owner/).length).toBeGreaterThan(0);

    await userEvent.click(screen.getByRole('tab', { name: 'Versioning' }));
    expect(within(screen.getByText(/Versioning:/).parentElement as HTMLElement).getByText(/Enabled/)).toBeInTheDocument();
  });

  it('refreshes all bucket detail data sources', async () => {
    renderWithRedux(<BucketDetailPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('button', { name: /Refresh/i }));

    await waitFor(() => {
      expect(mockRefetchBucket).toHaveBeenCalled();
      expect(mockRefetchObjects).toHaveBeenCalled();
      expect(mockRefetchTags).toHaveBeenCalled();
      expect(mockRefetchAcl).toHaveBeenCalled();
      expect(mockRefetchHistory).toHaveBeenCalled();
    });
  });

  it('links back to the object explorer for the selected bucket', async () => {
    renderWithRedux(<BucketDetailPage />, false, undefined, true);

    await userEvent.click(screen.getByRole('button', { name: /Objects/i }));

    expect(mockPush).toHaveBeenCalledWith('/admin/objects?bucket=test-bucket');
  });
});
