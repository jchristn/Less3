import { render, screen } from "@testing-library/react";
import Page from "#/app/admin/buckets/page";

jest.mock("#/page/buckets/BucketsPage", () => {
  return function MockBucketsPage() {
    return <div>Buckets Page</div>;
  };
});

describe("Buckets Page", () => {
  describe("Rendering", () => {
    it("should render BucketsPage", () => {
      render(<Page />);
      expect(screen.getByText("Buckets Page")).toBeInTheDocument();
    });
  });
});

