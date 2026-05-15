import { renderWithRedux } from "../store/utils";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoginPage from "#/page/login/LoginPage";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import {
  getInitialAdminApiKey,
  getInitialApiEndpoint,
  persistDashboardSession,
} from "#/services/sdk.service";
import { message } from "#/utils/message";

jest.mock("#/store/slice/sdkSlice");
jest.mock("#/services/sdk.service");

const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: mockPush,
    replace: jest.fn(),
  }),
  usePathname: () => "/",
}));

describe("LoginPage", () => {
  const mockValidateConnectivity = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: false },
    ]);
    (getInitialApiEndpoint as jest.Mock).mockReturnValue("http://localhost:8000");
    (getInitialAdminApiKey as jest.Mock).mockReturnValue("");
    (persistDashboardSession as jest.Mock).mockImplementation(() => {});
  });

  it("renders the admin login form", () => {
    renderWithRedux(<LoginPage />, false, undefined, true);

    expect(screen.getByText("Admin Sign In")).toBeInTheDocument();
    expect(screen.getByLabelText("Less3 Server URL")).toBeInTheDocument();
    expect(screen.getByLabelText("Admin API Key")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Sign In to Dashboard/i })).toBeInTheDocument();
  });

  it("loads saved URL and API key values", async () => {
    (getInitialApiEndpoint as jest.Mock).mockReturnValue("http://saved-url.com");
    (getInitialAdminApiKey as jest.Mock).mockReturnValue("saved-secret");
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue(true),
    });

    renderWithRedux(<LoginPage />, false, undefined, true);

    await waitFor(() => {
      expect(screen.getByLabelText("Less3 Server URL")).toHaveValue("http://saved-url.com");
      expect(screen.getByLabelText("Admin API Key")).toHaveValue("saved-secret");
    });
  });

  it("submits URL and API key for validation", async () => {
    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue(true),
    });

    renderWithRedux(<LoginPage />, false, undefined, true);

    await userEvent.clear(screen.getByLabelText("Less3 Server URL"));
    await userEvent.type(screen.getByLabelText("Less3 Server URL"), "http://test.com");
    await userEvent.type(screen.getByLabelText("Admin API Key"), "super-secret");
    await userEvent.click(screen.getByRole("button", { name: /Sign In to Dashboard/i }));

    await waitFor(() => {
      expect(mockValidateConnectivity).toHaveBeenCalledWith({
        endpoint: "http://test.com",
        apiKey: "super-secret",
      });
      expect(persistDashboardSession).toHaveBeenCalledWith("http://test.com", "super-secret");
      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });
  });

  it("shows the API key mismatch error returned by validation", async () => {
    const error = {
      data: "We are unable to authenticate using the supplied API key.",
    };

    mockValidateConnectivity.mockReturnValue({
      unwrap: jest.fn().mockRejectedValue(error),
    });

    renderWithRedux(<LoginPage />, false, undefined, true);

    await userEvent.clear(screen.getByLabelText("Less3 Server URL"));
    await userEvent.type(screen.getByLabelText("Less3 Server URL"), "http://test.com");
    await userEvent.type(screen.getByLabelText("Admin API Key"), "wrong-key");
    await userEvent.click(screen.getByRole("button", { name: /Sign In to Dashboard/i }));

    await waitFor(() => {
      expect(message.error).toHaveBeenCalledWith(
        "We are unable to authenticate using the supplied API key."
      );
      expect(
        screen.getByText("We are unable to authenticate using the supplied API key.")
      ).toBeInTheDocument();
    });
  });

  it("matches the login page snapshot", () => {
    const { container } = renderWithRedux(<LoginPage />, false, undefined, true);
    expect(container.firstChild).toMatchSnapshot();
  });
});
