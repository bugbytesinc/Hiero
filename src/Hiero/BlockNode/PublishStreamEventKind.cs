// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The kind of directive or acknowledgement a block node sends a
/// publisher over an open publish stream.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum PublishStreamEventKind
{
    /// <summary>
    /// The node verified and persisted blocks; the acknowledgement is
    /// cumulative — all blocks up to and including the event's block
    /// number are acknowledged.
    /// </summary>
    BlockAcknowledged = 1,
    /// <summary>
    /// The node is receiving the event's block from another publisher;
    /// do not (re)send it.  A skip is always followed later by either
    /// an acknowledgement or a resend request for that block.
    /// </summary>
    SkipBlock = 2,
    /// <summary>
    /// The node needs the event's block again from its header.
    /// </summary>
    ResendBlock = 3,
    /// <summary>
    /// The node is behind; the event's block number is its latest
    /// known.  Rewind to that block + 1, or end the stream with
    /// <see cref="PublishStreamEndCode.TooFarBehind"/>.
    /// </summary>
    BehindPublisher = 4,
    /// <summary>
    /// The node terminated the stream; see
    /// <see cref="PublishStreamEvent.EndReason"/>.
    /// </summary>
    StreamEnded = 5,
}
