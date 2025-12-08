import { render, screen } from "@testing-library/react";
import ErrorBoundary from "#/hoc/ErrorBoundary";

// Component that throws an error
const ThrowError = ({ shouldThrow }: { shouldThrow: boolean }) => {
  if (shouldThrow) {
    throw new Error("Test error");
  }
  return <div>No error</div>;
};

describe("ErrorBoundary", () => {
  beforeEach(() => {
    // Suppress console.error for error boundary tests
    jest.spyOn(console, "error").mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe("Rendering", () => {
    it("should render children when there is no error", () => {
      render(
        <ErrorBoundary>
          <div>Test Content</div>
        </ErrorBoundary>
      );
      expect(screen.getByText("Test Content")).toBeInTheDocument();
    });

    it("should render FallBack component when error occurs", () => {
      render(
        <ErrorBoundary>
          <ThrowError shouldThrow={true} />
        </ErrorBoundary>
      );
      expect(screen.getByText(/Unexpected Error/i)).toBeInTheDocument();
    });

    it("should render custom error component when provided", () => {
      const customErrorComponent = (errorMessage?: string) => (
        <div>Custom Error: {errorMessage}</div>
      );
      render(
        <ErrorBoundary errorComponent={customErrorComponent}>
          <ThrowError shouldThrow={true} />
        </ErrorBoundary>
      );
      expect(screen.getByText(/Custom Error/i)).toBeInTheDocument();
    });

    it("should render reload link when allowRefresh is true", () => {
      render(
        <ErrorBoundary allowRefresh={true}>
          <ThrowError shouldThrow={true} />
        </ErrorBoundary>
      );
      expect(screen.getByText("Reload page")).toBeInTheDocument();
    });

    it("should not render reload link when allowRefresh is false", () => {
      render(
        <ErrorBoundary allowRefresh={false}>
          <ThrowError shouldThrow={true} />
        </ErrorBoundary>
      );
      expect(screen.queryByText("Reload page")).not.toBeInTheDocument();
    });
  });
});

