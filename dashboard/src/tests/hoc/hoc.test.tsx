import { render, screen, waitFor } from "@testing-library/react";
import { withConnectivityValidation } from "#/hoc/hoc";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import { updateSdkEndPoint } from "#/services/sdk.service";
import { localStorageKeys } from "#/constants/constant";

jest.mock("#/store/slice/sdkSlice");
jest.mock("#/services/sdk.service");

const mockValidateConnectivity = jest.fn();
const mockUpdateSdkEndPoint = updateSdkEndPoint as jest.Mock;

describe("withConnectivityValidation", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    // Set a unique URL to avoid cache hits
    localStorage.setItem(localStorageKeys.documentAtomAPIUrl, `http://test-${Date.now()}.com`);
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue(true),
    });
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: false, isSuccess: false, isError: false, error: null },
    ]);
  });

  describe("Rendering", () => {
    it("should render wrapped component after successful validation", async () => {
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: true, isError: false, error: null },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);

      await waitFor(() => {
        expect(screen.getByText("Test Component")).toBeInTheDocument();
      });
    });

    it("should show loading state while validating", () => {
      // Set a unique URL to ensure no cache match
      localStorage.setItem(localStorageKeys.documentAtomAPIUrl, `http://loading-test-${Date.now()}.com`);
      
      // Mock unwrap to never resolve (stays in loading state)
      const unwrapPromise = new Promise(() => {}); // Never resolves
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockReturnValue(unwrapPromise),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: true, isSuccess: false, isError: false, error: null },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);
      // When isLoading is true and cachedValid is false, should show loading
      expect(screen.getByText("Validating connectivity...")).toBeInTheDocument();
    });

    it("should show error state on validation failure", () => {
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockRejectedValue(new Error("Connection failed")),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: false, isError: true, error: { message: "Connection failed" } },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);
      expect(screen.getByText("Failed to validate connectivity. Please check your connection.")).toBeInTheDocument();
      expect(screen.getByText(/Connection failed/)).toBeInTheDocument();
    });

    it("should handle retry on error", async () => {
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: false, isError: true, error: { message: "Connection failed" } },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);
      const retryButton = screen.getByText("Retry");
      await waitFor(() => {
        expect(retryButton).toBeInTheDocument();
      });

      // Click retry
      retryButton.click();
      await waitFor(() => {
        expect(mockValidateConnectivity).toHaveBeenCalled();
      });
    });

    it("should use cached validation result", async () => {
      localStorage.setItem(localStorageKeys.documentAtomAPIUrl, "http://test.com");
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: true, isError: false, error: null },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);

      await waitFor(() => {
        expect(mockUpdateSdkEndPoint).toHaveBeenCalled();
      });
    });

    it("should handle cache when URL changes", async () => {
      localStorage.setItem(localStorageKeys.documentAtomAPIUrl, "http://old-url.com");
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: true, isError: false, error: null },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      // Change URL
      localStorage.setItem(localStorageKeys.documentAtomAPIUrl, "http://new-url.com");

      render(<WrappedComponent />);

      await waitFor(() => {
        expect(mockUpdateSdkEndPoint).toHaveBeenCalled();
      });
    });

    it("should show fallback state when neither cached nor success", () => {
      localStorage.clear();
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });
      (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
        mockValidateConnectivity,
        { isLoading: false, isSuccess: false, isError: false, error: null },
      ]);

      const TestComponent = () => <div>Test Component</div>;
      const WrappedComponent = withConnectivityValidation(TestComponent);

      render(<WrappedComponent />);
      // Should show initializing message
      expect(screen.getByText("Initializing...")).toBeInTheDocument();
    });
  });
});

