/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo } from 'react';
import {
  BankOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  DatabaseOutlined,
  FolderOutlined,
  HddOutlined,
  KeyOutlined,
  SafetyCertificateOutlined,
  ThunderboltOutlined,
  UserOutlined,
  WarningOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import SummaryChart, { getQuickRange } from '#/page/request-history/SummaryChart';
import { useGetAdminHealthQuery, useGetDashboardStatsQuery } from '#/store/slice/dashboardStatsSlice';
import { useGetRequestHistorySummaryQuery } from '#/store/slice/requestHistorySlice';

interface QuickActionCardProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  onClick: () => void;
}

interface KpiCardProps {
  label: string;
  value: string;
  icon: React.ReactNode;
  color: string;
}

interface NodeStatusItemProps {
  label: string;
  value: string;
  healthy?: boolean;
}

const NodeStatusItem: React.FC<NodeStatusItemProps> = ({ label, value, healthy }) => (
  <Less3Flex vertical gap={2} style={{ minWidth: 140 }}>
    <Less3Text type="secondary" fontSize={12}>{label}</Less3Text>
    <Less3Flex align="center" gap={6}>
      {healthy !== undefined && (
        healthy ? (
          <CheckCircleOutlined style={{ color: '#22AF79' }} />
        ) : (
          <CloseCircleOutlined style={{ color: '#f5222d' }} />
        )
      )}
      <Less3Text weight={600} fontSize={13}>
        {value}
      </Less3Text>
    </Less3Flex>
  </Less3Flex>
);

const KpiCard: React.FC<KpiCardProps> = ({ label, value, icon, color }) => (
  <Less3Card style={{ flex: '1 1 220px', minWidth: 220 }}>
    <Less3Flex align="center" gap={14}>
      <div
        style={{
          width: 44,
          height: 44,
          borderRadius: 8,
          background: color + '14',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: color,
          fontSize: 20,
        }}
      >
        {icon}
      </div>
      <Less3Flex vertical gap={2}>
        <Less3Text type="secondary" fontSize={12}>{label}</Less3Text>
        <Less3Text weight={700} fontSize={24}>
          {value}
        </Less3Text>
      </Less3Flex>
    </Less3Flex>
  </Less3Card>
);

const QuickActionCard: React.FC<QuickActionCardProps> = ({ title, description, icon, color, onClick }) => (
  <Less3Card
    hoverable
    style={{ cursor: 'pointer', flex: '1 1 220px', minWidth: 220 }}
    onClick={onClick}
  >
    <Less3Flex align="center" gap={16}>
      <div
        style={{
          width: 48,
          height: 48,
          borderRadius: 12,
          background: color + '14',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 22,
          color: color,
        }}
      >
        {icon}
      </div>
      <Less3Flex vertical gap={2}>
        <Less3Text weight={600} fontSize={15}>
          {title}
        </Less3Text>
        <Less3Text fontSize={13} style={{ color: 'var(--ant-color-text-secondary)' }}>
          {description}
        </Less3Text>
      </Less3Flex>
    </Less3Flex>
  </Less3Card>
);

