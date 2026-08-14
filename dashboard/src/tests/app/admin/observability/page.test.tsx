import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/observability/page";

jest.mock("#/page/observability/ObservabilityPage", () => {
  return function MockObservabilityPage() {
    return <div>Observability Page</div>;
  };
});

describe("Observability Page", () => {
  describe("Rendering", () => {
    it("should render ObservabilityPage", () => {
      render(<Page />);
      expect(screen.getByText("Observability Page")).toBeInTheDocument();
    });
  });
});
