'use client';
import React, { useMemo } from 'react';
import {
  CheckCircleOutlined,
  LockOutlined,
  ReloadOutlined,
  SafetyOutlined,
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
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import { useGetLocksQuery, type LockInfo } from '#/store/slice/clusterSlice';
import { clusterPollingIntervalMs, grafanaUrl } from '#/constants/config';
import { formatDate, formatRelativeToNow } from '#/utils/dateUtils';
import styles from '../cluster/ClusterPage.module.scss';

const shortId = (value: string): string => {
  if (!value) return '-';
  return value.length > 12 ? `${value.slice(0, 8)}…${value.slice(-4)}` : value;
};

const MODE_COLORS: Record<string, string> = {
  exclusive: 'volcano',
  write: 'volcano',
  shared: 'blue',
  read: 'blue',
};

const LocksPage: React.FC = () => {
  const {
    data: locks,
    isLoading,
    isFetching,
    refetch,
  } = useGetLocksQuery(undefined, { pollingInterval: clusterPollingIntervalMs });

  const lockRows = useMemo<LockInfo[]>(() => locks ?? [], [locks]);
  const isSingleNode = !isLoading && lockRows.length === 0;

  const columns: DataTableColumn<LockInfo>[] = [
    {
      key: 'LockKey',
      label: 'Lock Key',
      render: (lock) => <TextWithCopy text={lock.LockKey} className="code-font-style" />,
      filterValue: (lock) => lock.LockKey,
    },
    {
      key: 'Mode',
      label: 'Mode',
      width: '120px',
      render: (lock) => {
        const color = MODE_COLORS[(lock.Mode || '').toLowerCase()] || 'default';
        return <Less3Tag color={color}>{lock.Mode || '-'}</Less3Tag>;
      },
      filterValue: (lock) => lock.Mode || '',
    },
    {
      key: 'HolderId',
      label: 'Holder',
      width: '180px',
      render: (lock) => (
        <Less3Flex align="center" gap={6}>
          <Less3Tooltip title={lock.HolderId}>
            <span className="code-font-style">{shortId(lock.HolderId)}</span>
          </Less3Tooltip>
          {lock.HolderId && <CopyToClipboard text={lock.HolderId} ariaLabel="Copy holder ID" />}
        </Less3Flex>
      ),
      filterValue: (lock) => lock.HolderId || '',
    },
    {
      key: 'FencingToken',
      label: 'Fencing Token',
      width: '140px',
      render: (lock) => <span className="code-font-style">{lock.FencingToken}</span>,
      sortValue: (lock) => lock.FencingToken,
      filterValue: (lock) => String(lock.FencingToken),
    },
    {
      key: 'NodeId',
      label: 'Node',
      width: '180px',
      render: (lock) => (
        <Less3Tooltip title={lock.NodeId}>
          <span className="code-font-style">{shortId(lock.NodeId)}</span>
        </Less3Tooltip>
      ),
      filterValue: (lock) => lock.NodeId || '',
    },
    {
      key: 'AcquiredUtc',
      label: 'Acquired',
      width: '190px',
      render: (lock) => formatDate(lock.AcquiredUtc),
      sortValue: (lock) => new Date(lock.AcquiredUtc).getTime(),
      filterValue: (lock) => formatDate(lock.AcquiredUtc),
    },
    {
      key: 'LeaseExpiresUtc',
      label: 'Lease Expires',
      width: '210px',
      render: (lock) => (
        <Less3Flex vertical gap={2}>
          <span>{formatDate(lock.LeaseExpiresUtc)}</span>
          <Less3Text type="secondary" fontSize={12}>
            {formatRelativeToNow(lock.LeaseExpiresUtc)}
          </Less3Text>
        </Less3Flex>
      ),
      sortValue: (lock) => new Date(lock.LeaseExpiresUtc).getTime(),
      filterValue: (lock) => formatDate(lock.LeaseExpiresUtc),
    },
  ];

  return (
    <PageContainer
      pageTitle="Locks"
      pageTitleRightContent={
        <Less3Button icon={<ReloadOutlined />} onClick={() => refetch()} loading={isFetching}>
          Refresh
        </Less3Button>
      }
    >
      <Less3Flex vertical gap={20}>
        <Less3Card>
          <Less3Flex align="flex-start" gap={12}>
            <div
              style={{
                flex: '0 0 40px',
                width: 40,
                height: 40,
                borderRadius: 8,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: '#22AF7914',
                color: '#22AF79',
                fontSize: 20,
              }}
            >
              <SafetyOutlined />
            </div>
            <Less3Flex vertical gap={4}>
              <Less3Flex align="center" gap={8} wrap="wrap">
                <Less3Text weight={600} fontSize={15}>
                  Data-integrity guard is active
                </Less3Text>
                <Less3Tag color="success" icon={<CheckCircleOutlined />}>
                  Monitored in Grafana
                </Less3Tag>
              </Less3Flex>
              <Less3Text type="secondary" fontSize={13}>
                Every lock carries a monotonically increasing fencing token. Stale holders are rejected, and any
                fencing conflict is surfaced on the <strong>Locks &amp; Data Integrity</strong> Grafana dashboard,
                where the fencing-conflict count should stay at zero.
              </Less3Text>
              <a href={grafanaUrl} target="_blank" rel="noreferrer">
                Open Grafana &rarr;
              </a>
            </Less3Flex>
          </Less3Flex>
        </Less3Card>

        {isSingleNode && (
          <WeAlert
            type="info"
            showIcon
            icon={<LockOutlined />}
            message="No active locks"
            description="There are no distributed locks held right now. On a standalone single-node deployment this is expected — coordination locks only appear when multiple nodes contend for the same resource."
          />
        )}

        <DataTable columns={columns} data={lockRows} loading={isLoading} rowKey="LockKey" />
      </Less3Flex>
    </PageContainer>
  );
};

export default LocksPage;
