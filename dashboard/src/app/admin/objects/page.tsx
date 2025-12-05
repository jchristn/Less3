import { Metadata } from 'next';
import React from 'react';
import ObjectsPage from '#/page/objects/ObjectsPage';

export const metadata: Metadata = {
  title: 'Objects | Less3',
  description: 'View and manage bucket objects',
};

const Page = () => {
  return <ObjectsPage />;
};

export default Page;

