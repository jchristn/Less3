#!/usr/bin/env bash
if [ -z "$1" ]; then
  echo
  echo "Provide an argument specifying the version or tag."
  echo "Example: ./build-all.sh v4.1.0"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if ! "$SCRIPT_DIR/build-dashboard.sh" "$@"; then
  echo
  echo "Build failed."
  exit 1
fi

if ! "$SCRIPT_DIR/build-server.sh" "$@"; then
  echo
  echo "Build failed."
  exit 1
fi

echo
echo "Done"
