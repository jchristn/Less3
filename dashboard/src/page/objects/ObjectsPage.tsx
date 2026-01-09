'use client';
import React, { useMemo, useState, useEffect } from 'react';
import { message, MenuProps, Form } from 'antd';
import {
  DownloadOutlined,
  SearchOutlined,
  PlusOutlined,
  MoreOutlined,
  DeleteOutlined,
  TagOutlined,
  FolderOutlined,
  FileOutlined,
  HomeOutlined,
  RollbackOutlined,
} from '@ant-design/icons';
import { Breadcrumb } from 'antd';
import { useSearchParams } from 'next/navigation';
import Less3Table from '#/components/base/table/Table';
import Less3Button from '#/components/base/button/Button';
import Less3Input from '#/components/base/input/Input';
import Less3Select from '#/components/base/select/Select';
import Less3Modal from '#/components/base/modal/Modal';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Flex from '#/components/base/flex/Flex';
import Less3Text from '#/components/base/typograpghy/Text';
import {
  useGetBucketsQuery,
  useListBucketObjectsQuery,
  useLazyDownloadBucketObjectQuery,
  useDeleteBucketObjectMutation,
  useDeleteMultipleObjectsMutation,
  useWriteObjectTagsMutation,
  useGetObjectTagsQuery,
  useDeleteObjectTagsMutation,
  useWriteObjectACLMutation,
  useGetObjectACLQuery,
  Bucket,
} from '#/store/slice/bucketsSlice';
import type { ColumnsType } from 'antd/es/table';
import { type BucketObject, type BucketTag, type ACLOwner, type ACLGrant, type ACLGrantee } from '#/utils/xmlUtils';
import { formatDate } from '#/utils/dateUtils';
import { transformToOptions } from '#/utils/appUtils';
import WriteObjectModal from '../buckets/WriteObjectModal';
import { Less3Theme } from '#/theme/theme';
import { useAppContext } from '#/hooks/appHooks';
import { ThemeEnum } from '#/types/types';

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

