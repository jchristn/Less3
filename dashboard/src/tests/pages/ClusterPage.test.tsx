import { screen } from '@testing-library/react';
import ClusterPage from '#/page/cluster/ClusterPage';
import { renderWithRedux } from '../store/utils';

const mockRefetchHealth = jest.fn();
const mockRefetchNodes = jest.fn();
const mockRefetchLeader = jest.fn();

let healthResult: any;
let nodesResult: any;
let leaderResult: any;

jest.mock('#/store/slice/clusterSlice', () => ({
  useGetClusterHealthQuery: () => healthResult,
  useGetClusterNodesQuery: () => nodesResult,
  useGetClusterLeaderQuery: () => leaderResult,
}));

const buildNodes = () => [
  {
    NodeId: 'node-alpha',
    Hostname: 'alpha.less3.local',
    Version: '4.0.0',
    StartedUtc: '2026-08-13T10:00:00.000Z',
    LastSeenUtc: '2026-08-13T12:00:00.000Z',
    Healthy: true,
    IsSelf: true,
  },
  {
    NodeId: 'node-bravo',
    Hostname: 'bravo.less3.local',
    Version: '4.0.0',
    StartedUtc: '2026-08-13T10:05:00.000Z',
    LastSeenUtc: '2026-08-13T12:00:00.000Z',
    Healthy: false,
    IsSelf: false,
  },
];

describe('ClusterPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    healthResult = {
      data: {
        ClusterEnabled: true,
        LockProvider: 'Redis',
        SelfNodeId: 'node-alpha',
        TotalNodes: 2,
        HealthyNodes: 1,
        Nodes: buildNodes(),
        GeneratedUtc: '2026-08-13T12:00:00.000Z',
      },
      isLoading: false,
      isFetching: false,
      refetch: mockRefetchHealth,
    };
    nodesResult = {
      data: buildNodes(),
      isLoading: false,
      refetch: mockRefetchNodes,
    };
    leaderResult = {
      data: { LeaderNodeId: 'node-bravo' },
      refetch: mockRefetchLeader,
    };
  });

  it('renders cluster aggregates and node rows with health, leader, and self badges', () => {
    renderWithRedux(<ClusterPage />, false, undefined, true);

    // Aggregate stats
    expect(screen.getByText('Enabled')).toBeInTheDocument();
    expect(screen.getByText('1 / 2')).toBeInTheDocument();
    expect(screen.getByText('Redis')).toBeInTheDocument();

    // Nodes table (node-alpha also appears in the self-node row; node-bravo also in the Leader stat card)
    expect(screen.getAllByText('node-alpha').length).toBeGreaterThan(0);
    expect(screen.getAllByText('node-bravo').length).toBeGreaterThan(0);
    expect(screen.getByText('alpha.less3.local')).toBeInTheDocument();
    expect(screen.getByText('Healthy')).toBeInTheDocument();
    expect(screen.getByText('Unhealthy')).toBeInTheDocument();

    // node-bravo is the leader; node-alpha is self ("Leader" is both a stat label and the row tag)
    expect(screen.getAllByText('Leader').length).toBeGreaterThan(0);
    expect(screen.getByText('This node')).toBeInTheDocument();

    // Standalone banner should NOT show while clustering is enabled
    expect(screen.queryByText('Standalone single-node deployment')).not.toBeInTheDocument();
  });

  it('shows a clear standalone state when clustering is disabled', () => {
    healthResult = {
      data: {
        ClusterEnabled: false,
        LockProvider: 'InMemory',
        SelfNodeId: 'node-solo',
        TotalNodes: 1,
        HealthyNodes: 1,
        Nodes: [
          {
            NodeId: 'node-solo',
            Hostname: 'solo.less3.local',
            Version: '4.0.0',
            StartedUtc: '2026-08-13T10:00:00.000Z',
            LastSeenUtc: '2026-08-13T12:00:00.000Z',
            Healthy: true,
            IsSelf: true,
          },
        ],
        GeneratedUtc: '2026-08-13T12:00:00.000Z',
      },
      isLoading: false,
      isFetching: false,
      refetch: mockRefetchHealth,
    };
    nodesResult = {
      data: healthResult.data.Nodes,
      isLoading: false,
      refetch: mockRefetchNodes,
    };
    leaderResult = { data: { LeaderNodeId: null }, refetch: mockRefetchLeader };

    renderWithRedux(<ClusterPage />, false, undefined, true);

    expect(screen.getByText('Standalone single-node deployment')).toBeInTheDocument();
    expect(screen.getByText('Standalone')).toBeInTheDocument();
    expect(screen.getAllByText('node-solo').length).toBeGreaterThan(0);
  });
});
