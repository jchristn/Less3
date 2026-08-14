import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/cluster/page";

jest.mock("#/page/cluster/ClusterPage", () => {
  return function MockClusterPage() {
    return <div>Cluster Page</div>;
  };
});

describe("Cluster Page", () => {
  describe("Rendering", () => {
    it("should render ClusterPage", () => {
      render(<Page />);
      expect(screen.getByText("Cluster Page")).toBeInTheDocument();
    });
  });
});
