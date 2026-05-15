/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo } from 'react';
import { DatabaseOutlined, FolderOutlined, UserOutlined, KeyOutlined, HddOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import SummaryChart, { getQuickRange } from '#/page/request-history/SummaryChart';
import { useGetDashboardStatsQuery } from '#/store/slice/dashboardStatsSlice';
import { useGetRequestHistorySummaryQuery } from '#/store/slice/requestHistorySlice';

interface QuickActionCardProps {
  title: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  onClick: () => void;
}

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

  return (
    <PageContainer pageTitle="Home">
      <Less3Flex vertical gap={24}>
        <Less3Flex gap={16} wrap="wrap">
          <Less3Card style={{ flex: '1 1 240px', minWidth: 240 }}>
            <Less3Flex align="center" gap={14}>
              <div
                style={{
                  width: 44,
                  height: 44,
                  borderRadius: 12,
                  background: '#22AF7914',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#22AF79',
                  fontSize: 20,
                }}
              >
                <DatabaseOutlined />
              </div>
              <Less3Flex vertical gap={2}>
                <Less3Text type="secondary" fontSize={12}>Total Buckets</Less3Text>
                <Less3Text weight={700} fontSize={24}>
                  {dashboardStatsLoading ? '...' : String(dashboardStats?.BucketCount ?? 0)}
                </Less3Text>
              </Less3Flex>
            </Less3Flex>
          </Less3Card>
          <Less3Card style={{ flex: '1 1 240px', minWidth: 240 }}>
            <Less3Flex align="center" gap={14}>
              <div
                style={{
                  width: 44,
                  height: 44,
                  borderRadius: 12,
                  background: '#fa8c1614',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#fa8c16',
                  fontSize: 20,
                }}
              >
                <FolderOutlined />
              </div>
              <Less3Flex vertical gap={2}>
                <Less3Text type="secondary" fontSize={12}>Total Objects</Less3Text>
                <Less3Text weight={700} fontSize={24}>
                  {dashboardStatsLoading ? '...' : String(dashboardStats?.TotalObjectCount ?? 0)}
                </Less3Text>
              </Less3Flex>
            </Less3Flex>
          </Less3Card>
          <Less3Card style={{ flex: '1 1 240px', minWidth: 240 }}>
            <Less3Flex align="center" gap={14}>
              <div
                style={{
                  width: 44,
                  height: 44,
                  borderRadius: 12,
                  background: '#1890ff14',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: '#1890ff',
                  fontSize: 20,
                }}
              >
                <HddOutlined />
              </div>
              <Less3Flex vertical gap={2}>
                <Less3Text type="secondary" fontSize={12}>Total Storage</Less3Text>
                <Less3Text weight={700} fontSize={24}>
                  {dashboardStatsLoading ? '...' : formatBytes(dashboardStats?.TotalBytes ?? 0)}
                </Less3Text>
              </Less3Flex>
            </Less3Flex>
          </Less3Card>
        </Less3Flex>

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
          </Less3Flex>
        </div>
      </Less3Flex>
    </PageContainer>
  );
};

export default DashboardPage;
