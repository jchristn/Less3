#!/bin/sh
# Runs Inno Setup's compiler (ISCC) under Wine in Docker, so the Windows installers can be built on macOS or Linux
# when CI is unavailable. Point the Publisher at it:
#
#   LESS3_ISCC="$PWD/src/Less3.Publisher/iscc-docker.sh" dotnet src/Less3.Publisher/bin/Release/net10.0/less3-publish.dll --channel inno
#
# The Publisher calls it as: iscc-docker.sh [options] <script.iss>. The folder holding the Publisher's .staging tree
# is mounted at the same path in the container, so the Z: paths written into the script resolve. Image: amake/innosetup
# (override with LESS3_ISCC_IMAGE).
set -eu

[ $# -ge 1 ] || { echo "usage: iscc-docker.sh [options] <script.iss>" >&2; exit 2; }
image="${LESS3_ISCC_IMAGE:-amake/innosetup}"

options=""
script=""
for arg in "$@"; do
    script="$arg"
done
for arg in "$@"; do
    [ "$arg" = "$script" ] || options="$options $arg"
done

case "$script" in
    */.staging/*) root="${script%%/.staging/*}" ;;
    *) root="$(dirname "$script")" ;;
esac

# shellcheck disable=SC2086
exec docker run --rm --platform linux/amd64 -v "$root:$root" -w "$(dirname "$script")" "$image" $options "$(basename "$script")"
