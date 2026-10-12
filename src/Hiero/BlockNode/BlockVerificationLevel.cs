// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The validation applied to each block assembled from the
/// (unverified) block stream before it is delivered.
/// </summary>
/// <remarks>
/// Block nodes forward subscribe-stream items as they arrive from
/// the publishing consensus node, before verification — malformed
/// or incomplete blocks are possible on the wire.  This setting
/// controls how much scrutiny the client applies before delivery.
/// A signature verification level is planned for a future release
/// once the upstream specifications stabilize.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockVerificationLevel
{
    /// <summary>
    /// No validation; every completed block is delivered as received.
    /// </summary>
    None = 0,
    /// <summary>
    /// Structural validation (the default): the block must open with
    /// a header whose number matches the stream's block boundary
    /// marker, contain a footer, and close with its proof.  Cheap,
    /// and catches truncated or garbled blocks; does not verify
    /// hashes or signatures.
    /// </summary>
    Structural = 1,
    /// <summary>
    /// Structural validation plus SHA-384 merkle root recomputation:
    /// each block's root hash is recomputed from its items and the
    /// chain is verified — every block's
    /// <see cref="BlockFooter.PreviousBlockRootHash"/> must equal the
    /// root computed for the preceding block.  A mismatch raises
    /// <see cref="BlockNodeExceptionCode.VerificationFailed"/> and is
    /// never automatically resubscribed (the same node would return
    /// the same bytes).  A block this SDK cannot hash with certainty
    /// (filtered or redacted items, or item types newer than this
    /// build) raises <see cref="BlockNodeExceptionCode.Unverifiable"/>
    /// under the same no-resubscribe rule, under every gap policy.
    /// Roots are computed with the 16-branch block root tree used by
    /// consensus node v0.77 and later, which also covers all wrapped
    /// record-stream history; blocks produced by earlier consensus
    /// node releases (for example, a local network that has not been
    /// upgraded) do not verify.  Verified blocks carry their computed
    /// root in <see cref="Block.VerifiedRootHash"/>.  The chain-continuity
    /// check is skipped under <see cref="BlockStreamGapPolicy.Deliver"/>
    /// (gaps break the chain by definition); per-block roots are still
    /// computed.
    /// </summary>
    HashChain = 2,
}
