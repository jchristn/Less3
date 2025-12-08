import { render, screen } from "@testing-library/react";
import Home from "#/app/page";
import { renderWithRedux } from "../store/utils";

jest.mock("#/page/login/LoginPage", () => {
  return function MockLoginPage() {
    return <div>Login Page</div>;
  };
});

describe("Home", () => {
  describe("Rendering", () => {
    it("should render LoginPage", () => {
      render(<Home />);
      expect(screen.getByText("Login Page")).toBeInTheDocument();
    });
  });
});

