import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Modal from "#/components/base/modal/Modal";

describe("Modal", () => {
  describe("Rendering", () => {
    it("should render when open is true", () => {
      render(
        <Modal open={true} title="Test Modal">
          Modal Content
        </Modal>
      );
      expect(screen.getByText("Test Modal")).toBeInTheDocument();
      expect(screen.getByText("Modal Content")).toBeInTheDocument();
    });

    it("should not render when open is false", () => {
      render(
        <Modal open={false} title="Test Modal">
          Modal Content
        </Modal>
      );
      expect(screen.queryByText("Modal Content")).not.toBeInTheDocument();
    });

    it("should render with footer", () => {
      render(
        <Modal open={true} title="Test Modal" footer={<button>Custom Footer</button>}>
          Modal Content
        </Modal>
      );
      expect(screen.getByRole("button", { name: "Custom Footer" })).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call onCancel when close button is clicked", async () => {
      const handleCancel = jest.fn();
      render(
        <Modal open={true} title="Test Modal" onCancel={handleCancel}>
          Modal Content
        </Modal>
      );

      const closeButton = screen.getByRole("button", { name: /close/i });
      await userEvent.click(closeButton);
      expect(handleCancel).toHaveBeenCalledTimes(1);
    });

    it("should call onOk when OK button is clicked", async () => {
      const handleOk = jest.fn();
      render(
        <Modal open={true} title="Test Modal" onOk={handleOk}>
          Modal Content
        </Modal>
      );

      const okButton = screen.getByRole("button", { name: /ok/i });
      await userEvent.click(okButton);
      expect(handleOk).toHaveBeenCalledTimes(1);
    });
  });

  describe("Props/Features", () => {
    it("should render with different sizes", () => {
      const { rerender } = render(
        <Modal open={true} title="Modal" width={500}>
          Content
        </Modal>
      );
      expect(screen.getByText("Content")).toBeInTheDocument();

      rerender(
        <Modal open={true} title="Modal" width={800}>
          Content
        </Modal>
      );
      expect(screen.getByText("Content")).toBeInTheDocument();
    });

    it("should render with centered prop", () => {
      render(
        <Modal open={true} title="Modal" centered>
          Content
        </Modal>
      );
      expect(screen.getByText("Content")).toBeInTheDocument();
    });

    it("should render with maskClosable prop", () => {
      render(
        <Modal open={true} title="Modal" maskClosable={false}>
          Content
        </Modal>
      );
      expect(screen.getByText("Content")).toBeInTheDocument();
    });
  });
});

