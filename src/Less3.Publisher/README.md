# Less3.Publisher

Builds the Less3 installers and package-manager manifests from [`publisher.json`](../../publisher.json), following
`~/Code/Agents/requirements/INSTALLERS.md`. It is the only place packaging logic lives: `build-installers.sh`,
`build-installers.bat`, and [`.github/workflows/release.yml`](../../.github/workflows/release.yml) all run it.

```bash
dotnet run --project src/Less3.Publisher -c Release -- --channel installers --sums
```

| Option | Meaning |
| --- | --- |
| `--channel <ids>` | Channels to run, comma-separated. `installers` means every installer channel this host builds. |
| `--rid <rid>` | Build one runtime instead of the channel's runtimes. |
| `--version <v>` | Override the version (default: `<Version>` in `src/Directory.Build.props`). |
| `--output <dir>` | Output folder (default: `installers/<version>`). |
| `--sums` | Write `SHA256SUMS` for the output folder. |
| `--keep-staging` | Keep the `.staging` scratch folder. |
| `--list` | List the channels. |
| `--print-version` | Print the version and exit. |

## Channels

| Channel | Host | Produces |
| --- | --- | --- |
| `inno` | Windows (Inno Setup 6) | `<name>-<v>-win-<arch>-setup.exe`: license page, logo, tagline, `C:\Program Files\Less3` on the system PATH, and a **Less3** Start menu entry |
| `macos` | macOS | `less3-<v>-osx-<arch>.pkg` (license, welcome page, `Less3.app`, `/usr/local/bin/less3`) and `.dmg` (the app, an Applications link, and the Gatekeeper note) |
| `debrpm` | Linux (fpm, rpm) | `less3_<v>_<arch>.deb`, `less3-<v>-1.<arch>.rpm`: `/opt/less3`, `/usr/bin/less3`, a menu entry with icons, and the `less3` systemd service (installed, not enabled) |
| `appimage` | Linux (appimagetool) | `less3-<v>-<arch>.AppImage` |
| `archive` | any (its own OS) | `less3-<v>-<rid>.zip` or `.tar.gz` with the bare executable and the license |
| `winget`, `scoop`, `choco`, `brew` | any | Manifests referencing the built installers or archives by URL and SHA-256 |
| `aptyum` | Linux | apt and yum repository trees for GitHub Pages |
| `snap`, `flatpak` | | Declared but not implemented |

`publisher.json` declares two artifacts, and every installer channel packages each into its own files under its own
name and identifiers (`project` settings, overridden per artifact by `name`, `displayName`, `bundleId`, `packageId`,
`tagline`, `description`, and `windowsAppId`):

| Artifact | Files | Payload per runtime |
| --- | --- | --- |
| `server` (Less3) | `less3-...` | A self-contained, single-file `dotnet publish` of `src/Less3/Less3.csproj` with the assembly renamed to `less3`, so the command is `less3` (`less3.exe`) everywhere. The developer scripts and assets the project copies to its output (`clean.bat`, `clean.sh`, `Assets/`) are left out. |
| `dashboard` (Less3 Dashboard) | `less3-dashboard-...` | The `less3-dashboard` launcher (`src/Less3.Dashboard`, single-file and trimmed), `app/` (the dashboard built once per run with `LESS3_DASHBOARD_STANDALONE=1`, which selects Next.js's standalone server), and `node/` (the official Node.js runtime for the target, downloaded from nodejs.org and checked against its `SHASUMS256.txt`). |

The dashboard build is the same on every runtime, so it must not contain native Node modules: the Publisher deletes
`nextApp.removeFromApp` (sharp's image library, which the standalone build traces but does not use with unoptimized
images) and fails if any `*.node` file remains. In a macOS app bundle the `app/` and `node/` folders go in
`Contents/Resources`, where the launcher looks for them; the bundled `node` keeps the Node.js project's own signature.
A channel refuses to run on the wrong host and names the tools it is missing with the command that installs each.

Less3 is a server that keeps `system.json`, its database, and its objects in its working directory, so no installer
runs it from the install folder. The **Less3** menu entry (a `.cmd` on Windows, a `.command` opened in Terminal by the
macOS app, and a shell script on Linux) creates `~/Less3` and runs the server there. The systemd unit runs it as the
`less3` account in `/var/lib/less3` and only starts once `system.json` exists there. The **Less3 Dashboard** entry
runs `less3-dashboard --open`, and its unit runs it as the `less3-dashboard` account on `127.0.0.1:3000`.

Every installer's artwork is generated from `project.logo` (`assets/logo-1024.png`): the Windows `.ico` and wizard
bitmaps, the macOS `.icns` and installer background, and the Linux hicolor icons.

The package-manager channels read installers another channel already built, so run them after downloading the
installer artifacts into the output folder. In CI they run only on a tag when the repository variable
`ENABLE_PACKAGE_MANAGERS` is `true`.

## Building a release without CI

When GitHub Actions is unavailable, every asset can still be built from one Mac with Docker:

- **macOS:** `./build-installers.sh`.
- **Windows:** the `inno` channel normally runs only on Windows. Set `LESS3_ISCC` to `src/Less3.Publisher/iscc-docker.sh`, which runs Inno Setup's compiler under Wine in the `amake/innosetup` image; the script's paths are then written as Wine `Z:` paths. Build the `.zip` archives with `--channel archive --rid win-x64` (and `win-arm64`).
- **Linux:** run the Publisher with `--channel debrpm,appimage,archive` in a `mcr.microsoft.com/dotnet/sdk:10.0` container with `fpm`, `rpm`, and `appimagetool` installed. Copy the repository into the container's own filesystem first; MSBuild stalls on a macOS bind mount.

Put every file in one folder, run `--sums --output <folder>` (with no channel) to write `SHA256SUMS`, and create the release from the tagged commit.

## Signing

Less3 is not code-signed, by policy. Every target in `publisher.json`'s `signing` block has `isConfigured: false`:
Windows files stay unsigned and macOS bundles and executables are ad-hoc signed (`codesign --sign -`), which Apple
silicon needs to run them and which keeps Gatekeeper from reporting a download as damaged. Setting `isConfigured` and
providing the named secrets turns real signing on without code changes.

## Tests

[`test/Test.Publisher`](../../test/Test.Publisher) checks the committed manifest, the single version source, asset
names, every rendered installer file, the generated artwork, and option parsing:

```bash
dotnet test test/Test.Publisher
```
