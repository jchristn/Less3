import { Metadata } from 'next';
import React from 'react';
import RequestHistoryPage from '#/page/request-history/RequestHistoryPage';

export const metadata: Metadata = {
  title: 'Request History | Less3',
  description: 'View API request history',
};

const Page = () => {
  return <RequestHistoryPage />;
};

export default Page;
