import { render, screen } from "@testing-library/react";
import LoginLayout from "#/components/layout/LoginLayout";
import { renderWithRedux } from "../../store/utils";

jest.mock("next/image", () => ({
  __esModule: true,
  default: (props: any) => {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />;
  },
}));

describe("LoginLayout", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      renderWithRedux(
        <LoginLayout>
          <div>Login Form</div>
        </LoginLayout>,
        true
      );
      expect(screen.getByText("Login Form")).toBeInTheDocument();
    });

    it("should render logo", () => {
      renderWithRedux(
        <LoginLayout>
          <div>Content</div>
        </LoginLayout>,
        true
      );
      expect(screen.getByText("Content")).toBeInTheDocument();
    });
  });
});

