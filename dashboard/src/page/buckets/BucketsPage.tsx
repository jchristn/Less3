/* eslint-disable max-lines-per-function */
'use client';
import React, { useMemo, useState, useEffect } from 'react';
import { Form, message, MenuProps } from 'antd';
import {
  PlusOutlined,
  SearchOutlined,
  MoreOutlined,
  DeleteOutlined,
  TagOutlined,
  SafetyOutlined,
  FolderOutlined,
  FolderOpenOutlined,
} from '@ant-design/icons';
import { useRouter } from 'next/navigation';
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
  useCreateBucketMutation,
  useDeleteBucketMutation,
  useWriteBucketTagsMutation,
  useGetBucketTagsQuery,
  useDeleteBucketTagsMutation,
  useWriteBucketACLMutation,
  useGetBucketACLQuery,
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';
import { formatDate } from '#/utils/dateUtils';
import type { ACLOwner, ACLGrant, ACLGrantee } from '#/utils/xmlUtils';
import type { BucketTag } from '#/utils/xmlUtils';
import { Less3Theme } from '#/theme/theme';
import { useAppContext } from '#/hooks/appHooks';
import { ThemeEnum } from '#/types/types';

interface BucketFormValues {
  Name: string;
}

interface TagFormValues {
  tags: Array<{ Key: string; Value: string }>;
}

interface ACLFormValues {
  grantee: {
    ID: string;
    DisplayName: string;
  };
  Permission: string;
}

const PERMISSION_OPTIONS = [
  { label: 'Read', value: 'READ' },
  { label: 'Write', value: 'WRITE' },
  { label: 'Read ACP', value: 'READ_ACP' },
  { label: 'Write ACP', value: 'WRITE_ACP' },
  { label: 'Full Control', value: 'FULL_CONTROL' },
];

const DEFAULT_ACL_OWNER = {
  ID: 'default',
  DisplayName: 'default',
};

