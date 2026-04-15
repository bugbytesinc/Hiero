#!/usr/bin/env bash
# Read Solo environment variables from the running container and export them.
# Usage: source ./solo/env.sh
#
# This script is meant to be SOURCED, not executed, so that the exports
# persist in your shell. You can re-source it any time Solo is running.

SOLO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "${SOLO_DIR}"

ENV_FILE=$(docker compose exec -T solo cat /output/solo.env 2>/dev/null) || {
    echo "ERROR: Could not read solo.env. Is the Solo container running?"
    echo "Start it with: ./solo/up.sh"
    return 1 2>/dev/null || exit 1
}

while IFS='=' read -r KEY VALUE; do
    [ -z "${KEY}" ] && continue
    export "${KEY}=${VALUE}"
    echo "  export ${KEY}=${VALUE}"
done <<< "${ENV_FILE}"

echo ""
echo "Environment set. Run: dotnet test --project test/Hiero.Test.Integration/"
