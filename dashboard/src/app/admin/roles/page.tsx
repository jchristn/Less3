import { Metadata } from 'next';
import React from 'react';
import RolesPage from '#/page/roles/RolesPage';

export const metadata: Metadata = {
  title: 'Roles | Less3',
  description: 'Manage roles',
};

const Page = () => {
  return <RolesPage />;
};

export default Page;
