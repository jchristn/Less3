import { Metadata } from 'next';
import React from 'react';
import ApiExplorerPage from '#/page/api-explorer/ApiExplorerPage';

export const metadata: Metadata = {
  title: 'API Explorer | Less3',
  description: 'Explore and test Less3 APIs',
};

const Page = () => {
  return <ApiExplorerPage />;
};

export default Page;
