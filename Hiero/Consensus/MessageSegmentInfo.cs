using Proto;

namespace Hiero;
/// <summary>
/// Optional metadata that may be attached to an
/// Segmented HCS message identifying the index
/// of the segment and which parent message this
/// segment correlates with.
/// </summary>
public sealed record MessageSegmentInfo
{
    /// <summary>
    /// The transaction that created the first segment
    /// of the message.  This acts as a correlation
    /// identifier to coalesce the segments of the
    /// message into one.
    /// </summary>
    public TransactionId ParentTransactionId { get; internal init; }
    /// <summary>
    /// The index of this segment (one based).
    /// </summary>
    public int Index { get; internal init; }
    /// <summary>
    /// The total number of segments making up
    /// the whole of the message when assembled.
    /// </summary>
    public int TotalSegmentCount { get; internal init; }
    /// <summary>
    /// Internal Constructor from Raw Data
    /// </summary>
    internal MessageSegmentInfo(ConsensusMessageChunkInfo info)
    {
        ParentTransactionId = info.InitialTransactionID.AsTxId();
        Index = info.Number;
        TotalSegmentCount = info.Total;
    }
}