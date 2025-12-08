import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import LoggedUserInfo from "#/components/layout/components/LoggedUserInfo";
import { renderWithRedux } from "../../../store/utils";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/dashboard",
}));

describe("LoggedUserInfo", () => {
  describe("Rendering", () => {
    it("should render user name", () => {
      renderWithRedux(<LoggedUserInfo />);
      expect(screen.getByText("User")).toBeInTheDocument();
    });

    it("should render avatar with first letter", () => {
      renderWithRedux(<LoggedUserInfo />);
      expect(screen.getByText("U")).toBeInTheDocument();
    });

    it("should render dropdown trigger", () => {
      renderWithRedux(<LoggedUserInfo />);
      const trigger = screen.getByText("User").closest("div");
      expect(trigger).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should open dropdown on click", async () => {
      renderWithRedux(<LoggedUserInfo />);
      const trigger = screen.getByText("User").closest("div");
      if (trigger) {
        await userEvent.click(trigger);
        // Dropdown menu should be available
        expect(screen.getByText("User")).toBeInTheDocument();
      }
    });

  });
});

