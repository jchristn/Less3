#!/usr/bin/env bash
if [ -z "$1" ]; then
  echo
  echo "Provide a tag argument."
  echo "Example: ./build-dashboard.sh v4.1.0"
  exit 1
fi

cd "$(dirname "$0")" || exit 1

echo
echo "Building for linux/amd64 and linux/arm64/v8..."
docker buildx build -f dashboard/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag "jchristn77/less3-ui:$1" --tag jchristn77/less3-ui:latest --push dashboard || exit 1

echo
echo "Done"
