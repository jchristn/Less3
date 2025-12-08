import { Metadata } from 'next';
import React from 'react';
import BucketsPage from '#/page/buckets/BucketsPage';

export const metadata: Metadata = {
  title: 'Buckets | Less3',
  description: 'Manage buckets',
};

const Page = () => {
  return <BucketsPage />;
};

export default Page;
