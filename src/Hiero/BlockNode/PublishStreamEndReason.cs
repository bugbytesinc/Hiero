// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The reason a block node gives when it terminates a publish stream.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum PublishStreamEndReason
{
    /// <summary>
    /// A reason this client does not recognize.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The stream ended normally.
    /// </summary>
    Success = 1,
    /// <summary>
    /// The node rejected the stream as malformed.
    /// </summary>
    InvalidRequest = 2,
    /// <summary>
    /// The node encountered an internal error.
    /// </summary>
    Error = 3,
    /// <summary>
    /// The node timed out the stream.
    /// </summary>
    Timeout = 4,
    /// <summary>
    /// The published block duplicates one the node already holds.
    /// </summary>
    DuplicateBlock = 5,
    /// <summary>
    /// The published block's proof failed verification.
    /// </summary>
    BadBlockProof = 6,
    /// <summary>
    /// The node failed to persist a published block.
    /// </summary>
    PersistenceFailed = 7,
}
