import { render, screen } from "@testing-library/react";
import Loading from "#/app/loading";

jest.mock("#/components/base/loading/PageLoading", () => {
  return function MockPageLoading() {
    return <div data-testid="page-loading">Loading...</div>;
  };
});

describe("Loading", () => {
  describe("Rendering", () => {
    it("should render PageLoading component", () => {
      render(<Loading />);
      expect(screen.getByTestId("page-loading")).toBeInTheDocument();
    });
  });
});

