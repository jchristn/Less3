import { render, screen, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoginPage from "#/page/login/LoginPage";
import { renderWithRedux } from "../store/utils";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import { updateSdkEndPoint } from "#/services/sdk.service";
import { localStorageKeys } from "#/constants/constant";

jest.mock("#/store/slice/sdkSlice");
jest.mock("#/services/sdk.service");
jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/",
}));

jest.mock("antd", () => {
  const actual = jest.requireActual("antd");
  return {
    ...actual,
    message: {
      success: jest.fn(),
      error: jest.fn(),
      warning: jest.fn(),
    },
  };
});

describe("LoginPage", () => {
  const mockValidateConnectivity = jest.fn();
  const mockPush = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    (useValidateConnectivityMutation as jest.Mock).mockReturnValue([
      mockValidateConnectivity,
      { isLoading: false },
    ]);
    (updateSdkEndPoint as jest.Mock).mockImplementation(() => {});
  });

  describe("Rendering", () => {
    it("should render login form", () => {
      renderWithRedux(<LoginPage />, true);
      const input = screen.getByPlaceholderText("https://your-less3-server.com");
      const submitButton = screen.getByRole("button");

      expect(screen.getByLabelText("Less3 Server URL")).toBeInTheDocument();
      expect(input).toBeInTheDocument();
      expect({
        label: screen.getByLabelText("Less3 Server URL").textContent,
        placeholder: input.getAttribute("placeholder"),
        buttonTag: submitButton.tagName,
        hasIcon: submitButton.querySelector("svg") !== null,
      }).toMatchInlineSnapshot(`
{
  "buttonTag": "BUTTON",
  "hasIcon": true,
  "label": "",
  "placeholder": "https://your-less3-server.com",
}
`);
    });

    it("should load saved URL from localStorage", async () => {
      const savedUrl = "http://saved-url.com";
      localStorage.setItem(localStorageKeys.less3APIUrl, savedUrl);
      mockValidateConnectivity.mockResolvedValue({ unwrap: () => Promise.resolve(true) });

      renderWithRedux(<LoginPage />, true);

      await waitFor(() => {
        const input = screen.getByPlaceholderText("https://your-less3-server.com");
        expect(input).toHaveValue(savedUrl);
      });
    });
  });

  describe("User Interactions", () => {
    it("should update URL input value", async () => {
      renderWithRedux(<LoginPage />, true);
      const input = screen.getByPlaceholderText("https://your-less3-server.com");

      await userEvent.clear(input);
      await userEvent.type(input, "http://test.com");

      expect(input).toHaveValue("http://test.com");
    });

    it("should validate connectivity on form submit", async () => {
      mockValidateConnectivity.mockResolvedValue({ unwrap: () => Promise.resolve(true) });

      renderWithRedux(<LoginPage />, true);
      const input = screen.getByPlaceholderText("https://your-less3-server.com");
      const submitButton = screen.getByRole("button");

      await userEvent.clear(input);
      await userEvent.type(input, "http://test.com");
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(mockValidateConnectivity).toHaveBeenCalled();
        expect(updateSdkEndPoint).toHaveBeenCalled();
      });
    });

    it("should handle connectivity validation error", async () => {
      const error = { message: "Connection failed" };
      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockRejectedValue(error),
      });
      const { message } = require("antd");

      renderWithRedux(<LoginPage />, true);
      const input = screen.getByPlaceholderText("https://your-less3-server.com");
      const submitButton = screen.getByRole("button");

      await userEvent.clear(input);
      await userEvent.type(input, "http://test.com");
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(message.error).toHaveBeenCalled();
      });
    });

    it("should handle successful connection and navigate", async () => {
      const mockPush = jest.fn();
      jest.spyOn(require("next/navigation"), "useRouter").mockReturnValue({
        push: mockPush,
        replace: jest.fn(),
      });

      mockValidateConnectivity.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue(true),
      });

      renderWithRedux(<LoginPage />, true);
      const input = screen.getByPlaceholderText("https://your-less3-server.com");
      const submitButton = screen.getByRole("button");

      await userEvent.clear(input);
      await userEvent.type(input, "http://test.com");
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalled();
      });
    });
  });

  describe("Snapshots", () => {
    it("should match default render", () => {
      const { container } = renderWithRedux(<LoginPage />, true);
      expect(container.firstChild).toMatchSnapshot();
    });
  });
});

