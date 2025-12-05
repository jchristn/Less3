/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState } from 'react';
import { Form, message, MenuProps } from 'antd';
import { PlusOutlined, SearchOutlined, MoreOutlined } from '@ant-design/icons';
import Less3Table from '#/components/base/table/Table';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetBucketsQuery,
  useCreateBucketMutation,
  useDeleteBucketMutation,
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';
import { formatDate } from '#/utils/dateUtils';

interface BucketFormValues {
  Name: string;
}

const BucketsPage: React.FC = () => {
  const [form] = Form.useForm<BucketFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [deletingBucket, setDeletingBucket] = useState<Bucket | null>(null);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetBucketsQuery();

  const [createBucket, { isLoading: isCreating }] = useCreateBucketMutation();
  const [deleteBucket, { isLoading: isDeleting }] = useDeleteBucketMutation();

  const handleCreate = () => {
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleDelete = (record: Bucket) => {
    setDeletingBucket(record);
    setIsDeleteModalVisible(true);
  };

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields();
      await createBucket({ Name: values.Name }).unwrap();
      message.success('Bucket created successfully');
      setIsModalVisible(false);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.data || error?.data?.message || 'Failed to create bucket');
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingBucket?.Name) return;

    try {
      await deleteBucket({ bucketName: deletingBucket.Name }).unwrap();
      message.success('Bucket deleted successfully');
      setIsDeleteModalVisible(false);
      setDeletingBucket(null);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.data || error?.data?.message || 'Failed to delete bucket');
    }
  };

  const columns: ColumnsType<Bucket> = [
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      ellipsis: true,
      sorter: (a: Bucket, b: Bucket) => (a.Name || '').localeCompare(b.Name || ''),
    },
    {
      title: 'Date Created',
      dataIndex: 'CreationDate',
      key: 'CreationDate',
      ellipsis: true,
      render: (text: string) => formatDate(text),
    },
    {
      title: 'Actions',
      key: 'actions',
      render: (_: any, record: Bucket) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'delete',
            label: 'Delete Bucket',
            onClick: () => handleDelete(record),
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
      const name = bucket.Name?.toLowerCase() ?? '';
      return name.includes(q);
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
      <div className="responsive-scrollbar" style={{ width: '100%' }}>
        <Less3Table
          columns={columns as ColumnsType<any>}
          dataSource={filteredData}
          loading={isLoading}
          rowKey="Name"
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
            label="Bucket Name"
            name="Name"
            rules={[
              { required: true, message: 'Please enter bucket name' },
              { min: 1, message: 'Name must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter bucket name" />
          </Less3FormItem>
        </Form>
      </Less3Modal>

      <Less3Modal
        title="Delete Bucket"
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingBucket(null);
        }}
        confirmLoading={isDeleting}
        okText="Delete"
        okButtonProps={{ danger: true }}
        centered
        footer={[
          <Less3Button key="confirm" type="primary" danger loading={isDeleting} onClick={handleDeleteConfirm}>
            Delete
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          <p>
            Are you sure you want to delete the bucket <strong>&quot;{deletingBucket?.Name}&quot;</strong>?
          </p>
          <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
            This action cannot be undone. All objects in this bucket will be permanently deleted.
          </p>
        </Less3Flex>
      </Less3Modal>
    </PageContainer>
  );
};

export default BucketsPage;
