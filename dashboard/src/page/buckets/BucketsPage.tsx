/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState } from 'react';
import { Form, message, Switch, MenuProps, Descriptions } from 'antd';
import { PlusOutlined, SearchOutlined, MoreOutlined } from '@ant-design/icons';
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
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';

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
  const [viewingBucketGUID, setViewingBucketGUID] = useState<string | null>(null);
  const [deletingBucket, setDeletingBucket] = useState<Bucket | null>(null);
  const [deleteWithDestroy, setDeleteWithDestroy] = useState(false);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetBucketsQuery();

  const { data: bucketMetadata, isLoading: isMetadataLoading } = useGetBucketByIdQuery(viewingBucketGUID || '', {
    skip: !viewingBucketGUID,
  });

  const [createBucket, { isLoading: isCreating }] = useCreateBucketMutation();
  const [deleteBucket, { isLoading: isDeleting }] = useDeleteBucketMutation();

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
      width: 200,
      sorter: (a: Bucket, b: Bucket) => (a.GUID || '').localeCompare(b.GUID || ''),
    },
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      width: 200,
      sorter: (a: Bucket, b: Bucket) => (a.Name || '').localeCompare(b.Name || ''),
    },
    {
      title: 'Storage Type',
      dataIndex: 'StorageType',
      key: 'StorageType',
      width: 120,
    },
    {
      title: 'Region',
      dataIndex: 'RegionString',
      key: 'RegionString',
      width: 120,
      render: (text: string) => text || '-',
    },
    {
      title: 'Public Read',
      dataIndex: 'EnablePublicRead',
      key: 'EnablePublicRead',
      width: 100,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Public Write',
      dataIndex: 'EnablePublicWrite',
      key: 'EnablePublicWrite',
      width: 100,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Versioning',
      dataIndex: 'EnableVersioning',
      key: 'EnableVersioning',
      width: 100,
      render: (value: boolean) => (value ? 'Yes' : 'No'),
    },
    {
      title: 'Date Created',
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 180,
      render: (text: string) => (text ? new Date(text).toLocaleString() : '-'),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 80,
      fixed: 'right',
      render: (_: any, record: Bucket) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(record),
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
      <Less3Table
        columns={columns as ColumnsType<any>}
        dataSource={filteredData}
        loading={isLoading}
        rowKey="GUID"
        pagination={false}
      />

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
              <Less3Text>
                {bucketMetadata.CreatedUtc ? new Date(bucketMetadata.CreatedUtc).toLocaleString() : '-'}
              </Less3Text>
            </Descriptions.Item>
          </Descriptions>
        ) : (
          <div style={{ textAlign: 'center', padding: '20px' }}>No metadata available</div>
        )}
      </Less3Modal>
    </PageContainer>
  );
};

export default BucketsPage;
