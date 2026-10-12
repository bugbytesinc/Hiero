// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A persisted point of verified block stream progress: a block
/// number and the merkle root hash this client computed for it.
/// </summary>
/// <remarks>
/// Consumers persist the pair after processing each verified block
/// (from <see cref="Block.Number"/> and
/// <see cref="Block.VerifiedRootHash"/>) and pass it back via
/// <see cref="SubscribeBlockStreamParams.ResumeFromCheckpoint"/>
/// after a restart: the resumed stream's first block must chain from
/// this hash, making continuity cryptographic rather than merely
/// numeric.
/// </remarks>
/// <param name="BlockNumber">The verified block's number.</param>
/// <param name="RootHash">The merkle root hash computed for that block.</param>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockStreamCheckpoint(ulong BlockNumber, ReadOnlyMemory<byte> RootHash);
