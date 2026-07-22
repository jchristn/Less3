'use client';
import React from 'react';
import RbacAdminPage, { RbacPayload, RbacRecord } from '#/page/rbac/RbacAdminPage';

const mapRoleToRecord = (role: any): RbacRecord => ({
  ID: role?.Id || '',
  TenantID: role?.TenantId || 'global',
  Name: role?.Name || '',
  Scope: role?.InheritsToChildren === false ? 'Resource' : 'Tenant',
  CreatedUtc: role?.CreatedUtc || '',
});

const mapRecordToRolePayload = (record: RbacRecord, existingRow: RbacRecord | null): RbacPayload => ({
  Id: existingRow?.ID,
  TenantId: record.TenantID === 'global' ? 'default' : record.TenantID || 'default',
  Name: record.Name,
  Description: record.Scope ? `${record.Scope} scoped role` : null,
  InheritsToChildren: record.Scope !== 'Resource',
  Active: true,
});

const RolesPage: React.FC = () => (
  <RbacAdminPage
    pageTitle="Roles"
    createLabel="Create Role"
    searchPlaceholder="Search roles..."
    resourcePath="roles"
    queryString="tenantId=default"
    columns={[
      { key: 'ID', label: 'ID', width: '240px' },
      { key: 'TenantID', label: 'Tenant', width: '160px' },
      { key: 'Name', label: 'Name', width: '220px' },
      { key: 'Scope', label: 'Scope', width: '160px' },
      { key: 'CreatedUtc', label: 'Date Created', width: '220px' },
    ]}
    fields={[
      { key: 'TenantID', label: 'Tenant', placeholder: 'Enter tenant ID', defaultValue: 'default' },
      { key: 'Name', label: 'Name', placeholder: 'Enter role name' },
      {
        key: 'Scope',
        label: 'Scope',
        placeholder: 'Select scope',
        defaultValue: 'Tenant',
        options: [
          { label: 'Tenant', value: 'Tenant' },
          { label: 'Bucket', value: 'Bucket' },
          { label: 'Object', value: 'Object' },
        ],
      },
    ]}
    initialRows={[
      {
        ID: 'rol_builtin_tenantadmin',
        TenantID: 'global',
        Name: 'TenantAdmin',
        Scope: 'Tenant',
        CreatedUtc: '2026-07-21T00:00:00.000Z',
      },
    ]}
    mapItemToRecord={mapRoleToRecord}
    mapRecordToPayload={mapRecordToRolePayload}
  />
);

export default RolesPage;
