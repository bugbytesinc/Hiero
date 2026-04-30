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

CONSENSUS_PORT="${ConsensusEndpoint##*:}"
MIRROR_REST_PORT="${MirrorRestUrl##*:}"
MIRROR_GRPC_PORT="${MirrorGrpcUrl##*:}"

echo ""
echo "To tunnel from another machine (ports are IPv4-only, use 127.0.0.1 not localhost):"
echo "  ssh -L ${CONSENSUS_PORT}:127.0.0.1:${CONSENSUS_PORT} -L ${MIRROR_REST_PORT}:127.0.0.1:${MIRROR_REST_PORT} -L ${MIRROR_GRPC_PORT}:127.0.0.1:${MIRROR_GRPC_PORT} $(hostname)"
echo ""
echo "User Secrets Configuration:"
echo "{"
echo "  \"MirrorRestUrl\": \"http://localhost:${MIRROR_REST_PORT}\","
echo "  \"MirrorGrpcUrl\": \"http://localhost:${MIRROR_GRPC_PORT}\","
echo "  \"PayerPrivateKey\": \"${PayerPrivateKey}\","
echo "  \"ConsensusEndpoint\": \"http://localhost:${CONSENSUS_PORT}\","
echo "  \"ConsensusNodeId\": \"${ConsensusNodeId}\","
echo "  \"PayerAccountId\": \"${PayerAccountId}\","
echo "}"



echo ""
echo "Environment set. Run: dotnet test --project test/Hiero.Test.Integration/"
