#!/usr/bin/env bash
# Start the Solo local Hiero network inside a Docker container.
# Prerequisites: Docker
# Usage: ./solo/up.sh
set -euo pipefail

SOLO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TIMEOUT="${SOLO_TIMEOUT:-1200}"

cd "${SOLO_DIR}"

echo "=== Building and starting Solo container ==="
docker compose up -d --build

echo ""
echo "=== Waiting for Solo to be ready (timeout: ${TIMEOUT}s) ==="
echo "First run takes ~10-15 min (image pulls). Subsequent runs are faster."
echo ""

ELAPSED=0
while true; do
    if docker compose logs solo 2>/dev/null | grep -q "SOLO READY"; then
        break
    fi

    STATUS=$(docker compose ps solo --format '{{.State}}' 2>/dev/null || echo "unknown")
    if [ "${STATUS}" = "exited" ] || [ "${STATUS}" = "dead" ]; then
        echo ""
        echo "ERROR: Solo container exited. Recent logs:"
        docker compose logs solo --tail 30
        exit 1
    fi

    if [ "${ELAPSED}" -ge "${TIMEOUT}" ]; then
        echo ""
        echo "ERROR: Solo did not become ready within ${TIMEOUT}s"
        docker compose logs solo --tail 30
        exit 1
    fi

    sleep 10
    ELAPSED=$((ELAPSED + 10))
    LAST_LINE=$(docker compose logs solo --tail 1 2>/dev/null | head -1)
    echo "  [${ELAPSED}s] ${LAST_LINE}"
done

echo ""
echo "========================================="
echo "  Solo is ready!"
echo "========================================="
echo ""
echo "Run: source ./solo/env.sh"
echo "Then: dotnet test --project test/Hiero.Test.Integration/"
echo ""
echo "To stop: ./solo/down.sh"
echo "========================================="
