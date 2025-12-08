import { render } from "@testing-library/react";
import PasswordCheckList from "#/components/base/password/PasswordCheckList";

describe("PasswordCheckList", () => {
  describe("Rendering", () => {
    it("should render password checklist", () => {
      const handleChange = jest.fn();
      const { container } = render(
        <PasswordCheckList value="test" valueAgain="test" onChange={handleChange} />
      );
      // PasswordChecklistMock is rendered from jest.setup.js
      expect(container.textContent).toContain("PasswordChecklistMock");
    });

    it("should render with custom className", () => {
      const handleChange = jest.fn();
      const { container } = render(
        <PasswordCheckList
          value="test"
          valueAgain="test"
          onChange={handleChange}
          className="custom-class"
        />
      );
      expect(container.firstChild).toBeInTheDocument();
    });
  });
});

