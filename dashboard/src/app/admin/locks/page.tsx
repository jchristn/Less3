import { Metadata } from 'next';
import React from 'react';
import LocksPage from '#/page/locks/LocksPage';

export const metadata: Metadata = {
  title: 'Locks | Less3',
  description: 'View active distributed locks and fencing tokens',
};

const Page = () => {
  return <LocksPage />;
};

export default Page;
