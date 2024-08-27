if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v2.1.11'
fi

echo Using image tag $IMG_TAG

if [ ! -f "system.json" ]
then
  echo Configuration file system.json not found.
  exit
fi

# Items that require persistence
#   system.json
#   less3.db
#   logs/
#   temp/

# Argument order matters!

docker run \
  -p 8000:8000 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./system.json:/app/system.json \
  -v ./less3.db:/app/less3.db \
  -v ./logs/:/app/logs/ \
  -v ./temp/:/app/temp/ \
  jchristn/less3:$IMG_TAG

