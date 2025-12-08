import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import WriteObjectModal from "#/page/buckets/WriteObjectModal";
import { renderWithRedux } from "../../store/utils";

const mockWriteBucketObject = jest.fn();
const mockOnCancel = jest.fn();
const mockOnSuccess = jest.fn();

jest.mock("#/store/slice/bucketsSlice", () => ({
  useWriteBucketObjectMutation: () => [mockWriteBucketObject, { isLoading: false }],
}));

jest.mock("antd", () => {
  const actual = jest.requireActual("antd");
  return {
    ...actual,
    message: {
      success: jest.fn(),
      error: jest.fn(),
    },
  };
});

describe("WriteObjectModal", () => {
  const mockBucket = { Name: "test-bucket", GUID: "test-guid" };

  beforeEach(() => {
    jest.clearAllMocks();
    mockWriteBucketObject.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
  });

  describe("Rendering", () => {
    it("should render modal when open", () => {
      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );
      expect(screen.getByText(/Write Object to Bucket/)).toBeInTheDocument();
    });

    it("should not render when closed", () => {
      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={false} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );
      expect(screen.queryByText(/Write Object to Bucket/)).not.toBeInTheDocument();
    });

    it("should render form fields", () => {
      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );
      expect(screen.getByPlaceholderText("hello.txt")).toBeInTheDocument();
      expect(screen.getByPlaceholderText("Enter content here...")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call onCancel when cancel button is clicked", async () => {
      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );
      const cancelButton = screen.getByText("Cancel");
      await userEvent.click(cancelButton);
      expect(mockOnCancel).toHaveBeenCalled();
    });

    it("should show error when bucket is null", async () => {
      const { message } = require("antd");
      renderWithRedux(
        <WriteObjectModal bucket={null} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );
      const okButton = screen.getByText("OK");
      await userEvent.click(okButton);
      await waitFor(() => {
        expect(message.error).toHaveBeenCalledWith("Bucket information not available");
      });
    });

    it("should submit form and call onSuccess", async () => {
      mockWriteBucketObject.mockReturnValue({
        unwrap: jest.fn().mockResolvedValue({}),
      });
      const { message } = require("antd");

      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );

      const filenameInput = screen.getByPlaceholderText("hello.txt");
      const contentInput = screen.getByPlaceholderText("Enter content here...");

      await userEvent.type(filenameInput, "test.txt");
      await userEvent.type(contentInput, "test content");

      const okButton = screen.getByText("OK");
      await userEvent.click(okButton);

      await waitFor(() => {
        expect(mockWriteBucketObject).toHaveBeenCalledWith({
          bucketGUID: "test-bucket",
          objectKey: "test.txt",
          content: "test content",
        });
        expect(message.success).toHaveBeenCalled();
        expect(mockOnSuccess).toHaveBeenCalled();
        expect(mockOnCancel).toHaveBeenCalled();
      });
    });

    it("should handle error on submit", async () => {
      const error = { data: { data: "Error message" } };
      mockWriteBucketObject.mockReturnValue({
        unwrap: jest.fn().mockRejectedValue(error),
      });
      const { message } = require("antd");

      renderWithRedux(
        <WriteObjectModal bucket={mockBucket} open={true} onCancel={mockOnCancel} onSuccess={mockOnSuccess} />
      );

      const filenameInput = screen.getByPlaceholderText("hello.txt");
      const contentInput = screen.getByPlaceholderText("Enter content here...");

      await userEvent.type(filenameInput, "test.txt");
      await userEvent.type(contentInput, "test content");

      const okButton = screen.getByText("OK");
      await userEvent.click(okButton);

      await waitFor(() => {
        expect(message.error).toHaveBeenCalledWith("Error message");
      });
    });
  });
});
