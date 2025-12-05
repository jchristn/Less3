'use client';
import React, { useMemo, useState, useEffect } from 'react';
import { message } from 'antd';
import { DownloadOutlined, SearchOutlined, PlusOutlined } from '@ant-design/icons';
import Less3Table from '#/components/base/table/Table';
import Less3Button from '#/components/base/button/Button';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetBucketsQuery,
  useListBucketObjectsQuery,
  useLazyDownloadBucketObjectQuery,
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';
import { type BucketObject } from '#/utils/xmlUtils';
import { formatDate } from '#/utils/dateUtils';
import { transformToOptions } from '#/utils/appUtils';
import WriteObjectModal from '../buckets/WriteObjectModal';

const ObjectsPage: React.FC = () => {
  const [selectedBucketName, setSelectedBucketName] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [downloadingObjectKey, setDownloadingObjectKey] = useState<string | null>(null);
  const [isWriteObjectModalVisible, setIsWriteObjectModalVisible] = useState(false);

  const { data: bucketsData, isLoading: isLoadingBuckets } = useGetBucketsQuery();

  const {
    data: bucketObjectsData,
    isLoading: isLoadingObjects,
    refetch: refetchObjects,
  } = useListBucketObjectsQuery(
    { bucketGUID: selectedBucketName || '' },
    { skip: !selectedBucketName }
  );

  const [downloadBucketObject] = useLazyDownloadBucketObjectQuery();

  const selectedBucket = useMemo(() => {
    return bucketsData?.find((bucket) => bucket.Name === selectedBucketName) || null;
  }, [bucketsData, selectedBucketName]);

  const bucketOptions = useMemo(() => {
    return transformToOptions(bucketsData, 'Name');
  }, [bucketsData]);

  // Auto-select the first bucket when buckets are loaded
  useEffect(() => {
    if (bucketsData && bucketsData.length > 0 && !selectedBucketName) {
      setSelectedBucketName(bucketsData[0].Name);
    }
  }, [bucketsData, selectedBucketName]);

  const filteredObjects = useMemo(() => {
    if (!bucketObjectsData?.Contents) return [];

    const q = searchText.trim().toLowerCase();
    if (!q) return bucketObjectsData.Contents;

    return bucketObjectsData.Contents.filter((obj) => {
      const key = obj.Key?.toLowerCase() ?? '';
      const contentType = obj.ContentType?.toLowerCase() ?? '';
      const storageClass = obj.StorageClass?.toLowerCase() ?? '';
      const owner = obj.Owner?.DisplayName?.toLowerCase() ?? obj.Owner?.ID?.toLowerCase() ?? '';

      return key.includes(q) || contentType.includes(q) || storageClass.includes(q) || owner.includes(q);
    });
  }, [bucketObjectsData, searchText]);

  const handleDownloadObject = async (record: BucketObject) => {
    if (!selectedBucketName) {
      message.error('Bucket information not available');
      return;
    }

    // Set the downloading key to show loading state for this specific button
    setDownloadingObjectKey(record.Key);

    try {
      const result = await downloadBucketObject({
        bucketGUID: selectedBucketName,
        objectKey: record.Key,
      }).unwrap();

      // Create a blob from the content
      const blob = new Blob([result.content], { type: result.contentType || record.ContentType || 'text/plain' });

      // Create a temporary URL for the blob
      const url = window.URL.createObjectURL(blob);

      // Create a temporary anchor element and trigger download
      const link = document.createElement('a');
      link.href = url;
      link.download = record.Key.split('/').pop() || record.Key; // Get filename from key (handle nested paths)
      document.body.appendChild(link);
      link.click();

      // Clean up
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      message.success(`Downloaded ${record.Key} successfully`);
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to download object');
    } finally {
      // Clear the downloading key to hide loading state
      setDownloadingObjectKey(null);
    }
  };

  const handleWriteObject = () => {
    if (!selectedBucket) {
      message.warning('Please select a bucket first');
      return;
    }
    setIsWriteObjectModalVisible(true);
  };

  const handleWriteObjectSuccess = () => {
    setIsWriteObjectModalVisible(false);
    refetchObjects(); // Refresh the objects list
  };

  const handleWriteObjectCancel = () => {
    setIsWriteObjectModalVisible(false);
  };

  const columns: ColumnsType<BucketObject> = [
    {
      title: 'Key',
      dataIndex: 'Key',
      key: 'Key',
      ellipsis: true,
    },
    {
      title: 'Last Modified',
      dataIndex: 'LastModified',
      key: 'LastModified',
      ellipsis: true,
      render: (text: string) => formatDate(text),
    },
    {
      title: 'Size',
      dataIndex: 'Size',
      key: 'Size',
      ellipsis: true,
      render: (size: number) => {
        if (size < 1024) return `${size} B`;
        if (size < 1024 * 1024) return `${(size / 1024).toFixed(2)} KB`;
        if (size < 1024 * 1024 * 1024) return `${(size / (1024 * 1024)).toFixed(2)} MB`;
        return `${(size / (1024 * 1024 * 1024)).toFixed(2)} GB`;
      },
    },
    {
      title: 'Content Type',
      dataIndex: 'ContentType',
      key: 'ContentType',
      ellipsis: true,
    },
    {
      title: 'ETag',
      dataIndex: 'ETag',
      key: 'ETag',
      ellipsis: true,
    },
    {
      title: 'Storage Class',
      dataIndex: 'StorageClass',
      key: 'StorageClass',
      ellipsis: true,
    },
    {
      title: 'Owner',
      key: 'Owner',
      ellipsis: true,
      render: (_: any, record: BucketObject) => (
        <Less3Text>{record.Owner?.DisplayName || record.Owner?.ID || '-'}</Less3Text>
      ),
    },
    {
      title: 'Actions',
      key: 'Actions',
      render: (_: any, record: BucketObject) => (
        <Less3Button
          type="text"
          icon={<DownloadOutlined />}
          loading={downloadingObjectKey === record.Key}
          onClick={() => handleDownloadObject(record)}
        />
      ),
    },
  ];

  return (
    <PageContainer
      pageTitle="Objects"
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Select
            placeholder="Select a bucket"
            options={bucketOptions}
            value={selectedBucketName}
            onChange={(value: string) => {
              setSelectedBucketName(value);
              setSearchText(''); // Clear search when bucket changes
            }}
            style={{ width: 250 }}
            loading={isLoadingBuckets}
            allowClear
          />
          {selectedBucketName && (
            <>
              <Less3Input
                placeholder="Search objects..."
                prefix={<SearchOutlined />}
                value={searchText}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                  setSearchText(e.target.value);
                }}
                style={{ width: 250 }}
                allowClear
              />
              <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleWriteObject}>
                Write Object
              </Less3Button>
            </>
          )}
        </Less3Flex>
      }
    >
      {!selectedBucketName ? (
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Less3Text type="secondary">Please select a bucket to view its objects</Less3Text>
        </div>
      ) : isLoadingObjects ? (
        <div style={{ textAlign: 'center', padding: '40px' }}>Loading objects...</div>
      ) : !bucketObjectsData || bucketObjectsData.Contents.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '40px' }}>
          <Less3Text type="secondary">No objects found in bucket &quot;{selectedBucket?.Name}&quot;</Less3Text>
        </div>
      ) : (
        <Less3Flex vertical gap={16}>
          {bucketObjectsData?.IsTruncated && (
            <Less3Flex justify="flex-end" align="center" style={{ padding: '0 4px' }}>
              <Less3Text type="warning" style={{ fontSize: '12px' }}>
                Results truncated - not all objects are shown
              </Less3Text>
            </Less3Flex>
          )}
          <div className="responsive-scrollbar" style={{ width: '100%' }}>
            <Less3Table
              columns={columns as ColumnsType<any>}
              dataSource={filteredObjects}
              loading={isLoadingObjects}
              rowKey="Key"
              pagination={false}
              scroll={{ x: true }}
            />
          </div>
        </Less3Flex>
      )}

      <WriteObjectModal
        bucket={selectedBucket}
        open={isWriteObjectModalVisible}
        onCancel={handleWriteObjectCancel}
        onSuccess={handleWriteObjectSuccess}
      />
    </PageContainer>
  );
};

export default ObjectsPage;

