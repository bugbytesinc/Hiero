// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Hiero;

/// <summary>
/// The status codes returned by a <see cref="BlockNodeException"/>,
/// identifying the underlying failure condition reported by (or
/// detected while communicating with) a Hiero Block Node.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockNodeExceptionCode
{
    /// <summary>
    /// The block node rejected the request as malformed or
    /// inconsistent.  <strong>Permanent</strong> — correct the
    /// request before retrying.
    /// </summary>
    [Description("Invalid Request")] InvalidRequest = 1,
    /// <summary>
    /// The requested block or range is outside the span of blocks
    /// this node holds (it may have been pruned, or not yet
    /// produced).  Check <c>GetServerStatusAsync</c> for the node's
    /// available range.  <strong>Permanent</strong> for this node —
    /// another node (or a later time) may serve the request.
    /// </summary>
    [Description("Not Available")] NotAvailable = 2,
    /// <summary>
    /// The requested starting block number was rejected by the
    /// block node.  <strong>Permanent</strong> — adjust the start.
    /// </summary>
    [Description("Invalid Start Block")] InvalidStartBlockNumber = 3,
    /// <summary>
    /// The requested ending block number was rejected by the
    /// block node.  <strong>Permanent</strong> — adjust the end.
    /// </summary>
    [Description("Invalid End Block")] InvalidEndBlockNumber = 4,
    /// <summary>
    /// The client detected a gap in the unverified block stream
    /// (a completed block arrived with a number that does not
    /// contiguously follow the last delivered block) and the
    /// subscription's gap policy is <c>Throw</c>.
    /// <strong>Transient</strong> — resubscribe from the last
    /// delivered block number + 1.
    /// </summary>
    [Description("Stream Gap Detected")] StreamGapDetected = 5,
    /// <summary>
    /// The block node reported the published block as a duplicate
    /// of one it already holds.  <strong>Permanent</strong> for
    /// that block — resume publishing after the node's last
    /// acknowledged block.
    /// </summary>
    [Description("Duplicate Block")] DuplicateBlock = 6,
    /// <summary>
    /// The block node could not verify the published block's proof.
    /// <strong>Permanent</strong> for that attempt — resend the
    /// block with a valid proof.
    /// </summary>
    [Description("Bad Block Proof")] BadBlockProof = 7,
    /// <summary>
    /// The block node failed to persist a published block.
    /// <strong>Transient</strong> — the publisher may reconnect
    /// and resend.
    /// </summary>
    [Description("Persistence Failed")] PersistenceFailed = 8,
    /// <summary>
    /// The block node timed out an in-progress publish stream.
    /// <strong>Transient</strong> — reconnect and resume from the
    /// last acknowledged block.
    /// </summary>
    [Description("Timeout")] Timeout = 9,
    /// <summary>
    /// The block node reported an internal error, or returned a
    /// status code unknown to this client.  <strong>Transient</strong>
    /// — the request may be retried, possibly against another node.
    /// </summary>
    [Description("Server Error")] ServerError = 10,
    /// <summary>
    /// The block node is not reachable at this time.
    /// <strong>Transient</strong> — retry after a delay.
    /// </summary>
    [Description("Unavailable")] Unavailable = 11,
    /// <summary>
    /// A network-level gRPC failure not covered by a more specific
    /// code.  <strong>Transient</strong> — retry.
    /// </summary>
    [Description("Other Communication Error")] CommunicationError = 12,
    /// <summary>
    /// A block failed hash-chain verification: its recomputed merkle
    /// root does not chain from the preceding verified block (or a
    /// checkpoint).  <strong>Permanent</strong> for this node and
    /// block — retrying returns the same bytes; the data itself does
    /// not verify.  Obtain the block from another node or
    /// investigate.
    /// </summary>
    [Description("Verification Failed")] VerificationFailed = 13,
    /// <summary>
    /// The block node does not implement the requested API.  Block
    /// nodes serve different subsets of the Block Node APIs — the
    /// HIP-1137 registry advertises which ones each endpoint offers,
    /// and proof and state-snapshot services in particular are absent
    /// from many deployments.  <strong>Permanent</strong> for this
    /// node — query a node that advertises the API instead.  A node
    /// that serves its status API on a separate endpoint needs
    /// <see cref="IBlockNodeContext.StatusUri"/> set before it can be
    /// subscribed to.
    /// </summary>
    [Description("Not Implemented")] NotImplemented = 14,
    /// <summary>
    /// A block could not be checked at the requested verification
    /// level: it is well formed, but holds content this SDK cannot
    /// hash with certainty — filtered or redacted items, or item types
    /// newer than this SDK build — so a recomputed root could disagree
    /// with the network's without the block being wrong.  Distinct
    /// from <see cref="VerificationFailed"/>, which means the block's
    /// data does not verify.  <strong>Permanent</strong> for this
    /// block — retrying returns the same bytes.  Lower the
    /// verification level to accept the block unchecked, or use a
    /// newer SDK.
    /// </summary>
    [Description("Unverifiable")] Unverifiable = 15,
}
