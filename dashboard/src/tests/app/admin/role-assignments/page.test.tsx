import { render, screen } from '@testing-library/react';
import Page from '#/app/admin/role-assignments/page';

jest.mock('#/page/role-assignments/RoleAssignmentsPage', () => {
  return function MockRoleAssignmentsPage() {
    return <div>Role Assignments Page</div>;
  };
});

describe('Role Assignments Page', () => {
  describe('Rendering', () => {
    it('should render RoleAssignmentsPage', () => {
      render(<Page />);
      expect(screen.getByText('Role Assignments Page')).toBeInTheDocument();
    });
  });
});
