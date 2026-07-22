/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo } from 'react';
import { Form, Descriptions, MenuProps } from 'antd';
import { PlusOutlined, SearchOutlined, MoreOutlined, ReloadOutlined } from '@ant-design/icons';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import Less3Button from '#/components/base/button/Button';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Text from '#/components/base/typograpghy/Text';
import IdDisplay from '#/components/id-display';
import TextWithCopy from '#/components/text-with-copy/TextWithCopy';
import {
  useGetCredentialsQuery,
  useGetCredentialByIdQuery,
  useCreateCredentialMutation,
  useUpdateCredentialMutation,
  useDeleteCredentialMutation,
  Credential,
} from '#/store/slice/credentialsSlice';
import { useGetUsersQuery } from '#/store/slice/usersSlice';
import { formatDate } from '#/utils/dateUtils';
import { message } from '#/utils/message';

interface CredentialFormValues {
  UserId: string;
  Description: string;
  AccessKey: string;
  SecretKey: string;
}

const CredentialsPage: React.FC = () => {
  const [form] = Form.useForm<CredentialFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isMetadataModalVisible, setIsMetadataModalVisible] = useState(false);
  const [editingCredential, setEditingCredential] = useState<Credential | null>(null);
  const [viewingCredentialId, setViewingCredentialId] = useState<string | null>(null);
  const [deletingCredential, setDeletingCredential] = useState<Credential | null>(null);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetCredentialsQuery();

  const { data: usersData } = useGetUsersQuery();

  const { data: credentialMetadata, isLoading: isMetadataLoading } = useGetCredentialByIdQuery(
    viewingCredentialId || '',
    {
      skip: !viewingCredentialId,
    }
  );

  const [createCredential, { isLoading: isCreating }] = useCreateCredentialMutation();
  const [updateCredential, { isLoading: isUpdating }] = useUpdateCredentialMutation();
  const [deleteCredential, { isLoading: isDeleting }] = useDeleteCredentialMutation();

  // Create user options for dropdown (show Name, store Id)
  const userOptions = useMemo(() => {
    if (!usersData) return [];
    return usersData.map((user) => ({
      value: user.Id,
      label: user.Name,
    }));
  }, [usersData]);

  // Helper to get username from Id
  const getUserName = (userId: string) => {
    const user = usersData?.find((u) => u.Id === userId);
    return user?.Name || userId;
  };

  const handleCreate = () => {
    setEditingCredential(null);
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleEdit = (record: Credential) => {
    setEditingCredential(record);
    form.setFieldsValue({
      UserId: record.UserId,
      Description: record.Description,
      AccessKey: record.AccessKey,
      SecretKey: record.SecretKey,
    });
    setIsModalVisible(true);
  };

  const handleViewMetadata = (record: Credential) => {
    setViewingCredentialId(record.Id);
    setIsMetadataModalVisible(true);
  };

  const handleDelete = (record: Credential) => {
    setDeletingCredential(record);
    setIsDeleteModalVisible(true);
  };

  const handleModalOk = async () => {
    try {
      const values = await form.validateFields();
      const createPayload = {
        UserId: values.UserId,
        Description: values.Description,
        AccessKey: values.AccessKey,
        SecretKey: values.SecretKey,
      };

      if (editingCredential?.Id) {
        await updateCredential({
          Id: editingCredential.Id,
          IsBase64: editingCredential.IsBase64,
          ...createPayload,
        }).unwrap();
        message.success('Credential updated successfully');
      } else {
        await createCredential(createPayload).unwrap();
        message.success('Credential created successfully');
      }

      setIsModalVisible(false);
      setEditingCredential(null);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || `Failed to ${editingCredential ? 'update' : 'create'} credential`);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingCredential?.Id) return;

    try {
      await deleteCredential({ id: deletingCredential.Id }).unwrap();
      message.success('Credential deleted successfully');
      setIsDeleteModalVisible(false);
      setDeletingCredential(null);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to delete credential');
    }
  };

  const columns: DataTableColumn<Credential>[] = [
    {
      key: 'Id',
      label: 'Id',
      width: '320px',
      render: (item) => <IdDisplay id={item.Id} />,
    },
    {
      key: 'UserId',
      label: 'User',
      width: '150px',
      render: (item) => getUserName(item.UserId),
      filterValue: (item) => getUserName(item.UserId),
    },
    {
      key: 'Description',
      label: 'Description',
      width: '200px',
    },
    {
      key: 'AccessKey',
      label: 'Access Key',
      width: '250px',
      render: (item) => <TextWithCopy text={item.AccessKey} className="code-font-style" />,
      filterValue: (item) => item.AccessKey,
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
            label: 'Edit Credential',
            onClick: () => handleEdit(item),
          },
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(item),
          },
          {
            key: 'delete',
            label: 'Delete Credential',
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

    return data.filter((cred) => {
      const id = cred.Id?.toLowerCase() ?? '';
      const desc = cred.Description?.toLowerCase() ?? '';
      const accessKey = cred.AccessKey?.toLowerCase() ?? '';
      const userName = getUserName(cred.UserId)?.toLowerCase() ?? '';

      return id.includes(q) || desc.includes(q) || accessKey.includes(q) || userName.includes(q);
    });
  }, [data, searchText, usersData]); // usersData is used via getUserName

  return (
    <PageContainer
      pageTitle="Credentials"
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Input
            placeholder="Search credentials..."
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
            Create Credential
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
        title={editingCredential ? 'Edit Credential' : 'Create Credential'}
        open={isModalVisible}
        forceRender
        onOk={handleModalOk}
        onCancel={() => {
          setIsModalVisible(false);
          setEditingCredential(null);
          form.resetFields();
        }}
        confirmLoading={isCreating || isUpdating}
        width={600}
        centered
      >
        <Form form={form} layout="vertical" autoComplete="off">
          <Less3FormItem label="User" name="UserId" rules={[{ required: true, message: 'Please select a user' }]}>
            <Less3Select
              options={userOptions}
              placeholder="Select user"
              showSearch
              filterOption={(input, option) =>
                (option?.label as string)?.toLowerCase().includes((input as string).toLowerCase())
              }
            />
          </Less3FormItem>
          <Less3FormItem
            label="Description"
            name="Description"
            rules={[
              { required: true, message: 'Please enter description' },
              { min: 1, message: 'Description must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter description" />
          </Less3FormItem>
          <Less3FormItem
            label="Access Key"
            name="AccessKey"
            rules={[
              { required: true, message: 'Please enter access key' },
              { min: 1, message: 'Access key must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter access key" />
          </Less3FormItem>
          <Less3FormItem
            label="Secret Key"
            name="SecretKey"
            rules={[
              { required: true, message: 'Please enter secret key' },
              { min: 1, message: 'Secret key must be at least 1 character' },
            ]}
          >
            <Less3Input placeholder="Enter secret key" type="password" />
          </Less3FormItem>
        </Form>
      </Less3Modal>

      <Less3Modal
        title="Delete Credential"
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingCredential(null);
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
            Are you sure you want to delete the credential{' '}
            <strong>&quot;{deletingCredential?.Description}&quot;</strong>?
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title="Credential Metadata"
        open={isMetadataModalVisible}
        onCancel={() => {
          setIsMetadataModalVisible(false);
          setViewingCredentialId(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsMetadataModalVisible(false);
              setViewingCredentialId(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
      >
        {isMetadataLoading ? (
          <div style={{ textAlign: 'center', padding: '20px' }}>Loading metadata...</div>
        ) : credentialMetadata ? (
          <Descriptions bordered column={1} size="small">
            <Descriptions.Item label="Id">
              <IdDisplay id={credentialMetadata.Id} />
            </Descriptions.Item>
            <Descriptions.Item label="User">
              <Less3Text>{getUserName(credentialMetadata.UserId)}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Description">
              <Less3Text>{credentialMetadata.Description}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Access Key">
              <TextWithCopy text={credentialMetadata.AccessKey} className="code-font-style" />
            </Descriptions.Item>
            <Descriptions.Item label="Secret Key">
              <Less3Text type="secondary">Hidden</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Is Base64">
              <Less3Text>{credentialMetadata.IsBase64 ? 'Yes' : 'No'}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Created At">
              <Less3Text>
                {formatDate(credentialMetadata.CreatedUtc || '')}
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

export default CredentialsPage;
