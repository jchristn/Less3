'use client';
import React from 'react';
import RbacAdminPage, { RbacPayload, RbacRecord } from '#/page/rbac/RbacAdminPage';

const mapAssignmentToRecord = (assignment: any): RbacRecord => ({
  ID: assignment?.Id || '',
  TenantID: assignment?.TenantId || 'default',
  RoleID: assignment?.RoleId || '',
  PrincipalType: assignment?.PrincipalType || '',
  PrincipalID: assignment?.PrincipalId || '',
  ResourceType: assignment?.ResourceType || '',
  ResourceID: assignment?.ResourceId || '',
  CreatedUtc: assignment?.CreatedUtc || '',
});

const mapRecordToAssignmentPayload = (record: RbacRecord, existingRow: RbacRecord | null): RbacPayload => ({
  Id: existingRow?.ID,
  TenantId: record.TenantID || 'default',
  RoleId: record.RoleID || 'rol_builtin_tenantadmin',
  PrincipalType: record.PrincipalType || 'User',
  PrincipalId: record.PrincipalID || 'usr_default_admin',
  ResourceType: record.ResourceType || 'Tenant',
  ResourceId: record.ResourceID || 'default',
  Active: true,
});

const RoleAssignmentsPage: React.FC = () => (
  <RbacAdminPage
    pageTitle="Role Assignments"
    createLabel="Create Assignment"
    searchPlaceholder="Search assignments..."
    resourcePath="roleassignments"
    queryString="tenantId=default"
    columns={[
      { key: 'ID', label: 'ID', width: '240px' },
      { key: 'TenantID', label: 'Tenant', width: '140px' },
      { key: 'RoleID', label: 'Role', width: '240px' },
      { key: 'PrincipalType', label: 'Principal Type', width: '160px' },
      { key: 'PrincipalID', label: 'Principal', width: '240px' },
      { key: 'ResourceType', label: 'Resource Type', width: '160px' },
      { key: 'ResourceID', label: 'Resource', width: '220px' },
      { key: 'CreatedUtc', label: 'Date Created', width: '220px' },
    ]}
    fields={[
      { key: 'TenantID', label: 'Tenant', placeholder: 'Enter tenant ID', defaultValue: 'default' },
      { key: 'RoleID', label: 'Role', placeholder: 'Enter role ID', defaultValue: 'rol_builtin_tenantadmin' },
      {
        key: 'PrincipalType',
        label: 'Principal Type',
        placeholder: 'Select principal type',
        defaultValue: 'User',
        options: [
          { label: 'User', value: 'User' },
          { label: 'Credential', value: 'Credential' },
        ],
      },
      {
        key: 'PrincipalID',
        label: 'Principal',
        placeholder: 'Enter user or credential ID',
        defaultValue: 'usr_default_admin',
      },
      {
        key: 'ResourceType',
        label: 'Resource Type',
        placeholder: 'Select resource type',
        defaultValue: 'Tenant',
        options: [
          { label: 'Tenant', value: 'Tenant' },
          { label: 'Bucket', value: 'Bucket' },
          { label: 'Object', value: 'Object' },
          { label: 'Credential', value: 'Credential' },
        ],
      },
      { key: 'ResourceID', label: 'Resource', placeholder: 'Enter resource ID', defaultValue: 'default' },
    ]}
    initialRows={[
      {
        ID: 'asn_default_tenantadmin',
        TenantID: 'default',
        RoleID: 'rol_builtin_tenantadmin',
        PrincipalType: 'User',
        PrincipalID: 'usr_default_admin',
        ResourceType: 'Tenant',
        ResourceID: 'default',
        CreatedUtc: '2026-07-21T00:00:00.000Z',
      },
    ]}
    mapItemToRecord={mapAssignmentToRecord}
    mapRecordToPayload={mapRecordToAssignmentPayload}
  />
);

export default RoleAssignmentsPage;
