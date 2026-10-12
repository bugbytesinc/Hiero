// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// Identifies the form a block's <see cref="BlockProof"/> takes.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockProofKind
{
    /// <summary>
    /// A proof form this client does not recognize.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// An aggregate threshold (TSS) signature over the block's root
    /// hash, signed by nodes holding a supermajority of stake.  The
    /// signature blob is exposed via <see cref="BlockProof.BlockSignature"/>.
    /// </summary>
    TssSigned = 1,
    /// <summary>
    /// A merkle-path proof connecting this block's root hash to a
    /// later, signed block — used when a block's own signature was
    /// not available in time (delayed proof).
    /// </summary>
    StateProof = 2,
    /// <summary>
    /// A proof anchored in a signed record file from the legacy
    /// record stream era.
    /// </summary>
    SignedRecordFile = 3,
}
