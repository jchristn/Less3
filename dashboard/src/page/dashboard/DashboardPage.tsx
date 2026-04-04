/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo } from 'react';
import { DatabaseOutlined, FolderOutlined, UserOutlined, KeyOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import SummaryChart, { getQuickRange } from '#/page/request-history/SummaryChart';
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

  return (
    <PageContainer pageTitle="Home">
      <Less3Flex vertical gap={24}>
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
