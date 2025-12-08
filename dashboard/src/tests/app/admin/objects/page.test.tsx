import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/objects/page";

jest.mock("#/page/objects/ObjectsPage", () => {
  return function MockObjectsPage() {
    return <div>Objects Page</div>;
  };
});

describe("Objects Page", () => {
  describe("Rendering", () => {
    it("should render ObjectsPage", () => {
      render(<Page />);
      expect(screen.getByText("Objects Page")).toBeInTheDocument();
    });
  });
});

