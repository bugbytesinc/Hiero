# Vendored protobuf sources — upstream plus documented trims

Upstream: https://github.com/hiero-ledger/hiero-consensus-node.git
Ref: v0.74.0 (6c6c608f679eb2cfaa6b347642a5ec74e7006c9d)
Path: hapi/hedera-protobuf-java-api/src/main/proto
Retrieved: 2026-07-07

The file set is the transitive import closure of the protos this SDK
compiles (see scripts/vendor-blocknode-protos.sh), minus a small set of
deprecated operations this SDK does not implement.

This tree is therefore upstream **plus a documented set of trims** — see
TRIMMED.md for what was removed and how to re-apply it. The vendor script
also disables imports upstream declares but never uses (listed and checked
by the script itself). Apart from those, do not hand-edit these files: to
refresh, update the pins in the vendor script and re-run it.
