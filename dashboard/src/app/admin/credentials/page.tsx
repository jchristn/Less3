import { Metadata } from 'next';
import React from 'react';
import CredentialsPage from '#/page/credentials/CredentialsPage';

export const metadata: Metadata = {
  title: 'Credentials | Less3',
  description: 'Manage credentials',
};

const Page = () => {
  return <CredentialsPage />;
};

export default Page;
