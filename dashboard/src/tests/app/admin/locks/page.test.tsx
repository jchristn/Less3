import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/locks/page";

jest.mock("#/page/locks/LocksPage", () => {
  return function MockLocksPage() {
    return <div>Locks Page</div>;
  };
});

describe("Locks Page", () => {
  describe("Rendering", () => {
    it("should render LocksPage", () => {
      render(<Page />);
      expect(screen.getByText("Locks Page")).toBeInTheDocument();
    });
  });
});
