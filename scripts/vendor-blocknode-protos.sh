#!/usr/bin/env bash
# Vendors the protobuf trees under reference/ from pinned upstream releases.
#
#   reference/hedera-protobufs/   <- hiero-consensus-node (HAPI + block stream)
#   reference/hiero-block-node/   <- hiero-block-node (org.hiero.block.api)
#
# The hedera-protobufs tree is replaced wholesale from a single consensus-node
# ref so it is never internally mixed-version. The file set is computed, not
# hand-picked: the transitive import closure of (every proto currently in the
# tree) + (block/stream/**) at the pinned ref, minus CN_TRIMMED_PROTOS below.
# Files present locally but gone upstream are dropped with a warning.
#
# Imports upstream declares but never uses are then disabled from an explicit
# list (UNUSED_IMPORTS), and the result is checked with the same protoc the
# build uses: a stale entry, a newly unused import or a now-used one fails
# the run with the exact fix.
#
# Pins are recorded in the README.md written into each tree. See
# docs/blocknode-client-phase1-plan.md for the rationale, including why the
# consensus-node ref intentionally differs from the block-node repo's own
# cnVersion pin.
set -euo pipefail

CN_REPO=${CN_REPO:-https://github.com/hiero-ledger/hiero-consensus-node.git}
CN_TAG=${CN_TAG:-v0.74.0}
BN_REPO=${BN_REPO:-https://github.com/hiero-ledger/hiero-block-node.git}
BN_TAG=${BN_TAG:-v0.37.0}

CN_PROTO_PATH=hapi/hedera-protobuf-java-api/src/main/proto
BN_PROTO_PATH=protobuf-sources/src/main/proto

# Protos excluded from the copy, recorded in reference/hedera-protobufs/
# TRIMMED.md: deprecated operations this SDK does not implement, internal-only
# node operations, and protos nothing in the tree or the SDK references. They
# are excluded even where upstream still imports them, so the tree does not
# silently regrow dead generated code on every refresh. Excluding by FILENAME
# is deliberate: filenames are stable, whereas a line- or context-based patch
# against query.proto / response.proto / transaction.proto — the highest-churn
# files in HAPI — would eventually misapply.
#
# Consequence, and it is the point: the files that import these are copied
# verbatim, so the build FAILS immediately after a refresh with an unresolved
# import. That is the signal to re-apply the commented-out field lines listed
# in TRIMMED.md. A silent revert — which is exactly what happened during the
# v0.74.0 refresh — is no longer possible for these files. (The one trim with
# no excluded file behind it, an enum value in basic_types.proto, is guarded
# by a unit test instead; see TRIMMED.md.)
CN_TRIMMED_PROTOS=(
    # Deprecated operations
    services/crypto_add_live_hash.proto
    services/crypto_delete_live_hash.proto
    services/crypto_get_live_hash.proto
    services/crypto_get_stakers.proto
    services/get_by_key.proto
    services/get_by_solidity_id.proto
    services/network_get_execution_time.proto
    services/transaction_get_fast_record.proto
    services/unchecked_submit.proto
    # Internal-only node operation (HookDispatch)
    services/hook_dispatch.proto
    # Retired by the public networks (July 2026; nodes answer BUSY)
    services/crypto_get_account_balance.proto
    # Unreferenced: imported by nothing, used by nothing
    block/stream/output/consensus_service.proto
    block/stream/output/file_service.proto
    block/stream/output/network_service.proto
    block/stream/output/token_service.proto
    platform/event/event_consensus_data.proto
    platform/event/gossip_event.proto
    services/auxiliary/tss/tss_encryption_key.proto
    services/throttle_definitions.proto
)
# The same, for the block node api tree, which is otherwise copied whole.
BN_TRIMMED_PROTOS=(
    # Unreferenced: imported by nothing, used by nothing
    network-data.proto
    reconnect_service.proto
)

# Imports upstream declares but never uses: "<tree>/<file>|<import>", paths
# relative to reference/. protoc reports each as an "unused import" notice;
# disabling them keeps the build at ZERO protoc notices, so a new one stands
# out. Applied on an exact line match, then verified with protoc below. The
# check reports a newly unused import but never adds it here: every edit to
# an upstream file stays a deliberate one.
UNUSED_IMPORTS=(
    "hedera-protobufs/block/stream/record_file_item.proto|services/transaction_contents.proto"
    "hedera-protobufs/block/stream/record_file_item.proto|services/transaction_record.proto"
    "hedera-protobufs/block/stream/state_proof.proto|block/stream/record_file_item.proto"
    "hedera-protobufs/block/stream/state_proof.proto|services/timestamp.proto"
    "hedera-protobufs/platform/event/event_core.proto|services/basic_types.proto"
    "hiero-block-node/block-node/api/proof_service.proto|block/stream/block_proof.proto"
)
DISABLED_SUFFIX="// unused upstream; disabled by scripts/vendor-blocknode-protos.sh"

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CN_DEST="$ROOT/reference/hedera-protobufs"
BN_DEST="$ROOT/reference/hiero-block-node"

# The import check runs the protoc the build uses: Grpc.Tools at the version
# src/Hiero/Hiero.csproj pins, from the NuGet cache. Resolved before anything
# is fetched or replaced, so a missing protoc stops the run with the tree
# untouched. PROTOC=/path/to/protoc overrides.
GRPC_TOOLS_VERSION="$(sed -n 's/.*Include="Grpc\.Tools" Version="\([^"]*\)".*/\1/p' "$ROOT/src/Hiero/Hiero.csproj")"
GRPC_TOOLS="${NUGET_PACKAGES:-$HOME/.nuget/packages}/grpc.tools/$GRPC_TOOLS_VERSION"
case "$(uname -s)-$(uname -m)" in
    Linux-x86_64) PROTOC_PLATFORM=linux_x64 ;;
    Linux-aarch64 | Linux-arm64) PROTOC_PLATFORM=linux_arm64 ;;
    Darwin-*) PROTOC_PLATFORM=macosx_x64 ;;  # Grpc.Tools ships x64 only for macOS
    *) PROTOC_PLATFORM=windows_x64 ;;
