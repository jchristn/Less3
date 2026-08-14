import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import ManageLocksModal from "#/page/maintenance/ManageLocksModal";
import { clutchApiUrl, clutchUiUrl } from "#/constants/config";

describe("ManageLocksModal", () => {
  it("does not show the modal until the trigger is clicked", () => {
    render(<ManageLocksModal />);

    expect(
      screen.getByRole("button", { name: /Manage Locks/ })
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Open Clutch Dashboard" })
    ).not.toBeInTheDocument();
  });

  it("shows the Clutch credentials and URLs when opened", async () => {
    const user = userEvent.setup();
    render(<ManageLocksModal />);

    await user.click(screen.getByRole("button", { name: /Manage Locks/ }));

    expect(screen.getByText("Clutch dashboard URL")).toBeInTheDocument();
    expect(screen.getByText("Clutch server API URL")).toBeInTheDocument();
    expect(screen.getByText("Access key (login)")).toBeInTheDocument();
    expect(screen.getByText("clutch-default-access-key")).toBeInTheDocument();
    expect(screen.getByText("admin@clutch.local")).toBeInTheDocument();
    expect(screen.getByText("clutchadmin")).toBeInTheDocument();

    // URLs are rendered as anchors pointing at the configured endpoints.
    const uiLink = screen.getByRole("link", { name: new RegExp(clutchUiUrl) });
    expect(uiLink).toHaveAttribute("href", clutchUiUrl);
    const apiLink = screen.getByRole("link", { name: new RegExp(clutchApiUrl) });
    expect(apiLink).toHaveAttribute("href", clutchApiUrl);
  });

  it("opens the configured Clutch UI URL in a new tab via the primary button", async () => {
    const user = userEvent.setup();
    const openSpy = jest.spyOn(window, "open").mockImplementation(() => null);

    render(<ManageLocksModal />);
    await user.click(screen.getByRole("button", { name: /Manage Locks/ }));
    await user.click(
      screen.getByRole("button", { name: "Open Clutch Dashboard" })
    );

    expect(openSpy).toHaveBeenCalledWith(
      clutchUiUrl,
      "_blank",
      "noopener,noreferrer"
    );

    openSpy.mockRestore();
  });
});
