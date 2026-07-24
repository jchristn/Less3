'use client';
import React, { useMemo, useState } from 'react';
import { Form, Descriptions, MenuProps, Checkbox } from 'antd';
import { PlusOutlined, SearchOutlined, MoreOutlined, ReloadOutlined, TeamOutlined, HistoryOutlined } from '@ant-design/icons';
import { useRouter } from 'next/navigation';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Text from '#/components/base/typograpghy/Text';
import IdDisplay from '#/components/id-display';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import {
  useGetUsersQuery,
  useGetUserByIdQuery,
  useCreateUserMutation,
  useUpdateUserMutation,
  useDeleteUserMutation,
  User,
} from '#/store/slice/usersSlice';
import { formatDate } from '#/utils/dateUtils';
import { message } from '#/utils/message';

interface UserFormValues {
  TenantId?: string;
  Name: string;
  Email: string;
  PasswordHash?: string;
  Active?: boolean;
  IsAdmin?: boolean;
  IsTenantAdmin?: boolean;
}

const UsersPage: React.FC = () => {
  const router = useRouter();
  const [form] = Form.useForm<UserFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isMetadataModalVisible, setIsMetadataModalVisible] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [viewingUserId, setViewingUserId] = useState<string | null>(null);
  const [deletingUser, setDeletingUser] = useState<User | null>(null);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetUsersQuery();

  const { data: userMetadata, isLoading: isMetadataLoading } = useGetUserByIdQuery(viewingUserId || '', {
    skip: !viewingUserId,
  });

  const [createUser, { isLoading: isCreating }] = useCreateUserMutation();
  const [updateUser, { isLoading: isUpdating }] = useUpdateUserMutation();
  const [deleteUser, { isLoading: isDeleting }] = useDeleteUserMutation();

  const handleCreate = () => {
    setEditingUser(null);
    form.resetFields();
    form.setFieldsValue({
      TenantId: 'default',
      Active: true,
      IsAdmin: false,
      IsTenantAdmin: false,
    });
    setIsModalVisible(true);
  };

  const handleEdit = (record: User) => {
    setEditingUser(record);
    form.setFieldsValue({
      TenantId: record.TenantId || 'default',
      Name: record.Name,
      Email: record.Email,
      Active: record.Active ?? true,
      IsAdmin: record.IsAdmin ?? false,
      IsTenantAdmin: record.IsTenantAdmin ?? false,
    });
    setIsModalVisible(true);
  };

  const handleViewMetadata = (record: User) => {
    setViewingUserId(record.Id);
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
        TenantId: values.TenantId || editingUser?.TenantId || 'default',
        Name: values.Name,
        Email: values.Email,
        ...(values.PasswordHash ? { PasswordHash: values.PasswordHash } : {}),
        Active: values.Active ?? true,
        IsAdmin: values.IsAdmin ?? false,
        IsTenantAdmin: values.IsTenantAdmin ?? false,
      };

      if (editingUser?.Id) {
        await updateUser({
          Id: editingUser.Id,
          ...createPayload,
        }).unwrap();
        message.success('User updated successfully');
      } else {
        await createUser(createPayload).unwrap();
        message.success('User created successfully');
      }

      setIsModalVisible(false);
      setEditingUser(null);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || `Failed to ${editingUser ? 'update' : 'create'} user`);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingUser?.Id) return;

    try {
      await deleteUser({ id: deletingUser.Id }).unwrap();
      message.success('User deleted successfully');
      setIsDeleteModalVisible(false);
      setDeletingUser(null);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to delete user');
    }
  };

  const columns: DataTableColumn<User>[] = [
    {
      key: 'Id',
      label: 'Id',
      width: '320px',
      render: (item) => <IdDisplay id={item.Id} />,
    },
    {
      key: 'TenantId',
      label: 'Tenant',
      width: '160px',
      render: (item) => <IdDisplay id={item.TenantId || 'default'} />,
      filterValue: (item) => item.TenantId || 'default',
    },
    {
      key: 'Name',
      label: 'Name',
      width: '200px',
    },
    {
      key: 'Email',
      label: 'Email',
      width: '280px',
      render: (item) => <TextWithCopy text={item.Email} className="code-font-style" />,
      filterValue: (item) => item.Email,
    },
    {
      key: 'Active',
      label: 'Active',
      width: '90px',
      render: (item) => (item.Active ?? true ? 'Yes' : 'No'),
      filterValue: (item) => (item.Active ?? true ? 'Yes' : 'No'),
    },
    {
      key: 'IsAdmin',
      label: 'System Admin',
      width: '130px',
      render: (item) => (item.IsAdmin ? 'Yes' : 'No'),
      filterValue: (item) => (item.IsAdmin ? 'Yes' : 'No'),
    },
    {
      key: 'IsTenantAdmin',
      label: 'Tenant Admin',
      width: '130px',
      render: (item) => (item.IsTenantAdmin ? 'Yes' : 'No'),
      filterValue: (item) => (item.IsTenantAdmin ? 'Yes' : 'No'),
    },
    {
      key: 'CreatedUtc',
      label: 'Date Created',
      width: '180px',
      render: (item) => formatDate(item.CreatedUtc),
      filterValue: (item) => formatDate(item.CreatedUtc),
    },
    {
      key: 'actions',
      label: 'Actions',
      width: '80px',
      isAction: true,
      sortable: false,
      filterable: false,
      render: (item) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'edit',
            label: 'Edit User',
            onClick: () => handleEdit(item),
          },
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(item),
          },
          {
            key: 'roles',
            icon: <TeamOutlined />,
            label: 'Role Assignments',
            onClick: () => router.push(`/admin/role-assignments?principalId=${encodeURIComponent(item.Id)}`),
          },
          {
            key: 'sessions',
            icon: <HistoryOutlined />,
            label: 'Sessions',
            onClick: () => router.push(`/admin/api-explorer?operation=rest-list-authsessions&userId=${encodeURIComponent(item.Id)}`),
          },
          {
            key: 'delete',
            label: 'Delete User',
            onClick: () => handleDelete(item),
          },
        ];

        return (
          <Less3Dropdown menu={{ items: menuItems }} trigger={['click']}>
            <Less3Button type="text" icon={<MoreOutlined />} size="small" onClick={(event) => event.stopPropagation()} />
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
      const id = user.Id?.toLowerCase() ?? '';
      const tenantId = user.TenantId?.toLowerCase() ?? '';
      const name = user.Name?.toLowerCase() ?? '';
      const email = user.Email?.toLowerCase() ?? '';

      return id.includes(q) || tenantId.includes(q) || name.includes(q) || email.includes(q);
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
          <Less3Button icon={<ReloadOutlined />} onClick={() => refetch()} loading={isLoading}>
            Refresh
          </Less3Button>
          <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Create User
          </Less3Button>
        </Less3Flex>
      }
    >
      <DataTable
        columns={columns}
        data={filteredData}
        loading={isLoading}
        rowKey="Id"
        onRowClick={handleEdit}
      />

      <Less3Modal
        title={editingUser ? 'Edit User' : 'Create User'}
        open={isModalVisible}
        forceRender
        onOk={handleModalOk}
        onCancel={() => {
          setIsModalVisible(false);
          setEditingUser(null);
          form.resetFields();
        }}
        confirmLoading={isCreating || isUpdating}
        width={600}
        centered
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Less3FormItem
            label="Tenant ID"
            name="TenantId"
            rules={[{ required: true, message: 'Please enter tenant ID' }]}
          >
            <Less3Input placeholder="default" />
          </Less3FormItem>
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
          <Less3FormItem
            label="Password Hash"
            name="PasswordHash"
          >
            <Less3Input placeholder={editingUser ? 'Leave unchanged' : 'Enter password or password hash'} type="password" />
          </Less3FormItem>
          <Less3Flex gap={16} align="center">
            <Less3FormItem name="Active" valuePropName="checked">
              <Checkbox>Active</Checkbox>
            </Less3FormItem>
            <Less3FormItem name="IsAdmin" valuePropName="checked">
              <Checkbox>System Admin</Checkbox>
            </Less3FormItem>
            <Less3FormItem name="IsTenantAdmin" valuePropName="checked">
              <Checkbox>Tenant Admin</Checkbox>
            </Less3FormItem>
          </Less3Flex>
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
          setViewingUserId(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsMetadataModalVisible(false);
              setViewingUserId(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
        centered
      >
        {isMetadataLoading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>Loading metadata...</div>
        ) : userMetadata ? (
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="ID">
              <IdDisplay id={userMetadata.Id} />
            </Descriptions.Item>
            <Descriptions.Item label="Name">
              <Less3Text>{userMetadata.Name}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Tenant ID">
              <IdDisplay id={userMetadata.TenantId || 'default'} />
            </Descriptions.Item>
            <Descriptions.Item label="Email">
              <TextWithCopy text={userMetadata.Email} className="code-font-style" />
            </Descriptions.Item>
            <Descriptions.Item label="Active">
              <Less3Text>{userMetadata.Active ?? true ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="System Admin">
              <Less3Text>{userMetadata.IsAdmin ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Tenant Admin">
              <Less3Text>{userMetadata.IsTenantAdmin ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Created At">
              <Less3Text>
                {formatDate(userMetadata.CreatedUtc || '')}
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
