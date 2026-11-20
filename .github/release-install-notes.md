## Installing

> [!IMPORTANT]
> **Less3 is free, open-source software and is not code-signed** (signing certificates are an ongoing cost we choose
> not to pass on), so Windows and macOS block the first launch. Your operating system may warn that the developer is
> unidentified. That warning doesn't mean the download is damaged. The steps to allow Less3 are below. To confirm a
> file is the one published here, check it against `SHA256SUMS`.

There are two products, each with its own installers. Nothing else needs to be installed; .NET and Node.js are
included.

- **Less3**, the server (`less3-...`). It puts the `less3` command on your PATH and adds a **Less3** menu entry that
  runs the server in `~/Less3` (`%USERPROFILE%\Less3` on Windows). Run `less3` in any folder to keep the server's
  configuration (`system.json`), database, and objects there instead; the first run asks a few setup questions.
- **Less3 Dashboard**, the web app for managing a server (`less3-dashboard-...`). It puts the `less3-dashboard`
  command on your PATH and adds a **Less3 Dashboard** menu entry that starts it on `http://127.0.0.1:3000` and opens
  your browser. Sign in with your server's URL (`http://127.0.0.1:8000` on this machine) and its admin API key.

### macOS: "Less3" Not Opened

macOS shows *"Less3" Not Opened. Apple could not verify "Less3" is free of malware that may harm your Mac or
compromise your privacy.* (or the same for "Less3 Dashboard") with **Move to Trash** and **Done** buttons. To open it
anyway:

1. Click **Done**. Do not click Move to Trash.
2. Open **System Settings > Privacy & Security** and scroll down to **Security**.
3. Next to *"Less3" was blocked to protect your Mac*, click **Open Anyway**. The button only appears for about an
   hour after the blocked attempt; if it is missing, try to open Less3 again first.
4. Click **Open Anyway** again in the dialog that follows and enter your password. You only do this once.

Or skip the prompt in Terminal:

```bash
xattr -dr com.apple.quarantine ~/Downloads/less3-{version}-osx-arm64.pkg   # before opening the .pkg
xattr -dr com.apple.quarantine /Applications/Less3.app                     # after copying the app from the .dmg
xattr -dr com.apple.quarantine "/Applications/Less3 Dashboard.app"         # the same for the dashboard
sudo installer -pkg ~/Downloads/less3-{version}-osx-arm64.pkg -target /    # installs the .pkg with no prompt at all
```

On macOS 15 and later, Control-click > **Open** no longer bypasses this prompt.

### Windows: "Windows protected your PC"

Click **More info**, then **Run anyway**. If your browser warns that the file *isn't commonly downloaded*, choose
**Keep** (in Edge: the **...** menu on the download, **Keep**, then **Show more > Keep anyway**). For the `.zip`, run
`Unblock-File .\less3-{version}-win-x64.zip` in PowerShell (or right-click > **Properties** > **Unblock**) before
extracting it.

### Linux

No prompt. `sudo apt install ./less3_{version}_amd64.deb` (the `./` matters), `sudo dnf install
./less3-{version}-1.x86_64.rpm`, or `chmod +x` the AppImage and run it from a terminal. The `.deb` and `.rpm` also
install a `less3` (or `less3-dashboard`) systemd service, which is not enabled until you turn it on (see INSTALL.md).

### Which file to download

| Platform | Less3 (server) | Less3 Dashboard |
| --- | --- | --- |
| Windows (x64) | `less3-{version}-win-x64-setup.exe` | `less3-dashboard-{version}-win-x64-setup.exe` |
| Windows (ARM64) | `less3-{version}-win-arm64-setup.exe` | `less3-dashboard-{version}-win-arm64-setup.exe` |
| macOS (Apple Silicon) | `less3-{version}-osx-arm64.pkg` | `less3-dashboard-{version}-osx-arm64.pkg` |
| macOS (Intel) | `less3-{version}-osx-x64.pkg` | `less3-dashboard-{version}-osx-x64.pkg` |
| Debian / Ubuntu | `less3_{version}_amd64.deb` (or `_arm64.deb`) | `less3-dashboard_{version}_amd64.deb` (or `_arm64.deb`) |
| Fedora / RHEL | `less3-{version}-1.x86_64.rpm` (or `.aarch64.rpm`) | `less3-dashboard-{version}-1.x86_64.rpm` (or `.aarch64.rpm`) |
| Any Linux | `less3-{version}-x86_64.AppImage` (or `-aarch64.AppImage`) | `less3-dashboard-{version}-x86_64.AppImage` (or `-aarch64.AppImage`) |
| Scripts and containers | `less3-{version}-<rid>.tar.gz` / `.zip` | `less3-dashboard-{version}-<rid>.tar.gz` / `.zip` |

The `.dmg` files are drag-to-Applications copies of the same apps; prefer the `.pkg`, which also puts the command on
your PATH.
For the multi-node cluster, use the Docker stack instead (see the README).

Not sure which Mac you have? Apple menu > **About This Mac**: "Chip: Apple M..." is Apple Silicon, "Processor:
Intel" is Intel.

Full instructions, including uninstalling and running as a service: [INSTALL.md](https://github.com/jchristn/Less3/blob/main/INSTALL.md).
To build from source, use the **Source code** archives below and follow
[the README's Initial Setup section](https://github.com/jchristn/Less3/blob/main/README.md#initial-setup).
