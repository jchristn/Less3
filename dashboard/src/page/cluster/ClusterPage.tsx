'use client';
import React, { useMemo } from 'react';
import {
  ApartmentOutlined,
  CheckCircleOutlined,
  ClusterOutlined,
  CloseCircleOutlined,
  CrownOutlined,
  LockOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import WeAlert from '#/components/base/alert/Alert';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Tag from '#/components/base/tag/Tag';
import Less3Text from '#/components/base/typograpghy/Text';
import Less3Button from '#/components/base/button/Button';
import Less3Tooltip from '#/components/base/tooltip/Tooltip';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import {
  useGetClusterHealthQuery,
  useGetClusterLeaderQuery,
  useGetClusterNodesQuery,
  type ClusterNodeInfo,
} from '#/store/slice/clusterSlice';
import { clusterPollingIntervalMs } from '#/constants/config';
import { formatDate, formatRelativeToNow } from '#/utils/dateUtils';
import styles from './ClusterPage.module.scss';

interface StatCardProps {
  label: string;
  value: string;
  icon: React.ReactNode;
  color: string;
}

const StatCard: React.FC<StatCardProps> = ({ label, value, icon, color }) => (
  <Less3Card className={styles.statCard}>
    <Less3Flex className={styles.statContent} align="center" gap={12}>
      <div className={styles.statIcon} style={{ background: color + '14', color }}>
        {icon}
      </div>
      <Less3Flex className={styles.statText} vertical gap={2}>
        <Less3Text className={styles.statLabel} type="secondary" fontSize={12}>
          {label}
        </Less3Text>
        <Less3Text className={styles.statValue} weight={700} fontSize={20}>
          {value}
        </Less3Text>
      </Less3Flex>
    </Less3Flex>
  </Less3Card>
);

const HealthBadge: React.FC<{ healthy: boolean }> = ({ healthy }) =>
  healthy ? (
    <Less3Tag color="success" icon={<CheckCircleOutlined />}>
      Healthy
    </Less3Tag>
  ) : (
    <Less3Tag color="error" icon={<CloseCircleOutlined />}>
      Unhealthy
    </Less3Tag>
  );

const ClusterPage: React.FC = () => {
  const {
    data: health,
    isLoading: healthLoading,
    isFetching: healthFetching,
    refetch: refetchHealth,
  } = useGetClusterHealthQuery(undefined, { pollingInterval: clusterPollingIntervalMs });
  const { data: nodes, isLoading: nodesLoading, refetch: refetchNodes } = useGetClusterNodesQuery(undefined, {
    pollingInterval: clusterPollingIntervalMs,
  });
  const { data: leader, refetch: refetchLeader } = useGetClusterLeaderQuery(undefined, {
    pollingInterval: clusterPollingIntervalMs,
  });

  const leaderNodeId = leader?.LeaderNodeId ?? null;
  const clusterEnabled = health?.ClusterEnabled ?? false;
  const nodeRows = useMemo<ClusterNodeInfo[]>(() => nodes ?? [], [nodes]);
  const selfNodeId = health?.SelfNodeId ?? nodeRows.find((node) => node.IsSelf)?.NodeId ?? '';

  const handleRefresh = () => {
    refetchHealth();
    refetchNodes();
    refetchLeader();
  };

  const columns: DataTableColumn<ClusterNodeInfo>[] = [
    {
      key: 'NodeId',
      label: 'Node',
      render: (node) => (
        <Less3Flex align="center" gap={8} wrap="wrap">
          <TextWithCopy text={node.NodeId} className="code-font-style" />
          {node.NodeId === leaderNodeId && (
            <Less3Tooltip title="Cluster leader">
              <Less3Tag color="gold" icon={<CrownOutlined />}>
                Leader
              </Less3Tag>
            </Less3Tooltip>
          )}
          {node.IsSelf && (
            <Less3Tooltip title="This dashboard is served by this node">
              <Less3Tag color="processing">This node</Less3Tag>
            </Less3Tooltip>
          )}
        </Less3Flex>
      ),
      filterValue: (node) => node.NodeId,
    },
    {
      key: 'Healthy',
      label: 'Health',
      width: '130px',
      render: (node) => <HealthBadge healthy={node.Healthy} />,
      sortValue: (node) => (node.Healthy ? 1 : 0),
      filterValue: (node) => (node.Healthy ? 'healthy' : 'unhealthy'),
    },
    {
      key: 'Hostname',
      label: 'Hostname',
      render: (node) => <span className="code-font-style">{node.Hostname || '-'}</span>,
      filterValue: (node) => node.Hostname || '',
    },
    {
      key: 'Version',
      label: 'Version',
      width: '120px',
      render: (node) => node.Version || '-',
      filterValue: (node) => node.Version || '',
    },
    {
      key: 'StartedUtc',
      label: 'Started',
      width: '190px',
      render: (node) => formatDate(node.StartedUtc),
      sortValue: (node) => new Date(node.StartedUtc).getTime(),
      filterValue: (node) => formatDate(node.StartedUtc),
    },
    {
      key: 'LastSeenUtc',
      label: 'Last Seen',
      width: '200px',
      render: (node) => (
        <Less3Tooltip title={formatDate(node.LastSeenUtc)}>
          <span>{formatRelativeToNow(node.LastSeenUtc)}</span>
        </Less3Tooltip>
      ),
      sortValue: (node) => new Date(node.LastSeenUtc).getTime(),
      filterValue: (node) => formatDate(node.LastSeenUtc),
    },
  ];

  return (
    <PageContainer
      pageTitle="Cluster"
      pageTitleRightContent={
        <Less3Button icon={<ReloadOutlined />} onClick={handleRefresh} loading={healthFetching}>
          Refresh
        </Less3Button>
      }
    >
      <Less3Flex vertical gap={20}>
        {!healthLoading && !clusterEnabled && (
          <WeAlert
            type="info"
            showIcon
            message="Standalone single-node deployment"
            description="Clustering is not enabled on this server. Less3 is running as a single, self-contained node, so there is no leader election or peer coordination to manage. Enable the cluster lock provider on the server to run multiple coordinated nodes."
          />
        )}

        <div className={styles.statGrid}>
          <StatCard
            label="Cluster Status"
            value={healthLoading ? '...' : clusterEnabled ? 'Enabled' : 'Standalone'}
            icon={<ClusterOutlined />}
            color={clusterEnabled ? '#22AF79' : '#8c8c8c'}
          />
          <StatCard
            label="Healthy Nodes"
            value={healthLoading ? '...' : `${health?.HealthyNodes ?? 0} / ${health?.TotalNodes ?? 0}`}
            icon={<ApartmentOutlined />}
            color="#1890ff"
          />
          <StatCard
            label="Lock Provider"
            value={healthLoading ? '...' : health?.LockProvider || 'None'}
            icon={<LockOutlined />}
            color="#722ed1"
          />
          <StatCard
            label="Leader"
            value={leaderNodeId ? leaderNodeId : clusterEnabled ? 'Electing...' : 'This node'}
            icon={<CrownOutlined />}
            color="#fa8c16"
          />
        </div>

        {selfNodeId && (
          <Less3Flex align="center" gap={8} wrap="wrap">
            <Less3Text type="secondary" fontSize={12}>
              Self node
            </Less3Text>
            <TextWithCopy text={selfNodeId} className="code-font-style" />
          </Less3Flex>
        )}

        <div>
          <Less3Text weight={600} fontSize={16} style={{ marginBottom: 12, display: 'block' }}>
            Nodes
          </Less3Text>
          <DataTable columns={columns} data={nodeRows} loading={nodesLoading} rowKey="NodeId" />
        </div>
      </Less3Flex>
    </PageContainer>
  );
};

export default ClusterPage;
