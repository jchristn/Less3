/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState, useCallback } from 'react';
import { message, MenuProps, Descriptions } from 'antd';
import {
  SearchOutlined,
  MoreOutlined,
  DeleteOutlined,
  EyeOutlined,
  ReloadOutlined,
  CopyOutlined,
  CheckOutlined,
} from '@ant-design/icons';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetRequestHistoryQuery,
  useGetRequestHistorySummaryQuery,
  useDeleteRequestHistoryMutation,
  RequestHistoryEntry,
} from '#/store/slice/requestHistorySlice';
import GuidDisplay from '#/components/guid-display';
import { copyToClipboard } from '#/utils/clipboardUtils';
import { formatDate } from '#/utils/dateUtils';
import SummaryChart, { getQuickRange } from './SummaryChart';

const METHOD_OPTIONS = [
  { label: 'All Methods', value: '' },
  { label: 'GET', value: 'GET' },
  { label: 'PUT', value: 'PUT' },
  { label: 'POST', value: 'POST' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'HEAD', value: 'HEAD' },
];

const METHOD_COLORS: Record<string, string> = {
  GET: '#22AF79',
  POST: '#1890ff',
  PUT: '#fa8c16',
  DELETE: '#d9383a',
  HEAD: '#722ed1',
};


const getStatusColor = (code: number): string => {
  if (code >= 200 && code < 300) return '#22AF79';
  if (code >= 400 && code < 500) return '#fa8c16';
  if (code >= 500) return '#d9383a';
  return '#8c8c8c';
};

const CopyIcon: React.FC<{ text: string }> = ({ text }) => {
  const [copied, setCopied] = useState(false);
  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation();
    copyToClipboard(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 1500);
  };
  return (
    <span
      onClick={handleCopy}
      style={{
        cursor: 'pointer', display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
        width: 24, height: 24, borderRadius: 4, transition: 'all 0.2s ease',
        color: copied ? '#22AF79' : 'var(--ant-color-text-quaternary)',
      }}
    >
      {copied ? <CheckOutlined style={{ fontSize: 14 }} /> : <CopyOutlined style={{ fontSize: 14 }} />}
    </span>
  );
};

const DetailBlock: React.FC<{ title: string; value: string }> = ({ title, value }) => {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      <Less3Flex justify="space-between" align="center">
        <Less3Text weight={600} fontSize={13}>{title}</Less3Text>
        <CopyIcon text={value} />
      </Less3Flex>
      <pre
        style={{
          margin: 0,
          padding: 12,
          borderRadius: 6,
          background: 'var(--ant-color-bg-layout)',
          border: '1px solid var(--color-separator)',
          fontSize: 12,
          fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-all',
          maxHeight: 300,
          overflow: 'auto',
          lineHeight: 1.6,
        }}
      >
        {value || '(empty)'}
      </pre>
    </div>
  );
};

