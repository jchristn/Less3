/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState } from 'react';
import { Form, message, Switch, MenuProps, Descriptions } from 'antd';
import { PlusOutlined, SearchOutlined, MoreOutlined, DownloadOutlined } from '@ant-design/icons';
import Less3Table from '#/components/base/table/Table';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetBucketsQuery,
  useGetBucketByIdQuery,
  useCreateBucketMutation,
  useDeleteBucketMutation,
  useListBucketObjectsQuery,
  useLazyDownloadBucketObjectQuery,
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';
import { type BucketObject } from '#/utils/xmlUtils';
import { formatDate } from '#/utils/dateUtils';
import WriteObjectModal from './WriteObjectModal';

interface BucketFormValues {
  Name: string;
  StorageType?: string;
  DiskDirectory?: string;
  EnableVersioning?: boolean;
  EnablePublicWrite?: boolean;
  EnablePublicRead?: boolean;
}

const BucketsPage: React.FC = () => {
  const [form] = Form.useForm<BucketFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isMetadataModalVisible, setIsMetadataModalVisible] = useState(false);
  const [isObjectsModalVisible, setIsObjectsModalVisible] = useState(false);
  const [isWriteObjectModalVisible, setIsWriteObjectModalVisible] = useState(false);
  const [viewingBucketGUID, setViewingBucketGUID] = useState<string | null>(null);
  const [viewingBucketForObjects, setViewingBucketForObjects] = useState<Bucket | null>(null);
  const [writingBucket, setWritingBucket] = useState<Bucket | null>(null);
  const [deletingBucket, setDeletingBucket] = useState<Bucket | null>(null);
  const [deleteWithDestroy, setDeleteWithDestroy] = useState(false);
  const [searchText, setSearchText] = useState('');
  const [downloadingObjectKey, setDownloadingObjectKey] = useState<string | null>(null);

  const { data, isLoading, refetch } = useGetBucketsQuery();

  const { data: bucketMetadata, isLoading: isMetadataLoading } = useGetBucketByIdQuery(viewingBucketGUID || '', {
    skip: !viewingBucketGUID,
  });

  const [createBucket, { isLoading: isCreating }] = useCreateBucketMutation();
  const [deleteBucket, { isLoading: isDeleting }] = useDeleteBucketMutation();

  const {
    data: bucketObjectsData,
    isLoading: isLoadingObjects,
    refetch: refetchObjects,
  } = useListBucketObjectsQuery(
    { bucketGUID: viewingBucketForObjects?.GUID || '' },
    { skip: !viewingBucketForObjects?.GUID || !isObjectsModalVisible }
  );

  const [downloadBucketObject] = useLazyDownloadBucketObjectQuery();

  const handleCreate = () => {
    form.resetFields();
    form.setFieldsValue({
      StorageType: 'Disk',
      EnableVersioning: false,
      EnablePublicWrite: false,
      EnablePublicRead: true,
    });
    setIsModalVisible(true);
  };

  const handleViewMetadata = (record: Bucket) => {
    setViewingBucketGUID(record.GUID);
    setIsMetadataModalVisible(true);
  };

  const handleListObjects = (record: Bucket) => {
    setViewingBucketForObjects(record);
    setIsObjectsModalVisible(true);
  };

  const handleWriteObject = (record: Bucket) => {
    setWritingBucket(record);
    setIsWriteObjectModalVisible(true);
  };

  const handleWriteObjectSuccess = () => {
    // Refresh objects list if the objects modal is open for this bucket
    if (viewingBucketForObjects?.GUID === writingBucket?.GUID && isObjectsModalVisible) {
      refetchObjects();
    }
    setWritingBucket(null);
  };

  const handleWriteObjectCancel = () => {
    setIsWriteObjectModalVisible(false);
    setWritingBucket(null);
  };

  const handleDelete = (record: Bucket, withDestroy: boolean) => {
    setDeletingBucket(record);
    setDeleteWithDestroy(withDestroy);
    setIsDeleteModalVisible(true);
  };

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields();
      const createPayload = {
        Name: values.Name,
        StorageType: values.StorageType || 'Disk',
        DiskDirectory: values.DiskDirectory || `./Storage/${values.Name}/Objects/`,
        EnableVersioning: values.EnableVersioning || false,
        EnablePublicWrite: values.EnablePublicWrite || false,
        EnablePublicRead: values.EnablePublicRead !== undefined ? values.EnablePublicRead : true,
        CreatedUtc: new Date().toISOString(),
      };
      await createBucket(createPayload).unwrap();
      message.success('Bucket created successfully');
      setIsModalVisible(false);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to create bucket');
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingBucket?.GUID) return;

    try {
      await deleteBucket({ guid: deletingBucket.GUID, destroy: deleteWithDestroy }).unwrap();
      message.success(`Bucket ${deleteWithDestroy ? 'destroyed' : 'deleted'} successfully`);
      setIsDeleteModalVisible(false);
      setDeletingBucket(null);
      setDeleteWithDestroy(false);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to delete bucket');
    }
  };

  const columns: ColumnsType<Bucket> = [
    {
      title: 'GUID',
      dataIndex: 'GUID',
      key: 'GUID',
      ellipsis: true,
      sorter: (a: Bucket, b: Bucket) => (a.GUID || '').localeCompare(b.GUID || ''),
    },
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      ellipsis: true,
      sorter: (a: Bucket, b: Bucket) => (a.Name || '').localeCompare(b.Name || ''),
    },
    {
      title: 'Storage Type',
      dataIndex: 'StorageType',
      key: 'StorageType',
      ellipsis: true,
    },
    {
      title: 'Region',
      dataIndex: 'RegionString',
      key: 'RegionString',
      ellipsis: true,
      render: (text: string) => text || '-',
    },
    {
      title: 'Public Read',
      dataIndex: 'EnablePublicRead',
      key: 'EnablePublicRead',
      ellipsis: true,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Public Write',
      dataIndex: 'EnablePublicWrite',
      key: 'EnablePublicWrite',
      ellipsis: true,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Versioning',
      dataIndex: 'EnableVersioning',
      key: 'EnableVersioning',
      ellipsis: true,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Date Created',
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      ellipsis: true,
      render: (text: string) => formatDate(text),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: any, record: Bucket) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(record),
          },
          {
            key: 'listObjects',
            label: 'List Objects in Bucket',
            onClick: () => handleListObjects(record),
          },
          {
            key: 'writeObject',
            label: 'Write Object',
            onClick: () => handleWriteObject(record),
          },
          {
            key: 'delete',
            label: 'Delete Bucket',
            onClick: () => handleDelete(record, false),
          },
          {
            key: 'destroy',
            label: 'Destroy Bucket',
            onClick: () => handleDelete(record, true),
          },
        ];

        return (
          <Less3Dropdown menu={{ items: menuItems }} trigger={['click']}>
            <Less3Button type="text" icon={<MoreOutlined />} size="small" />
          </Less3Dropdown>
        );
      },
    },
  ];

  const filteredData = useMemo(() => {
    if (!data) return [];

    const q = searchText.trim().toLowerCase();
    if (!q) return data;

    return data.filter((bucket) => {
      const guid = bucket.GUID?.toLowerCase() ?? '';
      const name = bucket.Name?.toLowerCase() ?? '';
      const storageType = bucket.StorageType?.toLowerCase() ?? '';
      const region = bucket.RegionString?.toLowerCase() ?? '';

      return guid.includes(q) || name.includes(q) || storageType.includes(q) || region.includes(q);
    });
  }, [data, searchText]);

  const handleDownloadObject = async (record: BucketObject) => {
    if (!viewingBucketForObjects?.GUID) {
      message.error('Bucket information not available');
      return;
    }

    // Set the downloading key to show loading state for this specific button
    setDownloadingObjectKey(record.Key);

    try {
      const result = await downloadBucketObject({
        bucketGUID: viewingBucketForObjects.GUID,
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

  return (
    <PageContainer
      pageTitle="Buckets"
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Input
            placeholder="Search buckets..."
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setSearchText(e.target.value);
            }}
            style={{ width: 250 }}
            allowClear
          />
          <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Create Bucket
          </Less3Button>
        </Less3Flex>
      }
    >
      <div className="responsive-scrollbar" style={{ width: '100%' }}>
        <Less3Table
          columns={columns as ColumnsType<any>}
          dataSource={filteredData}
          loading={isLoading}
          rowKey="GUID"
          pagination={false}
          scroll={{ x: true }}
        />
      </div>

      <Less3Modal
        title="Create Bucket"
        open={isModalVisible}
        onOk={handleModalOk}
        onCancel={() => {
          setIsModalVisible(false);
          form.resetFields();
        }}
        confirmLoading={isCreating}
        width={700}
        centered
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Less3FormItem
            label="Name"
            name="Name"
            rules={[
              { required: true, message: 'Please enter bucket name' },
              { min: 1, message: 'Name must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter bucket name" />
          </Less3FormItem>
          <Less3FormItem
            label="Storage Type"
            name="StorageType"
            rules={[{ required: true, message: 'Please select storage type' }]}
          >
            <Less3Select
              options={[
                { value: 'Disk', label: 'Disk' },
                { value: 'S3', label: 'S3' },
                { value: 'Azure', label: 'Azure' },
              ]}
              placeholder="Select storage type"
            />
          </Less3FormItem>
          <Less3FormItem label="Disk Directory" name="DiskDirectory">
            <Less3Input placeholder="Enter disk directory path" />
          </Less3FormItem>
          <Less3FormItem label="Enable Versioning" name="EnableVersioning" valuePropName="checked">
            <Switch />
          </Less3FormItem>
          <Less3FormItem label="Enable Public Write" name="EnablePublicWrite" valuePropName="checked">
            <Switch />
          </Less3FormItem>
          <Less3FormItem label="Enable Public Read" name="EnablePublicRead" valuePropName="checked">
            <Switch />
          </Less3FormItem>
        </Form>
      </Less3Modal>

      <Less3Modal
        title={deleteWithDestroy ? 'Destroy Bucket' : 'Delete Bucket'}
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingBucket(null);
          setDeleteWithDestroy(false);
        }}
        confirmLoading={isDeleting}
        okText={deleteWithDestroy ? 'Destroy' : 'Delete'}
        okButtonProps={{ danger: true }}
        centered
        footer={[
          <Less3Button key="confirm" type="primary" danger loading={isDeleting} onClick={handleDeleteConfirm}>
            {deleteWithDestroy ? 'Destroy' : 'Delete'}
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          {deleteWithDestroy ? (
            <>
              <p>
                Are you sure you want to destroy the bucket <strong>&quot;{deletingBucket?.Name}&quot;</strong> (GUID:{' '}
                {deletingBucket?.GUID})?
              </p>

              <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
                All objects and data stored in this bucket will be permanently removed from storage.
              </p>
            </>
          ) : (
            <>
              <p>
                Are you sure you want to delete the bucket <strong>&quot;{deletingBucket?.Name}&quot;</strong> (GUID:{' '}
                {deletingBucket?.GUID})?
              </p>
              <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
                This will remove the bucket metadata. The data in the bucket will remain intact.
              </p>
            </>
          )}
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title="Bucket Metadata"
        open={isMetadataModalVisible}
        onCancel={() => {
          setIsMetadataModalVisible(false);
          setViewingBucketGUID(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsMetadataModalVisible(false);
              setViewingBucketGUID(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
      >
        {isMetadataLoading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>Loading metadata...</div>
        ) : bucketMetadata ? (
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="GUID">
              <Less3Text>{bucketMetadata.GUID}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Name">
              <Less3Text>{bucketMetadata.Name}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Owner GUID">
              <Less3Text>{bucketMetadata.OwnerGUID}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Storage Type">
              <Less3Text>{bucketMetadata.StorageType}</Less3Text>
            </Descriptions.Item>
            {bucketMetadata.RegionString && (
              <Descriptions.Item label="Region">
                <Less3Text>{bucketMetadata.RegionString}</Less3Text>
              </Descriptions.Item>
            )}
            {bucketMetadata.DiskDirectory && (
              <Descriptions.Item label="Disk Directory">
                <Less3Text>{bucketMetadata.DiskDirectory}</Less3Text>
              </Descriptions.Item>
            )}
            <Descriptions.Item label="Enable Versioning">
              <Less3Text>{bucketMetadata.EnableVersioning ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Enable Public Write">
              <Less3Text>{bucketMetadata.EnablePublicWrite ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Enable Public Read">
              <Less3Text>{bucketMetadata.EnablePublicRead ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Created At">
              <Less3Text>{formatDate(bucketMetadata.CreatedUtc || '')}</Less3Text>
            </Descriptions.Item>
          </Descriptions>
        ) : (
          <div style={{ textAlign: 'center', padding: '20px' }}>No metadata available</div>
        )}
      </Less3Modal>

      <Less3Modal
        title={`Objects in Bucket: ${viewingBucketForObjects?.Name || ''}`}
        open={isObjectsModalVisible}
        onCancel={() => {
          setIsObjectsModalVisible(false);
          setViewingBucketForObjects(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsObjectsModalVisible(false);
              setViewingBucketForObjects(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width="90%"
        style={{ maxWidth: 1300 }}
      >
        {isLoadingObjects ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>Loading objects...</div>
        ) : !bucketObjectsData || bucketObjectsData.Contents.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>No objects found in this bucket</div>
        ) : (
          <Less3Flex vertical gap={16}>
            {bucketObjectsData && (
              <Less3Flex justify="space-between" align="center" style={{ padding: '0 4px' }} wrap="wrap" gap={8}>
                <Less3Text type="secondary" style={{ fontSize: '14px' }}>
                  Showing {bucketObjectsData.KeyCount} object{bucketObjectsData.KeyCount !== 1 ? 's' : ''}
                  {bucketObjectsData.IsTruncated && ` (more available, max ${bucketObjectsData.MaxKeys} per request)`}
                </Less3Text>
                {bucketObjectsData.IsTruncated && (
                  <Less3Text type="warning" style={{ fontSize: '12px' }}>
                    Results truncated - not all objects are shown
                  </Less3Text>
                )}
              </Less3Flex>
            )}
            <div className="responsive-scrollbar" style={{ width: '100%' }}>
              <Less3Table
                columns={[
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
                    render: (_: any, record: any) => (
                      <Less3Text>
                        {(record as BucketObject).Owner?.DisplayName || (record as BucketObject).Owner?.ID || '-'}
                      </Less3Text>
                    ),
                  },
                  {
                    title: 'Actions',
                    key: 'Actions',
                    render: (_: any, record: any) => (
                      <Less3Button
                        type="text"
                        icon={<DownloadOutlined />}
                        loading={downloadingObjectKey === (record as BucketObject).Key}
                        onClick={() => handleDownloadObject(record as BucketObject)}
                      />
                    ),
                  },
                ]}
                dataSource={bucketObjectsData.Contents}
                loading={isLoadingObjects}
                rowKey="Key"
                pagination={false}
                scroll={{ x: true }}
              />
            </div>
          </Less3Flex>
        )}
      </Less3Modal>

      <WriteObjectModal
        bucket={writingBucket}
        open={isWriteObjectModalVisible}
        onCancel={handleWriteObjectCancel}
        onSuccess={handleWriteObjectSuccess}
      />
    </PageContainer>
  );
};

export default BucketsPage;
