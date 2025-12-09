if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v2.1.12'
fi

echo Using image tag $IMG_TAG

docker run \
  -p 3000:3000 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  jchristn/less3-ui:$IMG_TAG
