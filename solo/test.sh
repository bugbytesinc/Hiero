#!/usr/bin/env bash
# Run integration tests against a running Solo network.
# Solo must already be running (via ./solo/up.sh).
# Config is passed as env vars scoped to the dotnet process only —
# does not pollute your shell or interfere with user secrets.
# Usage: ./solo/test.sh
set -euo pipefail

SOLO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SOLO_DIR}/.." && pwd)"
TEST_EXIT_CODE=0

cd "${SOLO_DIR}"

# ---- Read Solo config from running container ----
echo "=== Reading Solo configuration ==="
ENV_FILE=$(docker compose exec -T solo cat /output/solo.env 2>/dev/null) || {
    echo "ERROR: Could not read solo.env. Is Solo running? Start it with: ./solo/up.sh"
    exit 1
}

# Build env var prefix for the subprocess
ENV_PREFIX=""
while IFS='=' read -r KEY VALUE; do
    [ -z "${KEY}" ] && continue
    ENV_PREFIX="${ENV_PREFIX} ${KEY}=${VALUE}"
    echo "  ${KEY}=${VALUE}"
done <<< "${ENV_FILE}"

cd "${REPO_ROOT}"

RESULTS_DIR="${REPO_ROOT}/test-results"
mkdir -p "${RESULTS_DIR}"

echo ""
echo "=== Running integration tests ==="
echo "Results will be saved to: ${RESULTS_DIR}"
echo ""

# ---- Run tests with Solo config as process-scoped env vars ----
# These override user secrets (env vars > user secrets in config priority)
# but don't persist in your shell session.
eval env ${ENV_PREFIX} dotnet test --project test/Hiero.Test.Integration/ "$@" \
    2>&1 | tee "${RESULTS_DIR}/integration-output.log" \
    || TEST_EXIT_CODE=$?

echo ""
if [ "${TEST_EXIT_CODE}" -eq 0 ]; then
    echo "=== Tests passed ==="
else
    echo "=== Tests failed (exit code: ${TEST_EXIT_CODE}) ==="
    echo "Full output saved to: ${RESULTS_DIR}/integration-output.log"
fi

exit "${TEST_EXIT_CODE}"
