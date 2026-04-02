#!/bin/bash
#
# reset.sh - Reset Less3 docker environment to factory defaults
#
# This script destroys all runtime data (database, object storage, logs)
# and restores the factory-default database. Configuration files are
# preserved.
#
# Usage: ./factory/reset.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
FACTORY_DIR="$SCRIPT_DIR"

# -------------------------------------------------------------------------
# Confirmation prompt
# -------------------------------------------------------------------------
echo ""
echo "=========================================================="
echo "  Less3 - Reset to Factory Defaults"
echo "=========================================================="
echo ""
echo "WARNING: This is a DESTRUCTIVE action. The following will"
echo "be permanently deleted:"
echo ""
echo "  - Less3 SQLite database (buckets, objects, users,"
echo "    credentials, ACLs, tags)"
echo "  - All object storage files"
echo "  - All temporary files"
echo "  - All log files"
echo ""
echo "Configuration files (system.json) will NOT be modified."
echo ""
read -r -p "Type 'RESET' to confirm: " CONFIRM
echo ""

if [ "$CONFIRM" != "RESET" ]; then
  echo "Aborted. No changes were made."
  exit 1
fi

# -------------------------------------------------------------------------
# Ensure containers are stopped
# -------------------------------------------------------------------------
echo "[1/5] Stopping containers..."
cd "$DOCKER_DIR"
docker compose down 2>/dev/null || true

# -------------------------------------------------------------------------
# Restore factory database
# -------------------------------------------------------------------------
echo "[2/5] Restoring factory database..."
rm -f "$DOCKER_DIR/less3.db"
rm -f "$DOCKER_DIR/less3.db-shm"
rm -f "$DOCKER_DIR/less3.db-wal"
cp "$FACTORY_DIR/less3.db" "$DOCKER_DIR/less3.db"
echo "        Restored less3.db"

# -------------------------------------------------------------------------
# Clear object storage
# -------------------------------------------------------------------------
echo "[3/5] Clearing object storage..."
rm -rf "$DOCKER_DIR/disk/"*
rm -rf "$DOCKER_DIR/temp/"*
echo "        Cleared object storage and temp files"

# -------------------------------------------------------------------------
# Clear logs
# -------------------------------------------------------------------------
echo "[4/5] Clearing logs..."
rm -f "$DOCKER_DIR/logs/"*
echo "        Cleared log files"

# -------------------------------------------------------------------------
# Done
# -------------------------------------------------------------------------
echo "[5/5] Factory reset complete."
echo ""
echo "To start the environment:"
echo "  cd $DOCKER_DIR"
echo "  docker compose up -d"
echo ""
