/* eslint-disable max-lines-per-function */
'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { Form, MenuProps } from 'antd';
import { MoreOutlined, PlusOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import DataTable, { DataTableColumn } from '#/components/DataTable';
import Less3Button from '#/components/base/button/Button';
import Less3Dropdown from '#/components/base/dropdown/Dropdown';
import Less3Flex from '#/components/base/flex/Flex';
import Less3FormItem from '#/components/base/form/FormItem';
import Less3Input from '#/components/base/input/Input';
import Less3Modal from '#/components/base/modal/Modal';
import PageContainer from '#/components/base/pageContainer/PageContainer';
import Less3Select from '#/components/base/select/Select';
import { buildAdminApiHeaders, buildApiUrl } from '#/services/sdk.service';
import { message } from '#/utils/message';

export interface RbacAdminColumn {
  key: string;
  label: string;
  width: string;
}

export interface RbacAdminField {
  key: string;
  label: string;
  placeholder: string;
  defaultValue?: string;
  options?: Array<{ label: string; value: string }>;
}

export type RbacRecord = Record<string, string>;
export type RbacPayload = Record<string, unknown>;

interface RbacAdminPageProps {
  pageTitle: string;
  createLabel: string;
  searchPlaceholder: string;
  resourcePath: string;
  queryString?: string;
  columns: RbacAdminColumn[];
  fields: RbacAdminField[];
  initialRows: RbacRecord[];
  mapItemToRecord: (item: any) => RbacRecord;
  mapRecordToPayload: (record: RbacRecord, existingRow: RbacRecord | null) => RbacPayload;
}

const appendQueryString = (path: string, queryString?: string): string =>
  queryString ? `${path}?${queryString}` : path;

const readJson = async (response: Response): Promise<any> => {
  const text = await response.text();
  if (!text.trim()) return null;
  return JSON.parse(text);
};

const RbacAdminPage: React.FC<RbacAdminPageProps> = ({
  pageTitle,
  createLabel,
  searchPlaceholder,
  resourcePath,
  queryString,
  columns,
  fields,
  initialRows,
  mapItemToRecord,
  mapRecordToPayload,
}) => {
  const [form] = Form.useForm<RbacRecord>();
  const [rows, setRows] = useState<RbacRecord[]>(initialRows);
  const [searchText, setSearchText] = useState('');
  const [editingRow, setEditingRow] = useState<RbacRecord | null>(null);
  const [deletingRow, setDeletingRow] = useState<RbacRecord | null>(null);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isDeleteModalVisible, setIsDeleteModalVisible] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const collectionPath = appendQueryString(`admin/${resourcePath}`, queryString);

  const loadRows = async () => {
    setIsLoading(true);

    try {
      const response = await fetch(buildApiUrl(collectionPath), {
        method: 'GET',
        headers: buildAdminApiHeaders(),
        cache: 'no-store',
      });

      if (!response.ok) {
        throw new Error(`Failed to load ${pageTitle.toLowerCase()}: ${response.status}`);
      }

      const data = await readJson(response);
      const items = Array.isArray(data) ? data : data?.Items || [];
      setRows(items.map(mapItemToRecord));
    } catch (error: any) {
      message.error(error?.message || `Failed to load ${pageTitle.toLowerCase()}`);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadRows();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resourcePath, queryString]);

  const tableColumns: DataTableColumn<RbacRecord>[] = useMemo(
    () => [
      ...columns.map((column) => ({
        key: column.key,
        label: column.label,
        width: column.width,
      })),
      {
        key: 'actions',
        label: 'Actions',
        width: '80px',
        isAction: true,
        sortable: false,
        filterable: false,
        render: (item: RbacRecord) => {
          const menuItems: MenuProps['items'] = [
            {
              key: 'edit',
              label: 'Edit',
              onClick: () => handleEdit(item),
            },
            {
              key: 'delete',
              label: 'Delete',
              onClick: () => handleDelete(item),
            },
          ];

          return (
            <Less3Dropdown menu={{ items: menuItems }} trigger={['click']}>
              <Less3Button
                type="text"
                icon={<MoreOutlined />}
                size="small"
                onClick={(event) => event.stopPropagation()}
              />
            </Less3Dropdown>
          );
        },
      },
    ],
    [columns]
  );

  const filteredRows = useMemo(() => {
    const q = searchText.trim().toLowerCase();
    if (!q) return rows;

    return rows.filter((row) => Object.values(row).some((value) => value.toLowerCase().includes(q)));
  }, [rows, searchText]);

  const handleCreate = () => {
    setEditingRow(null);
    form.setFieldsValue(Object.fromEntries(fields.map((field) => [field.key, field.defaultValue ?? ''])));
    setIsModalVisible(true);
  };

  const handleEdit = (row: RbacRecord) => {
    setEditingRow(row);
    form.setFieldsValue(row);
    setIsModalVisible(true);
  };

  const handleDelete = (row: RbacRecord) => {
    setDeletingRow(row);
    setIsDeleteModalVisible(true);
  };

  const handleModalOk = async () => {
    const values = await form.validateFields();
    const payload = mapRecordToPayload(values, editingRow);
    const method = editingRow ? 'PUT' : 'POST';
    const itemPath = editingRow
      ? appendQueryString(`admin/${resourcePath}/${editingRow.ID}`, queryString)
      : collectionPath;

    try {
      const response = await fetch(buildApiUrl(itemPath), {
        method,
        headers: buildAdminApiHeaders({
          'Content-Type': 'application/json',
        }),
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`Failed to save ${pageTitle.slice(0, -1).toLowerCase()}: ${response.status}`);
      }

      const responseData = await readJson(response);
      const nextRow = mapItemToRecord(responseData || payload);

      setRows((currentRows) =>
        editingRow ? currentRows.map((row) => (row.ID === editingRow.ID ? nextRow : row)) : [nextRow, ...currentRows]
      );
      setIsModalVisible(false);
      setEditingRow(null);
      form.resetFields();
      message.success(`${pageTitle.slice(0, -1)} saved`);
    } catch (error: any) {
      message.error(error?.message || `Failed to save ${pageTitle.slice(0, -1).toLowerCase()}`);
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deletingRow) return;

    try {
      const itemPath = appendQueryString(`admin/${resourcePath}/${deletingRow.ID}`, queryString);
      const response = await fetch(buildApiUrl(itemPath), {
        method: 'DELETE',
        headers: buildAdminApiHeaders(),
      });

      if (!response.ok) {
        throw new Error(`Failed to delete ${pageTitle.slice(0, -1).toLowerCase()}: ${response.status}`);
      }

      setRows((currentRows) => currentRows.filter((row) => row.ID !== deletingRow.ID));
      setIsDeleteModalVisible(false);
      setDeletingRow(null);
      message.success(`${pageTitle.slice(0, -1)} deleted`);
    } catch (error: any) {
      message.error(error?.message || `Failed to delete ${pageTitle.slice(0, -1).toLowerCase()}`);
    }
  };

  const handleRefresh = () => {
    loadRows();
  };

  return (
    <PageContainer
      pageTitle={pageTitle}
      pageTitleRightContent={
        <Less3Flex gap={10} align="center">
          <Less3Input
            placeholder={searchPlaceholder}
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(event: React.ChangeEvent<HTMLInputElement>) => setSearchText(event.target.value)}
            style={{ width: 250 }}
            allowClear
          />
          <Less3Button icon={<ReloadOutlined />} onClick={handleRefresh}>
            Refresh
          </Less3Button>
          <Less3Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            {createLabel}
          </Less3Button>
        </Less3Flex>
      }
    >
      <DataTable columns={tableColumns} data={filteredRows} rowKey="ID" loading={isLoading} onRowClick={handleEdit} />

      <Less3Modal
        title={editingRow ? `Edit ${pageTitle.slice(0, -1)}` : createLabel}
        open={isModalVisible}
        forceRender
        onOk={handleModalOk}
        onCancel={() => {
          setIsModalVisible(false);
          setEditingRow(null);
          form.resetFields();
        }}
        width={600}
        centered
      >
        <Form form={form} layout="vertical" autoComplete="off">
          {fields.map((field) => (
            <Less3FormItem
              key={field.key}
              label={field.label}
              name={field.key}
              rules={[{ required: true, message: `${field.label} is required` }]}
            >
              {field.options ? (
                <Less3Select options={field.options} placeholder={field.placeholder} />
              ) : (
                <Less3Input placeholder={field.placeholder} />
              )}
            </Less3FormItem>
          ))}
        </Form>
      </Less3Modal>

      <Less3Modal
        title={`Delete ${pageTitle.slice(0, -1)}`}
        open={isDeleteModalVisible}
        onCancel={() => {
          setIsDeleteModalVisible(false);
          setDeletingRow(null);
        }}
        footer={[
          <Less3Button key="confirm" type="primary" danger onClick={handleDeleteConfirm}>
            Delete
          </Less3Button>,
        ]}
        centered
      >
        <p>
          Delete <strong>{deletingRow?.Name ?? deletingRow?.ID}</strong>?
        </p>
      </Less3Modal>
    </PageContainer>
  );
};

export default RbacAdminPage;
