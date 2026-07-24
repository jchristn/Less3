import RoleAssignmentsPage from '#/page/role-assignments/RoleAssignmentsPage';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Role Assignments | Less3',
  description: 'Manage Less3 role assignments',
};

export default function Page() {
  return <RoleAssignmentsPage />;
}
