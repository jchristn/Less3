import { render, screen } from "@testing-library/react";
import { Form } from "antd";
import FormItem from "#/components/base/form/FormItem";

describe("FormItem", () => {
  describe("Rendering", () => {
    it("should render form item with label", () => {
      render(
        <Form>
          <FormItem label="Test Label" name="test">
            <input />
          </FormItem>
        </Form>
      );
      expect(screen.getByText("Test Label")).toBeInTheDocument();
    });

    it("should render with children", () => {
      render(
        <Form>
          <FormItem name="test">
            <input placeholder="Test input" />
          </FormItem>
        </Form>
      );
      expect(screen.getByPlaceholderText("Test input")).toBeInTheDocument();
    });

    it("should render with custom className", () => {
      const { container } = render(
        <Form>
          <FormItem className="custom-form-item" name="test">
            <input />
          </FormItem>
        </Form>
      );
      expect(container.querySelector(".custom-form-item")).toBeInTheDocument();
    });
  });
});

