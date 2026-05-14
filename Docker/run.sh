if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v2.2.0'
fi

echo Using image tag $IMG_TAG

mkdir -p ./db ./logs ./temp ./disk

# Items that require persistence
#   system.json
#   db/
#   logs/
#   temp/
#   disk/

# Argument order matters!

if [ -f "system.json" ]
then
  echo "Using mounted system.json from the Docker directory."
  docker run \
    -p 8000:8000 \
    -t \
    -i \
    -e "TERM=xterm-256color" \
    -v ./system.json:/app/system.json \
    -v ./db/:/app/db/ \
    -v ./logs/:/app/logs/ \
    -v ./temp/:/app/temp/ \
    -v ./disk/:/app/disk/ \
    jchristn77/less3:$IMG_TAG
else
  echo "system.json not found. Less3 will generate a default container configuration."
  docker run \
    -p 8000:8000 \
    -t \
    -i \
    -e "TERM=xterm-256color" \
    -v ./db/:/app/db/ \
    -v ./logs/:/app/logs/ \
    -v ./temp/:/app/temp/ \
    -v ./disk/:/app/disk/ \
    jchristn77/less3:$IMG_TAG
fi
