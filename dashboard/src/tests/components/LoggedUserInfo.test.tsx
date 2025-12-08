import { render, screen } from "@testing-library/react";
import LoggedUserInfo from "#/components/layout/components/LoggedUserInfo";
import { usePathname } from "next/navigation";

jest.mock("next/navigation", () => ({
  usePathname: jest.fn(),
}));

describe("LoggedUserInfo", () => {
  beforeAll(() => {
    (global as any).ResizeObserver = class {
      observe() {}
      unobserve() {}
      disconnect() {}
    };
  });

  it("renders the user name and avatar initial", () => {
    (usePathname as jest.Mock).mockReturnValue("/dashboard");

    render(<LoggedUserInfo />);

    expect(screen.getByText("User")).toBeInTheDocument();
    expect(screen.getByText("U")).toBeInTheDocument();
  });
});

