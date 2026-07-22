import type { Metadata } from 'next';
import MaintenancePage from '#/page/maintenance/MaintenancePage';

export const metadata: Metadata = {
  title: 'Maintenance | Less3',
  description: 'Manage maintenance and runtime settings',
};

export default function Page() {
  return <MaintenancePage />;
}
