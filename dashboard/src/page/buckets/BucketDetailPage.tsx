'use client';
import React, { useMemo } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { ArrowLeftOutlined, FolderOpenOutlined, ReloadOutlined } from '@ant-design/icons';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import Less3Button from '#/components/base/button/Button';
import Less3Card from '#/components/base/card/Card';
import Less3Flex from '#/components/base/flex/Flex';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Tabs from '#/components/base/tabs/Tabs';
import Less3Text from '#/components/base/typograpghy/Text';
import IdDisplay from '#/components/id-display';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import {
  Bucket,
  useGetBucketACLQuery,
  useGetBucketByIdQuery,
  useGetBucketTagsQuery,
  useGetBucketsQuery,
  useListBucketObjectsQuery,
} from '#/store/slice/bucketsSlice';
import { RequestHistoryEntry, useGetRequestHistoryQuery } from '#/store/slice/requestHistorySlice';
import { type BucketObject, type BucketTag } from '#/utils/xmlUtils';
import { formatDate } from '#/utils/dateUtils';

const formatBoolean = (value: unknown): string => (value ? 'Enabled' : 'Disabled');

const BucketDetailPage: React.FC = () => {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const routeId = decodeURIComponent(String(params?.id || ''));
  const bucketNameFromQuery = searchParams.get('name') || '';

  const { data: bucketsData } = useGetBucketsQuery();
  const { data: bucketById, isLoading: isBucketLoading, refetch: refetchBucket } = useGetBucketByIdQuery(routeId, {
    skip: !routeId,
  });

  const bucket = useMemo<Bucket | null>(() => {
    return (
      bucketById ||
      bucketsData?.find((item) => item.Id === routeId || item.Name === routeId || item.Name === bucketNameFromQuery) ||
      null
    );
  }, [bucketById, bucketNameFromQuery, bucketsData, routeId]);

  const bucketName = bucket?.Name || bucketNameFromQuery || routeId;
  const { data: objectsData, isLoading: isObjectsLoading, refetch: refetchObjects } = useListBucketObjectsQuery(
    { bucketId: bucketName },
    { skip: !bucketName }
  );
  const { data: tagsData, isLoading: isTagsLoading, refetch: refetchTags } = useGetBucketTagsQuery(
    { bucketName },
    { skip: !bucketName }
  );
  const { data: aclData, isLoading: isAclLoading, refetch: refetchAcl } = useGetBucketACLQuery(
    { bucketName },
    { skip: !bucketName }
  );
  const { data: requestHistoryData, isLoading: isHistoryLoading, refetch: refetchHistory } = useGetRequestHistoryQuery();

  const objectColumns: DataTableColumn<BucketObject>[] = [
    { key: 'Key', label: 'Key', render: (item) => <TextWithCopy text={item.Key} className="code-font-style" /> },
    { key: 'Size', label: 'Size', width: '110px', render: (item) => String(item.Size), sortValue: (item) => item.Size },
    { key: 'ContentType', label: 'Type', width: '180px' },
    { key: 'LastModified', label: 'Modified', width: '180px', render: (item) => formatDate(item.LastModified || '') },
  ];

  const tagColumns: DataTableColumn<BucketTag & { _id: number }>[] = [
    { key: 'Key', label: 'Key' },
    { key: 'Value', label: 'Value' },
  ];

  const activityRows = useMemo<RequestHistoryEntry[]>(() => {
    return (requestHistoryData || [])
      .filter((entry) => (entry.RequestUrl || '').split('?')[0].includes(`/${bucketName}`))
      .sort((a, b) => new Date(b.CreatedUtc).getTime() - new Date(a.CreatedUtc).getTime());
  }, [bucketName, requestHistoryData]);

  const activityColumns: DataTableColumn<RequestHistoryEntry>[] = [
    { key: 'CreatedUtc', label: 'Time', render: (item) => formatDate(item.CreatedUtc) },
    { key: 'HttpMethod', label: 'Method', width: '100px' },
    { key: 'StatusCode', label: 'Status', width: '100px', render: (item) => String(item.StatusCode) },
    { key: 'RequestUrl', label: 'URL', render: (item) => <TextWithCopy text={item.RequestUrl} className="code-font-style" /> },
  ];

  const tabs = [
    {
      key: 'overview',
      label: 'Overview',
      children: (
        <Less3Card size="small">
          <table style={{ width: '100%', tableLayout: 'fixed', borderCollapse: 'collapse' }}>
            <tbody>
              {[
                { label: 'ID', value: bucket?.Id || routeId, id: true },
                { label: 'Name', value: bucketName },
                { label: 'Tenant ID', value: bucket?.TenantId || 'default', id: true },
                { label: 'Owner ID', value: bucket?.OwnerId || 'Not set', id: Boolean(bucket?.OwnerId) },
                { label: 'Created', value: formatDate(bucket?.CreationDate || bucket?.CreatedUtc || '') },
                { label: 'Region', value: bucket?.RegionString || 'us-west-1' },
                { label: 'Storage Type', value: String(bucket?.StorageType || 'Disk') },
                { label: 'Disk Directory', value: bucket?.DiskDirectory || 'Not set' },
                { label: 'Versioning', value: formatBoolean(bucket?.EnableVersioning) },
                { label: 'Public Read', value: formatBoolean(bucket?.EnablePublicRead) },
                { label: 'Public Write', value: formatBoolean(bucket?.EnablePublicWrite) },
              ].map((item) => (
                <tr key={item.label}>
                  <td style={{ width: 150, padding: '7px 12px 7px 0', verticalAlign: 'top' }}>
                    <Less3Text type="secondary" fontSize={12}>{item.label}</Less3Text>
                  </td>
                  <td style={{ padding: '7px 0', wordBreak: 'break-word' }}>
                    {item.id ? <IdDisplay id={item.value} /> : <Less3Text>{item.value}</Less3Text>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Less3Card>
      ),
    },
    {
      key: 'objects',
      label: 'Objects',
      children: (
        <DataTable
          columns={objectColumns}
          data={objectsData?.Contents || []}
          loading={isObjectsLoading}
          rowKey="Key"
        />
      ),
    },
    {
      key: 'activity',
      label: 'Activity',
      children: <DataTable columns={activityColumns} data={activityRows} loading={isHistoryLoading} rowKey="Id" />,
    },
    {
      key: 'tags',
      label: 'Tags',
      children: (
        <DataTable
          columns={tagColumns}
          data={(tagsData?.tags || []).map((tag, index) => ({ ...tag, _id: index }))}
          loading={isTagsLoading}
          rowKey="_id"
        />
      ),
    },
    {
      key: 'acl',
      label: 'ACL',
      children: (
        <pre style={{ margin: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {isAclLoading ? 'Loading ACL...' : JSON.stringify(aclData?.acl || {}, null, 2)}
        </pre>
      ),
    },
    {
      key: 'versioning',
      label: 'Versioning',
      children: (
        <Less3Flex vertical gap={8}>
          <Less3Text>Versioning: {formatBoolean(bucket?.EnableVersioning)}</Less3Text>
          <Less3Button onClick={() => router.push(`/admin/objects?bucket=${encodeURIComponent(bucketName)}`)}>
            Browse Versions
          </Less3Button>
        </Less3Flex>
      ),
    },
    {
      key: 'settings',
      label: 'Settings',
      children: (
        <Less3Flex vertical gap={8}>
          <Less3Text>Storage: {String(bucket?.StorageType || 'Disk')}</Less3Text>
          <Less3Text>Public read: {formatBoolean(bucket?.EnablePublicRead)}</Less3Text>
          <Less3Text>Public write: {formatBoolean(bucket?.EnablePublicWrite)}</Less3Text>
        </Less3Flex>
      ),
    },
  ];

  return (
    <PageContainer
      pageTitle={`Bucket: ${bucketName}`}
      pageTitleRightContent={
        <Less3Flex gap={8} align="center">
          <Less3Button icon={<ArrowLeftOutlined />} onClick={() => router.push('/admin/buckets')}>
            Back
          </Less3Button>
          <Less3Button icon={<FolderOpenOutlined />} onClick={() => router.push(`/admin/objects?bucket=${encodeURIComponent(bucketName)}`)}>
            Objects
          </Less3Button>
          <Less3Button
            icon={<ReloadOutlined />}
            onClick={() => {
              refetchBucket();
              refetchObjects();
              refetchTags();
              refetchAcl();
              refetchHistory();
            }}
            loading={isBucketLoading}
          >
            Refresh
          </Less3Button>
        </Less3Flex>
      }
    >
      <Less3Tabs items={tabs} />
    </PageContainer>
  );
};

export default BucketDetailPage;
