# Copilot Instructions for Hiero SDK

## Project Overview
- Hiero is a .NET client library for interacting with the 
  Hiero Network and Hedera Hashgraph.
- The codebase is organized by domain: Consensus, Contract, 
  Crypto, File, Nft, Token, Utilities, etc. Each domain has 
  its own directory and related classes.
- Reference protocols and protobufs are stored 
  in `Reference/hedera-mirror/` and `Reference/hedera-protobufs/`.
  They are manually maintained and not auto-generated, since they
  are copied from upstream sources, they should not be modified directly.

## Architecture & Patterns
- Core client logic is in `ConsensusClient.cs`, `MirrorRestClient.cs` and 
  `MirrorGrpcClient.cs`.
- Data transfer objects (DTOs) are named with `Params`, `Receipt`, `Record`,
  and `Info` suffixes (e.g., `CreateTopicParams`, `SubmitMessageReceipt`).
- Exception handling uses custom exception classes 
  (e.g., `ConsensusException`, `MirrorGrpcException`, `TransactionException`).
- Endorsements, signatories, and entity IDs are modeled as distinct types
  for security and clarity.

## Developer Workflows
- Build with .NET 10.0 SDK (`dotnet build` from the root).
- Code quality checks are automated via GitHub Actions
  (`.github/workflows/codeql.yml`).
- No explicit test or CI configuration found; verify manually before PRs.
- External dependencies are managed via NuGet and referenced in the project
  file (not included in this context).

## Conventions
- Use domain-specific directories for new features 
  (e.g., add new NFT logic to `Nft/`).
- Prefer custom types for cryptography, entity IDs, and protocol 
  objects over primitives.
- Exception classes should be used for error signaling, not return codes.
- All DTOs should be immutable where possible.

## Integration Points
- Communicates with Hedera Mirror Node and Network via gRPC and REST 
  (see `MirrorGrpcClient.cs`, `MirrorRestClient.cs`).
- Protocol definitions are in `Reference/hedera-mirror/proto/` 
  and `Reference/hedera-protobufs/`.

## Examples
- To create a topic: use `Consensus/CreateTopicParams.cs` and `ConsensusClient.cs`.
- To transfer tokens: use `Token/TokenTransfer.cs` and related DTOs.
- For NFT operations: see `Nft/Nft.cs`, `Nft/NftTransfer.cs`, and `NftRoyalty.cs`.

## Key Files & Directories
- `ConsensusClient.cs`, `MirrorGrpcClient.cs`, `MirrorRestClient.cs`: Core 
  client logic
- `Consensus/`, `Contract/`, `Crypto/`, `Nft/`, `Token/`: Domain logic
- `Reference/hedera-mirror/`, `Reference/hedera-protobufs/`: Protocol definitions
- `.github/workflows/codeql.yml`: Code quality automation

---

_If any section is unclear or missing, please provide feedback for further refinement._

## Command: spellcheck
When the user types `/spellcheck` or asks for `spellcheck`, do the following:

Review only the currently open file for spelling issues.

Steps:
1. Search for misspelled words in identifiers, method names, comments,
   XML docstrings, and string literals.
2. List each suspected spelling issue with:
   - the original text,
   - the suggested correction,
   - and a short explanation.
3. Wait for user confirmation before making changes.
4. Once approved, apply minimal edits that only correct spelling.
5. Before applying any changes, open a diff view showing the patch.
6. Apply the diff to the file in the workspace.
7. After applying the edit, open the modified file in the editor.
8. Do NOT modify domain-specific terms such as Hedera, Hashgraph, gRPC, CLOB, Perpetuals, EvmAddress, EntityId, Ed25519, Secp256k1, MirrorNode.
9. Do NOT refactor, rename APIs, or change behavior.
