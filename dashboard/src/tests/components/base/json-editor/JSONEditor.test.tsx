import { render, screen } from "@testing-library/react";
import JSONEditor from "#/components/base/json-editor/JSONEditor";

jest.mock("jsoneditor-react", () => ({
  JsonEditor: ({ value, onChange, mode, ...props }: any) => {
    const testId = props["data-testid"] || "json-editor";
    return (
      <div data-testid={testId} data-mode={mode}>
        <button onClick={() => onChange({ test: "value" })}>Change</button>
        <div>{JSON.stringify(value)}</div>
      </div>
    );
  },
}));

describe("JSONEditor", () => {
  const mockOnChange = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("Rendering", () => {
    it("should render with default props", () => {
      render(<JSONEditor value={{ test: "data" }} onChange={mockOnChange} uniqueKey="test-key" />);
      expect(screen.getByTestId("json-editor")).toBeInTheDocument();
      expect(screen.getByTestId("json-editor")).toHaveAttribute("data-mode", "code");
    });

    it("should render in tree mode", () => {
      render(<JSONEditor value={{ test: "data" }} onChange={mockOnChange} mode="tree" uniqueKey="test-key" />);
      expect(screen.getByTestId("json-editor")).toHaveAttribute("data-mode", "tree");
    });

    it("should render with custom testId", () => {
      render(
        <JSONEditor value={{ test: "data" }} onChange={mockOnChange} uniqueKey="test-key" testId="custom-id" />
      );
      expect(screen.getByTestId("custom-id")).toBeInTheDocument();
    });

    it("should call onChange when value changes", () => {
      render(<JSONEditor value={{ test: "data" }} onChange={mockOnChange} uniqueKey="test-key" />);
      const changeButton = screen.getByText("Change");
      changeButton.click();
      expect(mockOnChange).toHaveBeenCalledWith({ test: "value" });
    });

    it("should render with enableSort prop", () => {
      render(
        <JSONEditor
          value={{ test: "data" }}
          onChange={mockOnChange}
          uniqueKey="test-key"
          enableSort={true}
        />
      );
      expect(screen.getByTestId("json-editor")).toBeInTheDocument();
    });

    it("should render with enableTransform prop", () => {
      render(
        <JSONEditor
          value={{ test: "data" }}
          onChange={mockOnChange}
          uniqueKey="test-key"
          enableTransform={true}
        />
      );
      expect(screen.getByTestId("json-editor")).toBeInTheDocument();
    });

    it("should render with expandOnStart prop", () => {
      render(
        <JSONEditor
          value={{ test: "data" }}
          onChange={mockOnChange}
          uniqueKey="test-key"
          expandOnStart={false}
        />
      );
      expect(screen.getByTestId("json-editor")).toBeInTheDocument();
    });
  });
});

