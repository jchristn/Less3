#!/bin/sh

# Replace the default server URL with the LESS3_SERVER_URL environment variable at runtime
if [ -n "$LESS3_SERVER_URL" ]; then
  find /app/.next -name "*.js" -exec sed -i "s|http://localhost:3000|${LESS3_SERVER_URL}|g" {} +
  echo "LESS3_SERVER_URL set to: $LESS3_SERVER_URL"
else
  echo "LESS3_SERVER_URL not set, using default: http://localhost:3000"
fi

exec npm run start
