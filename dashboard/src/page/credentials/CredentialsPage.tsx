/* eslint-disable max-lines-per-function */
'use client';
import React, { useState, useMemo } from 'react';
import { Form, message, Descriptions, MenuProps } from 'antd';
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
  useGetCredentialsQuery,
  useGetCredentialByIdQuery,
  useCreateCredentialMutation,
  useDeleteCredentialMutation,
  Credential,
} from '#/store/slice/credentialsSlice';
import { useGetUsersQuery } from '#/store/slice/usersSlice';
import type { ColumnsType } from 'antd/es/table';
import { formatDate } from '#/utils/dateUtils';

interface CredentialFormValues {
  UserGUID: string;
  Description: string;
  AccessKey: string;
  SecretKey: string;
}

const CredentialsPage: React.FC = () => {
  const [form] = Form.useForm<CredentialFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isMetadataModalVisible, setIsMetadataModalVisible] = useState(false);
  const [viewingCredentialGUID, setViewingCredentialGUID] = useState<string | null>(null);
  const [deletingCredential, setDeletingCredential] = useState<Credential | null>(null);
  const [searchText, setSearchText] = useState('');

  const { data, isLoading, refetch } = useGetCredentialsQuery();

  const { data: usersData } = useGetUsersQuery();

  const { data: credentialMetadata, isLoading: isMetadataLoading } = useGetCredentialByIdQuery(
    viewingCredentialGUID || '',
    {
      skip: !viewingCredentialGUID,
    }
  );

  const [createCredential, { isLoading: isCreating }] = useCreateCredentialMutation();
  const [deleteCredential, { isLoading: isDeleting }] = useDeleteCredentialMutation();

  // Create user options for dropdown (show Name, store GUID)
  const userOptions = useMemo(() => {
    if (!usersData) return [];
    return usersData.map((user) => ({
      value: user.GUID,
      label: user.Name,
    }));
  }, [usersData]);

  // Helper to get username from GUID
  const getUserName = (userGUID: string) => {
    const user = usersData?.find((u) => u.GUID === userGUID);
    return user?.Name || userGUID;
  };

  const handleCreate = () => {
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleViewMetadata = (record: Credential) => {
    setViewingCredentialGUID(record.GUID);
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
        UserGUID: values.UserGUID,
        Description: values.Description,
        AccessKey: values.AccessKey,
        SecretKey: values.SecretKey,
      };
      await createCredential(createPayload).unwrap();
      message.success('Credential created successfully');
      setIsModalVisible(false);
      form.resetFields();
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to create credential');
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingCredential?.GUID) return;

    try {
      await deleteCredential({ guid: deletingCredential.GUID }).unwrap();
      message.success('Credential deleted successfully');
      setIsDeleteModalVisible(false);
      setDeletingCredential(null);
      refetch();
    } catch (error: any) {
      message.error(error?.data?.message || 'Failed to delete credential');
    }
  };

  const columns: ColumnsType<Credential> = [
    {
      title: 'GUID',
      dataIndex: 'GUID',
      key: 'GUID',
      width: 250,
      sorter: (a: Credential, b: Credential) => (a.GUID || '').localeCompare(b.GUID || ''),
    },
    {
      title: 'User',
      dataIndex: 'UserGUID',
      key: 'UserGUID',
      width: 150,
      render: (userGUID: string) => getUserName(userGUID),
    },
    {
      title: 'Description',
      dataIndex: 'Description',
      key: 'Description',
      width: 200,
    },
    {
      title: 'Access Key',
      dataIndex: 'AccessKey',
      key: 'AccessKey',
      width: 150,
    },
    {
      title: 'Date Created',
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 180,
      render: (text: string) => formatDate(text),
    },
    {
      title: 'Actions',
      key: 'actions',
      width: 80,
      fixed: 'right',
      render: (_: any, record: Credential) => {
        const menuItems: MenuProps['items'] = [
          {
            key: 'metadata',
            label: 'View Metadata',
            onClick: () => handleViewMetadata(record),
          },
          {
            key: 'delete',
            label: 'Delete Credential',
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

    return data.filter((cred) => {
      const guid = cred.GUID?.toLowerCase() ?? '';
      const desc = cred.Description?.toLowerCase() ?? '';
      const accessKey = cred.AccessKey?.toLowerCase() ?? '';
      const userName = getUserName(cred.UserGUID)?.toLowerCase() ?? '';

      return guid.includes(q) || desc.includes(q) || accessKey.includes(q) || userName.includes(q);
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
          <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            Create Credential
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
        title="Create Credential"
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
          <Less3FormItem label="User" name="UserGUID" rules={[{ required: true, message: 'Please select a user' }]}>
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
          setViewingCredentialGUID(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsMetadataModalVisible(false);
              setViewingCredentialGUID(null);
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
            <Descriptions.Item label="GUID">
              <Less3Text>{credentialMetadata.GUID}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="User">
              <Less3Text>{getUserName(credentialMetadata.UserGUID)}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Description">
              <Less3Text>{credentialMetadata.Description}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Access Key">
              <Less3Text>{credentialMetadata.AccessKey}</Less3Text>
            </Descriptions.Item>
            <Descriptions.Item label="Secret Key">
              <Less3Text>{credentialMetadata.SecretKey}</Less3Text>
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
