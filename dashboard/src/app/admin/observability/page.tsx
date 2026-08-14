import { Metadata } from 'next';
import React from 'react';
import ObservabilityPage from '#/page/observability/ObservabilityPage';

export const metadata: Metadata = {
  title: 'Observability | Less3',
  description: 'Links to bundled Grafana and Prometheus observability tooling',
};

const Page = () => {
  return <ObservabilityPage />;
};

export default Page;