const BucketsPage: React.FC = () => {
  const { theme } = useAppContext();
  const router = useRouter();
  const [form] = Form.useForm<BucketFormValues>();
  const [tagForm] = Form.useForm<TagFormValues>();
  const [aclForm] = Form.useForm<ACLFormValues>();
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isWriteTagsModalVisible, setIsWriteTagsModalVisible] = useState(false);
  const [isViewTagsModalVisible, setIsViewTagsModalVisible] = useState(false);
  const [isDeleteTagsModalVisible, setIsDeleteTagsModalVisible] = useState(false);
  const [isWriteACLModalVisible, setIsWriteACLModalVisible] = useState(false);
  const [isViewACLModalVisible, setIsViewACLModalVisible] = useState(false);
  const [selectedBucket, setSelectedBucket] = useState<Bucket | null>(null);
  const [deletingBucket, setDeletingBucket] = useState<Bucket | null>(null);
  const [searchText, setSearchText] = useState('');
  const [openDropdownKey, setOpenDropdownKey] = useState<string | null>(null);

  const { data, isLoading, refetch } = useGetBucketsQuery();

  const [createBucket, { isLoading: isCreating }] = useCreateBucketMutation();
  const [deleteBucket, { isLoading: isDeleting }] = useDeleteBucketMutation();
  const [writeBucketTags, { isLoading: isWritingTags }] = useWriteBucketTagsMutation();
  const [deleteBucketTags, { isLoading: isDeletingTags }] = useDeleteBucketTagsMutation();
  const [writeBucketACL, { isLoading: isWritingACL }] = useWriteBucketACLMutation();

  const shouldFetchTags = selectedBucket && (isViewTagsModalVisible || isWriteTagsModalVisible);
  const shouldFetchACL = selectedBucket && (isViewACLModalVisible || isWriteACLModalVisible);

  const {
    data: bucketTagsData,
    isLoading: isLoadingTags,
    isError: isTagsError,
    error: tagsError,
    refetch: refetchTags,
  } = useGetBucketTagsQuery(
    { bucketName: selectedBucket?.Name || '' },
    {
      skip: !shouldFetchTags,
    }
  );

  const {
    data: bucketACLData,
    isLoading: isLoadingACL,
    isError: isACLError,
    error: aclError,
    refetch: refetchACL,
  } = useGetBucketACLQuery({ bucketName: selectedBucket?.Name || '' }, { skip: !shouldFetchACL });

  // Pre-populate form with existing tags when write modal opens and tags are loaded
  useEffect(() => {
    if (isWriteTagsModalVisible && selectedBucket) {
      // Wait for loading to complete
      if (!isLoadingTags) {
        if (bucketTagsData !== undefined) {
          const existingTags = bucketTagsData.tags || [];

          if (existingTags.length > 0) {
            // Pre-populate with existing tags
            tagForm.setFieldsValue({
              tags: existingTags.map((tag) => ({ Key: tag.Key, Value: tag.Value })),
            });
          } else {
            // Start with one empty tag field if no tags exist
            tagForm.setFieldsValue({ tags: [{ Key: '', Value: '' }] });
          }
        } else if (isTagsError) {
          // If there's an error (like 404), start with empty form
          tagForm.setFieldsValue({ tags: [{ Key: '', Value: '' }] });
        }
      }
    } else if (!isWriteTagsModalVisible) {
      // Reset form when modal closes
      tagForm.resetFields();
    }
  }, [isWriteTagsModalVisible, selectedBucket, bucketTagsData, isLoadingTags, isTagsError, tagForm]);

  const handleCreate = () => {
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleDelete = (record: Bucket) => {
    setDeletingBucket(record);
    setIsDeleteModalVisible(true);
  };

  const handleViewObjects = (record: Bucket) => {
    router.push(`/admin/objects?bucket=${encodeURIComponent(record.Name)}`);
  };

  const handleWriteTags = (record: Bucket) => {
    setSelectedBucket(record);
    setIsWriteTagsModalVisible(true);
    // Tags will be pre-populated via useEffect when data is available
  };

  const handleViewTags = (record: Bucket) => {
    setSelectedBucket(record);
    setIsViewTagsModalVisible(true);
  };

  const handleDeleteTags = (record: Bucket) => {
    setSelectedBucket(record);
    setIsDeleteTagsModalVisible(true);
  };

  const handleWriteACL = (record: Bucket) => {
    setSelectedBucket(record);
    setIsWriteACLModalVisible(true);
  };

  const handleViewACL = (record: Bucket) => {
    setSelectedBucket(record);
    setIsViewACLModalVisible(true);
  };

  const handleWriteACLOk = async () => {
    if (!selectedBucket?.Name) return;

    try {
      const values = await aclForm.validateFields();

      const owner: ACLOwner = bucketACLData?.acl?.Owner || DEFAULT_ACL_OWNER;

      const grantee: ACLGrantee = {
        ID: values.grantee.ID.trim() || owner.ID || DEFAULT_ACL_OWNER.ID,
        DisplayName: values.grantee.DisplayName.trim(),
        Type: 'CanonicalUser',
      };

      const grants: ACLGrant[] = [
        {
          Grantee: grantee,
          Permission: values.Permission,
        },
      ];

      await writeBucketACL({ bucketName: selectedBucket.Name, owner, grants }).unwrap();
      message.success('Bucket ACL written successfully');

      // Refetch ACL before closing modal (while query is still active)
      if (shouldFetchACL) {
        refetchACL();
      }

      // Close modal and reset state
      setIsWriteACLModalVisible(false);
      setSelectedBucket(null);
      aclForm.resetFields();
    } catch (error: any) {
      // Check if it's a validation error (from Ant Design form)
      if (error?.errorFields || (Array.isArray(error) && error.length > 0 && error[0]?.name)) {
        // Validation error - form will show field errors, don't show generic error message
        return;
      }
      // API error - only show if it's not a validation issue
      const errorMessage = error?.data?.data || error?.data?.message || error?.message;
      if (errorMessage && !errorMessage.includes('validation') && !errorMessage.includes('required')) {
        message.error(errorMessage || 'Failed to write bucket ACL');
      }
    }
  };

  // Pre-populate form with existing ACL when write modal opens and ACL is loaded
  useEffect(() => {
    if (isWriteACLModalVisible && selectedBucket) {
      if (!isLoadingACL) {
        if (bucketACLData?.acl) {
          const acl = bucketACLData.acl;
          const grants = Array.isArray(acl.AccessControlList.Grant)
            ? acl.AccessControlList.Grant
            : [acl.AccessControlList.Grant];

          if (grants.length > 0) {
            const firstGrant = grants[0];
            aclForm.setFieldsValue({
              grantee: {
                ID: firstGrant.Grantee.ID || acl.Owner.ID || DEFAULT_ACL_OWNER.ID,
                DisplayName: firstGrant.Grantee.DisplayName || '',
              },
              Permission: firstGrant.Permission || 'FULL_CONTROL',
            });
          } else {
            aclForm.setFieldsValue({
              grantee: { ID: acl.Owner.ID || DEFAULT_ACL_OWNER.ID, DisplayName: '' },
              Permission: 'FULL_CONTROL',
            });
          }
        } else if (isACLError) {
          aclForm.setFieldsValue({
            grantee: { ID: DEFAULT_ACL_OWNER.ID, DisplayName: '' },
            Permission: 'FULL_CONTROL',
          });
        }
      }
    } else if (!isWriteACLModalVisible) {
      aclForm.resetFields();
    }
  }, [isWriteACLModalVisible, selectedBucket, bucketACLData, isLoadingACL, isACLError, aclForm]);

  const handleWriteTagsOk = async () => {
    if (!selectedBucket?.Name) return;

    try {
      const values = await tagForm.validateFields();
      const tags: BucketTag[] = (values.tags || [])
        .filter((tag) => tag.Key?.trim() && tag.Value?.trim())
        .map((tag) => ({
          Key: tag.Key.trim(),
          Value: tag.Value.trim(),
        }));

      if (tags.length === 0) {
        message.error('Please add at least one tag with both Key and Value');
        return;
      }

      await writeBucketTags({ bucketName: selectedBucket.Name, tags }).unwrap();
      message.success(`Bucket ${tags.length} tag(s) written successfully`);

      // Refetch tags before closing modal (while query is still active)
      if (shouldFetchTags) {
        refetchTags();
      }

      // Close modal and reset state
      setIsWriteTagsModalVisible(false);
      setSelectedBucket(null);
      tagForm.resetFields();
    } catch (error: any) {
      // Check if it's a validation error (from Ant Design form)
      if (error?.errorFields || (Array.isArray(error) && error.length > 0 && error[0]?.name)) {
        // Validation error - form will show field errors, don't show generic error message
        return;
      }
      // API error - only show if it's not a validation issue
      const errorMessage = error?.data?.data || error?.data?.message || error?.message;
      if (errorMessage && !errorMessage.includes('validation') && !errorMessage.includes('required')) {
        message.error(errorMessage || 'Failed to write bucket tags');
      }
    }
  };

  const handleDeleteTagsConfirm = async () => {
    if (!selectedBucket?.Name) return;

    try {
      await deleteBucketTags({ bucketName: selectedBucket.Name }).unwrap();
      message.success('Bucket tags deleted successfully');

      // Refetch tags before closing modal (while query is still active)
      if (shouldFetchTags) {
        refetchTags();
      }

      // Close modal and reset state
      setIsDeleteTagsModalVisible(false);
      setSelectedBucket(null);
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to delete bucket tags');
    }
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
      render: (name: string, record: Bucket) => (
        <Less3Flex align="center" gap={8}>
          <FolderOutlined style={{ color: 'var(--ant-color-primary)', fontSize: 16 }} />
          <span
            style={{ cursor: 'pointer', color: 'var(--ant-color-primary)' }}
            onClick={() => handleViewObjects(record)}
          >
            {name}
          </span>
        </Less3Flex>
      ),
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
        const dropdownKey = record.Name || '';
        const isOpen = openDropdownKey === dropdownKey;

        const menuItems: MenuProps['items'] = [
          {
            key: 'view-objects',
            icon: <FolderOpenOutlined />,
            label: 'View Objects',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleViewObjects(record);
            },
          },
          {
            type: 'divider',
          },
          {
            key: 'write-tags',
            label: 'Write Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleWriteTags(record);
            },
          },
          {
            key: 'read-tags',
            label: 'Read Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleViewTags(record);
            },
          },
          {
            key: 'delete-tags',
            label: 'Delete Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleDeleteTags(record);
            },
          },
          {
            type: 'divider',
          },
          {
            key: 'write-acl',
            label: 'Write ACL',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleWriteACL(record);
            },
          },
          {
            key: 'read-acl',
            label: 'Read ACL',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleViewACL(record);
            },
          },
          {
            type: 'divider',
          },
          {
            key: 'delete',
            label: 'Delete Bucket',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleDelete(record);
            },
          },
        ];

        return (
          <Less3Dropdown
            menu={{ items: menuItems }}
            trigger={['click']}
            open={isOpen}
            onOpenChange={(open) => {
              setOpenDropdownKey(open ? dropdownKey : null);
            }}
          >
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
        keyboard={true}
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
        keyboard={true}
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

      <Less3Modal
        title={'Write Tags'}
        open={isWriteTagsModalVisible}
        onOk={handleWriteTagsOk}
        onCancel={() => {
          setIsWriteTagsModalVisible(false);
          setSelectedBucket(null);
          tagForm.resetFields();
        }}
        confirmLoading={isWritingTags}
        width={700}
        centered
        maskClosable={true}
        closable={true}
        keyboard={true}
      >
        <Form form={tagForm} layout="vertical" autoComplete="off">
          <Form.List name="tags">
            {(fields, { add, remove }) => (
              <>
                {fields.map(({ key, name, ...restField }) => (
                  <Less3Flex key={key} gap={10} align="flex-start">
                    <Less3FormItem
                      {...restField}
                      name={[name, 'Key']}
                      label={key === 0 ? 'Key' : ''}
                      rules={[{ required: true, message: 'Please enter tag key' }]}
                      style={{ flex: 1 }}
                    >
                      <Less3Input placeholder="Enter tag key" />
                    </Less3FormItem>
                    <Less3FormItem
                      {...restField}
                      name={[name, 'Value']}
                      label={key === 0 ? 'Value' : ''}
                      rules={[{ required: true, message: 'Please enter tag value' }]}
                      style={{ flex: 1 }}
                    >
                      <Less3Input placeholder="Enter tag value" />
                    </Less3FormItem>
                    {fields.length > 1 && (
                      <Less3FormItem label={key === 0 ? ' ' : ''}>
                        <Less3Button
                          type="text"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={() => remove(name)}
                          style={{ marginTop: key === 0 ? '32px' : '0' }}
                        />
                      </Less3FormItem>
                    )}
                  </Less3Flex>
                ))}
                <Less3FormItem>
                  <Less3Button type="dashed" onClick={() => add()} block icon={<PlusOutlined />}>
                    Add Tag
                  </Less3Button>
                </Less3FormItem>
              </>
            )}
          </Form.List>
        </Form>
      </Less3Modal>

      <Less3Modal
        title={'Read Tags'}
        open={isViewTagsModalVisible}
        onCancel={() => {
          setIsViewTagsModalVisible(false);
          setSelectedBucket(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsViewTagsModalVisible(false);
              setSelectedBucket(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
        centered
        keyboard={true}
      >
        {isLoadingTags ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>Loading tags...</div>
        ) : isTagsError ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="danger">
              {tagsError && typeof tagsError === 'object' && 'data' in tagsError
                ? (tagsError.data as string) || 'Failed to load tags'
                : 'Failed to load tags'}
            </Less3Text>
          </div>
        ) : bucketTagsData?.tags && bucketTagsData.tags.length > 0 ? (
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
                  title: 'Value',
                  dataIndex: 'Value',
                  key: 'Value',
                  ellipsis: true,
                },
              ]}
              dataSource={bucketTagsData.tags.map((tag, index) => ({ ...tag, key: index }))}
              loading={isLoadingTags}
              pagination={false}
              scroll={{ x: true }}
            />
          </div>
        ) : (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="secondary">No tags found for this bucket</Less3Text>
          </div>
        )}
      </Less3Modal>

      <Less3Modal
        title={`Delete Tags - ${selectedBucket?.Name || ''}`}
        open={isDeleteTagsModalVisible}
        onCancel={() => {
          setIsDeleteTagsModalVisible(false);
          setSelectedBucket(null);
        }}
        confirmLoading={isDeletingTags}
        okText="Delete"
        okButtonProps={{ danger: true }}
        centered
        keyboard={true}
        footer={[
          <Less3Button key="confirm" type="primary" danger loading={isDeletingTags} onClick={handleDeleteTagsConfirm}>
            Delete
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          <p>
            Are you sure you want to delete the created tags for bucket{' '}
            <strong>&quot;{selectedBucket?.Name}&quot;</strong>?
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title={`Write ACL - ${selectedBucket?.Name || ''}`}
        open={isWriteACLModalVisible}
        onOk={handleWriteACLOk}
        onCancel={() => {
          setIsWriteACLModalVisible(false);
          setSelectedBucket(null);
          aclForm.resetFields();
        }}
        confirmLoading={isWritingACL}
        width={700}
        centered
        maskClosable={true}
        closable={true}
        keyboard={true}
      >
        <Form form={aclForm} layout="vertical" autoComplete="off">
          <Less3Text strong style={{ marginBottom: '16px' }}>
            Grantee
          </Less3Text>
          <Less3Flex gap={10}>
            <Less3FormItem
              label="Grantee ID"
              name={['grantee', 'ID']}
              rules={[{ required: true, message: 'Please enter grantee ID' }]}
              style={{ flex: 1 }}
            >
              <Less3Input
                placeholder="Enter grantee ID"
                disabled
                style={{
                  backgroundColor: theme === ThemeEnum.DARK ? '#333333' : Less3Theme.colorBgContainerDisabled,
                  color: Less3Theme.textDisabled,
                  cursor: 'not-allowed',
                  borderColor: theme === ThemeEnum.DARK ? '#444444' : Less3Theme.borderSecondary,
                }}
              />
            </Less3FormItem>
            <Less3FormItem
              label="Grantee Display Name"
              name={['grantee', 'DisplayName']}
              rules={[{ required: true, message: 'Please enter grantee display name' }]}
              style={{ flex: 1 }}
            >
              <Less3Input placeholder="Enter grantee display name" />
            </Less3FormItem>
          </Less3Flex>

          <Less3FormItem
            label="Permission"
            name="Permission"
            rules={[{ required: true, message: 'Please select permission' }]}
          >
            <Less3Select options={PERMISSION_OPTIONS} placeholder="Select permission" />
          </Less3FormItem>
        </Form>
      </Less3Modal>

      <Less3Modal
        title={'Read ACL'}
        open={isViewACLModalVisible}
        onCancel={() => {
          setIsViewACLModalVisible(false);
          setSelectedBucket(null);
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsViewACLModalVisible(false);
              setSelectedBucket(null);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
        centered
        keyboard={true}
      >
        {isLoadingACL ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>Loading ACL...</div>
        ) : isACLError ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="danger">
              {aclError && typeof aclError === 'object' && 'data' in aclError
                ? (aclError.data as string) || 'Failed to load ACL'
                : 'Failed to load ACL'}
            </Less3Text>
          </div>
        ) : bucketACLData?.acl ? (
          <Less3Flex vertical gap={16}>
            <div>
              <Less3Text strong>Owner</Less3Text>
              <Less3Flex vertical gap={8} style={{ marginTop: '8px', paddingLeft: '16px' }}>
                <Less3Text>ID: {bucketACLData.acl.Owner.ID}</Less3Text>
                <Less3Text>Display Name: {bucketACLData.acl.Owner.DisplayName}</Less3Text>
              </Less3Flex>
            </div>
            <div>
              <Less3Text strong>Access Control List</Less3Text>
              {(() => {
                const grants = Array.isArray(bucketACLData.acl.AccessControlList.Grant)
                  ? bucketACLData.acl.AccessControlList.Grant
                  : [bucketACLData.acl.AccessControlList.Grant];
                return (
                  <div className="responsive-scrollbar" style={{ width: '100%', marginTop: '8px' }}>
                    <Less3Table
                      columns={[
                        {
                          title: 'Grantee ID',
                          dataIndex: ['Grantee', 'ID'],
                          key: 'granteeId',
                          ellipsis: true,
                        },
                        {
                          title: 'Grantee Display Name',
                          dataIndex: ['Grantee', 'DisplayName'],
                          key: 'granteeDisplayName',
                          ellipsis: true,
                        },
                        {
                          title: 'Permission',
                          dataIndex: 'Permission',
                          key: 'permission',
                          ellipsis: true,
                        },
                      ]}
                      dataSource={grants.map((grant: ACLGrant, index: number) => ({ ...grant, key: index }))}
                      loading={isLoadingACL}
                      pagination={false}
                      scroll={{ x: true }}
                    />
                  </div>
                );
              })()}
            </div>
          </Less3Flex>
        ) : (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="secondary">No ACL found for this bucket</Less3Text>
          </div>
        )}
      </Less3Modal>
    </PageContainer>
  );
};

export default BucketsPage;