esac
PROTOC="${PROTOC:-$GRPC_TOOLS/tools/$PROTOC_PLATFORM/protoc}"
if [ ! -x "$PROTOC" ] && [ -x "$PROTOC.exe" ]; then
    PROTOC="$PROTOC.exe"
fi
if [ ! -x "$PROTOC" ]; then
    echo "ERROR: protoc not found at $PROTOC" >&2
    echo "       Run 'dotnet restore src/Hiero/Hiero.csproj' to fetch Grpc.Tools $GRPC_TOOLS_VERSION," >&2
    echo "       or set PROTOC=/path/to/protoc." >&2
    exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "Fetching hiero-consensus-node @ $CN_TAG ..."
git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$CN_TAG" --filter=blob:none --sparse "$CN_REPO" "$WORK/cn"
git -C "$WORK/cn" sparse-checkout set "$CN_PROTO_PATH"
CN_SHA="$(git -C "$WORK/cn" rev-parse HEAD)"

echo "Fetching hiero-block-node @ $BN_TAG ..."
git -c advice.detachedHead=false clone --quiet --depth 1 --branch "$BN_TAG" --filter=blob:none --sparse "$BN_REPO" "$WORK/bn"
git -C "$WORK/bn" sparse-checkout set "$BN_PROTO_PATH"
BN_SHA="$(git -C "$WORK/bn" rev-parse HEAD)"
BN_CN_PIN="$(grep -oP 'val cnVersion = "\K[^"]+' "$WORK/bn/protobuf-sources/build.gradle.kts" || echo unknown)"

echo "Computing import closure ..."
printf '%s\n' "${CN_TRIMMED_PROTOS[@]}" > "$WORK/trimmed.txt"
python3 - "$WORK/cn/$CN_PROTO_PATH" "$CN_DEST" "$WORK/closure.txt" "$WORK/trimmed.txt" <<'PYEOF'
import os, re, sys

upstream, dest, out, trimmed_file = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
with open(trimmed_file) as fh:
    trimmed = {ln.strip() for ln in fh if ln.strip()}

def protos_under(base, sub=''):
    found = []
    top = os.path.join(base, sub)
    for root, _, files in os.walk(top):
        for f in files:
            if f.endswith('.proto'):
                found.append(os.path.relpath(os.path.join(root, f), base))
    return found

# Roots: everything currently vendored, plus the block stream tree upstream.
roots = set(protos_under(dest)) | set(protos_under(upstream, 'block/stream'))

dropped = sorted(r for r in roots if not os.path.exists(os.path.join(upstream, r)))
for r in dropped:
    print(f'  WARNING: dropped (not present upstream): {r}')

seen, todo = set(), [r for r in roots if r not in dropped]
while todo:
    f = todo.pop()
    if f in seen or f.startswith('google/'):
        continue  # google/* well-known types ship with protoc
    seen.add(f)
    with open(os.path.join(upstream, f)) as fh:
        todo.extend(re.findall(r'^import\s+"(.+?)";', fh.read(), re.M))

closure = sorted(seen - trimmed)
excluded = sorted(seen & trimmed)
with open(out, 'w') as fh:
    fh.write('\n'.join(closure) + '\n')
print(f'  closure: {len(closure)} files ({len(dropped)} dropped, {len(excluded)} trimmed)')
for e in excluded:
    print(f'  TRIMMED (deliberately not copied): {e}')
if excluded:
    print('')
    print('  NOTE: the files importing those are copied verbatim, so the build')
    print('        will FAIL with unresolved imports until the field lines listed')
    print('        in reference/hedera-protobufs/TRIMMED.md are commented out again.')
    print('        That failure is intentional. See docs/proto-trim-plan.md.')
PYEOF

echo "Replacing $CN_DEST ..."
find "$CN_DEST" -name '*.proto' -delete
find "$CN_DEST" -type d -empty -delete 2>/dev/null || true
while IFS= read -r f || [ -n "$f" ]; do
    mkdir -p "$CN_DEST/$(dirname "$f")"
    cp "$WORK/cn/$CN_PROTO_PATH/$f" "$CN_DEST/$f"
done < "$WORK/closure.txt"

echo "Replacing $BN_DEST ..."
rm -rf "$BN_DEST/block-node"
mkdir -p "$BN_DEST/block-node/api"
# Top-level api protos only; block-node/api/internal/ is server-side.
for f in "$WORK/bn/$BN_PROTO_PATH/block-node/api/"*.proto; do
    name="$(basename "$f")"
    if printf '%s\n' "${BN_TRIMMED_PROTOS[@]}" | grep -qxF "$name"; then
        echo "  TRIMMED (deliberately not copied): block-node/api/$name"
    else
        cp "$f" "$BN_DEST/block-node/api/"
    fi
done

echo "Disabling unused imports ..."
STALE_IMPORTS=()
for entry in "${UNUSED_IMPORTS[@]}"; do
    file="$ROOT/reference/${entry%%|*}"
    line="import \"${entry#*|}\";"
    if [ -f "$file" ] && grep -qxF "$line" "$file"; then
        awk -v line="$line" -v disabled="// $line $DISABLED_SUFFIX" \
            '$0 == line { print disabled; next } { print }' "$file" > "$WORK/disabled.proto"
        mv "$WORK/disabled.proto" "$file"
    else
        STALE_IMPORTS+=("$entry")
    fi
done
echo "  disabled: $(( ${#UNUSED_IMPORTS[@]} - ${#STALE_IMPORTS[@]} )) of ${#UNUSED_IMPORTS[@]}"

STAMP="$(date -u +%Y-%m-%d)"

cat > "$CN_DEST/README.md" <<EOF
# Vendored protobuf sources — upstream plus documented trims

Upstream: $CN_REPO
Ref: $CN_TAG ($CN_SHA)
Path: $CN_PROTO_PATH
Retrieved: $STAMP

The file set is the transitive import closure of the protos this SDK
compiles (see scripts/vendor-blocknode-protos.sh), minus a small set of
deprecated operations this SDK does not implement.

This tree is therefore upstream **plus a documented set of trims** — see
TRIMMED.md for what was removed and how to re-apply it. The vendor script
also disables imports upstream declares but never uses (listed and checked
by the script itself). Apart from those, do not hand-edit these files: to
refresh, update the pins in the vendor script and re-run it.
EOF

cat > "$BN_DEST/README.md" <<EOF
# Vendored protobuf sources — do not edit

Verbatim upstream, minus the protos listed in BN_TRIMMED_PROTOS and with
imports upstream declares but never uses disabled — both done by
scripts/vendor-blocknode-protos.sh; see ../hedera-protobufs/TRIMMED.md.

Upstream: $BN_REPO
Ref: $BN_TAG ($BN_SHA)
Path: $BN_PROTO_PATH/block-node/api (excluding internal/)
Retrieved: $STAMP

Paired consensus-node ref: $CN_TAG (see ../hedera-protobufs/README.md).
Upstream's own build pins consensus-node $BN_CN_PIN; this SDK deliberately
pairs a newer consensus-node release instead — the api protos import only
five files from that tree and are compatible across nearby versions. See
docs/blocknode-client-phase1-plan.md for the analysis.

To refresh, update the pins in scripts/vendor-blocknode-protos.sh and
re-run it; never hand-edit files in this tree.
EOF

# Check the result with protoc. The vendored trees come first on the include
# path; the upstream clones come after them so that the protos excluded by
# CN_TRIMMED_PROTOS / BN_TRIMMED_PROTOS still resolve for files whose trims
# have not been re-applied yet (see TRIMMED.md). Anything protoc reports is a failure.
echo "Checking imports with protoc ($PROTOC) ..."
PROTOC_INCLUDES=(-I hedera-protobufs -I hiero-block-node -I "$WORK/cn/$CN_PROTO_PATH" -I "$WORK/bn/$BN_PROTO_PATH")
if [ -d "$GRPC_TOOLS/build/native/include" ]; then
    PROTOC_INCLUDES+=(-I "$GRPC_TOOLS/build/native/include")
fi
PROTOC_INPUTS=()
while IFS= read -r f; do PROTOC_INPUTS+=("$f"); done < <(
    cd "$CN_DEST" && find . -name '*.proto' | sed 's|^\./||' | sort
    cd "$BN_DEST" && find block-node -name '*.proto' | sort
)
PROTOC_STATUS=0
(cd "$ROOT/reference" && "$PROTOC" "${PROTOC_INCLUDES[@]}" --descriptor_set_out="$WORK/check.pb" "${PROTOC_INPUTS[@]}") \
    > "$WORK/protoc.log" 2>&1 || PROTOC_STATUS=$?
NEW_UNUSED="$(sed -n 's|^\([^:]*\):[0-9]*:[0-9]*: warning: Import \(.*\) is unused\.$|"\1\|\2"|p' "$WORK/protoc.log")"
OTHER_OUTPUT="$(grep -v ': warning: Import .* is unused\.$' "$WORK/protoc.log" || true)"
CHECK_FAILED=0
if [ ${#STALE_IMPORTS[@]} -gt 0 ]; then
    CHECK_FAILED=1
    echo "  STALE — upstream no longer has these imports; remove them from UNUSED_IMPORTS:"
    printf '      "%s"\n' "${STALE_IMPORTS[@]}"
fi
if [ -n "$NEW_UNUSED" ]; then
    CHECK_FAILED=1
    echo "  NEW unused imports — review each, then add it to UNUSED_IMPORTS:"
    echo "$NEW_UNUSED" | sed 's/^/      /'
fi
if [ "$PROTOC_STATUS" -ne 0 ] || [ -n "$OTHER_OUTPUT" ]; then
    CHECK_FAILED=1
    echo "  protoc reported problems. A type 'not imported' by a file means upstream now"
    echo "  uses an import listed in UNUSED_IMPORTS; remove that entry:"
    echo "$OTHER_OUTPUT" | sed 's/^/      /'
    if [ "$PROTOC_STATUS" -ne 0 ]; then
        echo "  (protoc stops reporting unused imports once it errors, so the list of"
        echo "  new unused imports above may be incomplete until these are fixed.)"
    fi
fi
if [ "$CHECK_FAILED" -eq 0 ]; then
    echo "  clean: no unused imports, no errors"
fi

echo
echo "Done. Summary:"
echo "  hedera-protobufs: $(find "$CN_DEST" -name '*.proto' | wc -l) protos @ $CN_TAG"
echo "  hiero-block-node: $(find "$BN_DEST" -name '*.proto' | wc -l) protos @ $BN_TAG"
echo "Review with: git status reference/"
if [ "$CHECK_FAILED" -ne 0 ]; then
    echo
    echo "IMPORT CHECK FAILED — the tree has been replaced; fix UNUSED_IMPORTS as shown above and re-run." >&2
    exit 1
fi
