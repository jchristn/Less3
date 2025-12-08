import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import UsersPage from "#/page/users/UsersPage";
import { renderWithRedux } from "../store/utils";

const mockCreateUser = jest.fn();
const mockDeleteUser = jest.fn();
const mockRefetch = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/admin/users",
}));

jest.mock("#/store/slice/usersSlice", () => ({
  useGetUsersQuery: () => ({
    data: [
      { GUID: "1", Name: "John Doe", Email: "john@example.com", CreatedUtc: "2024-01-01" },
      { GUID: "2", Name: "Jane Smith", Email: "jane@example.com", CreatedUtc: "2024-01-02" },
    ],
    isLoading: false,
    error: null,
    refetch: mockRefetch,
  }),
  useGetUserByIdQuery: () => ({
    data: { GUID: "1", Name: "John Doe", Email: "john@example.com" },
    isLoading: false,
  }),
  useCreateUserMutation: () => [mockCreateUser, { isLoading: false }],
  useDeleteUserMutation: () => [mockDeleteUser, { isLoading: false }],
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

describe("UsersPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockCreateUser.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
    mockDeleteUser.mockReturnValue({
      unwrap: jest.fn().mockResolvedValue({}),
    });
  });

  describe("Rendering", () => {
    it("should render users page", () => {
      renderWithRedux(<UsersPage />);
      const usersTexts = screen.getAllByText("Users");
      expect(usersTexts.length).toBeGreaterThan(0);
      expect(screen.getByText("John Doe")).toBeInTheDocument();
      expect(screen.getByText("Jane Smith")).toBeInTheDocument();
    });

    it("should render create user button", () => {
      renderWithRedux(<UsersPage />);
      expect(screen.getByText("Create User")).toBeInTheDocument();
    });

    it("should render search input", () => {
      renderWithRedux(<UsersPage />);
      expect(screen.getByPlaceholderText("Search users...")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should open create modal when create button is clicked", async () => {
      renderWithRedux(<UsersPage />);
      const createButtons = screen.getAllByText("Create User");
      const createButton = createButtons.find((btn) => btn.closest("button"));
      if (createButton) {
        await userEvent.click(createButton);
        await waitFor(() => {
          const modal = screen.getByRole("dialog");
          expect(modal).toBeInTheDocument();
        });
      }
    });

    it("should filter users by search text", async () => {
      renderWithRedux(<UsersPage />);
      const searchInput = screen.getByPlaceholderText("Search users...");
      await userEvent.type(searchInput, "John");
      expect(screen.getByText("John Doe")).toBeInTheDocument();
      expect(screen.queryByText("Jane Smith")).not.toBeInTheDocument();
    });

    it("should create user on form submit", async () => {
      renderWithRedux(<UsersPage />);
      const createButtons = screen.getAllByText("Create User");
      const createButton = createButtons.find((btn) => btn.closest("button"));
      if (createButton) {
        await userEvent.click(createButton);
        // Wait for modal to appear
        const modal = await screen.findByRole("dialog", { timeout: 2000 });
        const nameInput = modal.querySelector('input[id="Name"]') as HTMLInputElement;
        const emailInput = modal.querySelector('input[id="Email"]') as HTMLInputElement;
        if (nameInput && emailInput) {
          await userEvent.type(nameInput, "New User");
          await userEvent.type(emailInput, "newuser@example.com");
          const okButton = modal.querySelector('button[class*="ant-btn-primary"]') as HTMLButtonElement;
          if (okButton) {
            await userEvent.click(okButton);
          }
        }
        await waitFor(() => {
          expect(mockCreateUser).toHaveBeenCalled();
        });
      }
    });

    it("should delete user when delete is clicked", async () => {
      const { message } = require("antd");
      renderWithRedux(<UsersPage />);
      // Wait for table to render first
      await waitFor(() => {
        expect(screen.getByText("John Doe")).toBeInTheDocument();
      }, { timeout: 3000 });
      // Find the more button
      const moreButtons = screen.getAllByRole("button");
      const moreButton = moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      if (moreButton) {
        await userEvent.click(moreButton);
        // Wait for dropdown menu
        const deleteButton = await screen.findByText("Delete User", { timeout: 3000 });
        await userEvent.click(deleteButton);
        // Wait for confirmation modal - delete button says "Delete" not "OK"
        const confirmButton = await screen.findByText("Delete", { timeout: 3000 });
        await userEvent.click(confirmButton);
        // Verify API was called and success message was shown
        await waitFor(() => {
          expect(mockDeleteUser).toHaveBeenCalled();
          expect(message.success).toHaveBeenCalledWith('User deleted successfully');
        }, { timeout: 3000 });
      }
    }, 10000);

    it("should view user metadata", async () => {
      renderWithRedux(<UsersPage />);
      await waitFor(() => {
        const moreButtons = screen.getAllByRole("button");
        return moreButtons.find((btn) => btn.querySelector(".anticon-more"));
      }, { timeout: 3000 }).then(async (moreButton) => {
        if (moreButton) {
          await userEvent.click(moreButton);
          await waitFor(async () => {
            const viewMetadataButton = await screen.findByText("View Metadata", { timeout: 2000 });
            await userEvent.click(viewMetadataButton);
          });
          await waitFor(() => {
            const metadataTexts = screen.getAllByText(/Metadata/i);
            expect(metadataTexts.length).toBeGreaterThan(0);
          });
        }
      });
    });
  });
});
