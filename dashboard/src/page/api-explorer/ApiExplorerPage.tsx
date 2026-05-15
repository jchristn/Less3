/* eslint-disable max-lines-per-function */
'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  SendOutlined,
  CloseOutlined,
  HistoryOutlined,
  LoadingOutlined,
} from '@ant-design/icons';
import Less3Button from '#/components/base/button/Button';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Input from '#/components/base/input/Input';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Select from '#/components/base/select/Select';
import Less3Tabs from '#/components/base/tabs/Tabs';
import Less3Text from '#/components/base/typograpghy/Text';
import CopyToClipboard from '#/components/copy-to-clipboard/CopyToClipboard';
import { API_KEY } from '#/constants/config';
import { getApiEndpoint } from '#/services/sdk.service';
import { useGetCredentialByIdQuery, useGetCredentialsQuery } from '#/store/slice/credentialsSlice';
import { getPrettyPrintedTextContent } from '#/utils/objectContentUtils';
import {
  buildS3AuthorizationHeader,
  buildSignedS3Headers,
  clearPreferredS3CredentialGuid,
  getPreferredS3CredentialGuid,
  setPreferredS3CredentialGuid,
} from '#/utils/s3Auth';
import { message } from '#/utils/message';

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
  { id: 's3-list-buckets', group: 'Service', label: 'List Buckets', method: 'GET', pathTemplate: '/', params: [] },
  { id: 's3-create-bucket', group: 'Bucket', label: 'Create Bucket', method: 'PUT', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-delete-bucket', group: 'Bucket', label: 'Delete Bucket', method: 'DELETE', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-head-bucket', group: 'Bucket', label: 'Head Bucket', method: 'HEAD', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-list-objects', group: 'Bucket', label: 'List Objects', method: 'GET', pathTemplate: '/{bucket}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-tags', group: 'Bucket', label: 'Get Bucket Tags', method: 'GET', pathTemplate: '/{bucket}?tagging', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-acl', group: 'Bucket', label: 'Get Bucket ACL', method: 'GET', pathTemplate: '/{bucket}?acl', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-bucket-versioning', group: 'Bucket', label: 'Get Bucket Versioning', method: 'GET', pathTemplate: '/{bucket}?versioning', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }] },
  { id: 's3-get-object', group: 'Object', label: 'Get Object', method: 'GET', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-put-object', group: 'Object', label: 'Put Object', method: 'PUT', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }], hasBody: true, bodyPlaceholder: 'Object content...' },
  { id: 's3-delete-object', group: 'Object', label: 'Delete Object', method: 'DELETE', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-head-object', group: 'Object', label: 'Head Object', method: 'HEAD', pathTemplate: '/{bucket}/{key}', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-get-object-tags', group: 'Object', label: 'Get Object Tags', method: 'GET', pathTemplate: '/{bucket}/{key}?tagging', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
  { id: 's3-get-object-acl', group: 'Object', label: 'Get Object ACL', method: 'GET', pathTemplate: '/{bucket}/{key}?acl', params: [{ name: 'bucket', label: 'Bucket Name', placeholder: 'my-bucket', required: true }, { name: 'key', label: 'Object Key', placeholder: 'path/to/file.txt', required: true }] },
];

const ADMIN_OPERATIONS: ApiOperation[] = [
  { id: 'admin-list-buckets', group: 'Buckets', label: 'List Buckets', method: 'GET', pathTemplate: '/admin/buckets', params: [] },
  { id: 'admin-get-bucket', group: 'Buckets', label: 'Get Bucket', method: 'GET', pathTemplate: '/admin/buckets/{guid}', params: [{ name: 'guid', label: 'Bucket GUID', placeholder: 'bucket-guid', required: true }] },
  { id: 'admin-create-bucket', group: 'Buckets', label: 'Create Bucket', method: 'POST', pathTemplate: '/admin/buckets', params: [], hasBody: true, bodyPlaceholder: '{\n  "Name": "my-bucket"\n}' },
  { id: 'admin-delete-bucket', group: 'Buckets', label: 'Delete Bucket', method: 'DELETE', pathTemplate: '/admin/buckets/{guid}', params: [{ name: 'guid', label: 'Bucket GUID', placeholder: 'bucket-guid', required: true }] },
  { id: 'admin-list-users', group: 'Users', label: 'List Users', method: 'GET', pathTemplate: '/admin/users', params: [] },
  { id: 'admin-get-user', group: 'Users', label: 'Get User', method: 'GET', pathTemplate: '/admin/users/{guid}', params: [{ name: 'guid', label: 'User GUID', placeholder: 'user-guid', required: true }] },
  { id: 'admin-create-user', group: 'Users', label: 'Create User', method: 'POST', pathTemplate: '/admin/users', params: [], hasBody: true, bodyPlaceholder: '{\n  "Name": "username",\n  "Email": "user@example.com"\n}' },
  { id: 'admin-delete-user', group: 'Users', label: 'Delete User', method: 'DELETE', pathTemplate: '/admin/users/{guid}', params: [{ name: 'guid', label: 'User GUID', placeholder: 'user-guid', required: true }] },
  { id: 'admin-list-credentials', group: 'Credentials', label: 'List Credentials', method: 'GET', pathTemplate: '/admin/credentials', params: [] },
  { id: 'admin-get-credential', group: 'Credentials', label: 'Get Credential', method: 'GET', pathTemplate: '/admin/credentials/{guid}', params: [{ name: 'guid', label: 'Credential GUID', placeholder: 'credential-guid', required: true }] },
  { id: 'admin-create-credential', group: 'Credentials', label: 'Create Credential', method: 'POST', pathTemplate: '/admin/credentials', params: [], hasBody: true, bodyPlaceholder: '{\n  "UserGUID": "user-guid",\n  "Description": "My key",\n  "AccessKey": "mykey",\n  "SecretKey": "mysecret"\n}' },
  { id: 'admin-delete-credential', group: 'Credentials', label: 'Delete Credential', method: 'DELETE', pathTemplate: '/admin/credentials/{guid}', params: [{ name: 'guid', label: 'Credential GUID', placeholder: 'credential-guid', required: true }] },
  { id: 'admin-list-history', group: 'Request History', label: 'List Request History', method: 'GET', pathTemplate: '/admin/requesthistory', params: [] },
  { id: 'admin-get-history', group: 'Request History', label: 'Get Request History Entry', method: 'GET', pathTemplate: '/admin/requesthistory/{guid}', params: [{ name: 'guid', label: 'Entry GUID', placeholder: 'entry-guid', required: true }] },
  { id: 'admin-get-history-summary', group: 'Request History', label: 'Get Summary', method: 'GET', pathTemplate: '/admin/requesthistory/summary', params: [] },
  { id: 'admin-delete-history', group: 'Request History', label: 'Delete Request History Entry', method: 'DELETE', pathTemplate: '/admin/requesthistory/{guid}', params: [{ name: 'guid', label: 'Entry GUID', placeholder: 'entry-guid', required: true }] },
];

const METHOD_COLORS: Record<string, string> = {
  GET: '#22AF79',
  POST: '#1890ff',
  PUT: '#fa8c16',
  DELETE: '#d9383a',
  HEAD: '#722ed1',
};

const RECENT_REQUESTS_KEY = 'less3_api_explorer_recent';
const MAX_RECENT_ITEMS = 12;
const NO_CREDENTIAL_VALUE = '__none__';

interface RecentRequest {
  operationId: string;
  method: string;
  url: string;
  apiType: string;
  statusCode: number | null;
  timestamp: string;
  body: string;
  credentialGuid?: string;
}

interface ResponseData {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  body: string;
  durationMs: number;
  size: number;
}

const inputStyle: React.CSSProperties = {
  border: '1px solid var(--color-separator)',
  borderRadius: 6,
  background: 'var(--ant-color-bg-container)',
  color: 'var(--ant-color-text)',
};

const responseBlockStyle: React.CSSProperties = {
  padding: 12,
  fontSize: 12,
  maxHeight: 400,
  overflow: 'auto',
  whiteSpace: 'pre-wrap',
  wordBreak: 'break-word',
  margin: 0,
  background: 'var(--ant-color-bg-layout)',
  borderRadius: 6,
  border: '1px solid var(--color-separator)',
  fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
};

const getStatusColor = (code: number): string => {
  if (code >= 200 && code < 300) return '#22AF79';
  if (code >= 400 && code < 500) return '#fa8c16';
  if (code >= 500) return '#d9383a';
  return '#8c8c8c';
};

const getPrettyPrintedResponseBody = (body: string, headers: Record<string, string>): string | null => {
  return getPrettyPrintedTextContent(body, headers['content-type']);
};

const getPrettyPrintedResponseHeaders = (headers: Record<string, string>): string =>
  JSON.stringify(headers, null, 2);

const getOperationApiType = (operationId: string): 's3' | 'admin' =>
  operationId.startsWith('s3-') ? 's3' : 'admin';

const loadRecentRequests = (): RecentRequest[] => {
  try {
    const stored = localStorage.getItem(RECENT_REQUESTS_KEY);
    if (!stored) {
      return [];
    }

    const parsed: unknown = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed as RecentRequest[] : [];
  } catch {
    return [];
  }
};

const saveRecentRequests = (requests: RecentRequest[]): void => {
  try {
    localStorage.setItem(RECENT_REQUESTS_KEY, JSON.stringify(requests.slice(0, MAX_RECENT_ITEMS)));
  } catch {
    // Ignore storage failures.
  }
};

const ALL_API_FILTER_VALUE = 'all';

const ApiExplorerPage: React.FC = () => {
  const [operationFilter, setOperationFilter] = useState<string>(ALL_API_FILTER_VALUE);
  const [selectedOpId, setSelectedOpId] = useState<string>('s3-list-buckets');
  const [paramValues, setParamValues] = useState<Record<string, string>>({});
  const [body, setBody] = useState<string>('');
  const [response, setResponse] = useState<ResponseData | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeResponseTab, setActiveResponseTab] = useState<string>('body');
  const [isPrettyPrintEnabled, setIsPrettyPrintEnabled] = useState(false);
  const [recentRequests, setRecentRequests] = useState<RecentRequest[]>(() => loadRecentRequests());
  const [selectedCredentialGuid, setSelectedCredentialGuid] = useState<string>(
    () => getPreferredS3CredentialGuid() || NO_CREDENTIAL_VALUE
  );

  const abortControllerRef = useRef<AbortController | null>(null);
  const { data: credentialsData } = useGetCredentialsQuery();
  const { data: selectedCredentialDetails } = useGetCredentialByIdQuery(selectedCredentialGuid, {
    skip: selectedCredentialGuid === NO_CREDENTIAL_VALUE,
  });

  const allOperations = useMemo<ApiOperation[]>(() => [...S3_OPERATIONS, ...ADMIN_OPERATIONS], []);
  const operations = useMemo<ApiOperation[]>(() => {
    if (operationFilter === ALL_API_FILTER_VALUE) {
      return allOperations;
    }

    return allOperations.filter((operation) => getOperationApiType(operation.id) === operationFilter);
  }, [allOperations, operationFilter]);

  const operationOptions = useMemo(
    () => operations.map((operation) => ({
      label: `[${operation.group}] ${operation.method} - ${operation.label}`,
      value: operation.id,
    })),
    [operations]
  );

  const selectedOp = allOperations.find((operation) => operation.id === selectedOpId);
  const selectedOperationApiType: 's3' | 'admin' = selectedOp ? getOperationApiType(selectedOp.id) : 'admin';

  const selectedCredential = useMemo(
    () => credentialsData?.find((credential) => credential.GUID === selectedCredentialGuid) || null,
    [credentialsData, selectedCredentialGuid]
  );

  const activeS3Credential = selectedCredentialDetails || selectedCredential;

  const credentialOptions = useMemo(
    () => [
      { label: 'No credential', value: NO_CREDENTIAL_VALUE },
      ...(
        credentialsData?.map((credential) => ({
          label: `${credential.Description || credential.AccessKey} (${credential.AccessKey})`,
          value: credential.GUID,
        })) || []
      ),
    ],
    [credentialsData]
  );

  const resolvedUrl = useMemo(() => {
    if (!selectedOp) {
      return '';
    }

    const baseUrl = getApiEndpoint().replace(/\/$/, '');
    let path = selectedOp.pathTemplate;

    for (const param of selectedOp.params) {
      const value = paramValues[param.name] || `{${param.name}}`;
      path = path.replace(`{${param.name}}`, encodeURIComponent(value));
    }

    return baseUrl + path;
  }, [paramValues, selectedOp]);

  const hasMissingRequiredParams = useMemo(() => {
    if (!selectedOp) {
      return true;
    }

    return selectedOp.params.some((param) => param.required && !String(paramValues[param.name] || '').trim());
  }, [paramValues, selectedOp]);

  useEffect(() => {
    if (selectedCredentialGuid === NO_CREDENTIAL_VALUE) {
      clearPreferredS3CredentialGuid();
      return;
    }

    setPreferredS3CredentialGuid(selectedCredentialGuid);
  }, [selectedCredentialGuid]);

  useEffect(() => {
    setIsPrettyPrintEnabled(false);
  }, [response?.body]);

  useEffect(() => {
    if (!selectedOp || operationFilter === ALL_API_FILTER_VALUE || selectedOperationApiType === operationFilter) {
      return;
    }

    const nextOperation = operations[0];
    if (nextOperation) {
      setSelectedOpId(nextOperation.id);
    }
  }, [operationFilter, operations, selectedOp, selectedOperationApiType]);

  const handleOperationFilterChange = useCallback((value: string) => {
    setOperationFilter(value);
    setParamValues({});
    setBody('');
    setResponse(null);
    setActiveResponseTab('body');
  }, []);

  const handleOperationChange = useCallback((value: string) => {
    const operation = allOperations.find((item) => item.id === value);

    setSelectedOpId(value);
    setParamValues({});
    setResponse(null);
    setBody(operation?.bodyPlaceholder || '');
    setActiveResponseTab('body');
  }, [allOperations]);

  const handleCancel = useCallback(() => {
    if (!abortControllerRef.current) {
      return;
    }

    abortControllerRef.current.abort();
    abortControllerRef.current = null;
    setIsLoading(false);
  }, []);

  const handleSend = useCallback(async () => {
    if (!selectedOp) {
      return;
    }

    handleCancel();

    const controller = new AbortController();
    abortControllerRef.current = controller;
    setIsLoading(true);
    setResponse(null);
    setActiveResponseTab('body');
    setIsPrettyPrintEnabled(false);

    const startTime = performance.now();

    try {
      const fetchHeaders: Record<string, string> = {};

      if (selectedOperationApiType === 'admin') {
        fetchHeaders['x-api-key'] = API_KEY;
      } else if (selectedCredentialGuid !== NO_CREDENTIAL_VALUE) {
        if (!activeS3Credential?.AccessKey || !activeS3Credential?.SecretKey) {
          message.error('Selected S3 credential is unavailable');
          setIsLoading(false);
          abortControllerRef.current = null;
          return;
        }

        const signedHeaders = await buildSignedS3Headers({
          method: selectedOp.method,
          url: resolvedUrl,
          accessKey: activeS3Credential.AccessKey,
          secretKey: activeS3Credential.SecretKey,
          headers: selectedOp.hasBody ? { 'Content-Type': 'application/json' } : undefined,
          body: body.trim() && selectedOp.hasBody ? body : undefined,
        });

        Object.assign(fetchHeaders, signedHeaders);
      }

      if (selectedOp.hasBody && !fetchHeaders['Content-Type']) {
        fetchHeaders['Content-Type'] = 'application/json';
      }

      const requestOptions: RequestInit = {
        method: selectedOp.method,
        headers: fetchHeaders,
        signal: controller.signal,
      };

      if (body.trim() && selectedOp.hasBody) {
        requestOptions.body = body;
      }

      const result = await fetch(resolvedUrl, requestOptions);
      const responseBody = await result.text().catch(() => '');
      const responseHeaders: Record<string, string> = {};

      result.headers.forEach((value, key) => {
        responseHeaders[key] = value;
      });

      const durationMs = Math.round(performance.now() - startTime);

      setResponse({
        status: result.status,
        statusText: result.statusText,
        headers: responseHeaders,
        body: responseBody,
        durationMs,
        size: new Blob([responseBody]).size,
      });

      const recentEntry: RecentRequest = {
        operationId: selectedOp.id,
        method: selectedOp.method,
        url: resolvedUrl,
        apiType: selectedOperationApiType,
        statusCode: result.status,
        timestamp: new Date().toISOString(),
        body,
        credentialGuid: selectedCredentialGuid,
      };

      const updatedRecent = [recentEntry, ...recentRequests].slice(0, MAX_RECENT_ITEMS);
      setRecentRequests(updatedRecent);
      saveRecentRequests(updatedRecent);
    } catch (error: any) {
      if (error?.name === 'AbortError') {
        message.info('Request cancelled');
      } else {
        message.error(error?.message || 'Request failed');
        setResponse({
          status: 0,
          statusText: 'Error',
          headers: {},
          body: error?.message || 'Network error',
          durationMs: Math.round(performance.now() - startTime),
          size: 0,
        });
      }
    } finally {
      setIsLoading(false);
      abortControllerRef.current = null;
    }
  }, [
    activeS3Credential,
    body,
    handleCancel,
    recentRequests,
    resolvedUrl,
    selectedCredentialGuid,
    selectedOp,
    selectedOperationApiType,
  ]);

  const handleLoadRecent = useCallback((recent: RecentRequest) => {
    setOperationFilter(ALL_API_FILTER_VALUE);
    setSelectedOpId(recent.operationId);
    setBody(recent.body || '');
    setSelectedCredentialGuid(recent.credentialGuid || NO_CREDENTIAL_VALUE);
    setResponse(null);
    setActiveResponseTab('body');
  }, []);

  const formatResponseSize = (bytes: number): string => {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const curlCommand = useMemo(() => {
    if (!selectedOp) {
      return '';
    }

    let curl = `curl -X ${selectedOp.method}`;

    if (selectedOperationApiType === 'admin') {
      curl += ` \\\n  -H 'x-api-key: ${API_KEY}'`;
    }

    if (selectedOperationApiType === 's3' && activeS3Credential?.AccessKey) {
      curl += ` \\\n  -H 'Authorization: ${buildS3AuthorizationHeader(activeS3Credential.AccessKey)}'`;
    }

    if (selectedOp.hasBody) {
      curl += ` \\\n  -H 'Content-Type: application/json'`;
    }

    if (body.trim() && selectedOp.hasBody) {
      curl += ` \\\n  -d '${body}'`;
    }

    curl += ` \\\n  '${resolvedUrl}'`;
    return curl;
  }, [activeS3Credential, body, resolvedUrl, selectedOp, selectedOperationApiType]);

  const responseHeadersJsonText = useMemo(() => {
    if (!response) {
      return '{}';
    }

    return getPrettyPrintedResponseHeaders(response.headers);
  }, [response]);

  const prettyPrintedResponseBody = useMemo(() => {
    if (!response) {
      return null;
    }

    return getPrettyPrintedResponseBody(response.body, response.headers);
  }, [response]);

  const canPrettyPrintResponseBody = Boolean(prettyPrintedResponseBody);
  const displayedResponseBody = response
    ? (isPrettyPrintEnabled && prettyPrintedResponseBody ? prettyPrintedResponseBody : response.body)
    : '';

  const responseTabs = useMemo(() => [
    {
      key: 'body',
      label: 'Body',
      children: (
        <div>
          <Less3Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
            <Less3Text type="secondary" fontSize={12}>
              {isPrettyPrintEnabled && canPrettyPrintResponseBody ? 'Pretty-printed response' : 'Raw response'}
            </Less3Text>
            <Less3Flex gap={8} align="center">
              {canPrettyPrintResponseBody && (
                <Less3Button size="small" onClick={() => setIsPrettyPrintEnabled((current) => !current)}>
                  {isPrettyPrintEnabled ? 'Show Raw' : 'Pretty Print'}
                </Less3Button>
              )}
              <CopyToClipboard
                text={displayedResponseBody}
                tooltip="Copy response body"
                ariaLabel="Copy response body"
              />
            </Less3Flex>
          </Less3Flex>
          <pre style={responseBlockStyle}>
            {response ? (displayedResponseBody || '(empty)') : 'No response yet'}
          </pre>
        </div>
      ),
    },
    {
      key: 'headers',
      label: 'Headers',
      children: (
        <div>
          <Less3Flex justify="flex-end" style={{ marginBottom: 8 }}>
            <CopyToClipboard
              text={responseHeadersJsonText}
              tooltip="Copy response headers"
              ariaLabel="Copy response headers"
            />
          </Less3Flex>
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
        <div>
          <Less3Flex justify="flex-end" style={{ marginBottom: 8 }}>
            <CopyToClipboard
              text={curlCommand}
              tooltip="Copy cURL command"
              ariaLabel="Copy cURL command"
            />
          </Less3Flex>
          <pre style={responseBlockStyle}>{curlCommand || '(empty)'}</pre>
        </div>
      ),
    },
  ], [
    canPrettyPrintResponseBody,
    curlCommand,
    displayedResponseBody,
    isPrettyPrintEnabled,
    response,
    responseHeadersJsonText,
  ]);

  return (
    <PageContainer pageTitle="API Explorer">
      <Less3Flex gap={16} style={{ flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 400 }}>
          <Less3Card title="Request" style={{ marginBottom: 16 }}>
            <Less3Flex vertical gap={14}>
              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Operation Filter</Less3Text>
                <Less3Select
                  options={[
                    { label: 'All APIs', value: ALL_API_FILTER_VALUE },
                    { label: 'S3 API', value: 's3' },
                    { label: 'Admin API', value: 'admin' },
                  ]}
                  value={operationFilter}
                  onChange={(value) => handleOperationFilterChange(value as string)}
                  style={{ width: '100%', ...inputStyle }}
                />
              </div>

              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Operation</Less3Text>
                <Less3Select
                  options={operationOptions}
                  value={selectedOpId}
                  onChange={(value) => handleOperationChange(value as string)}
                  style={{ width: '100%', ...inputStyle }}
                  showSearch
                  filterOption={(input, option) =>
                    String(option?.label || '').toLowerCase().includes(String(input).toLowerCase())
                  }
                />
              </div>

              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>
                  S3 Credential
                </Less3Text>
                <Less3Select
                  options={credentialOptions}
                  value={selectedCredentialGuid}
                  onChange={(value) => setSelectedCredentialGuid(value as string)}
                  style={{ width: '100%', ...inputStyle }}
                  showSearch
                  filterOption={(input, option) =>
                    String(option?.label || '').toLowerCase().includes(String(input).toLowerCase())
                  }
                />
                <Less3Text type="secondary" fontSize={11} style={{ display: 'block', marginTop: 4 }}>
                  {selectedOperationApiType === 's3'
                    ? 'The selected credential will be used for this S3 request. Choose "No credential" to send it unsigned.'
                    : 'Credential selection is preserved for S3 requests. Admin requests continue to use x-api-key.'}
                </Less3Text>
              </div>

              <div>
                <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Request</Less3Text>
                <Less3Flex gap={8} align="center">
                  <span
                    style={{
                      display: 'inline-block',
                      padding: '4px 10px',
                      borderRadius: 4,
                      fontSize: 12,
                      fontWeight: 700,
                      color: '#fff',
                      background: METHOD_COLORS[selectedOp?.method || 'GET'] || '#8c8c8c',
                      minWidth: 55,
                      textAlign: 'center',
                    }}
                  >
                    {selectedOp?.method || 'GET'}
                  </span>
                  <div
                    style={{
                      flex: 1,
                      padding: '6px 10px',
                      borderRadius: 6,
                      fontSize: 13,
                      fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
                      background: 'var(--ant-color-bg-layout)',
                      border: '1px solid var(--color-separator)',
                      color: 'var(--ant-color-text)',
                      wordBreak: 'break-all',
                    }}
                  >
                    {resolvedUrl || '-'}
                  </div>
                </Less3Flex>
              </div>

              {selectedOp && selectedOp.params.length > 0 && (
                <div>
                  <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 6 }}>Parameters</Less3Text>
                  <Less3Flex vertical gap={8}>
                    {selectedOp.params.map((param) => (
                      <Less3Flex key={param.name} gap={8} align="center">
                        <Less3Text fontSize={12} style={{ minWidth: 100 }}>
                          {param.label}
                          {param.required ? ' *' : ''}
                        </Less3Text>
                        <Less3Input
                          value={paramValues[param.name] || ''}
                          onChange={(event: React.ChangeEvent<HTMLInputElement>) =>
                            setParamValues((current) => ({ ...current, [param.name]: event.target.value }))
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

              {selectedOp?.hasBody && (
                <div>
                  <Less3Text fontSize={12} weight={500} style={{ display: 'block', marginBottom: 4 }}>Body</Less3Text>
                  <textarea
                    value={body}
                    onChange={(event) => setBody(event.target.value)}
                    placeholder={selectedOp.bodyPlaceholder || 'Request body (JSON)'}
                    rows={6}
                    style={{
                      width: '100%',
                      fontFamily: "'SF Mono', 'Monaco', 'Inconsolata', 'Fira Code', 'Consolas', monospace",
                      fontSize: 12,
                      padding: 10,
                      borderRadius: 6,
                      border: '1px solid var(--color-separator)',
                      resize: 'vertical',
                      background: 'var(--ant-color-bg-container)',
                      color: 'var(--ant-color-text)',
                      boxSizing: 'border-box',
                      outline: 'none',
                      lineHeight: 1.6,
                    }}
                  />
                </div>
              )}

              <Less3Flex gap={8}>
                <Less3Button
                  type="primary"
                  icon={isLoading ? <LoadingOutlined /> : <SendOutlined />}
                  onClick={handleSend}
                  loading={isLoading}
                  disabled={hasMissingRequiredParams}
                >
                  Send
                </Less3Button>
                {isLoading && (
                  <Less3Button icon={<CloseOutlined />} onClick={handleCancel}>
                    Cancel
                  </Less3Button>
                )}
              </Less3Flex>
            </Less3Flex>
          </Less3Card>

          {recentRequests.length > 0 && (
            <Less3Card
              title={<Less3Flex align="center" gap={6}><HistoryOutlined /><span>Recent Requests</span></Less3Flex>}
              size="small"
            >
              <Less3Flex vertical gap={4}>
                {recentRequests.map((recent, index) => {
                  const methodColor = METHOD_COLORS[recent.method] || '#8c8c8c';
                  const statusColor = recent.statusCode ? getStatusColor(recent.statusCode) : '#8c8c8c';

                  return (
                    <div
                      key={`${recent.operationId}-${recent.timestamp}-${index}`}
                      onClick={() => handleLoadRecent(recent)}
                      style={{
                        padding: '6px 10px',
                        borderRadius: 4,
                        cursor: 'pointer',
                        border: '1px solid var(--color-separator)',
                        fontSize: 12,
                      }}
                    >
                      <Less3Flex gap={8} align="center">
                        <span
                          style={{
                            display: 'inline-block',
                            padding: '1px 6px',
                            borderRadius: 3,
                            fontSize: 10,
                            fontWeight: 600,
                            color: '#fff',
                            background: methodColor,
                            minWidth: 42,
                            textAlign: 'center',
                          }}
                        >
                          {recent.method}
                        </span>
                        <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {recent.url}
                        </span>
                        {recent.statusCode !== null && (
                          <span
                            style={{
                              display: 'inline-block',
                              padding: '1px 6px',
                              borderRadius: 3,
                              fontSize: 10,
                              fontWeight: 600,
                              color: '#fff',
                              background: statusColor,
                            }}
                          >
                            {recent.statusCode}
                          </span>
                        )}
                        <Less3Text type="secondary" fontSize={10}>
                          {new Date(recent.timestamp).toLocaleTimeString()}
                        </Less3Text>
                      </Less3Flex>
                    </div>
                  );
                })}
              </Less3Flex>
            </Less3Card>
          )}
        </div>

        <div style={{ flex: 1, minWidth: 400 }}>
          <Less3Card title="Response" style={{ marginBottom: 16 }}>
            {response ? (
              <Less3Flex vertical gap={12}>
                <Less3Flex gap={12} align="center">
                  <span
                    style={{
                      display: 'inline-block',
                      padding: '3px 10px',
                      borderRadius: 4,
                      fontSize: 12,
                      fontWeight: 600,
                      color: '#fff',
                      background: getStatusColor(response.status),
                    }}
                  >
                    {response.status} {response.statusText}
                  </span>
                  <Less3Text type="secondary" fontSize={12}>{response.durationMs} ms</Less3Text>
                  <Less3Text type="secondary" fontSize={12}>{formatResponseSize(response.size)}</Less3Text>
                </Less3Flex>
                <Less3Tabs
                  activeKey={activeResponseTab}
                  onChange={setActiveResponseTab}
                  items={responseTabs}
                  size="small"
                />
              </Less3Flex>
            ) : (
              <div style={{ textAlign: 'center', padding: '60px 0' }}>
                <Less3Text type="secondary">
                  {isLoading ? 'Sending request...' : 'Send a request to see the response'}
                </Less3Text>
              </div>
            )}
          </Less3Card>
        </div>
      </Less3Flex>
    </PageContainer>
  );
};

export default ApiExplorerPage;
