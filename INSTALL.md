# Installing Less3

Download the installer for your machine from the [latest release](https://github.com/jchristn/Less3/releases/latest).
Every installer is self-contained, so nothing else (not even .NET) needs to be installed. Each one adds:

- the `less3` command, on your PATH, which runs the Less3 server in the current folder
- a **Less3** menu entry (Start menu, Applications, or the application menu) that runs the server in a terminal
  window in `~/Less3` (`%USERPROFILE%\Less3` on Windows)
- on Linux (`.deb` and `.rpm`), a `less3` systemd service, installed but not enabled

Less3 keeps its configuration (`system.json`), its SQLite database, and the objects you store in the folder it runs
in. The first run in a new folder asks a few setup questions and writes `system.json` there; see the README's
[configuration section](README.md#key-configuration-settings-systemjson) for what it holds. The installers are for a
standalone single node. For the multi-node cluster, use the Docker stack ([MULTINODE_SETUP.md](MULTINODE_SETUP.md));
to build from source, see the README's [Initial Setup](README.md#initial-setup).

> **Less3 is free, open-source software and is not code-signed** (signing certificates are an ongoing cost we choose
> not to pass on). Your operating system may warn that the developer is unidentified. That warning doesn't mean the
> download is damaged; to confirm a file is the one published, check it against `SHA256SUMS`.

## Which file to download

| Platform | File |
| --- | --- |
| Windows (x64) | `less3-<version>-win-x64-setup.exe` |
| Windows (ARM64) | `less3-<version>-win-arm64-setup.exe` |
| macOS (Apple Silicon) | `less3-<version>-osx-arm64.pkg` |
| macOS (Intel) | `less3-<version>-osx-x64.pkg` |
| Debian / Ubuntu | `less3_<version>_amd64.deb` (or `_arm64.deb`) |
| Fedora / RHEL | `less3-<version>-1.x86_64.rpm` (or `.aarch64.rpm`) |
| Any Linux | `less3-<version>-x86_64.AppImage` (or `-aarch64.AppImage`) |
| Scripts and containers | `less3-<version>-<rid>.tar.gz` (macOS and Linux) or `.zip` (Windows) |

Not sure which Mac you have? Apple menu > **About This Mac**: "Chip: Apple M..." is Apple Silicon, "Processor: Intel"
is Intel.

After installing, open a new terminal, make a folder for your data, and run `less3` there:

```bash
mkdir ~/less3-data && cd ~/less3-data
less3
```

The server listens on `http://localhost:8000` with access key `default` and secret key `default` until you change
`system.json`. Type `q` at the `Command (? for help) >` prompt to stop it.

## Windows

Run `less3-<version>-win-x64-setup.exe` (or the ARM64 one). It shows the license, installs to
`C:\Program Files\Less3`, adds that folder to the system PATH, and adds **Less3** to the Start menu. **Less3** opens a
console window running the server in `%USERPROFILE%\Less3`.

### "Windows protected your PC"

Windows warns in two places. The browser comes first: Chrome and Edge may say the file *isn't commonly downloaded*.
In Chrome choose **Keep**; in Edge open the **...** menu on the download, choose **Keep**, then **Show more > Keep
anyway**. Then, when the installer runs, SmartScreen shows:

> **Windows protected your PC**
> Microsoft Defender SmartScreen prevented an unrecognized app from starting.

Click **More info**, then **Run anyway**. The publisher shows as Unknown publisher.

For the portable `.zip`, unblock it before extracting, or the extracted `less3.exe` gets the same prompt (or fails
silently from a terminal): right-click > **Properties** > **Unblock**, or in PowerShell:

```powershell
Unblock-File .\less3-<version>-win-x64.zip
```

If Microsoft Defender (not SmartScreen) quarantines the file as malware, that is a false positive that self-contained
single-file executables sometimes trigger. Submit it at <https://www.microsoft.com/en-us/wdsi/filesubmission> as a
software developer; reclassification usually takes a day or two. Don't turn Defender off.

Listening on a wildcard hostname (`*`, `+`, or `0.0.0.0` in `Webserver.Hostname`) needs an administrator console on
Windows.

### Uninstall

**Settings > Apps > Installed apps > Less3 > Uninstall**. It removes the program and the PATH entry, and leaves the
folders Less3 ran in (including `%USERPROFILE%\Less3`) alone.

## macOS

Open `less3-<version>-osx-arm64.pkg` (or the Intel one). The installer shows the license, installs `Less3.app` in
Applications, and links `less3` into `/usr/local/bin`. Opening **Less3** from Applications or Launchpad opens a
Terminal window running the server in `~/Less3`. The `.dmg` holds the same app for a drag-to-Applications install,
but it cannot put `less3` on your PATH, so prefer the `.pkg`.

### "Less3" Not Opened

On macOS 15 (Sequoia) and later, opening the downloaded `.pkg` or app shows:

> **"Less3" Not Opened**
> Apple could not verify "Less3" is free of malware that may harm your Mac or compromise your privacy.

Older versions say *"Less3" cannot be opened because it is from an unidentified developer*. To open it anyway:

1. Click **Done**. Do not click Move to Trash.
2. Open **System Settings > Privacy & Security** and scroll down to **Security**.
3. Next to *"Less3" was blocked to protect your Mac*, click **Open Anyway**. The button only appears for about an
   hour after the blocked attempt; if it is missing, try to open Less3 again first.
4. Click **Open Anyway** again in the dialog that follows and enter your password. macOS remembers the choice, so you
   only do this once.

Control-click > **Open** used to bypass this prompt and no longer does on macOS 15 and later.

Terminal users can skip the prompt by removing the quarantine attribute the browser set on the download:

```bash
xattr -dr com.apple.quarantine ~/Downloads/less3-<version>-osx-arm64.pkg   # the .pkg, before opening it
xattr -dr com.apple.quarantine /Applications/Less3.app                      # the app, after copying it from the .dmg
xattr -d  com.apple.quarantine ./less3                                      # a bare binary from the .tar.gz
sudo installer -pkg ~/Downloads/less3-<version>-osx-arm64.pkg -target /    # installs the .pkg with no prompt at all
```

A file fetched with `curl` never gets the quarantine attribute, so `curl -L ... | tar xz` has no prompt.

### Uninstall

```bash
sudo "/Applications/Less3.app/Contents/Resources/uninstall.sh"
```

It removes the app, `/usr/local/bin/less3`, and the installer receipt. The folders Less3 ran in (including
`~/Less3`) are kept.

## Linux

```bash
sudo apt install ./less3_<version>_amd64.deb          # Debian, Ubuntu (the ./ matters; without it apt searches its repositories)
sudo dnf install ./less3-<version>-1.x86_64.rpm        # Fedora, RHEL
```

Use `apt install` rather than `dpkg -i`, because only apt installs the declared dependencies (ICU and OpenSSL). The
packages install to `/opt/less3`, link `less3` into `/usr/bin`, add **Less3** to the application menu, and install a
`less3` systemd service with a `less3` system account and its working folder, `/var/lib/less3`.

### Running Less3 as a service

The service is installed but not enabled, because the server has to be configured once first. It starts only when
`/var/lib/less3/system.json` exists:

```bash
sudo -u less3 sh -c 'cd /var/lib/less3 && less3 setup'   # answer the setup questions, then type q to stop
sudo systemctl enable --now less3
systemctl status less3
journalctl -u less3 -f                                    # the server's console output
```

`system.json` starts with `Webserver.Hostname` set to `localhost`, so only this machine can reach the server. To serve
other machines, set it to a DNS name or `*` (see the README), then `sudo systemctl restart less3`. Upgrading the
package restarts a running service; removing it stops and disables the service. `/var/lib/less3` and the `less3`
account are kept either way.

### AppImage

The AppImage needs no install: `chmod +x less3-<version>-x86_64.AppImage`, then run it from a terminal in the folder
you want Less3 to use. It runs from inside the image, so it is not on your PATH and installs no service; older
distributions need `libfuse2` to mount it.

### Uninstall

```bash
sudo apt remove less3       # or: sudo dnf remove less3
```

The folders Less3 ran in, `/var/lib/less3`, and the `less3` account are kept. Remove them yourself if you no longer
need the data: `sudo rm -rf /var/lib/less3 && sudo userdel less3`.

## Portable archives

The `.tar.gz` and `.zip` archives hold the same self-contained `less3` executable as the installers, for scripts, CI
jobs, and container images that pin a version:

```bash
curl -fsSL https://github.com/jchristn/Less3/releases/download/v<version>/less3-<version>-linux-x64.tar.gz | tar xz
cd less3-<version>-linux-x64 && ./less3
```

## Checking a download

Every release has a `SHA256SUMS` file covering every asset. Download it into the same folder as the file and run:

```bash
shasum -a 256 -c SHA256SUMS --ignore-missing      # macOS
sha256sum -c SHA256SUMS --ignore-missing          # Linux
```

On Windows, compare the output of `Get-FileHash .\less3-<version>-win-x64-setup.exe` with the file's line in
`SHA256SUMS`.

## Package managers

`winget`, Chocolatey, Scoop, Homebrew, and apt/yum repositories are planned. They are wired into the release workflow
but not live yet; until a command is listed here, use the downloads above.
