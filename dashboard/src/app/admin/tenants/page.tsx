import { Metadata } from 'next';
import React from 'react';
import TenantsPage from '#/page/tenants/TenantsPage';

export const metadata: Metadata = {
  title: 'Tenants | Less3',
  description: 'Manage tenants',
};

const Page = () => {
  return <TenantsPage />;
};

export default Page;
