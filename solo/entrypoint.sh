#!/usr/bin/env bash
# Hiero Solo bootstrap entrypoint
# Based on a proven Solo setup script, adapted for Docker container use
# with Solo 0.68.0 command syntax.
set -uo pipefail

# Configuration
SOLO_NAMESPACE="${SOLO_NAMESPACE:-solo}"
SOLO_CLUSTER_NAME="${SOLO_CLUSTER_NAME:-${SOLO_NAMESPACE}}"
SOLO_CLUSTER_SETUP_NAMESPACE="${SOLO_CLUSTER_SETUP_NAMESPACE:-${SOLO_NAMESPACE}-cluster}"
SOLO_DEPLOYMENT="${SOLO_DEPLOYMENT:-${SOLO_NAMESPACE}-deployment}"
MIRROR_NODE_VERSION="${MIRROR_NODE_VERSION:-v0.133.0}"
OUTPUT_DIR="${OUTPUT_DIR:-/output}"

# Well-known test account (ED25519)
TEST_ACCOUNT_KEY="302e020100300506032b6570042204208ea963ae7d7019a02ce581fac98a32ec04ac0b1bba4cfee24a6243dd1b098574"
TEST_ACCOUNT_HBAR="1000000"

# Exposed ports (host-side, via port-forward with --network host)
CONSENSUS_PORT="${CONSENSUS_PORT:-8211}"
MIRROR_REST_PORT="${MIRROR_REST_PORT:-8551}"
MIRROR_GRPC_PORT="${MIRROR_GRPC_PORT:-8600}"
CONSENSUS_NODE_ID="0.0.3"

# ---------------------------------------------------------------------------
# Cleanup on signal (docker compose down sends SIGTERM)
# ---------------------------------------------------------------------------
cleanup() {
    echo ""
    echo "=== Shutting down Solo network ==="
    kill $(jobs -p) 2>/dev/null || true
    kind delete clusters --all 2>/dev/null || true
    echo "=== Cleanup complete ==="
}
trap cleanup SIGTERM SIGINT

# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------
echo "=== Hiero Solo Bootstrap ==="

if [ ! -S /var/run/docker.sock ]; then
    echo "ERROR: /var/run/docker.sock not mounted."
    exit 1
fi

docker info > /dev/null 2>&1 || {
    echo "ERROR: Cannot connect to Docker daemon."
    exit 1
}

echo "Docker OK."

# ---------------------------------------------------------------------------
# Clean up leftover Kind cluster from a previous run
# ---------------------------------------------------------------------------
if docker ps -a --format '{{.Names}}' | grep -q "solo.*control-plane\|${SOLO_CLUSTER_NAME}.*control-plane"; then
    echo "Found leftover Kind cluster, removing..."
    kind delete clusters --all 2>/dev/null || true
    sleep 2
fi

# Check for existing deployment namespace
if kubectl get namespace "${SOLO_DEPLOYMENT}" &>/dev/null 2>&1; then
    echo "Found existing namespace ${SOLO_DEPLOYMENT}, cleaning up..."
    kind delete clusters --all 2>/dev/null || true
    sleep 2
fi

# ---------------------------------------------------------------------------
# Create Kind cluster
# ---------------------------------------------------------------------------
echo ""
echo "=== Creating Kind cluster: ${SOLO_CLUSTER_NAME} ==="
kind create cluster -n "${SOLO_CLUSTER_NAME}"

# ---------------------------------------------------------------------------
# Initialize Solo
# ---------------------------------------------------------------------------
echo ""
echo "=== Initializing Solo ==="
solo init

# Connect to the cluster
solo cluster-ref config connect \
    --cluster-ref "kind-${SOLO_CLUSTER_NAME}" \
    --context "kind-${SOLO_CLUSTER_NAME}"
# Create the deployment
solo deployment config create \
    -n "${SOLO_NAMESPACE}" \
    --deployment "${SOLO_DEPLOYMENT}"
# Add cluster to deployment
solo deployment cluster attach \
    --deployment "${SOLO_DEPLOYMENT}" \
    --cluster-ref "kind-${SOLO_CLUSTER_NAME}" \
    --num-consensus-nodes 1
# Generate keys
solo keys consensus generate \
    --gossip-keys --tls-keys \
    --deployment "${SOLO_DEPLOYMENT}"
# Setup cluster
solo cluster-ref config setup \
    -s "${SOLO_CLUSTER_SETUP_NAMESPACE}"