const ObjectsPage: React.FC = () => {
  const { theme } = useAppContext();
  const searchParams = useSearchParams();
  const [selectedBucketName, setSelectedBucketName] = useState<string | null>(null);
  const [currentPrefix, setCurrentPrefix] = useState<string>('');
  const [searchText, setSearchText] = useState('');
  const [downloadingObjectKey, setDownloadingObjectKey] = useState<string | null>(null);
  const [isWriteObjectModalVisible, setIsWriteObjectModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [deletingObject, setDeletingObject] = useState<BucketObject | null>(null);
  const [isWriteTagsModalVisible, setIsWriteTagsModalVisible] = useState(false);
  const [isViewTagsModalVisible, setIsViewTagsModalVisible] = useState(false);
  const [isDeleteTagsModalVisible, setIsDeleteTagsModalVisible] = useState(false);
  const [isWriteACLModalVisible, setIsWriteACLModalVisible] = useState(false);
  const [isViewACLModalVisible, setIsViewACLModalVisible] = useState(false);
  const [selectedObject, setSelectedObject] = useState<BucketObject | null>(null);
  const [tagForm] = Form.useForm<TagFormValues>();
  const [aclForm] = Form.useForm<ACLFormValues>();
  const [openDropdownKey, setOpenDropdownKey] = useState<string | null>(null);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [isBulkDeleteModalVisible, setIsBulkDeleteModalVisible] = useState(false);
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  const { data: bucketsData, isLoading: isLoadingBuckets } = useGetBucketsQuery();

  const {
    data: bucketObjectsData,
    isLoading: isLoadingObjects,
    refetch: refetchObjects,
  } = useListBucketObjectsQuery({ bucketGUID: selectedBucketName || '' }, { skip: !selectedBucketName });

  const [downloadBucketObject] = useLazyDownloadBucketObjectQuery();
  const [deleteBucketObject, { isLoading: isDeleting }] = useDeleteBucketObjectMutation();
  const [deleteMultipleObjects] = useDeleteMultipleObjectsMutation();
  const [writeObjectTags, { isLoading: isWritingTags }] = useWriteObjectTagsMutation();
  const [deleteObjectTags, { isLoading: isDeletingTags }] = useDeleteObjectTagsMutation();
  const [writeObjectACL, { isLoading: isWritingACL }] = useWriteObjectACLMutation();

  const shouldFetchObjectTags =
    selectedObject && selectedBucketName && (isViewTagsModalVisible || isWriteTagsModalVisible);
  const shouldFetchObjectACL =
    selectedObject && selectedBucketName && (isViewACLModalVisible || isWriteACLModalVisible);

  const {
    data: objectTagsData,
    isLoading: isLoadingObjectTags,
    isError: isObjectTagsError,
    error: objectTagsError,
    refetch: refetchObjectTags,
  } = useGetObjectTagsQuery(
    {
      bucketGUID: selectedBucketName || '',
      objectKey: selectedObject?.Key || '',
    },
    { skip: !shouldFetchObjectTags }
  );

  const {
    data: objectACLData,
    isLoading: isLoadingObjectACL,
    isError: isObjectACLError,
    error: objectACLError,
    refetch: refetchObjectACL,
  } = useGetObjectACLQuery(
    {
      bucketGUID: selectedBucketName || '',
      objectKey: selectedObject?.Key || '',
    },
    { skip: !shouldFetchObjectACL }
  );

  const selectedBucket = useMemo(() => {
    return bucketsData?.find((bucket) => bucket.Name === selectedBucketName) || null;
  }, [bucketsData, selectedBucketName]);

  const bucketOptions = useMemo(() => {
    return transformToOptions(bucketsData, 'Name');
  }, [bucketsData]);

  // Select bucket from URL parameter or auto-select the first bucket
  useEffect(() => {
    if (bucketsData && bucketsData.length > 0 && !selectedBucketName) {
      const bucketFromUrl = searchParams.get('bucket');
      if (bucketFromUrl && bucketsData.some((b) => b.Name === bucketFromUrl)) {
        setSelectedBucketName(bucketFromUrl);
      } else {
        setSelectedBucketName(bucketsData[0].Name);
      }
    }
  }, [bucketsData, selectedBucketName, searchParams]);

  // Get objects at the current prefix level (not deeper)
  const objectsAtCurrentLevel = useMemo(() => {
    if (!bucketObjectsData?.Contents) return { folders: [] as string[], files: [] as BucketObject[] };

    const folders = new Set<string>();
    const files: BucketObject[] = [];

    bucketObjectsData.Contents.forEach((obj) => {
      const key = obj.Key;

      // Skip if doesn't start with current prefix
      if (!key.startsWith(currentPrefix)) return;

      // Get the remaining path after the prefix
      const remainingPath = key.slice(currentPrefix.length);

      // Skip empty remaining path (shouldn't happen, but guard)
      if (!remainingPath) return;

      // Check if there's a folder separator in the remaining path
      const slashIndex = remainingPath.indexOf('/');

      if (slashIndex === -1) {
        // No slash - this is a file at current level
        files.push(obj);
      } else if (slashIndex === remainingPath.length - 1) {
        // Slash is at the end - this is a folder marker at current level
        folders.add(remainingPath.slice(0, slashIndex));
      } else {
        // Slash in middle - this is an item in a subfolder, extract the folder name
        folders.add(remainingPath.slice(0, slashIndex));
      }
    });

    return { folders: Array.from(folders).sort(), files };
  }, [bucketObjectsData, currentPrefix]);

  const filteredObjects = useMemo(() => {
    const { folders, files } = objectsAtCurrentLevel;

    // Create virtual folder objects for display
    const folderObjects: BucketObject[] = folders.map((folderName) => ({
      Key: currentPrefix + folderName + '/',
      LastModified: '',
      ETag: '',
      Size: 0,
      StorageClass: '',
      ContentType: 'folder',
      Owner: { ID: '', DisplayName: '' },
    }));

    // Combine folders and files
    const allItems = [...folderObjects, ...files];

    const q = searchText.trim().toLowerCase();
    if (!q) return allItems;

    return allItems.filter((obj) => {
      const key = obj.Key?.toLowerCase() ?? '';
      const contentType = obj.ContentType?.toLowerCase() ?? '';
      const storageClass = obj.StorageClass?.toLowerCase() ?? '';
      const owner = obj.Owner?.DisplayName?.toLowerCase() ?? obj.Owner?.ID?.toLowerCase() ?? '';

      return key.includes(q) || contentType.includes(q) || storageClass.includes(q) || owner.includes(q);
    });
  }, [objectsAtCurrentLevel, currentPrefix, searchText]);

  // Breadcrumb path parts
  const breadcrumbParts = useMemo(() => {
    if (!currentPrefix) return [];
    return currentPrefix.split('/').filter(Boolean);
  }, [currentPrefix]);

  // Navigate to a folder
  const navigateToFolder = (folderKey: string) => {
    setCurrentPrefix(folderKey);
    setSearchText('');
    setSelectedRowKeys([]);
  };

  // Navigate up one level
  const navigateUp = () => {
    if (!currentPrefix) return;
    const parts = currentPrefix.split('/').filter(Boolean);
    parts.pop();
    setCurrentPrefix(parts.length > 0 ? parts.join('/') + '/' : '');
    setSelectedRowKeys([]);
  };

  // Navigate to root
  const navigateToRoot = () => {
    setCurrentPrefix('');
    setSearchText('');
    setSelectedRowKeys([]);
  };

  // Navigate to a specific breadcrumb level
  const navigateToBreadcrumb = (index: number) => {
    if (index < 0) {
      navigateToRoot();
    } else {
      const parts = breadcrumbParts.slice(0, index + 1);
      setCurrentPrefix(parts.join('/') + '/');
      setSelectedRowKeys([]);
    }
    setSearchText('');
  };

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

  const handleDeleteObject = (record: BucketObject) => {
    setDeletingObject(record);
    setIsDeleteModalVisible(true);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingObject || !selectedBucketName) {
      message.error('Object or bucket information not available');
      return;
    }

    try {
      await deleteBucketObject({
        bucketGUID: selectedBucketName,
        objectKey: deletingObject.Key,
      }).unwrap();

      message.success(`Object "${deletingObject.Key}" deleted successfully`);
      setIsDeleteModalVisible(false);
      setDeletingObject(null);
      refetchObjects(); // Refresh the objects list
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to delete object');
    }
  };

  const handleDeleteCancel = () => {
    setIsDeleteModalVisible(false);
    setDeletingObject(null);
  };

  const handleBulkDelete = () => {
    if (selectedRowKeys.length === 0) {
      message.warning('Please select at least one object to delete');
      return;
    }
    setIsBulkDeleteModalVisible(true);
  };

  const handleBulkDeleteConfirm = async () => {
    if (!selectedBucketName || selectedRowKeys.length === 0) {
      message.error('Bucket or selection information not available');
      return;
    }

    setIsBulkDeleting(true);
    const keysToDelete = selectedRowKeys.filter((key) => !String(key).endsWith('/')) as string[];

    try {
      const result = await deleteMultipleObjects({
        bucketGUID: selectedBucketName,
        objectKeys: keysToDelete,
      }).unwrap();

      const successCount = result.deleted.length;
      const failCount = result.errors.length;

      if (failCount === 0) {
        message.success(`Successfully deleted ${successCount} object(s)`);
      } else {
        message.warning(`Deleted ${successCount} object(s), ${failCount} failed`);
      }
    } catch (error: any) {
      message.error(error?.data || 'Failed to delete objects');
    }

    setIsBulkDeleting(false);
    setIsBulkDeleteModalVisible(false);
    setSelectedRowKeys([]);
    refetchObjects();
  };

  const handleBulkDeleteCancel = () => {
    setIsBulkDeleteModalVisible(false);
  };

  const handleWriteObjectTags = (record: BucketObject) => {
    setSelectedObject(record);
    setIsWriteTagsModalVisible(true);
  };

  const handleViewObjectTags = (record: BucketObject) => {
    setSelectedObject(record);
    setIsViewTagsModalVisible(true);
  };

  const handleDeleteObjectTags = (record: BucketObject) => {
    setSelectedObject(record);
    setIsDeleteTagsModalVisible(true);
  };

  const handleWriteObjectACL = (record: BucketObject) => {
    setSelectedObject(record);
    setIsWriteACLModalVisible(true);
  };

  const handleViewObjectACL = (record: BucketObject) => {
    setSelectedObject(record);
    setIsViewACLModalVisible(true);
  };

  const handleWriteObjectTagsOk = async () => {
    if (!selectedObject?.Key || !selectedBucketName) return;

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

      await writeObjectTags({
        bucketGUID: selectedBucketName,
        objectKey: selectedObject.Key,
        tags,
      }).unwrap();
      message.success(`Object ${tags.length} tag(s) written successfully`);

      // Refetch tags before closing modal (while query is still active)
      if (shouldFetchObjectTags) {
        refetchObjectTags();
      }

      // Close modal and reset state
      setIsWriteTagsModalVisible(false);

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
        message.error(errorMessage || 'Failed to write object tags');
      }
    }
  };

  const handleDeleteObjectTagsConfirm = async () => {
    if (!selectedObject?.Key || !selectedBucketName) return;

    try {
      await deleteObjectTags({
        bucketGUID: selectedBucketName,
        objectKey: selectedObject.Key,
      }).unwrap();
      message.success('Object tags deleted successfully');

      // Refetch tags before closing modal (while query is still active)
      if (shouldFetchObjectTags) {
        refetchObjectTags();
      }

      // Close modal and reset state
      setIsDeleteTagsModalVisible(false);
    } catch (error: any) {
      message.error(error?.data?.data || error?.message || 'Failed to delete object tags');
    }
  };

  const handleWriteObjectACLOk = async () => {
    if (!selectedObject?.Key || !selectedBucketName) return;

    try {
      const values = await aclForm.validateFields();

      const owner: ACLOwner = objectACLData?.acl?.Owner || DEFAULT_ACL_OWNER;

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

      await writeObjectACL({
        bucketGUID: selectedBucketName,
        objectKey: selectedObject.Key,
        owner,
        grants,
      }).unwrap();

      // Refetch ACL before closing modal (while query is still active)
      if (shouldFetchObjectACL) {
        refetchObjectACL();
      }

      // Close modal and reset state
      setIsWriteACLModalVisible(false);

      aclForm.resetFields();
      message.success('Object ACL written successfully');
    } catch (error: any) {
      // Check if it's a validation error (from Ant Design form)
      if (error?.errorFields || (Array.isArray(error) && error.length > 0 && error[0]?.name)) {
        // Validation error - form will show field errors, don't show generic error message
        return;
      }
      // API error - only show if it's not a validation issue
      const errorMessage = error?.data?.data || error?.data?.message || error?.message;
      if (errorMessage && !errorMessage.includes('validation') && !errorMessage.includes('required')) {
        message.error(errorMessage || 'Failed to write object ACL');
      }
    }
  };

  // Pre-populate form with existing ACL when write modal opens and ACL is loaded
  useEffect(() => {
    if (isWriteACLModalVisible && selectedObject && selectedBucketName) {
      if (!isLoadingObjectACL) {
        if (objectACLData?.acl) {
          const acl = objectACLData.acl;
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
        } else if (isObjectACLError) {
          aclForm.setFieldsValue({
            grantee: { ID: DEFAULT_ACL_OWNER.ID, DisplayName: '' },
            Permission: 'FULL_CONTROL',
          });
        }
      }
    } else if (!isWriteACLModalVisible) {
      aclForm.resetFields();
    }
  }, [
    isWriteACLModalVisible,
    selectedObject,
    selectedBucketName,
    objectACLData,
    isLoadingObjectACL,
    isObjectACLError,
    aclForm,
  ]);

  // Pre-populate form with existing tags when write modal opens and tags are loaded
  useEffect(() => {
    if (isWriteTagsModalVisible && selectedObject && selectedBucketName) {
      if (!isLoadingObjectTags) {
        if (objectTagsData !== undefined) {
          const existingTags = objectTagsData.tags || [];

          if (existingTags.length > 0) {
            tagForm.setFieldsValue({
              tags: existingTags.map((tag) => ({ Key: tag.Key, Value: tag.Value })),
            });
          } else {
            tagForm.setFieldsValue({ tags: [{ Key: '', Value: '' }] });
          }
        } else if (isObjectTagsError) {
          tagForm.setFieldsValue({ tags: [{ Key: '', Value: '' }] });
        }
      }
    } else if (!isWriteTagsModalVisible) {
      tagForm.resetFields();
    }
  }, [
    isWriteTagsModalVisible,
    selectedObject,
    selectedBucketName,
    objectTagsData,
    isLoadingObjectTags,
    isObjectTagsError,
    tagForm,
  ]);

  const isFolder = (key: string): boolean => {
    return key.endsWith('/');
  };

  // Get display name from full key (just the last part without prefix)
  const getDisplayName = (key: string): string => {
    const withoutPrefix = key.slice(currentPrefix.length);
    // Remove trailing slash for folders
    return withoutPrefix.endsWith('/') ? withoutPrefix.slice(0, -1) : withoutPrefix;
  };

  const columns: ColumnsType<BucketObject> = [
    {
      title: 'Key',
      dataIndex: 'Key',
      key: 'Key',
      ellipsis: true,
      render: (key: string, record: BucketObject) => {
        const displayName = getDisplayName(key);
        const isFolderItem = isFolder(key);

        if (isFolderItem) {
          return (
            <Less3Flex align="center" gap={8}>
              <FolderOutlined style={{ color: 'var(--ant-color-primary)', fontSize: 16 }} />
              <span
                style={{ cursor: 'pointer', color: 'var(--ant-color-primary)' }}
                onClick={() => navigateToFolder(key)}
              >
                {displayName}
              </span>
            </Less3Flex>
          );
        }

        return (
          <Less3Flex align="center" gap={8}>
            <FileOutlined style={{ color: 'var(--ant-color-text-secondary)', fontSize: 16 }} />
            <span>{displayName}</span>
          </Less3Flex>
        );
      },
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
      render: (_: any, record: BucketObject) => {
        const dropdownKey = record.Key || '';
        const isOpen = openDropdownKey === dropdownKey;

        const menuItems: MenuProps['items'] = [
          {
            key: 'write-tags',
            label: 'Write Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleWriteObjectTags(record);
            },
          },
          {
            key: 'read-tags',
            label: 'Read Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleViewObjectTags(record);
            },
          },
          {
            key: 'delete-tags',
            label: 'Delete Tags',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleDeleteObjectTags(record);
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
              handleWriteObjectACL(record);
            },
          },
          {
            key: 'read-acl',
            label: 'Read ACL',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleViewObjectACL(record);
            },
          },
          {
            type: 'divider',
          },
          {
            key: 'download',
            label: 'Download Object',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleDownloadObject(record);
            },
            disabled: isFolder(record.Key) || downloadingObjectKey === record.Key,
          },
          {
            key: 'delete',
            label: 'Delete Object',
            onClick: () => {
              setOpenDropdownKey(null); // Close dropdown immediately
              handleDeleteObject(record);
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
            <Less3Button
              type="text"
              icon={<MoreOutlined />}
              size="small"
              loading={downloadingObjectKey === record.Key}
            />
          </Less3Dropdown>
        );
      },
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
            onChange={(value: string | number | string[]) => {
              setSelectedBucketName(typeof value === 'string' ? value : null);
              setSearchText(''); // Clear search when bucket changes
              setCurrentPrefix(''); // Reset to root when bucket changes
              setSelectedRowKeys([]); // Clear selection when bucket changes
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
      ) : !bucketObjectsData || (bucketObjectsData.Contents.length === 0 && !currentPrefix) ? (
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

          {/* Breadcrumb navigation */}
          <Less3Flex align="center" gap={8} style={{ padding: '0 4px' }}>
            <Breadcrumb
              items={[
                {
                  title: (
                    <span
                      onClick={navigateToRoot}
                      style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}
                    >
                      <HomeOutlined />
                      <span>{selectedBucket?.Name || 'Root'}</span>
                    </span>
                  ),
                },
                ...breadcrumbParts.map((part, index) => ({
                  title: (
                    <span
                      onClick={() => navigateToBreadcrumb(index)}
                      style={{ cursor: index < breadcrumbParts.length - 1 ? 'pointer' : 'default' }}
                    >
                      {part}
                    </span>
                  ),
                })),
              ]}
            />
            {currentPrefix && (
              <Less3Button
                type="text"
                size="small"
                icon={<RollbackOutlined />}
                onClick={navigateUp}
                style={{ marginLeft: 8 }}
              >
                Go up
              </Less3Button>
            )}
            <Less3Flex style={{ marginLeft: 'auto' }} align="center" gap={8}>
              {selectedRowKeys.length > 0 && (
                <Less3Text type="secondary" style={{ fontSize: 12 }}>
                  {selectedRowKeys.length} selected
                </Less3Text>
              )}
              <Less3Button
                type="text"
                danger
                icon={<DeleteOutlined />}
                onClick={handleBulkDelete}
                disabled={selectedRowKeys.length === 0}
              >
                Delete
              </Less3Button>
            </Less3Flex>
          </Less3Flex>

          <div className="responsive-scrollbar" style={{ width: '100%' }}>
            <Less3Table
              columns={columns as ColumnsType<any>}
              dataSource={filteredObjects}
              loading={isLoadingObjects}
              rowKey="Key"
              scroll={{ x: true }}
              rowSelection={{
                selectedRowKeys,
                onChange: (newSelectedRowKeys: React.Key[]) => {
                  setSelectedRowKeys(newSelectedRowKeys);
                },
                getCheckboxProps: (record: BucketObject) => ({
                  disabled: record.Key.endsWith('/'), // Disable checkbox for folders
                }),
              }}
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

      <Less3Modal
        title="Delete Object"
        open={isDeleteModalVisible}
        onCancel={handleDeleteCancel}
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
            Are you sure you want to delete the object <strong>&quot;{deletingObject?.Key}&quot;</strong>?
          </p>
          <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
            This action cannot be undone. The object will be permanently deleted.
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title="Delete Selected Objects"
        open={isBulkDeleteModalVisible}
        onCancel={handleBulkDeleteCancel}
        confirmLoading={isBulkDeleting}
        okText="Delete All"
        okButtonProps={{ danger: true }}
        centered
        keyboard={true}
        footer={[
          <Less3Button key="cancel" onClick={handleBulkDeleteCancel} disabled={isBulkDeleting}>
            Cancel
          </Less3Button>,
          <Less3Button key="confirm" type="primary" danger loading={isBulkDeleting} onClick={handleBulkDeleteConfirm}>
            Delete All
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          <p>
            Are you sure you want to delete <strong>{selectedRowKeys.filter((key) => !String(key).endsWith('/')).length}</strong> selected object(s)?
          </p>
          <p style={{ fontSize: '13px', color: '#8c8c8c' }}>
            This action cannot be undone. All selected objects will be permanently deleted.
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title={'Write Tags'}
        open={isWriteTagsModalVisible}
        onOk={handleWriteObjectTagsOk}
        onCancel={() => {
          setIsWriteTagsModalVisible(false);

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
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsViewTagsModalVisible(false);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
        centered
        keyboard={true}
      >
        {isLoadingObjectTags ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>Loading tags...</div>
        ) : isObjectTagsError ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="danger">
              {objectTagsError && typeof objectTagsError === 'object' && 'data' in objectTagsError
                ? (objectTagsError.data as string) || 'Failed to load tags'
                : 'Failed to load tags'}
            </Less3Text>
          </div>
        ) : objectTagsData?.tags && objectTagsData.tags.length > 0 ? (
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
              dataSource={objectTagsData.tags.map((tag, index) => ({ ...tag, key: index }))}
              loading={isLoadingObjectTags}
              pagination={false}
              scroll={{ x: true }}
            />
          </div>
        ) : (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="secondary">No tags found for this object</Less3Text>
          </div>
        )}
      </Less3Modal>

      <Less3Modal
        title={`Delete Tags - ${selectedObject?.Key || ''}`}
        open={isDeleteTagsModalVisible}
        onCancel={() => {
          setIsDeleteTagsModalVisible(false);
        }}
        confirmLoading={isDeletingTags}
        okText="Delete"
        okButtonProps={{ danger: true }}
        centered
        keyboard={true}
        footer={[
          <Less3Button
            key="confirm"
            type="primary"
            danger
            loading={isDeletingTags}
            onClick={handleDeleteObjectTagsConfirm}
          >
            Delete
          </Less3Button>,
        ]}
      >
        <Less3Flex vertical={true} gap={16}>
          <p>
            Are you sure you want to delete the created tags for object{' '}
            <strong>&quot;{selectedObject?.Key}&quot;</strong>?
          </p>
        </Less3Flex>
      </Less3Modal>

      <Less3Modal
        title={`Write ACL - ${selectedObject?.Key || ''}`}
        open={isWriteACLModalVisible}
        onOk={handleWriteObjectACLOk}
        onCancel={() => {
          setIsWriteACLModalVisible(false);
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
        }}
        footer={[
          <Less3Button
            key="close"
            onClick={() => {
              setIsViewACLModalVisible(false);
            }}
          >
            Close
          </Less3Button>,
        ]}
        width={700}
        centered
        keyboard={true}
      >
        {isLoadingObjectACL ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>Loading ACL...</div>
        ) : isObjectACLError ? (
          <div style={{ textAlign: 'center', padding: '40px' }}>
            <Less3Text type="danger">
              {objectACLError && typeof objectACLError === 'object' && 'data' in objectACLError
                ? (objectACLError.data as string) || 'Failed to load ACL'
                : 'Failed to load ACL'}
            </Less3Text>
          </div>
        ) : objectACLData?.acl ? (
          <Less3Flex vertical gap={16}>
            <div>
              <Less3Text strong>Owner</Less3Text>
              <Less3Flex vertical gap={8} style={{ marginTop: '8px', paddingLeft: '16px' }}>
                <Less3Text>ID: {objectACLData.acl.Owner.ID}</Less3Text>
                <Less3Text>Display Name: {objectACLData.acl.Owner.DisplayName}</Less3Text>
              </Less3Flex>
            </div>
            <div>
              <Less3Text strong>Access Control List</Less3Text>
              {(() => {
                const grants = Array.isArray(objectACLData.acl.AccessControlList.Grant)
                  ? objectACLData.acl.AccessControlList.Grant
                  : [objectACLData.acl.AccessControlList.Grant];
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
                      loading={isLoadingObjectACL}
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
            <Less3Text type="secondary">No ACL found for this object</Less3Text>
          </div>
        )}
      </Less3Modal>
    </PageContainer>
  );
};

export default ObjectsPage;
