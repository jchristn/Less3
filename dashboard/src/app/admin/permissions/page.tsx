import { Metadata } from 'next';
import React from 'react';
import PermissionsPage from '#/page/permissions/PermissionsPage';

export const metadata: Metadata = {
  title: 'Permissions | Less3',
  description: 'Manage permissions',
};

const Page = () => {
  return <PermissionsPage />;
};

export default Page;