# ---------------------------------------------------------------------------
# Deploy network with custom application.properties (throttle exemption)
# ---------------------------------------------------------------------------
echo ""
echo "=== Deploying consensus network (with throttle exemption) ==="
solo consensus network deploy \
    --deployment "${SOLO_DEPLOYMENT}" \
    --application-properties /config/application.properties
# Setup and start consensus node
solo consensus node setup \
    --deployment "${SOLO_DEPLOYMENT}"
solo consensus node start \
    --deployment "${SOLO_DEPLOYMENT}"
# ---------------------------------------------------------------------------
# Deploy mirror node
# ---------------------------------------------------------------------------
echo ""
echo "=== Deploying mirror node ==="
solo mirror node add \
    --deployment "${SOLO_DEPLOYMENT}" \
    --cluster-ref "kind-${SOLO_CLUSTER_NAME}" \
    --mirror-node-version "${MIRROR_NODE_VERSION}" \
    --pinger || {
    echo ""
    echo "WARNING: Mirror node deploy returned an error (likely REST Java)."
    echo "Checking if essential services are running..."
}

# ---------------------------------------------------------------------------
# Verify essential pods
# ---------------------------------------------------------------------------
echo ""
echo "=== Checking essential services ==="
SOLO_NS=$(kubectl get namespaces -o jsonpath='{.items[*].metadata.name}' 2>/dev/null \
    | tr ' ' '\n' | grep -E "^${SOLO_NAMESPACE}$|^${SOLO_DEPLOYMENT}$" | head -1) || true
if [ -z "${SOLO_NS}" ]; then
    SOLO_NS=$(kubectl get namespaces -o jsonpath='{.items[*].metadata.name}' 2>/dev/null \
        | tr ' ' '\n' | grep -v "kube-\|default\|local-path\|solo-setup\|${SOLO_CLUSTER_SETUP_NAMESPACE}" | head -1) || true
fi
echo "Using namespace: ${SOLO_NS}"

ESSENTIAL_OK=true
for POD_PATTERN in "network-node" "mirror.*rest" "mirror.*grpc" "mirror.*importer"; do
    if kubectl get pods -n "${SOLO_NS}" --no-headers 2>/dev/null \
        | grep -E "${POD_PATTERN}" | grep -q "Running"; then
        echo "  ✔ ${POD_PATTERN}"
    else
        echo "  ✖ ${POD_PATTERN} — not running"
        ESSENTIAL_OK=false
    fi
done

if [ "${ESSENTIAL_OK}" = false ]; then
    echo ""
    echo "ERROR: Essential services are not running."
    kubectl get pods -n "${SOLO_NS}" --no-headers 2>/dev/null || true
    echo "Container staying alive for debugging. Stop with: docker compose down"
    while true; do sleep 30; done
fi

# ---------------------------------------------------------------------------
# Deploy custom HAProxy for mirror REST (bypasses rest-java dependency)
# ---------------------------------------------------------------------------
echo ""
echo "=== Setting up mirror REST HAProxy ==="
helm repo add haproxytech https://haproxytech.github.io/helm-charts >/dev/null 2>&1
helm upgrade --install mirror-rest haproxytech/haproxy \
    --version 1.24.0 \
    --namespace "${SOLO_NS}" \
    --values /config/haproxy-values.yaml \
    >/dev/null 2>&1

# Wait for all pods to be ready (matches original proven script)
echo "Waiting for all pods to be ready..."
if ! kubectl wait --for=condition=ready pod --all -n "${SOLO_NS}" --timeout=120s 2>/dev/null; then
    echo "WARNING: Some pods did not become ready. Status:"
    kubectl get pods -n "${SOLO_NS}" --no-headers 2>/dev/null || true
fi

# ---------------------------------------------------------------------------
# Create well-known test account
# ---------------------------------------------------------------------------
echo ""
echo "=== Creating test account ==="
ACCOUNT_OUTPUT=$(solo ledger account create \
    --deployment "${SOLO_DEPLOYMENT}" \
    --hbar-amount "${TEST_ACCOUNT_HBAR}" \
    --ed25519-private-key "${TEST_ACCOUNT_KEY}" \
    2>&1) || {
    echo "WARNING: Account creation returned an error."
    echo "${ACCOUNT_OUTPUT}"
}

