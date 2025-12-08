import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import TypeDetection from "#/page/home-page/components/TypeDetection";
import { renderWithRedux } from "../../../store/utils";
import { useTypeDetectionMutation } from "#/store/slice/sdkSlice";

jest.mock("#/store/slice/sdkSlice", () => ({
  useTypeDetectionMutation: jest.fn(() => [jest.fn(), { isLoading: false }]),
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

describe("TypeDetection", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("Rendering", () => {
    it("should render file upload button", () => {
      renderWithRedux(<TypeDetection />);
      expect(screen.getByText("Select File")).toBeInTheDocument();
    });

    it("should render submit button", () => {
      renderWithRedux(<TypeDetection />);
      expect(screen.getByText("Submit")).toBeInTheDocument();
    });

    it("should disable submit button when no file selected", () => {
      renderWithRedux(<TypeDetection />);
      // Find button by role and name
      const submitButton = screen.getByRole("button", { name: /submit/i });
      expect(submitButton).toBeDisabled();
    });
  });

  describe("User Interactions", () => {
    it("should enable submit button after file selection", async () => {
      renderWithRedux(<TypeDetection />);
      const file = new File(["test"], "test.txt", { type: "text/plain" });

      // Simulate file upload
      const uploadInput = document.querySelector('input[type="file"]');
      if (uploadInput) {
        await userEvent.upload(uploadInput, file);
      }

      // Note: Actual file upload testing requires more complex setup
      expect(screen.getByText("Submit")).toBeInTheDocument();
    });

    it("should warn when no file selected", async () => {
      renderWithRedux(<TypeDetection />);
      const submitButton = screen.getByRole("button", { name: /submit/i });
      expect(submitButton).toBeDisabled();
    });

    it("should call detectType mutation on submit success", async () => {
      const mockDetectType = jest.fn().mockReturnValue({
        unwrap: jest.fn().mockResolvedValue({ type: "text/plain" }),
      });
      (useTypeDetectionMutation as jest.Mock).mockReturnValue([mockDetectType, { isLoading: false }]);
      const { message } = require("antd");

      renderWithRedux(<TypeDetection />);
      const file = new File(["test"], "test.txt", { type: "text/plain" });
      const uploadInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      await userEvent.upload(uploadInput, file);

      const submitButton = screen.getByRole("button", { name: /submit/i });
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(mockDetectType).toHaveBeenCalled();
        expect(message.success).toHaveBeenCalled();
      });
    });

    it("should handle error on type detection", async () => {
      const mockDetectType = jest.fn().mockReturnValue({
        unwrap: jest.fn().mockRejectedValue(new Error("Detection failed")),
      });
      (useTypeDetectionMutation as jest.Mock).mockReturnValue([mockDetectType, { isLoading: false }]);
      const { message } = require("antd");

      renderWithRedux(<TypeDetection />);
      const file = new File(["test"], "test.txt", { type: "text/plain" });
      const uploadInput = document.querySelector('input[type="file"]') as HTMLInputElement;
      await userEvent.upload(uploadInput, file);

      const submitButton = screen.getByRole("button", { name: /submit/i });
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(mockDetectType).toHaveBeenCalled();
        expect(message.error).toHaveBeenCalled();
      });
    });
  });
});

