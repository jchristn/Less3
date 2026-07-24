'use client';
import React from 'react';
import RbacAdminPage, { RbacPayload, RbacRecord } from '#/page/rbac/RbacAdminPage';

const mapTenantToRecord = (tenant: any): RbacRecord => ({
  ID: tenant?.Id || '',
  Name: tenant?.Name || '',
  Status: tenant?.Active === false ? 'Inactive' : 'Active',
  CreatedUtc: tenant?.CreatedUtc || '',
});

const mapRecordToTenantPayload = (record: RbacRecord, existingRow: RbacRecord | null): RbacPayload => ({
  Id: existingRow?.ID,
  Name: record.Name,
  Active: record.Status !== 'Inactive',
});

const TenantsPage: React.FC = () => (
  <RbacAdminPage
    pageTitle="Tenants"
    createLabel="Create Tenant"
    searchPlaceholder="Search tenants..."
    resourcePath="tenants"
    columns={[
      { key: 'ID', label: 'ID', width: '240px' },
      { key: 'Name', label: 'Name', width: '220px' },
      { key: 'Status', label: 'Status', width: '140px' },
      { key: 'CreatedUtc', label: 'Date Created', width: '220px' },
    ]}
    fields={[
      { key: 'Name', label: 'Name', placeholder: 'Enter tenant name' },
      {
        key: 'Status',
        label: 'Status',
        placeholder: 'Select status',
        defaultValue: 'Active',
        options: [
          { label: 'Active', value: 'Active' },
          { label: 'Inactive', value: 'Inactive' },
        ],
      },
    ]}
    initialRows={[
      {
        ID: 'default',
        Name: 'Default',
        Status: 'Active',
        CreatedUtc: '2026-07-21T00:00:00.000Z',
      },
    ]}
    mapItemToRecord={mapTenantToRecord}
    mapRecordToPayload={mapRecordToTenantPayload}
  />
);

export default TenantsPage;