ACCOUNT_ID=$(echo "${ACCOUNT_OUTPUT}" | grep -oP '0\.0\.\d+' | head -1)
if [ -z "${ACCOUNT_ID}" ]; then
    echo "WARNING: Could not parse account ID. Output:"
    echo "${ACCOUNT_OUTPUT}"
    # Try to find the account from secrets
    ACCOUNT_ID=$(kubectl get secrets -n "${SOLO_NS}" --no-headers 2>/dev/null \
        | awk '{print $1}' | grep "^account-key-0" | head -1 | sed 's/account-key-//') || true
fi

if [ -z "${ACCOUNT_ID}" ]; then
    echo "WARNING: Falling back to genesis account 0.0.2"
    ACCOUNT_ID="0.0.2"
    TEST_ACCOUNT_KEY="302e020100300506032b65700422042091132178e72057a1d7528025956fe39b0b847f200ab59b2fdd367017f3087137"
fi

echo "Test account: ${ACCOUNT_ID}"

# ---------------------------------------------------------------------------
# Setup port-forwarding
# ---------------------------------------------------------------------------
echo ""
echo "=== Setting up port-forwards ==="

# Consensus via HAProxy
echo "  Consensus: localhost:${CONSENSUS_PORT}"
kubectl port-forward svc/haproxy-node1-svc -n "${SOLO_NS}" \
    "${CONSENSUS_PORT}:50211" --address 0.0.0.0 >/dev/null 2>&1 &

# Mirror REST via our custom HAProxy (not the built-in api-proxy)
echo "  Mirror REST: localhost:${MIRROR_REST_PORT}"
kubectl port-forward svc/mirror-rest-haproxy -n "${SOLO_NS}" \
    "${MIRROR_REST_PORT}:80" --address 0.0.0.0 >/dev/null 2>&1 &

# Mirror gRPC
echo "  Mirror gRPC: localhost:${MIRROR_GRPC_PORT}"
kubectl port-forward svc/mirror-1-grpc -n "${SOLO_NS}" \
    "${MIRROR_GRPC_PORT}:5600" --address 0.0.0.0 >/dev/null 2>&1 &

sleep 3

# ---------------------------------------------------------------------------
# Wait for mirror REST to be reachable
# ---------------------------------------------------------------------------
echo ""
echo "=== Waiting for mirror REST API ==="
MIRROR_REST_URL="http://localhost:${MIRROR_REST_PORT}"
MAX_WAIT=180
ELAPSED=0
until curl -sf "${MIRROR_REST_URL}/api/v1/transactions?limit=1" > /dev/null 2>&1; do
    if [ "${ELAPSED}" -ge "${MAX_WAIT}" ]; then
        echo "WARNING: Mirror REST not reachable within ${MAX_WAIT}s"
        echo "Container staying alive — port-forwards may need more time."
        break
    fi
    sleep 5
    ELAPSED=$((ELAPSED + 5))
    echo "  Waiting... (${ELAPSED}s)"
done
if [ "${ELAPSED}" -lt "${MAX_WAIT}" ]; then
    echo "Mirror REST is ready at ${MIRROR_REST_URL}"
fi

# ---------------------------------------------------------------------------
# Write environment file
# ---------------------------------------------------------------------------
mkdir -p "${OUTPUT_DIR}"
cat > "${OUTPUT_DIR}/solo.env" <<EOF
MirrorRestUrl=http://localhost:${MIRROR_REST_PORT}
MirrorGrpcUrl=http://localhost:${MIRROR_GRPC_PORT}
PayerPrivateKey=${TEST_ACCOUNT_KEY}
ConsensusEndpoint=http://localhost:${CONSENSUS_PORT}
ConsensusNodeId=${CONSENSUS_NODE_ID}
PayerAccountId=${ACCOUNT_ID}
EOF

echo ""
echo "========================================="
echo "  SOLO READY"
echo "========================================="
echo ""
echo "Run: source ./scripts/solo-env.sh"
echo "Then: dotnet test --project test/Hiero.Test.Integration/"
echo ""
echo "========================================="

# ---------------------------------------------------------------------------
# Keep alive with port-forward health monitoring
# ---------------------------------------------------------------------------
echo "Container alive. Stop with: docker compose down"
while true; do
    # Restart mirror REST port-forward if it dies
    if ! curl -sf "http://localhost:${MIRROR_REST_PORT}/api/v1/transactions?limit=1" > /dev/null 2>&1; then
        echo "$(date): Mirror REST port-forward died, restarting..."
        kubectl port-forward svc/mirror-rest-haproxy -n "${SOLO_NS}" \
            "${MIRROR_REST_PORT}:80" --address 0.0.0.0 >/dev/null 2>&1 &
    fi
    sleep 30
done