const formatBytes = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`;
};

const formatDuration = (seconds: number): string => {
  if (seconds < 60) return `${seconds}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h`;
  return `${Math.floor(seconds / 86400)}d`;
};

const DashboardPage: React.FC = () => {
  const router = useRouter();
  const [timeRange, setTimeRange] = useState('day');

  const summaryParams = useMemo(() => {
    const range = getQuickRange(timeRange);
    return {
      startUtc: range.startUtc.toISOString(),
      endUtc: range.endUtc.toISOString(),
      interval: range.interval,
    };
  }, [timeRange]);

  const { data: summary, isLoading: summaryLoading, refetch: refetchSummary } = useGetRequestHistorySummaryQuery(summaryParams, {
    pollingInterval: 10000,
  });
  const { data: dashboardStats, isLoading: dashboardStatsLoading } = useGetDashboardStatsQuery(undefined, {
    pollingInterval: 10000,
  });
  const { data: health, isLoading: healthLoading } = useGetAdminHealthQuery(undefined, {
    pollingInterval: 10000,
  });
  const requestCount = (summary?.TotalSuccess ?? 0) + (summary?.TotalFailure ?? 0);
  const failureRate = requestCount === 0 ? '0%' : `${(((summary?.TotalFailure ?? 0) / requestCount) * 100).toFixed(1)}%`;

  return (
    <PageContainer pageTitle="Home">
      <Less3Flex vertical gap={24}>
        <Less3Flex gap={16} wrap="wrap">
          <KpiCard label="Tenants" value="1" icon={<BankOutlined />} color="#13c2c2" />
          <KpiCard
            label="Total Buckets"
            value={dashboardStatsLoading ? '...' : String(dashboardStats?.BucketCount ?? 0)}
            icon={<DatabaseOutlined />}
            color="#22AF79"
          />
          <KpiCard
            label="Total Objects"
            value={dashboardStatsLoading ? '...' : String(dashboardStats?.TotalObjectCount ?? 0)}
            icon={<FolderOutlined />}
            color="#fa8c16"
          />
          <KpiCard
            label="Storage Used"
            value={dashboardStatsLoading ? '...' : formatBytes(dashboardStats?.TotalBytes ?? 0)}
            icon={<HddOutlined />}
            color="#1890ff"
          />
          <KpiCard label="Active Credentials" value="1" icon={<KeyOutlined />} color="#722ed1" />
          <KpiCard
            label="Requests"
            value={summaryLoading ? '...' : String(requestCount)}
            icon={<ThunderboltOutlined />}
            color="#eb2f96"
          />
          <KpiCard
            label="Failure Rate"
            value={summaryLoading ? '...' : failureRate}
            icon={<WarningOutlined />}
            color="#f5222d"
          />
        </Less3Flex>

        <div
          style={{
            background: 'var(--ant-color-bg-container)',
            border: '1px solid var(--color-separator)',
            borderRadius: 8,
            padding: 16,
          }}
        >
          <Less3Flex gap={20} wrap="wrap" align="center">
            <NodeStatusItem label="Version" value={healthLoading ? '...' : (health?.ServerVersion ?? 'unknown')} />
            <NodeStatusItem
              label="Uptime"
              value={healthLoading ? '...' : formatDuration(health?.UptimeSeconds ?? 0)}
            />
            <NodeStatusItem
              label="Database"
              value={healthLoading ? '...' : (health?.DatabaseType ?? 'unknown')}
              healthy={health?.DatabaseReachable}
            />
            <NodeStatusItem
              label="Storage"
              value={healthLoading ? '...' : (health?.StoragePathWritable ? 'Writable' : 'Not writable')}
              healthy={health?.StoragePathWritable}
            />
            <NodeStatusItem
              label="Free Disk"
              value={healthLoading ? '...' : formatBytes(health?.FreeDiskBytes ?? 0)}
            />
            <NodeStatusItem
              label="Temp Uploads"
              value={healthLoading ? '...' : String(health?.TempUploadCount ?? 0)}
            />
            <NodeStatusItem
              label="History Retention"
              value={healthLoading ? '...' : `${health?.RequestHistoryRetentionDays ?? 30}d`}
            />
          </Less3Flex>
        </div>

        <SummaryChart
          summary={summary || null}
          timeRange={timeRange}
          onTimeRangeChange={setTimeRange}
          loading={summaryLoading}
          onRefresh={refetchSummary}
        />

        <div>
          <Less3Text weight={600} fontSize={16} style={{ marginBottom: 12, display: 'block' }}>
            Quick Actions
          </Less3Text>
          <Less3Flex gap={16} wrap="wrap">
            <QuickActionCard
              title="Create a Bucket"
              description="Set up a new storage bucket"
              icon={<DatabaseOutlined />}
              color="#22AF79"
              onClick={() => router.push('/admin/buckets')}
            />
            <QuickActionCard
              title="Manage Objects"
              description="Browse and manage stored objects"
              icon={<FolderOutlined />}
              color="#1890ff"
              onClick={() => router.push('/admin/objects')}
            />
            <QuickActionCard
              title="Manage Users"
              description="View and manage user accounts"
              icon={<UserOutlined />}
              color="#fa8c16"
              onClick={() => router.push('/admin/users')}
            />
            <QuickActionCard
              title="Manage Credentials"
              description="Configure access keys and secrets"
              icon={<KeyOutlined />}
              color="#722ed1"
              onClick={() => router.push('/admin/credentials')}
            />
            <QuickActionCard
              title="Manage Tenants"
              description="Configure tenant boundaries"
              icon={<BankOutlined />}
              color="#13c2c2"
              onClick={() => router.push('/admin/tenants')}
            />
            <QuickActionCard
              title="Manage Roles"
              description="Configure RBAC roles"
              icon={<SafetyCertificateOutlined />}
              color="#eb2f96"
              onClick={() => router.push('/admin/roles')}
            />
          </Less3Flex>
        </div>
      </Less3Flex>
    </PageContainer>
  );
};

export default DashboardPage;
