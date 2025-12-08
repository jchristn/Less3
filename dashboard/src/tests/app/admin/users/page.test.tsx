import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/users/page";

jest.mock("#/page/users/UsersPage", () => {
  return function MockUsersPage() {
    return <div>Users Page</div>;
  };
});

describe("Users Page", () => {
  describe("Rendering", () => {
    it("should render UsersPage", () => {
      render(<Page />);
      expect(screen.getByText("Users Page")).toBeInTheDocument();
    });
  });
});

