'use client';
import React from 'react';
import RbacAdminPage, { RbacPayload, RbacRecord } from '#/page/rbac/RbacAdminPage';

const mapPermissionToRecord = (permission: any): RbacRecord => {
  const resource = permission?.ResourceType || '';
  const operation = permission?.Operation || '';

  return {
    ID: permission?.Id || '',
    RoleID: permission?.RoleId || '',
    Name: `${resource} ${operation}`.trim(),
    Resource: resource,
    Action: operation === 'Admin' ? 'Administer' : operation,
    CreatedUtc: permission?.CreatedUtc || '',
  };
};

const mapRecordToPermissionPayload = (record: RbacRecord, existingRow: RbacRecord | null): RbacPayload => ({
  Id: existingRow?.ID,
  TenantId: 'default',
  RoleId: record.RoleID || 'rol_builtin_tenantadmin',
  ResourceType: record.Resource || 'Bucket',
  Operation: record.Action === 'Administer' ? 'Admin' : record.Action || 'Read',
  Permit: true,
  Active: true,
});

const PermissionsPage: React.FC = () => (
  <RbacAdminPage
    pageTitle="Permissions"
    createLabel="Create Permission"
    searchPlaceholder="Search permissions..."
    resourcePath="permissions"
    queryString="tenantId=default"
    columns={[
      { key: 'ID', label: 'ID', width: '240px' },
      { key: 'RoleID', label: 'Role', width: '240px' },
      { key: 'Name', label: 'Name', width: '220px' },
      { key: 'Resource', label: 'Resource', width: '160px' },
      { key: 'Action', label: 'Action', width: '160px' },
      { key: 'CreatedUtc', label: 'Date Created', width: '220px' },
    ]}
    fields={[
      { key: 'RoleID', label: 'Role', placeholder: 'Enter role ID', defaultValue: 'rol_builtin_tenantadmin' },
      { key: 'Name', label: 'Name', placeholder: 'Enter permission name' },
      {
        key: 'Resource',
        label: 'Resource',
        placeholder: 'Select resource',
        defaultValue: 'Bucket',
        options: [
          { label: 'Tenant', value: 'Tenant' },
          { label: 'Bucket', value: 'Bucket' },
          { label: 'Object', value: 'Object' },
          { label: 'Credential', value: 'Credential' },
          { label: 'Role', value: 'Role' },
        ],
      },
      {
        key: 'Action',
        label: 'Action',
        placeholder: 'Select action',
        defaultValue: 'Read',
        options: [
          { label: 'Read', value: 'Read' },
          { label: 'Create', value: 'Create' },
          { label: 'Update', value: 'Update' },
          { label: 'Delete', value: 'Delete' },
          { label: 'Administer', value: 'Administer' },
        ],
      },
    ]}
    initialRows={[
      {
        ID: 'per_builtin_tenantadmin_all',
        RoleID: 'rol_builtin_tenantadmin',
        Name: 'Tenant Admin',
        Resource: 'Tenant',
        Action: 'Administer',
        CreatedUtc: '2026-07-21T00:00:00.000Z',
      },
    ]}
    mapItemToRecord={mapPermissionToRecord}
    mapRecordToPayload={mapRecordToPermissionPayload}
  />
);

export default PermissionsPage;
