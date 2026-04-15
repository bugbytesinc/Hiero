#!/usr/bin/env bash
# Stop the Solo local Hiero network and clean up.
# Usage: ./solo/down.sh
set -euo pipefail

SOLO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "${SOLO_DIR}"

echo "=== Stopping Solo container ==="
docker compose down --timeout 60 2>/dev/null || true

# Clean up orphaned Kind containers
ORPHANS=$(docker ps -a --format '{{.Names}}' | grep "solo.*control-plane" || true)
if [ -n "${ORPHANS}" ]; then
    echo "=== Cleaning up orphaned Kind containers ==="
    echo "${ORPHANS}" | xargs docker rm -f 2>/dev/null || true
fi

docker network rm kind 2>/dev/null || true

echo "=== Solo stopped ==="
