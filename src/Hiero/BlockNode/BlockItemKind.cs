// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// Identifies the kind of content a <see cref="BlockItem"/> carries.
/// </summary>
/// <remarks>
/// Values mirror the wire-level field numbers of the block stream's
/// <c>BlockItem</c> oneof so the mapping is self-evident and stable.
/// Item kinds introduced by future network versions that this client
/// does not yet recognize surface as <see cref="Unknown"/> rather
/// than failing.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockItemKind
{
    /// <summary>
    /// An item kind this client does not recognize (introduced by a
    /// newer network version), or an empty item.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The block header opening a block: block number, consensus
    /// timestamp, and software versions.
    /// </summary>
    BlockHeader = 1,
    /// <summary>
    /// A consensus event header (input subtree).
    /// </summary>
    EventHeader = 2,
    /// <summary>
    /// A consensus round header (input subtree).
    /// </summary>
    RoundHeader = 3,
    /// <summary>
    /// A serialized signed transaction as submitted to the network.
    /// The bytes are exposed via <see cref="BlockItem.SignedTransactionBytes"/>.
    /// </summary>
    SignedTransaction = 4,
    /// <summary>
    /// The consensus result of a transaction: status, consensus
    /// timestamp, transfers and fees.
    /// </summary>
    TransactionResult = 5,
    /// <summary>
    /// Additional output of a transaction, including smart contract
    /// call results and EVM logs.
    /// </summary>
    TransactionOutput = 6,
    /// <summary>
    /// State changes applied as a result of the preceding transactions.
    /// </summary>
    StateChanges = 7,
    /// <summary>
    /// A placeholder hash for an item removed from a filtered stream;
    /// keeps block proofs verifiable without the item's content.
    /// </summary>
    FilteredSingleItem = 8,
    /// <summary>
    /// The block proof closing a block.
    /// </summary>
    BlockProof = 9,
    /// <summary>
    /// A legacy record file carried over the block stream
    /// (record-stream migration era).
    /// </summary>
    RecordFile = 10,
    /// <summary>
    /// Execution trace data (e.g. EVM traces).
    /// </summary>
    TraceData = 11,
    /// <summary>
    /// The block footer: previous block root hash and related
    /// merkle tree roots, preceding the block proof.
    /// </summary>
    BlockFooter = 12,
    /// <summary>
    /// A redacted item: content replaced by hashes, keeping block
    /// proofs verifiable.
    /// </summary>
    RedactedItem = 19,
}
