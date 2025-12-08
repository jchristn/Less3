import { render, screen } from "@testing-library/react";
import Upload from "#/components/base/upload/Upload";

describe("Upload", () => {
  describe("Rendering", () => {
    it("should render upload component", () => {
      const { container } = render(<Upload />);
      expect(container.querySelector(".ant-upload")).toBeInTheDocument();
    });

    it("should render with custom className", () => {
      const { container } = render(<Upload className="custom-upload" />);
      expect(container.querySelector(".custom-upload")).toBeInTheDocument();
    });

    it("should render with children", () => {
      render(
        <Upload>
          <button>Upload Button</button>
        </Upload>
      );
      expect(screen.getByRole("button", { name: "Upload Button" })).toBeInTheDocument();
    });
  });
});

