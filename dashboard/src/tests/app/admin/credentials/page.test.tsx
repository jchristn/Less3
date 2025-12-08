import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/credentials/page";

jest.mock("#/page/credentials/CredentialsPage", () => {
  return function MockCredentialsPage() {
    return <div>Credentials Page</div>;
  };
});

describe("Credentials Page", () => {
  describe("Rendering", () => {
    it("should render CredentialsPage", () => {
      render(<Page />);
      expect(screen.getByText("Credentials Page")).toBeInTheDocument();
    });
  });
});

