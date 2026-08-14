import { Metadata } from 'next';
import React from 'react';
import ClusterPage from '#/page/cluster/ClusterPage';

export const metadata: Metadata = {
  title: 'Cluster | Less3',
  description: 'View cluster nodes, health, and leadership',
};

const Page = () => {
  return <ClusterPage />;
};

export default Page;
