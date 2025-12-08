import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ConfirmationModal from "#/components/confirmation-modal/ConfirmationModal";

describe("ConfirmationModal", () => {
  describe("Rendering", () => {
    it("should render when isModelVisible is true", () => {
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={jest.fn()}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
        />
      );
      expect(screen.getByText("Confirm Action")).toBeInTheDocument();
      expect(screen.getByText("Are you sure?")).toBeInTheDocument();
    });

    it("should not render when isModelVisible is false", () => {
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={false}
          setIsModelVisible={jest.fn()}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
        />
      );
      expect(screen.queryByText("Are you sure?")).not.toBeInTheDocument();
    });

    it("should render cancel and confirm buttons", () => {
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={jest.fn()}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
        />
      );
      expect(screen.getByTestId("confirmation-modal-cancel-button")).toBeInTheDocument();
      expect(screen.getByTestId("confirmation-modal-confirm-button")).toBeInTheDocument();
    });

    it("should show loading state on confirm button", () => {
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={jest.fn()}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
          isLoading={true}
        />
      );
      const confirmButton = screen.getByTestId("confirmation-modal-confirm-button");
      expect(confirmButton).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call setIsModelVisible(false) when cancel is clicked", async () => {
      const setIsModelVisible = jest.fn();
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={setIsModelVisible}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
        />
      );

      const cancelButton = screen.getByTestId("confirmation-modal-cancel-button");
      await userEvent.click(cancelButton);
      expect(setIsModelVisible).toHaveBeenCalledWith(false);
    });

    it("should call handleConfirm when confirm is clicked", async () => {
      const handleConfirm = jest.fn();
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={jest.fn()}
          handleConfirm={handleConfirm}
          paragraphText="Are you sure?"
        />
      );

      const confirmButton = screen.getByTestId("confirmation-modal-confirm-button");
      await userEvent.click(confirmButton);
      expect(handleConfirm).toHaveBeenCalledTimes(1);
    });

    it("should call setIsModelVisible(false) when modal is closed", async () => {
      const setIsModelVisible = jest.fn();
      render(
        <ConfirmationModal
          title="Confirm Action"
          isModelVisible={true}
          setIsModelVisible={setIsModelVisible}
          handleConfirm={jest.fn()}
          paragraphText="Are you sure?"
        />
      );

      const closeButton = screen.getByRole("button", { name: /close/i });
      await userEvent.click(closeButton);
      expect(setIsModelVisible).toHaveBeenCalledWith(false);
    });
  });
});

