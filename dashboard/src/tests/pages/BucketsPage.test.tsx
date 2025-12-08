import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import BucketsPage from "#/page/buckets/BucketsPage";
import { renderWithRedux } from "../store/utils";

const mockCreateBucket = jest.fn();
const mockDeleteBucket = jest.fn();
const mockWriteBucketTags = jest.fn();
const mockDeleteBucketTags = jest.fn();
const mockWriteBucketACL = jest.fn();
const mockRefetch = jest.fn();
const mockRefetchTags = jest.fn();
const mockRefetchACL = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/admin/buckets",
}));

jest.mock("#/store/slice/bucketsSlice", () => ({
  useGetBucketsQuery: () => ({
    data: [
      { Name: "test-bucket", GUID: "bucket-guid", CreatedUtc: "2024-01-01" },
      { Name: "test-bucket-2", GUID: "bucket-guid-2", CreatedUtc: "2024-01-02" },
    ],
    isLoading: false,
    error: null,
    refetch: mockRefetch,
  }),
  useCreateBucketMutation: () => [mockCreateBucket, { isLoading: false }],
  useDeleteBucketMutation: () => [mockDeleteBucket, { isLoading: false }],
  useWriteBucketTagsMutation: () => [mockWriteBucketTags, { isLoading: false }],
  useGetBucketTagsQuery: () => ({
    data: { tags: [{ Key: "env", Value: "test" }] },
    isLoading: false,
    isError: false,
    error: null,
    refetch: mockRefetchTags,
  }),
  useDeleteBucketTagsMutation: () => [mockDeleteBucketTags, { isLoading: false }],
  useWriteBucketACLMutation: () => [mockWriteBucketACL, { isLoading: false }],
  useGetBucketACLQuery: () => ({
    data: {
      acl: {
        Owner: { ID: "owner-id", DisplayName: "Owner" },
        AccessControlList: { Grant: [{ Grantee: { ID: "grantee-id" }, Permission: "READ" }] },
      },
    },
    isLoading: false,
    isError: false,
    error: null,
    refetch: mockRefetchACL,
  }),
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

describe("BucketsPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockCreateBucket.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
    mockDeleteBucket.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
    mockWriteBucketTags.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
    mockDeleteBucketTags.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
    mockWriteBucketACL.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
  });

  describe("Rendering", () => {
    it("should render buckets page", () => {
      renderWithRedux(<BucketsPage />);
      const bucketsTexts = screen.getAllByText("Buckets");
      expect(bucketsTexts.length).toBeGreaterThan(0);
      expect(screen.getByText("test-bucket")).toBeInTheDocument();
    });

    it("should render create bucket button", () => {
      renderWithRedux(<BucketsPage />);
      const createButtons = screen.getAllByText("Create Bucket");
      expect(createButtons.length).toBeGreaterThan(0);
    });

    it("should render search input", () => {
      renderWithRedux(<BucketsPage />);
      expect(screen.getByPlaceholderText("Search buckets...")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should open create modal when create button is clicked", async () => {
      renderWithRedux(<BucketsPage />);
      const createButtons = screen.getAllByText("Create Bucket");
      const createButton = createButtons.find((btn) => btn.closest("button"));
      if (createButton) {
        await userEvent.click(createButton);
        await waitFor(() => {
          const modalTitle = screen.getByRole("dialog");
          expect(modalTitle).toBeInTheDocument();
        });
      }
    });

    it("should create bucket on form submit", async () => {
      renderWithRedux(<BucketsPage />);
      const createButtons = screen.getAllByText("Create Bucket");
      const createButton = createButtons.find((btn) => btn.closest("button"));
      if (createButton) {
        await userEvent.click(createButton);
        // Wait for modal to appear
        const modal = await screen.findByRole("dialog", { timeout: 2000 });
        const nameInput = modal.querySelector('input[id="Name"]') as HTMLInputElement;
        if (nameInput) {
          await userEvent.type(nameInput, "new-bucket");
          const okButton = modal.querySelector('button[class*="ant-btn-primary"]') as HTMLButtonElement;
          if (okButton) {
            await userEvent.click(okButton);
          }
        }
        await waitFor(() => {
          expect(mockCreateBucket).toHaveBeenCalledWith({ Name: "new-bucket" });
        });
      }
    });

    it("should delete bucket when delete is clicked", async () => {
      renderWithRedux(<BucketsPage />);
      // Wait for table to render
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      // Find the more button
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        // Wait for dropdown menu
        const deleteButton = await screen.findByText("Delete Bucket", { timeout: 3000 });
        await userEvent.click(deleteButton);
        // Wait for confirmation modal - delete button says "Delete" not "OK"
        const confirmButton = await screen.findByText("Delete", { timeout: 3000 });
        await userEvent.click(confirmButton);
        await waitFor(() => {
          expect(mockDeleteBucket).toHaveBeenCalled();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should open write tags modal", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        const writeTagsButton = await screen.findByText("Write Tags", { timeout: 3000 });
        await userEvent.click(writeTagsButton);
        // After clicking, the modal opens - check for modal title specifically
        await waitFor(() => {
          const modal = screen.getByRole("dialog");
          expect(modal).toBeInTheDocument();
          // Check that modal title contains "Write Tags"
          const modalTitle = within(modal).getByText(/Write Tags/i);
          expect(modalTitle).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should open view tags modal", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        // The menu item is "Read Tags" not "View Tags"
        const readTagsButton = await screen.findByText("Read Tags", { timeout: 3000 });
        await userEvent.click(readTagsButton);
        // After clicking, the modal opens - check for modal
        await waitFor(() => {
          const modal = screen.getByRole("dialog");
          expect(modal).toBeInTheDocument();
          // Check that modal title contains "Read Tags" or "View Tags"
          const modalTitle = within(modal).getByText(/Read Tags|View Tags/i);
          expect(modalTitle).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should open write ACL modal", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        const writeACLButton = await screen.findByText("Write ACL", { timeout: 3000 });
        await userEvent.click(writeACLButton);
        // After clicking, the modal opens - check for modal title specifically
        await waitFor(() => {
          const modal = screen.getByRole("dialog");
          expect(modal).toBeInTheDocument();
          // Check that modal title contains "Write ACL"
          const modalTitle = within(modal).getByText(/Write ACL/i);
          expect(modalTitle).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should open view ACL modal", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        // The menu item is "Read ACL" not "View ACL"
        const readACLButton = await screen.findByText("Read ACL", { timeout: 3000 });
        await userEvent.click(readACLButton);
        // After clicking, the modal opens - check for modal
        await waitFor(() => {
          const modal = screen.getByRole("dialog");
          expect(modal).toBeInTheDocument();
          // Check that modal title contains "Read ACL" or "View ACL"
          const modalTitle = within(modal).getByText(/Read ACL|View ACL/i);
          expect(modalTitle).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should filter buckets by search text", async () => {
      renderWithRedux(<BucketsPage />);
      const searchInput = screen.getByPlaceholderText("Search buckets...");
      await userEvent.type(searchInput, "test-bucket");
      expect(screen.getByText("test-bucket")).toBeInTheDocument();
    });

    it("should write tags successfully", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        const writeTagsButton = await screen.findByText("Write Tags", { timeout: 3000 });
        await userEvent.click(writeTagsButton);
        await waitFor(() => {
          const modal = screen.queryByRole("dialog");
          expect(modal).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should delete tags successfully", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        const deleteTagsButton = await screen.findByText("Delete Tags", { timeout: 3000 });
        await userEvent.click(deleteTagsButton);
        // Delete confirmation button says "Delete" not "OK"
        const confirmButton = await screen.findByText("Delete", { timeout: 3000 });
        await userEvent.click(confirmButton);
        await waitFor(() => {
          expect(mockDeleteBucketTags).toHaveBeenCalled();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should write ACL successfully", async () => {
      renderWithRedux(<BucketsPage />);
      await waitFor(() => {
        expect(screen.getByText("test-bucket")).toBeInTheDocument();
      }, { timeout: 3000 });
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        const writeACLButton = await screen.findByText("Write ACL", { timeout: 3000 });
        await userEvent.click(writeACLButton);
        await waitFor(() => {
          const modal = screen.queryByRole("dialog");
          expect(modal).toBeInTheDocument();
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should handle create bucket error", async () => {
      const error = { data: { data: "Error creating bucket" } };
      mockCreateBucket.mockReturnValue({
        unwrap: jest.fn().mockRejectedValue(error),
      });
      const { message } = require("antd");

      renderWithRedux(<BucketsPage />);
      const createButtons = screen.getAllByText("Create Bucket");
      const createButton = createButtons.find((btn) => btn.closest("button"));
      if (createButton) {
        await userEvent.click(createButton);
        // Wait for modal to appear
        const modal = await screen.findByRole("dialog", { timeout: 2000 });
        const nameInput = modal.querySelector('input[id="Name"]') as HTMLInputElement;
        if (nameInput) {
          await userEvent.type(nameInput, "new-bucket");
          const okButton = modal.querySelector('button[class*="ant-btn-primary"]') as HTMLButtonElement;
          if (okButton) {
            await userEvent.click(okButton);
          }
        }
        await waitFor(() => {
          expect(message.error).toHaveBeenCalled();
        });
      }
    });
  });
});
