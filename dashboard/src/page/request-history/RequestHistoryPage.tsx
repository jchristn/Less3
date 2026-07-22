/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState, useCallback, useEffect } from 'react';
import { MenuProps } from 'antd';
import {
  SearchOutlined,
  MoreOutlined,
  DeleteOutlined,
  EyeOutlined,
  ReloadOutlined,
  DownloadOutlined,
  CopyOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
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
import { formatDate } from '#/utils/dateUtils';
import { getPrettyPrintedTextContent } from '#/utils/objectContentUtils';
import { message } from '#/utils/message';
import SummaryChart, { getQuickRange } from './SummaryChart';

const METHOD_OPTIONS = [
  { label: 'All Methods', value: '' },
  { label: 'GET', value: 'GET' },
  { label: 'PUT', value: 'PUT' },
  { label: 'POST', value: 'POST' },
  { label: 'DELETE', value: 'DELETE' },
  { label: 'HEAD', value: 'HEAD' },
];

const STATUS_FAMILY_OPTIONS = [
  { label: 'All Families', value: '' },
  { label: '2xx', value: '2' },
  { label: '3xx', value: '3' },
  { label: '4xx', value: '4' },
  { label: '5xx', value: '5' },
];

const SAVED_FILTERS_KEY = 'less3_request_history_saved_filters';

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

const formatDurationMs = (value: number): string => `${value.toFixed(2)}ms`;

const createCurlCommand = (entry: RequestHistoryEntry): string => {
  const method = entry.HttpMethod || 'GET';
  const url = entry.RequestUrl || '/';
  let command = `curl -X ${method} '${url}'`;

  if (entry.RequestContentType) {
    command += ` \\\n  -H 'Content-Type: ${entry.RequestContentType}'`;
  }

  if (entry.RequestBody) {
    command += ` \\\n  -d '${entry.RequestBody.replace(/'/g, "'\\''")}'`;
  }

  return command;
};

const csvEscape = (value: unknown): string => `"${String(value ?? '').replace(/"/g, '""')}"`;

interface DetailBlockProps {
  title: string;
  value: string;
  contentType?: string;
  allowPrettyPrint?: boolean;
}

const DetailBlock: React.FC<DetailBlockProps> = ({
  title,
  value,
  contentType,
  allowPrettyPrint = false,
}) => {
  const [isPrettyPrintEnabled, setIsPrettyPrintEnabled] = useState(false);

  const prettyPrintedValue = useMemo(() => {
    if (!allowPrettyPrint) {
      return null;
    }

    return getPrettyPrintedTextContent(value, contentType);
  }, [allowPrettyPrint, contentType, value]);

  const canPrettyPrint = Boolean(prettyPrintedValue);
  const displayedValue = value
    ? (isPrettyPrintEnabled && prettyPrintedValue ? prettyPrintedValue : value)
    : '(empty)';

  React.useEffect(() => {
    setIsPrettyPrintEnabled(false);
  }, [contentType, value]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
      <Less3Flex justify="space-between" align="center">
        <Less3Text weight={600} fontSize={13}>{title}</Less3Text>
        <Less3Flex gap={8} align="center">
          {canPrettyPrint && (
            <Less3Button size="small" onClick={() => setIsPrettyPrintEnabled((current) => !current)}>
              {isPrettyPrintEnabled ? 'Show Raw' : 'Pretty Print'}
            </Less3Button>
          )}
          <CopyToClipboard
            text={isPrettyPrintEnabled && prettyPrintedValue ? prettyPrintedValue : value}
            tooltip={`Copy ${title}`}
            ariaLabel={`Copy ${title}`}
          />
        </Less3Flex>
      </Less3Flex>
      {allowPrettyPrint && value && (
        <Less3Text type="secondary" fontSize={12}>
          {isPrettyPrintEnabled && canPrettyPrint ? 'Pretty-printed content' : 'Raw content'}
        </Less3Text>
      )}
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
        {displayedValue}
      </pre>
    </div>
  );
};

const RequestHistoryPage: React.FC = () => {
  const [methodFilter, setMethodFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [statusFamilyFilter, setStatusFamilyFilter] = useState<string>('');
  const [sourceIpFilter, setSourceIpFilter] = useState<string>('');
  const [slowRequestMs, setSlowRequestMs] = useState<string>('');
  const [failedOnly, setFailedOnly] = useState(false);
  const [groupedView, setGroupedView] = useState(false);
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

  useEffect(() => {
    try {
      const stored = localStorage.getItem(SAVED_FILTERS_KEY);
      if (!stored) return;

      const parsed = JSON.parse(stored);
      setMethodFilter(typeof parsed.methodFilter === 'string' ? parsed.methodFilter : '');
      setStatusFilter(typeof parsed.statusFilter === 'string' ? parsed.statusFilter : '');
      setStatusFamilyFilter(typeof parsed.statusFamilyFilter === 'string' ? parsed.statusFamilyFilter : '');
      setSourceIpFilter(typeof parsed.sourceIpFilter === 'string' ? parsed.sourceIpFilter : '');
      setSlowRequestMs(typeof parsed.slowRequestMs === 'string' ? parsed.slowRequestMs : '');
      setFailedOnly(Boolean(parsed.failedOnly));
      setGroupedView(Boolean(parsed.groupedView));
    } catch {
      localStorage.removeItem(SAVED_FILTERS_KEY);
    }
  }, []);

  const handleViewDetail = useCallback((entry: RequestHistoryEntry) => {
    setSelectedEntry(entry);
    setIsDetailModalVisible(true);
  }, []);

  const handleDeleteClick = useCallback((entry: RequestHistoryEntry) => {
    setDeletingEntry(entry);
    setIsDeleteModalVisible(true);
  }, []);

  const handleDeleteConfirm = async () => {
    if (!deletingEntry?.Id) return;
    try {
      await deleteRequestHistory({ id: deletingEntry.Id }).unwrap();
      message.success('Request history entry deleted');
      setIsDeleteModalVisible(false);
      setDeletingEntry(null);
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to delete entry');
    }
  };

  const handleCopyCurl = useCallback(async (entry: RequestHistoryEntry) => {
    const command = createCurlCommand(entry);
    try {
      await navigator.clipboard.writeText(command);
      message.success('cURL command copied');
    } catch {
      message.error('Failed to copy cURL command');
    }
  }, []);

  const handleSaveFilters = useCallback(() => {
    const savedFilters = {
      methodFilter,
      statusFilter,
      statusFamilyFilter,
      sourceIpFilter,
      slowRequestMs,
      failedOnly,
      groupedView,
      savedUtc: new Date().toISOString(),
    };

    try {
      localStorage.setItem(SAVED_FILTERS_KEY, JSON.stringify(savedFilters));
      message.success('Filters saved');
    } catch {
      message.error('Failed to save filters');
    }
  }, [failedOnly, groupedView, methodFilter, slowRequestMs, sourceIpFilter, statusFamilyFilter, statusFilter]);

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

    if (statusFamilyFilter) {
      result = result.filter((entry) => String(entry.StatusCode).startsWith(statusFamilyFilter));
    }

    if (sourceIpFilter.trim()) {
      const ipStr: string = sourceIpFilter.trim().toLowerCase();
      result = result.filter((entry) => (entry.SourceIp || '').toLowerCase().includes(ipStr));
    }

    if (failedOnly) {
      result = result.filter((entry) => !entry.Success || entry.StatusCode >= 400);
    }

    if (slowRequestMs.trim()) {
      const threshold = Number(slowRequestMs);
      if (!Number.isNaN(threshold)) {
        result = result.filter((entry) => entry.DurationMs >= threshold);
      }
    }

    return result;
  }, [data, failedOnly, methodFilter, slowRequestMs, sourceIpFilter, statusFamilyFilter, statusFilter]);

  const groupedRows = useMemo(() => {
    const groups = new Map<string, { Key: string; Count: number; Failures: number; AverageMs: number; TotalMs: number }>();

    filteredData.forEach((entry) => {
      const family = `${Math.floor(entry.StatusCode / 100)}xx`;
      const key = `${entry.HttpMethod || 'GET'} ${family}`;
      const existing = groups.get(key) || {
        Key: key,
        Count: 0,
        Failures: 0,
        AverageMs: 0,
        TotalMs: 0,
      };

      existing.Count += 1;
      existing.Failures += entry.StatusCode >= 400 ? 1 : 0;
      existing.TotalMs += entry.DurationMs;
      existing.AverageMs = existing.TotalMs / existing.Count;
      groups.set(key, existing);
    });

    return Array.from(groups.values()).sort((a, b) => b.Count - a.Count);
  }, [filteredData]);

  const handleExportCsv = useCallback(() => {
    const header = [
      'Id',
      'CreatedUtc',
      'HttpMethod',
      'RequestUrl',
      'StatusCode',
      'Success',
      'DurationMs',
      'SourceIp',
      'RequestType',
      'UserId',
      'AccessKey',
    ];

    const rows = filteredData.map((entry) => [
      entry.Id,
      entry.CreatedUtc,
      entry.HttpMethod,
      entry.RequestUrl,
      entry.StatusCode,
      entry.Success,
      entry.DurationMs,
      entry.SourceIp,
      entry.RequestType,
      entry.UserId,
      entry.AccessKey,
    ]);

    const csv = [header, ...rows]
      .map((row) => row.map(csvEscape).join(','))
      .join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `less3-request-history-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }, [filteredData]);

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
        const dropdownKey: string = item.Id || '';
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
          {
            key: 'copy-curl',
            icon: <CopyOutlined />,
            label: 'Copy as cURL',
            onClick: () => {
              setOpenDropdownKey(null);
              void handleCopyCurl(item);
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
            <Less3Button type="text" icon={<MoreOutlined />} size="small" onClick={(event) => event.stopPropagation()} />
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
          <Less3Button icon={<SaveOutlined />} onClick={handleSaveFilters}>
            Save Filters
          </Less3Button>
          <Less3Button icon={<DownloadOutlined />} onClick={handleExportCsv}>
            Export CSV
          </Less3Button>
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
        <Less3Select
          options={STATUS_FAMILY_OPTIONS}
          value={statusFamilyFilter}
          onChange={(value) => setStatusFamilyFilter(value as string)}
          style={{ width: 140 }}
          placeholder="Status Family"
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
        <Less3Input
          placeholder="Slow >= ms"
          prefix={<SearchOutlined />}
          value={slowRequestMs}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSlowRequestMs(e.target.value)}
          style={{ width: 140 }}
          allowClear
        />
        <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12 }}>
          <input type="checkbox" checked={failedOnly} onChange={(event) => setFailedOnly(event.target.checked)} />
          Failed only
        </label>
        <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12 }}>
          <input type="checkbox" checked={groupedView} onChange={(event) => setGroupedView(event.target.checked)} />
          Grouped
        </label>
      </Less3Flex>

      {groupedView && (
        <div style={{ marginBottom: 16 }}>
          <DataTable
            columns={[
              { key: 'Key', label: 'Group' },
              { key: 'Count', label: 'Requests', sortValue: (item) => item.Count },
              { key: 'Failures', label: 'Failures', sortValue: (item) => item.Failures },
              {
                key: 'AverageMs',
                label: 'Avg Duration',
                render: (item) => formatDurationMs(item.AverageMs),
                sortValue: (item) => item.AverageMs,
              },
            ]}
            data={groupedRows}
            loading={isLoading}
            rowKey="Key"
            hidePagination
          />
        </div>
      )}

      <DataTable
        columns={columns}
        data={filteredData}
        loading={isLoading}
        rowKey="Id"
        onRowClick={handleViewDetail}
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
              <CopyToClipboard text={selectedEntry.RequestUrl} tooltip="Copy URL" ariaLabel="Copy URL" />
              <Less3Button icon={<CopyOutlined />} onClick={() => void handleCopyCurl(selectedEntry)}>
                Copy as cURL
              </Less3Button>
            </Less3Flex>

            {/* Summary Header */}
            <div
              style={{
                padding: '4px 0 4px 16px',
              }}
            >
              <table
                style={{
                  width: '100%',
                  borderCollapse: 'collapse',
                  tableLayout: 'fixed',
                }}
              >
                <tbody>
                  {[
                    {
                      label: 'Entry ID',
                      value: selectedEntry.Id,
                      copyable: true,
                      mono: true,
                    },
                    {
                      label: 'Route',
                      value: `${selectedEntry.HttpMethod} ${selectedEntry.RequestUrl}`,
                    },
                    {
                      label: 'Source IP',
                      value: selectedEntry.SourceIp || 'Unknown',
                    },
                    {
                      label: 'Status',
                      value: String(selectedEntry.StatusCode),
                      accentColor: getStatusColor(selectedEntry.StatusCode),
                    },
                    {
                      label: 'Response Time',
                      value: formatDurationMs(selectedEntry.DurationMs),
                    },
                  ].map((item: {
                    label: string;
                    value: string;
                    copyable?: boolean;
                    mono?: boolean;
                    accentColor?: string;
                  }) => (
                    <tr key={item.label}>
                      <td
                        style={{
                          width: 140,
                          padding: '8px 12px 8px 0',
                          verticalAlign: 'top',
                        }}
                      >
                        <Less3Text type="secondary" fontSize={12}>
                          {item.label}
                        </Less3Text>
                      </td>
                      <td style={{ padding: '8px 0', verticalAlign: 'top' }}>
                        <Less3Flex align="center" gap={8} style={{ flexWrap: 'wrap' }}>
                          <Less3Text
                            weight={600}
                            fontSize={13}
                            style={{
                              wordBreak: 'break-all',
                              color: item.accentColor,
                              fontFamily: item.mono
                                ? "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace"
                                : undefined,
                            }}
                          >
                            {item.value}
                          </Less3Text>
                          {item.copyable && (
                            <CopyToClipboard
                              text={item.value}
                              tooltip={`Copy ${item.label}`}
                              ariaLabel={`Copy ${item.label}`}
                            />
                          )}
                        </Less3Flex>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Detail Grid - 4 columns */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16 }}>
              <DetailBlock title="Request Info" value={JSON.stringify({
                HttpMethod: selectedEntry.HttpMethod,
                RequestType: selectedEntry.RequestType || null,
                ContentType: selectedEntry.RequestContentType || null,
                BodyLength: selectedEntry.RequestBodyLength,
                Duration: formatDurationMs(selectedEntry.DurationMs),
              }, null, 2)} />
              <DetailBlock title="Response Info" value={JSON.stringify({
                StatusCode: selectedEntry.StatusCode,
                Success: selectedEntry.Success,
                ContentType: selectedEntry.ResponseContentType || null,
                BodyLength: selectedEntry.ResponseBodyLength,
              }, null, 2)} />
              <DetailBlock title="Authentication" value={JSON.stringify({
                UserId: selectedEntry.UserId || null,
                AccessKey: selectedEntry.AccessKey || null,
              }, null, 2)} />
              <DetailBlock title="Metadata" value={JSON.stringify({
                Id: selectedEntry.Id,
                CreatedUtc: selectedEntry.CreatedUtc,
                SourceIp: selectedEntry.SourceIp,
              }, null, 2)} />
            </div>

            {/* Request Body */}
            <DetailBlock
              title="Request Body"
              value={selectedEntry.RequestBody || ''}
              contentType={selectedEntry.RequestContentType}
              allowPrettyPrint
            />

            {/* Response Body */}
            <DetailBlock
              title="Response Body"
              value={selectedEntry.ResponseBody || ''}
              contentType={selectedEntry.ResponseContentType}
              allowPrettyPrint
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
