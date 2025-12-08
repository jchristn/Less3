import { Metadata } from 'next';
import React from 'react';
import DashboardPage from '#/page/dashboard/DashboardPage';

export const metadata: Metadata = {
  title: 'Home | Less3',
  description: 'Less3 Home',
};

const Page = () => {
  return <DashboardPage />;
};

export default Page;
