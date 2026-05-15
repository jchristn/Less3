import { render, screen, waitFor } from "@testing-library/react";
import { withConnectivityValidation } from "#/hoc/hoc";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import {
  clearDashboardSession,
  getInitialAdminApiKey,
  getInitialApiEndpoint,
  updateSdkEndPoint,
} from "#/services/sdk.service";
import { localStorageKeys } from "#/constants/constant";

jest.mock("#/store/slice/sdkSlice");
jest.mock("#/services/sdk.service");

const mockPush = jest.fn();
const mockReplace = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    replace: mockReplace,
  }),
}));

const mockValidateConnectivity = jest.fn();
const mockGetInitialApiEndpoint = getInitialApiEndpoint as jest.Mock;
const mockGetInitialAdminApiKey = getInitialAdminApiKey as jest.Mock;
const mockUpdateSdkEndPoint = updateSdkEndPoint as jest.Mock;
const mockClearDashboardSession = clearDashboardSession as jest.Mock;

describe("withConnectivityValidation", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    localStorage.setItem(localStorageKeys.less3APIUrl, `http://test-${Date.now()}.com`);
    localStorage.setItem(localStorageKeys.adminApiKey, "less3admin");
    mockGetInitialApiEndpoint.mockImplementation(
      () => localStorage.getItem(localStorageKeys.less3APIUrl) || "http://localhost:8000"
    );
    mockGetInitialAdminApiKey.mockImplementation(
      () => localStorage.getItem(localStorageKeys.adminApiKey) || ""
    );
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue(true),
    });
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: false, isSuccess: false, isError: false, error: null },
    ]);
    mockClearDashboardSession.mockImplementation(() => {
      localStorage.removeItem(localStorageKeys.less3APIUrl);
      localStorage.removeItem(localStorageKeys.adminApiKey);
    });
  });

  it("renders the wrapped component after successful validation", async () => {
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockReturnValue(new Promise(() => {})),
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

  it("shows a loading state while validation is pending", () => {
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockReturnValue(new Promise(() => {})),
    });
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: true, isSuccess: false, isError: false, error: null },
    ]);

    const TestComponent = () => <div>Test Component</div>;
    const WrappedComponent = withConnectivityValidation(TestComponent);

    render(<WrappedComponent />);

    expect(screen.getByText("Validating connectivity...")).toBeInTheDocument();
  });

  it("shows an error state on validation failure", () => {
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
    expect(screen.getByText("Back to Login")).toBeInTheDocument();
  });

  it("retries validation from the error state", async () => {
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
    screen.getByText("Retry").click();

    await waitFor(() => {
      expect(mockValidateConnectivity).toHaveBeenCalled();
    });
  });

  it("clears the saved session and navigates to login", async () => {
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
    screen.getByText("Back to Login").click();

    await waitFor(() => {
      expect(localStorage.getItem(localStorageKeys.less3APIUrl)).toBeNull();
      expect(localStorage.getItem(localStorageKeys.adminApiKey)).toBeNull();
      expect(mockPush).toHaveBeenCalledWith("/");
    });
  });

  it("redirects to login immediately when no admin API key is saved", async () => {
    localStorage.removeItem(localStorageKeys.adminApiKey);
    mockGetInitialAdminApiKey.mockReturnValue("");

    const TestComponent = () => <div>Test Component</div>;
    const WrappedComponent = withConnectivityValidation(TestComponent);

    render(<WrappedComponent />);

    await waitFor(() => {
      expect(mockReplace).toHaveBeenCalledWith("/");
    });
  });

  it("reuses the validation cache for the same URL and key", async () => {
    localStorage.setItem(localStorageKeys.less3APIUrl, "http://test.com");
    localStorage.setItem(localStorageKeys.adminApiKey, "less3admin");
    mockGetInitialApiEndpoint.mockReturnValue("http://test.com");
    mockGetInitialAdminApiKey.mockReturnValue("less3admin");
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockReturnValue(new Promise(() => {})),
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

  it("shows the initializing fallback before validation resolves", () => {
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockReturnValue(new Promise(() => {})),
    });
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: false, isSuccess: false, isError: false, error: null },
    ]);

    const TestComponent = () => <div>Test Component</div>;
    const WrappedComponent = withConnectivityValidation(TestComponent);

    render(<WrappedComponent />);

    expect(screen.getByText("Initializing...")).toBeInTheDocument();
  });
});
