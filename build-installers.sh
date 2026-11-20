#!/bin/sh
# Build every Less3 and Less3 Dashboard installer this machine's operating system can produce into installers/<version>/:
#   macOS  .pkg and .dmg (osx-x64, osx-arm64), and the portable .tar.gz archives
#   Linux  .deb, .rpm, and AppImage (linux-x64, linux-arm64), and the portable .tar.gz archives
# plus SHA256SUMS for the folder. Run build-installers.bat on Windows for the setup .exe and .zip files.
# All three operating systems build together in CI: push a v<version> tag (see .github/workflows/release.yml).
#
# Usage: ./build-installers.sh [version]
#   version   override the version (default: <Version> in src/Directory.Build.props)
set -eu

cd "$(dirname "$0")"

VERSION_ARGS=""
for arg in "$@"; do
    case "$arg" in
        -h|--help) sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        -*) echo "Unknown option: $arg" >&2; exit 2 ;;
        *) VERSION_ARGS="--version $arg" ;;
    esac
done

case "$(uname -s)" in
    Darwin|Linux) ;;
    *) echo "Use build-installers.bat on Windows." >&2; exit 1 ;;
esac

echo "Building the Less3 publisher..."
dotnet build src/Less3.Publisher/Less3.Publisher.csproj -c Release --nologo -v q
PUB="src/Less3.Publisher/bin/Release/net10.0/less3-publish.dll"

# shellcheck disable=SC2086
VERSION="$(dotnet "$PUB" --print-version $VERSION_ARGS)"
OUT="installers/$VERSION"

echo
echo "Version : $VERSION"
echo "Output  : $OUT"

# shellcheck disable=SC2086
dotnet "$PUB" --channel installers --sums --output "$OUT" $VERSION_ARGS

echo
echo "Installers are in $OUT. Install one, open a new terminal, and run: less3 (or less3-dashboard)"
