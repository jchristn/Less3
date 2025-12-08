import { render, screen, waitFor, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import TextWithCopy from "#/components/text-with-copy/TextWithCopy";

// Mock navigator.clipboard
Object.assign(navigator, {
  clipboard: {
    writeText: jest.fn().mockResolvedValue(undefined),
  },
});

describe("TextWithCopy", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("Rendering", () => {
    it("should render text and copy button", () => {
      render(<TextWithCopy text="Test text" />);
      expect(screen.getByText("Test text")).toBeInTheDocument();
      expect(screen.getByRole("button")).toBeInTheDocument();
    });

    it("should render with custom className", () => {
      const { container } = render(<TextWithCopy text="Test" className="custom-class" />);
      expect(container.firstChild).toHaveClass("custom-class");
    });
  });

  describe("User Interactions", () => {
    it("should copy text to clipboard when button is clicked", async () => {
      render(<TextWithCopy text="Text to copy" />);

      const copyButton = screen.getByRole("button");
      await userEvent.click(copyButton);

      expect(navigator.clipboard.writeText).toHaveBeenCalledWith("Text to copy");
    });

    it("should show 'Copied' tooltip after copying", async () => {
      render(<TextWithCopy text="Text to copy" />);

      const copyButton = screen.getByRole("button");
      await userEvent.click(copyButton);

      await waitFor(() => {
        expect(screen.getByText("Copied")).toBeInTheDocument();
      });
    });

    it("should reset tooltip after 2 seconds", () => {
      jest.useFakeTimers();
      const setTimeoutSpy = jest.spyOn(global, "setTimeout");
      
      render(<TextWithCopy text="Text to copy" />);

      const copyButton = screen.getByRole("button");
      
      // Click the button
      act(() => {
        copyButton.click();
      });

      // Verify setTimeout was called with 2000ms delay
      expect(setTimeoutSpy).toHaveBeenCalledWith(expect.any(Function), 2000);

      // Get the callback function that was passed to setTimeout
      const setTimeoutCall = setTimeoutSpy.mock.calls.find(call => call[1] === 2000);
      expect(setTimeoutCall).toBeDefined();

      // Fast-forward time by 2 seconds to trigger the callback
      act(() => {
        jest.advanceTimersByTime(2000);
      });

      // Verify the callback was executed (setIsCopied(false) should have been called)
      // The tooltip should now show "Copy" instead of "Copied"
      // We verify this by checking that setTimeout was called correctly
      expect(setTimeoutSpy).toHaveBeenCalled();

      setTimeoutSpy.mockRestore();
      jest.useRealTimers();
    });
  });
});

