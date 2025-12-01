/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState } from 'react';
import { Form, message, Descriptions, MenuProps } from 'antd';
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
  useGetUsersQuery,
  useGetUserByIdQuery,
  useCreateUserMutation,
  useDeleteUserMutation,
  User,
} from '#/store/slice/usersSlice';
import type { ColumnsType } from 'antd/es/table';

interface UserFormValues {
  Name: string;
  Email: string;
}

const UsersPage: React.FC = () => {
  const [form] = Form.useForm<UserFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isMetadataModalVisible, setIsMetadataModalVisible] = useState(false);
  const [viewingUserGUID, setViewingUserGUID] = useState<string | null>(null);
  const [deletingUser, setDeletingUser] = useState<User | null>(null);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetUsersQuery();

  const { data: userMetadata, isLoading: isMetadataLoading } = useGetUserByIdQuery(viewingUserGUID || '', {
    skip: !viewingUserGUID,
  });

  const [createUser, { isLoading: isCreating }] = useCreateUserMutation();
  const [deleteUser, { isLoading: isDeleting }] = useDeleteUserMutation();

  const handleCreate = () => {
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleViewMetadata = (record: User) => {
    setViewingUserGUID(record.GUID);
    setIsMetadataModalVisible(true);
  };

  const handleDelete = (record: User) => {
    setDeletingUser(record);
    setIsDeleteModalVisible(true);
  };

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields();
      const createPayload = {
        Name: values.Name,
        Email: values.Email,
      };
      await createUser(createPayload).unwrap();
      message.success('User created successfully');
      setIsModalVisible(false);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to create user');
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingUser?.GUID) return;

    try {
      await deleteUser({ guid: deletingUser.GUID }).unwrap();
      message.success('User deleted successfully');
      setIsDeleteModalVisible(false);
      setDeletingUser(null);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to delete user');
    }
  };

  const columns: ColumnsType<User> = [
    {
      title: 'GUID',
      dataIndex: 'GUID',
      key: 'GUID',
      width: 200,
      sorter: (a: User, b: User) => (a.GUID || '').localeCompare(b.GUID || ''),
    },
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      width: 200,
      sorter: (a: User, b: User) => (a.Name || '').localeCompare(b.Name || ''),
    },
    {
      title: 'Email',
      dataIndex: 'Email',
      key: 'Email',
      width: 250,
      sorter: (a: User, b: User) => (a.Email || '').localeCompare(b.Email || ''),
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
      render: (_: any, record: User) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(record),
          },
          {
            key: 'delete',
            label: 'Delete User',
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

    return data.filter((user) => {
      const guid = user.GUID?.toLowerCase() ?? '';
      const name = user.Name?.toLowerCase() ?? '';
      const email = user.Email?.toLowerCase() ?? '';

      return guid.includes(q) || name.includes(q) || email.includes(q);
    });
  }, [data, searchText]);

  return (
    <PageContainer
      pageTitle="Users"
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Input
            placeholder="Search users..."
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              setSearchText(e.target.value);
            }}
            style={{ width: 250 }}
            allowClear
          />
          <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Create User
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
        title="Create User"
        open={isModalVisible}
        onOk={handleModalOk}
        onCancel={() => {
          setIsModalVisible(false);
          form.resetFields();
        }}
        confirmLoading={isCreating}
        width={600}
        centered
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Less3FormItem
            label="Name"
            name="Name"
            rules={[
              { required: true, message: 'Please enter user name' },
              { min: 1, message: 'Name must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter user name" />
          </Less3FormItem>
          <Less3FormItem
            label="Email"
            name="Email"
            rules={[
              { required: true, message: 'Please enter email address' },
              { type: 'email', message: 'Please enter a valid email address' },
            ]}
          >
            <Less3Input placeholder="Enter email address" type="email" />
          </Less3FormItem>
        </Form>
      </Less3Modal>

      <Less3Modal
        title="Delete User"
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingUser(null);
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
            Are you sure you want to delete the user <strong>&quot;{deletingUser?.Name}&quot;</strong>?
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title="User Metadata"
        open={isMetadataModalVisible}
        onCancel={() => {
          setIsMetadataModalVisible(false);
          setViewingUserGUID(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsMetadataModalVisible(false);
              setViewingUserGUID(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
      >
        {isMetadataLoading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>Loading metadata...</div>
        ) : userMetadata ? (
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="GUID">
              <Less3Text>{userMetadata.GUID}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Name">
              <Less3Text>{userMetadata.Name}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Email">
              <Less3Text>{userMetadata.Email}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Created At">
              <Less3Text>
                {userMetadata.CreatedUtc ? new Date(userMetadata.CreatedUtc).toLocaleString() : '-'}
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

export default UsersPage;