const RequestHistoryPage: React.FC = () => {
  const [methodFilter, setMethodFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [sourceIpFilter, setSourceIpFilter] = useState<string>('');
  const [timeRange, setTimeRange] = useState<string>('hour');
  const [isDetailModalVisible, setIsDetailModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [selectedEntry, setSelectedEntry] = useState<RequestHistoryEntry | null>(null);
  const [deletingEntry, setDeletingEntry] = useState<RequestHistoryEntry | null>(null);
  const [openDropdownKey, setOpenDropdownKey] = useState<string | null>(null);

  const { data, isLoading, refetch } = useGetRequestHistoryQuery();
  const [deleteRequestHistory, { isLoading: isDeleting }] = useDeleteRequestHistoryMutation();

  const summaryParams = useMemo(() => {
    const range = getQuickRange(timeRange);
    return {
      startUtc: range.startUtc.toISOString(),
      endUtc: range.endUtc.toISOString(),
      interval: range.interval,
    };
  }, [timeRange]);

  const { data: summaryData, isLoading: isSummaryLoading, refetch: refetchSummary } = useGetRequestHistorySummaryQuery(summaryParams, {
    pollingInterval: 10000,
  });

  const handleViewDetail = useCallback((entry: RequestHistoryEntry) => {
    setSelectedEntry(entry);
    setIsDetailModalVisible(true);
  }, []);

  const handleDeleteClick = useCallback((entry: RequestHistoryEntry) => {
    setDeletingEntry(entry);
    setIsDeleteModalVisible(true);
  }, []);

  const handleDeleteConfirm = async () => {
    if (!deletingEntry?.GUID) return;
    try {
      await deleteRequestHistory({ guid: deletingEntry.GUID }).unwrap();
      message.success('Request history entry deleted');
      setIsDeleteModalVisible(false);
      setDeletingEntry(null);
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to delete entry');
    }
  };

  const filteredData = useMemo(() => {
    if (!data) return [];
    let result: RequestHistoryEntry[] = [...data];

    // Sort by most recent first
    result.sort((a, b) => new Date(b.CreatedUtc).getTime() - new Date(a.CreatedUtc).getTime());

    if (methodFilter) {
      result = result.filter((entry) => entry.HttpMethod === methodFilter);
    }

    if (statusFilter.trim()) {
      const statusStr: string = statusFilter.trim();
      result = result.filter((entry) => String(entry.StatusCode).includes(statusStr));
    }

    if (sourceIpFilter.trim()) {
      const ipStr: string = sourceIpFilter.trim().toLowerCase();
      result = result.filter((entry) => (entry.SourceIp || '').toLowerCase().includes(ipStr));
    }

    return result;
  }, [data, methodFilter, statusFilter, sourceIpFilter]);

  const columns: DataTableColumn<RequestHistoryEntry>[] = [
    {
      key: 'CreatedUtc',
      label: 'Time',
      render: (item) => formatDate(item.CreatedUtc),
      sortValue: (item) => new Date(item.CreatedUtc).getTime(),
      filterValue: (item) => formatDate(item.CreatedUtc),
    },
    {
      key: 'HttpMethod',
      label: 'Method',
      width: '90px',
      render: (item) => {
        const color: string = METHOD_COLORS[item.HttpMethod] || '#8c8c8c';
        return (
          <span
            style={{
              display: 'inline-block',
              padding: '2px 8px',
              borderRadius: 4,
              fontSize: 11,
              fontWeight: 600,
              color: '#fff',
              background: color,
            }}
          >
            {item.HttpMethod}
          </span>
        );
      },
      filterValue: (item) => item.HttpMethod,
    },
    {
      key: 'RequestUrl',
      label: 'URL',
      render: (item) => (
        <span style={{ wordBreak: 'break-all', fontSize: 12 }}>{item.RequestUrl}</span>
      ),
      filterValue: (item) => item.RequestUrl || '',
    },
    {
      key: 'StatusCode',
      label: 'Status',
      width: '80px',
      render: (item) => {
        const color: string = getStatusColor(item.StatusCode);
        return (
          <span
            style={{
              display: 'inline-block',
              padding: '2px 8px',
              borderRadius: 4,
              fontSize: 11,
              fontWeight: 600,
              color: '#fff',
              background: color,
            }}
          >
            {item.StatusCode}
          </span>
        );
      },
      sortValue: (item) => item.StatusCode,
      filterValue: (item) => String(item.StatusCode),
    },
    {
      key: 'DurationMs',
      label: 'Duration',
      width: '90px',
      render: (item) => `${item.DurationMs} ms`,
      sortValue: (item) => item.DurationMs,
      filterValue: (item) => `${item.DurationMs} ms`,
    },
    {
      key: 'SourceIp',
      label: 'Source IP',
      width: '120px',
      filterValue: (item) => item.SourceIp || '',
    },
    {
      key: 'actions',
      label: 'Actions',
      width: '80px',
      isAction: true,
      sortable: false,
      filterable: false,
      render: (item) => {
        const dropdownKey: string = item.GUID || '';
        const isOpen: boolean = openDropdownKey === dropdownKey;

        const menuItems: MenuProps['items'] = [
          {
            key: 'view',
            icon: <EyeOutlined />,
            label: 'View Details',
            onClick: () => {
              setOpenDropdownKey(null);
              handleViewDetail(item);
            },
          },
          { type: 'divider' },
          {
            key: 'delete',
            icon: <DeleteOutlined />,
            label: 'Delete',
            danger: true,
            onClick: () => {
              setOpenDropdownKey(null);
              handleDeleteClick(item);
            },
          },
        ];

        return (
          <Less3Dropdown
            menu={{ items: menuItems }}
            trigger={['click']}
            open={isOpen}
            onOpenChange={(open) => {
              setOpenDropdownKey(open ? dropdownKey : null);
            }}
          >
            <Less3Button type="text" icon={<MoreOutlined />} size="small" />
          </Less3Dropdown>
        );
      },
    },
  ];

  return (
    <PageContainer
      pageTitle="Request History"
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Button icon={<ReloadOutlined />} onClick={() => refetch()} loading={isLoading}>
            Refresh
          </Less3Button>
        </Less3Flex>
      }
    >
      <SummaryChart
        summary={summaryData || null}
        timeRange={timeRange}
        onTimeRangeChange={setTimeRange}
        loading={isSummaryLoading}
        onRefresh={refetchSummary}
      />

      <Less3Flex gap={10} align="center" style={{ marginBottom: 16 }}>
        <Less3Select
          options={METHOD_OPTIONS}
          value={methodFilter}
          onChange={(value) => setMethodFilter(value as string)}
          style={{ width: 150 }}
          placeholder="HTTP Method"
        />
        <Less3Input
          placeholder="Status Code"
          prefix={<SearchOutlined />}
          value={statusFilter}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setStatusFilter(e.target.value)}
          style={{ width: 140 }}
          allowClear
        />
        <Less3Input
          placeholder="Source IP"
          prefix={<SearchOutlined />}
          value={sourceIpFilter}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSourceIpFilter(e.target.value)}
          style={{ width: 180 }}
          allowClear
        />
      </Less3Flex>

      <DataTable
        columns={columns}
        data={filteredData}
        loading={isLoading}
        rowKey="GUID"
      />

      {/* Detail Modal - Verbex-style fullscreen */}
      <Less3Modal
        title="Request Detail"
        open={isDetailModalVisible}
        onCancel={() => {
          setIsDetailModalVisible(false);
          setSelectedEntry(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsDetailModalVisible(false);
              setSelectedEntry(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width="90vw"
        style={{ top: 20 }}
        keyboard={true}
      >
        {selectedEntry && (
          <Less3Flex vertical gap={16} style={{ minHeight: 'calc(80vh - 120px)' }}>
            {/* Full URL with copy */}
            <Less3Flex align="center" gap={8}>
              <span
                style={{
                  flex: 1,
                  fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
                  fontSize: 13,
                  wordBreak: 'break-all',
                  padding: '10px 14px',
                  background: 'var(--ant-color-bg-layout)',
                  border: '1px solid var(--color-separator)',
                  borderRadius: 8,
                  lineHeight: 1.5,
                }}
              >
                {selectedEntry.RequestUrl}
              </span>
              <CopyIcon text={selectedEntry.RequestUrl} />
            </Less3Flex>

            {/* Summary Header - 4 cards in a row */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
              {[
                { label: 'Entry ID', value: selectedEntry.GUID, mono: true, copyable: true },
                { label: 'Route', value: selectedEntry.RequestUrl, verb: selectedEntry.HttpMethod },
                { label: 'Source IP', value: selectedEntry.SourceIp || 'Unknown' },
                { label: 'Status', value: String(selectedEntry.StatusCode), badge: true },
              ].map((item: { label: string; value: string; mono?: boolean; badge?: boolean; copyable?: boolean; verb?: string }) => (
                <div
                  key={item.label}
                  style={{
                    padding: 14,
                    border: '1px solid var(--color-separator)',
                    borderRadius: 8,
                    background: 'var(--ant-color-bg-layout)',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: 6,
                  }}
                >
                  <Less3Text type="secondary" fontSize={12}>{item.label}</Less3Text>
                  {item.badge ? (
                    <span
                      style={{
                        display: 'inline-block',
                        width: 'fit-content',
                        padding: '2px 10px',
                        borderRadius: 4,
                        fontSize: 13,
                        fontWeight: 600,
                        color: '#fff',
                        background: getStatusColor(selectedEntry.StatusCode),
                      }}
                    >
                      {item.value}
                    </span>
                  ) : (
                    <Less3Flex align="center" gap={6} style={{ flexWrap: 'wrap' }}>
                      {item.verb && (
                        <span
                          style={{
                            display: 'inline-block',
                            padding: '2px 8px',
                            borderRadius: 4,
                            fontSize: 11,
                            fontWeight: 600,
                            color: '#fff',
                            background: METHOD_COLORS[item.verb] || '#8c8c8c',
                            flexShrink: 0,
                          }}
                        >
                          {item.verb}
                        </span>
                      )}
                      <Less3Text
                        weight={600}
                        fontSize={13}
                        style={{
                          wordBreak: 'break-all',
                          fontFamily: item.mono
                            ? "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace"
                            : undefined,
                        }}
                      >
                        {item.value}
                      </Less3Text>
                      {item.copyable && <CopyIcon text={item.value} />}
                    </Less3Flex>
                  )}
                </div>
              ))}
            </div>

            {/* Detail Grid - 4 columns */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16 }}>
              <DetailBlock title="Request Info" value={JSON.stringify({
                HttpMethod: selectedEntry.HttpMethod,
                RequestType: selectedEntry.RequestType || null,
                ContentType: selectedEntry.RequestContentType || null,
                BodyLength: selectedEntry.RequestBodyLength,
                Duration: `${selectedEntry.DurationMs} ms`,
              }, null, 2)} />
              <DetailBlock title="Response Info" value={JSON.stringify({
                StatusCode: selectedEntry.StatusCode,
                Success: selectedEntry.Success,
                ContentType: selectedEntry.ResponseContentType || null,
                BodyLength: selectedEntry.ResponseBodyLength,
              }, null, 2)} />
              <DetailBlock title="Authentication" value={JSON.stringify({
                UserGUID: selectedEntry.UserGUID || null,
                AccessKey: selectedEntry.AccessKey || null,
              }, null, 2)} />
              <DetailBlock title="Metadata" value={JSON.stringify({
                GUID: selectedEntry.GUID,
                CreatedUtc: selectedEntry.CreatedUtc,
                SourceIp: selectedEntry.SourceIp,
              }, null, 2)} />
            </div>

            {/* Request Body */}
            <DetailBlock
              title="Request Body"
              value={selectedEntry.RequestBody || '(empty)'}
            />

            {/* Response Body */}
            <DetailBlock
              title="Response Body"
              value={selectedEntry.ResponseBody || '(empty)'}
            />
          </Less3Flex>
        )}
      </Less3Modal>

      {/* Delete Confirmation Modal */}
      <Less3Modal
        title="Delete Request History Entry"
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingEntry(null);
        }}
        confirmLoading={isDeleting}
        centered
        keyboard={true}
        footer={[
          <Less3Button key="confirm" type="primary" danger loading={isDeleting} onClick={handleDeleteConfirm}>
            Delete
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          <p>
            Are you sure you want to delete this request history entry?
          </p>
          {deletingEntry && (
            <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
              {deletingEntry.HttpMethod} {deletingEntry.RequestUrl} - {formatDate(deletingEntry.CreatedUtc)}
            </p>
          )}
        </Less3Flex>
      </Less3Modal>
    </PageContainer>
  );
};

export default RequestHistoryPage;
