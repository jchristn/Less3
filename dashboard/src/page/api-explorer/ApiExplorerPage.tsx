/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useRef, useCallback, useMemo } from 'react';
import { message } from 'antd';
import {
  SendOutlined,
  CopyOutlined,
  CloseOutlined,
  HistoryOutlined,
  LoadingOutlined,
} from '@ant-design/icons';
import Less3Button from '#/components/base/button/Button';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import Less3Card from '#/components/base/card/Card';
import Less3Tabs from '#/components/base/tabs/Tabs';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import { getApiEndpoint } from '#/services/sdk.service';
import { API_KEY } from '#/constants/config';
import { copyToClipboard } from '#/utils/clipboardUtils';

// ── Operation definitions ────────────────────────────────────────────

interface OperationParam {
  name: string;
  label: string;
  placeholder: string;
  required?: boolean;
}

interface ApiOperation {
  id: string;
  group: string;
  label: string;
  method: string;
  pathTemplate: string;
  params: OperationParam[];
  hasBody?: boolean;
  bodyPlaceholder?: string;
}

const S3_OPERATIONS: ApiOperation[] = [
  // Service
  { id: 's3-list-buckets', group: 'Service', label: 'List Buckets', method: 'GET', pathTemplate: '/', params: [] },
  // Bucket
  { id: 's3-create-bucket', group: 'Bucket', label: 'Create Bucket', method: 'PUT', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-delete-bucket', group: 'Bucket', label: 'Delete Bucket', method: 'DELETE', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-head-bucket', group: 'Bucket', label: 'Head Bucket', method: 'HEAD', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-list-objects', group: 'Bucket', label: 'List Objects', method: 'GET', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-tags', group: 'Bucket', label: 'Get Bucket Tags', method: 'GET', pathTemplate: '/{bucket}?tagging', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-acl', group: 'Bucket', label: 'Get Bucket ACL', method: 'GET', pathTemplate: '/{bucket}?acl', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-versioning', group: 'Bucket', label: 'Get Bucket Versioning', method: 'GET', pathTemplate: '/{bucket}?versioning', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  // Object
  { id: 's3-get-object', group: 'Object', label: 'Get Object', method: 'GET', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-put-object', group: 'Object', label: 'Put Object', method: 'PUT', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }], hasBody: true, bodyPlaceholder: 'Object content...' },
  { id: 's3-delete-object', group: 'Object', label: 'Delete Object', method: 'DELETE', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-head-object', group: 'Object', label: 'Head Object', method: 'HEAD', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-get-object-tags', group: 'Object', label: 'Get Object Tags', method: 'GET', pathTemplate: '/{bucket}/{key}?tagging', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-get-object-acl', group: 'Object', label: 'Get Object ACL', method: 'GET', pathTemplate: '/{bucket}/{key}?acl', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
];

const ADMIN_OPERATIONS: ApiOperation[] = [
  // Buckets
  { id: 'admin-list-buckets', group: 'Buckets', label: 'List Buckets', method: 'GET', pathTemplate: '/admin/buckets', params: [] },
  { id: 'admin-get-bucket', group: 'Buckets', label: 'Get Bucket', method: 'GET', pathTemplate: '/admin/buckets/{guid}', params: [{ name: 'guid', label: 'Bucket GUID', placeholder: 'bucket-guid', required: true }] },
  { id: 'admin-create-bucket', group: 'Buckets', label: 'Create Bucket', method: 'POST', pathTemplate: '/admin/buckets', params: [], hasBody: true, bodyPlaceholder: '{\n  "Name": "my-bucket"\n}' },
  { id: 'admin-delete-bucket', group: 'Buckets', label: 'Delete Bucket', method: 'DELETE', pathTemplate: '/admin/buckets/{guid}', params: [{ name: 'guid', label: 'Bucket GUID', placeholder: 'bucket-guid', required: true }] },
  // Users
  { id: 'admin-list-users', group: 'Users', label: 'List Users', method: 'GET', pathTemplate: '/admin/users', params: [] },
  { id: 'admin-get-user', group: 'Users', label: 'Get User', method: 'GET', pathTemplate: '/admin/users/{guid}', params: [{ name: 'guid', label: 'User GUID', placeholder: 'user-guid', required: true }] },
  { id: 'admin-create-user', group: 'Users', label: 'Create User', method: 'POST', pathTemplate: '/admin/users', params: [], hasBody: true, bodyPlaceholder: '{\n  "Name": "username",\n  "Email": "user@example.com"\n}' },
  { id: 'admin-delete-user', group: 'Users', label: 'Delete User', method: 'DELETE', pathTemplate: '/admin/users/{guid}', params: [{ name: 'guid', label: 'User GUID', placeholder: 'user-guid', required: true }] },
  // Credentials
  { id: 'admin-list-credentials', group: 'Credentials', label: 'List Credentials', method: 'GET', pathTemplate: '/admin/credentials', params: [] },
  { id: 'admin-get-credential', group: 'Credentials', label: 'Get Credential', method: 'GET', pathTemplate: '/admin/credentials/{guid}', params: [{ name: 'guid', label: 'Credential GUID', placeholder: 'credential-guid', required: true }] },
  { id: 'admin-create-credential', group: 'Credentials', label: 'Create Credential', method: 'POST', pathTemplate: '/admin/credentials', params: [], hasBody: true, bodyPlaceholder: '{\n  "UserGUID": "user-guid",\n  "Description": "My key",\n  "AccessKey": "mykey",\n  "SecretKey": "mysecret"\n}' },
  { id: 'admin-delete-credential', group: 'Credentials', label: 'Delete Credential', method: 'DELETE', pathTemplate: '/admin/credentials/{guid}', params: [{ name: 'guid', label: 'Credential GUID', placeholder: 'credential-guid', required: true }] },
  // Request History
  { id: 'admin-list-history', group: 'Request History', label: 'List Request History', method: 'GET', pathTemplate: '/admin/requesthistory', params: [] },
  { id: 'admin-get-history', group: 'Request History', label: 'Get Request History Entry', method: 'GET', pathTemplate: '/admin/requesthistory/{guid}', params: [{ name: 'guid', label: 'Entry GUID', placeholder: 'entry-guid', required: true }] },
  { id: 'admin-get-history-summary', group: 'Request History', label: 'Get Summary', method: 'GET', pathTemplate: '/admin/requesthistory/summary', params: [] },
  { id: 'admin-delete-history', group: 'Request History', label: 'Delete Request History Entry', method: 'DELETE', pathTemplate: '/admin/requesthistory/{guid}', params: [{ name: 'guid', label: 'Entry GUID', placeholder: 'entry-guid', required: true }] },
];

// ── Helpers ──────────────────────────────────────────────────────────

const METHOD_COLORS: Record<string, string> = {
  GET: '#22AF79',
  POST: '#1890ff',
  PUT: '#fa8c16',
  DELETE: '#d9383a',
  HEAD: '#722ed1',
};

const RECENT_REQUESTS_KEY = 'less3_api_explorer_recent';
const MAX_RECENT_ITEMS = 12;

interface RecentRequest {
  operationId: string;
  method: string;
  url: string;
  apiType: string;
  statusCode: number | null;
  timestamp: string;
  body: string;
}

interface ResponseData {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  body: string;
  durationMs: number;
  size: number;
}

const getStatusColor = (code: number): string => {
  if (code >= 200 && code < 300) return '#22AF79';
  if (code >= 400 && code < 500) return '#fa8c16';
  if (code >= 500) return '#d9383a';
  return '#8c8c8c';
};

const formatJson = (text: string): string => {
  try {
    const parsed: unknown = JSON.parse(text);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return text;
  }
};

const loadRecentRequests = (): RecentRequest[] => {
  try {
    const stored: string | null = localStorage.getItem(RECENT_REQUESTS_KEY);
    if (stored) {
      const parsed: unknown = JSON.parse(stored);
      if (Array.isArray(parsed)) return parsed as RecentRequest[];
    }
  } catch { /* ignore */ }
  return [];
};

const saveRecentRequests = (requests: RecentRequest[]): void => {
  try {
    localStorage.setItem(RECENT_REQUESTS_KEY, JSON.stringify(requests.slice(0, MAX_RECENT_ITEMS)));
  } catch { /* ignore */ }
};

const inputStyle: React.CSSProperties = {
  border: '1px solid var(--color-separator)',
  borderRadius: 6,
  background: 'var(--ant-color-bg-container)',
  color: 'var(--ant-color-text)',
};

// ── Component ────────────────────────────────────────────────────────

const ApiExplorerPage: React.FC = () => {
  const [apiType, setApiType] = useState<string>('admin');
  const [selectedOpId, setSelectedOpId] = useState<string>('admin-list-buckets');
  const [paramValues, setParamValues] = useState<Record<string, string>>({});
  const [body, setBody] = useState<string>('');
  const [response, setResponse] = useState<ResponseData | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeResponseTab, setActiveResponseTab] = useState<string>('body');
  const [recentRequests, setRecentRequests] = useState<RecentRequest[]>(() => loadRecentRequests());

  const abortControllerRef = useRef<AbortController | null>(null);

  const operations: ApiOperation[] = apiType === 's3' ? S3_OPERATIONS : ADMIN_OPERATIONS;

  const operationOptions = useMemo(() => {
    return operations.map((op) => ({
      label: `[${op.group}] ${op.method} — ${op.label}`,
      value: op.id,
    }));
  }, [operations]);

  const selectedOp: ApiOperation | undefined = operations.find((op) => op.id === selectedOpId);

  const resolvedUrl: string = useMemo(() => {
    if (!selectedOp) return '';
    const baseUrl: string = getApiEndpoint().replace(/\/$/, '');
    let path: string = selectedOp.pathTemplate;
    for (const param of selectedOp.params) {
      const val: string = paramValues[param.name] || `{${param.name}}`;
      path = path.replace(`{${param.name}}`, encodeURIComponent(val));
    }
    return baseUrl + path;
  }, [selectedOp, paramValues]);

  const handleApiTypeChange = useCallback((value: string) => {
    setApiType(value);
    setSelectedOpId(value === 's3' ? 's3-list-buckets' : 'admin-list-buckets');
    setParamValues({});
    setBody('');
    setResponse(null);
  }, []);

  const handleOperationChange = useCallback((value: string) => {
    setSelectedOpId(value);
    setParamValues({});
    setResponse(null);
    const op: ApiOperation | undefined = [...S3_OPERATIONS, ...ADMIN_OPERATIONS].find((o) => o.id === value);
    setBody(op?.bodyPlaceholder || '');
  }, []);

  const handleCancel = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
      setIsLoading(false);
    }
  }, []);

  const handleSend = useCallback(async () => {
    if (!selectedOp) return;

    handleCancel();
    const controller: AbortController = new AbortController();
    abortControllerRef.current = controller;
    setIsLoading(true);
    setResponse(null);

    const startTime: number = performance.now();

    try {
      const fetchHeaders: Record<string, string> = {};
      if (apiType === 'admin') {
        fetchHeaders['x-api-key'] = API_KEY;
      }
      if (selectedOp.hasBody) {
        fetchHeaders['Content-Type'] = 'application/json';
      }

      const fetchOptions: RequestInit = {
        method: selectedOp.method,
        headers: fetchHeaders,
        signal: controller.signal,
      };

      if (body.trim() && selectedOp.hasBody) {
        fetchOptions.body = body;
      }

      const res: Response = await fetch(resolvedUrl, fetchOptions);
      const endTime: number = performance.now();
      const durationMs: number = Math.round(endTime - startTime);

      let responseBody: string = '';
      try { responseBody = await res.text(); } catch { /* empty */ }

      const responseHeaders: Record<string, string> = {};
      res.headers.forEach((val, key) => { responseHeaders[key] = val; });

      const responseData: ResponseData = {
        status: res.status,
        statusText: res.statusText,
        headers: responseHeaders,
        body: responseBody,
        durationMs: durationMs,
        size: new Blob([responseBody]).size,
      };
      setResponse(responseData);

      const recentEntry: RecentRequest = {
        operationId: selectedOp.id,
        method: selectedOp.method,
        url: resolvedUrl,
        apiType: apiType,
        statusCode: res.status,
        timestamp: new Date().toISOString(),
        body: body,
      };
      const updatedRecent: RecentRequest[] = [recentEntry, ...recentRequests].slice(0, MAX_RECENT_ITEMS);
      setRecentRequests(updatedRecent);
      saveRecentRequests(updatedRecent);
    } catch (error: any) {
      if (error?.name === 'AbortError') {
        message.info('Request cancelled');
      } else {
        message.error(error?.message || 'Request failed');
        setResponse({ status: 0, statusText: 'Error', headers: {}, body: error?.message || 'Network error', durationMs: Math.round(performance.now() - startTime), size: 0 });
      }
    } finally {
      setIsLoading(false);
      abortControllerRef.current = null;
    }
  }, [selectedOp, resolvedUrl, body, apiType, recentRequests, handleCancel]);

  const handleLoadRecent = useCallback((recent: RecentRequest) => {
    setApiType(recent.apiType);
    setSelectedOpId(recent.operationId);
    setBody(recent.body || '');
  }, []);

  const formatResponseSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const curlCommand: string = useMemo(() => {
    if (!selectedOp) return '';
    let curl: string = `curl -X ${selectedOp.method}`;
    if (apiType === 'admin') curl += ` \\\n  -H 'x-api-key: ${API_KEY}'`;
    if (selectedOp.hasBody) curl += ` \\\n  -H 'Content-Type: application/json'`;
    if (body.trim() && selectedOp.hasBody) curl += ` \\\n  -d '${body}'`;
    curl += ` \\\n  '${resolvedUrl}'`;
    return curl;
  }, [selectedOp, resolvedUrl, body, apiType]);

  const handleCopy = useCallback((text: string) => {
    copyToClipboard(text);
    message.success('Copied to clipboard');
  }, []);

  const responseTabs = [
    {
      key: 'body',
      label: 'Body',
      children: (
        <div style={{ position: 'relative' }}>
          <Less3Button type="text" icon={<CopyOutlined />} size="small" style={{ position: 'absolute', top: 0, right: 0, zIndex: 1 }} onClick={() => handleCopy(response?.body || '')} />
          <pre style={{ padding: 12, fontSize: 12, maxHeight: 400, overflow: 'auto', whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 0, background: 'var(--ant-color-bg-layout)', borderRadius: 6, border: '1px solid var(--color-separator)', fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace" }}>
            {response ? formatJson(response.body) : 'No response yet'}
          </pre>
        </div>
      ),
    },
    {
      key: 'headers',
      label: 'Headers',
      children: (
        <div style={{ position: 'relative' }}>
          <Less3Button type="text" icon={<CopyOutlined />} size="small" style={{ position: 'absolute', top: 0, right: 0, zIndex: 1 }} onClick={() => handleCopy(response ? Object.entries(response.headers).map(([k, v]) => `${k}: ${v}`).join('\n') : '')} />
          {response && Object.keys(response.headers).length > 0 ? (
            <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th style={{ textAlign: 'left', padding: '6px 8px', borderBottom: '1px solid var(--color-separator)', fontWeight: 600 }}>Header</th>
                  <th style={{ textAlign: 'left', padding: '6px 8px', borderBottom: '1px solid var(--color-separator)', fontWeight: 600 }}>Value</th>
                </tr>
              </thead>
              <tbody>
                {Object.entries(response.headers).map(([key, value]) => (
                  <tr key={key}>
                    <td style={{ padding: '4px 8px', borderBottom: '1px solid var(--color-separator)', fontWeight: 500 }}>{key}</td>
                    <td style={{ padding: '4px 8px', borderBottom: '1px solid var(--color-separator)', wordBreak: 'break-all' }}>{value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <Less3Text type="secondary">No headers</Less3Text>
          )}
        </div>
      ),
    },
    {
      key: 'curl',
      label: 'cURL',
      children: (
        <div style={{ position: 'relative' }}>
          <Less3Button type="text" icon={<CopyOutlined />} size="small" style={{ position: 'absolute', top: 0, right: 0, zIndex: 1 }} onClick={() => handleCopy(curlCommand)} />
          <pre style={{ padding: 12, fontSize: 12, maxHeight: 400, overflow: 'auto', whiteSpace: 'pre-wrap', wordBreak: 'break-word', margin: 0, background: 'var(--ant-color-bg-layout)', borderRadius: 6, border: '1px solid var(--color-separator)', fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace" }}>
            {curlCommand}
          </pre>
        </div>
      ),
    },
  ];

  return (
    <PageContainer pageTitle="API Explorer">
      <Less3Flex gap={16} style={{ flexWrap: 'wrap' }}>
        {/* Left Panel - Request Builder */}
        <div style={{ flex: 1, minWidth: 400 }}>
          <Less3Card title="Request" style={{ marginBottom: 16 }}>
            <Less3Flex vertical gap={14}>
              {/* API Type */}
              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>API Type</Less3Text>
                <Less3Select
                  options={[{ label: 'S3 API', value: 's3' }, { label: 'Admin API', value: 'admin' }]}
                  value={apiType}
                  onChange={(value) => handleApiTypeChange(value as string)}
                  style={{ width: '100%', ...inputStyle }}
                />
              </div>

              {/* Operation */}
              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Operation</Less3Text>
                <Less3Select
                  options={operationOptions}
                  value={selectedOpId}
                  onChange={(value) => handleOperationChange(value as string)}
                  style={{ width: '100%', ...inputStyle }}
                  showSearch
                  filterOption={(input, option) =>
                    (option?.label as string)?.toLowerCase().includes((input as string).toLowerCase())
                  }
                />
              </div>

              {/* Method + URL (read-only) */}
              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Request</Less3Text>
                <Less3Flex gap={8} align="center">
                  <span style={{
                    display: 'inline-block', padding: '4px 10px', borderRadius: 4, fontSize: 12, fontWeight: 700,
                    color: '#fff', background: METHOD_COLORS[selectedOp?.method || 'GET'] || '#8c8c8c', minWidth: 55, textAlign: 'center',
                  }}>
                    {selectedOp?.method || 'GET'}
                  </span>
                  <div style={{
                    flex: 1, padding: '6px 10px', borderRadius: 6, fontSize: 13,
                    fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
                    background: 'var(--ant-color-bg-layout)', border: '1px solid var(--color-separator)',
                    color: 'var(--ant-color-text)', wordBreak: 'break-all',
                  }}>
                    {resolvedUrl || '—'}
                  </div>
                </Less3Flex>
              </div>

              {/* Parameters */}
              {selectedOp && selectedOp.params.length > 0 && (
                <div>
                  <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 6 }}>Parameters</Less3Text>
                  <Less3Flex vertical gap={8}>
                    {selectedOp.params.map((param) => (
                      <Less3Flex key={param.name} gap={8} align="center">
                        <Less3Text fontSize={12} style={{ minWidth: 100 }}>{param.label}{param.required ? ' *' : ''}</Less3Text>
                        <Less3Input
                          value={paramValues[param.name] || ''}
                          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                            setParamValues((prev) => ({ ...prev, [param.name]: e.target.value }))
                          }
                          placeholder={param.placeholder}
                          style={{ flex: 1, ...inputStyle }}
                          size="small"
                        />
                      </Less3Flex>
                    ))}
                  </Less3Flex>
                </div>
              )}

              {/* Request Body */}
              {selectedOp?.hasBody && (
                <div>
                  <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Body</Less3Text>
                  <textarea
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    placeholder={selectedOp.bodyPlaceholder || 'Request body (JSON)'}
                    rows={6}
                    style={{
                      width: '100%',
                      fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
                      fontSize: 12, padding: 10, borderRadius: 6,
                      border: '1px solid var(--color-separator)',
                      resize: 'vertical', background: 'var(--ant-color-bg-container)',
                      color: 'var(--ant-color-text)', boxSizing: 'border-box',
                      outline: 'none', lineHeight: 1.6,
                    }}
                  />
                </div>
              )}

              {/* Send / Cancel */}
              <Less3Flex gap={8}>
                <Less3Button
                  type="primary"
                  icon={isLoading ? <LoadingOutlined /> : <SendOutlined />}
                  onClick={handleSend}
                  loading={isLoading}
                >
                  Send
                </Less3Button>
                {isLoading && (
                  <Less3Button icon={<CloseOutlined />} onClick={handleCancel}>Cancel</Less3Button>
                )}
              </Less3Flex>
            </Less3Flex>
          </Less3Card>

          {/* Recent Requests */}
          {recentRequests.length > 0 && (
            <Less3Card
              title={<Less3Flex align="center" gap={6}><HistoryOutlined /><span>Recent Requests</span></Less3Flex>}
              size="small"
            >
              <Less3Flex vertical gap={4}>
                {recentRequests.map((recent, index) => {
                  const methodColor: string = METHOD_COLORS[recent.method] || '#8c8c8c';
                  const statusColor: string = recent.statusCode ? getStatusColor(recent.statusCode) : '#8c8c8c';
                  return (
                    <div
                      key={index}
                      onClick={() => handleLoadRecent(recent)}
                      style={{ padding: '6px 10px', borderRadius: 4, cursor: 'pointer', border: '1px solid var(--color-separator)', fontSize: 12 }}
                    >
                      <Less3Flex gap={8} align="center">
                        <span style={{ display: 'inline-block', padding: '1px 6px', borderRadius: 3, fontSize: 10, fontWeight: 600, color: '#fff', background: methodColor, minWidth: 42, textAlign: 'center' }}>
                          {recent.method}
                        </span>
                        <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{recent.url}</span>
                        {recent.statusCode !== null && (
                          <span style={{ display: 'inline-block', padding: '1px 6px', borderRadius: 3, fontSize: 10, fontWeight: 600, color: '#fff', background: statusColor }}>{recent.statusCode}</span>
                        )}
                        <Less3Text type="secondary" fontSize={10}>{new Date(recent.timestamp).toLocaleTimeString()}</Less3Text>
                      </Less3Flex>
                    </div>
                  );
                })}
              </Less3Flex>
            </Less3Card>
          )}
        </div>

        {/* Right Panel - Response */}
        <div style={{ flex: 1, minWidth: 400 }}>
          <Less3Card title="Response" style={{ marginBottom: 16 }}>
            {response ? (
              <Less3Flex vertical gap={12}>
                <Less3Flex gap={12} align="center">
                  <span style={{ display: 'inline-block', padding: '3px 10px', borderRadius: 4, fontSize: 12, fontWeight: 600, color: '#fff', background: getStatusColor(response.status) }}>
                    {response.status} {response.statusText}
                  </span>
                  <Less3Text type="secondary" fontSize={12}>{response.durationMs} ms</Less3Text>
                  <Less3Text type="secondary" fontSize={12}>{formatResponseSize(response.size)}</Less3Text>
                </Less3Flex>
                <Less3Tabs activeKey={activeResponseTab} onChange={setActiveResponseTab} items={responseTabs} size="small" />
              </Less3Flex>
            ) : (
              <div style={{ textAlign: 'center', padding: '60px 0' }}>
                <Less3Text type="secondary">{isLoading ? 'Sending request...' : 'Send a request to see the response'}</Less3Text>
              </div>
            )}
          </Less3Card>
        </div>
      </Less3Flex>
    </PageContainer>
  );
};

export default ApiExplorerPage;
