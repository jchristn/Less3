#!/bin/bash
#
# reset.sh - Reset the Less3 Docker environment to factory defaults.
#
# The Docker deployment runs on PostgreSQL, with metadata in the 'pgdata'
# volume and object data in the 'less3-data' volume. A factory reset drops
# those volumes (wiping the database and object storage) and clears the
# host-mounted logs. On the next 'docker compose up' the nodes recreate their
# schema and re-seed the default tenant, credentials, and sample bucket
# automatically - no database file to restore.
#
# Usage: ./factory/reset.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DOCKER_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

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
echo "  - The PostgreSQL data volume (all metadata, lock state,"
echo "    cluster membership, buckets, users, credentials, ACLs)"
echo "  - The shared object-storage volume (all object data and"
echo "    multipart parts)"
echo "  - All host log files"
echo ""
echo "Configuration files (system.node.json, clutch/clutch.json)"
echo "are NOT modified. The nodes re-seed the default data on the"
echo "next startup."
echo ""
read -r -p "Type 'RESET' to confirm: " CONFIRM
echo ""

if [ "$CONFIRM" != "RESET" ]; then
  echo "Aborted. No changes were made."
  exit 1
fi

# -------------------------------------------------------------------------
# Stop containers and drop the data volumes (covers the optional clutch
# profile too). This removes the 'pgdata' and 'less3-data' named volumes.
# -------------------------------------------------------------------------
echo "[1/2] Stopping containers and removing data volumes..."
cd "$DOCKER_DIR"
docker compose down -v --remove-orphans 2>/dev/null || true

# -------------------------------------------------------------------------
# Clear host logs
# -------------------------------------------------------------------------
echo "[2/2] Clearing logs..."
mkdir -p "$DOCKER_DIR/logs"
rm -f "$DOCKER_DIR/logs/"* 2>/dev/null || true
touch "$DOCKER_DIR/logs/.gitkeep"

echo ""
echo "Factory reset complete."
echo ""
echo "To start fresh (the nodes will recreate the schema and seed defaults):"
echo "  cd $DOCKER_DIR"
echo "  docker compose up -d"
echo ""
