# Vendored protobuf sources — do not edit

Verbatim upstream, minus the protos listed in BN_TRIMMED_PROTOS and with
imports upstream declares but never uses disabled — both done by
scripts/vendor-blocknode-protos.sh; see ../hedera-protobufs/TRIMMED.md.

Upstream: https://github.com/hiero-ledger/hiero-block-node.git
Ref: v0.37.0 (732f7b65d53f26475bba6a857af24be61233877a)
Path: protobuf-sources/src/main/proto/block-node/api (excluding internal/)
Retrieved: 2026-07-07

Paired consensus-node ref: v0.74.0 (see ../hedera-protobufs/README.md).
Upstream's own build pins consensus-node 0.73.0-rc.1; this SDK deliberately
pairs a newer consensus-node release instead — the api protos import only
five files from that tree and are compatible across nearby versions. See
docs/blocknode-client-phase1-plan.md for the analysis.

To refresh, update the pins in scripts/vendor-blocknode-protos.sh and
re-run it; never hand-edit files in this tree.
