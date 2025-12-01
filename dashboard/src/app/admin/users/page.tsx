import { Metadata } from 'next';
import React from 'react';
import UsersPage from '#/page/users/UsersPage';

export const metadata: Metadata = {
  title: 'Users | Less3',
  description: 'Manage users',
};

const Page = () => {
  return <UsersPage />;
};

export default Page;
