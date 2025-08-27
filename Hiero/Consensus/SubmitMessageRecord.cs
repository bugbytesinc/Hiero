using System;

namespace Hiero;
/// <summary>
/// Record produced from creating a new contract.
/// </summary>
public sealed record SubmitMessageRecord : TransactionRecord
{
    /// <summary>
    /// A SHA-384 Running Hash of the following: Previous RunningHash,
    /// TopicId, ConsensusTimestamp, SequenceNumber and this Message
    /// Submission.
    /// </summary>
    public ReadOnlyMemory<byte> RunningHash { get; internal init; }
    /// <summary>
    /// The version of the layout of message and metadata 
    /// producing the bytes sent to the SHA-384 algorithm 
    /// generating the running hash digest.
    /// </summary>
    public ulong RunningHashVersion { get; internal init; }
    /// <summary>
    /// The sequence number of this message submission.
    /// </summary>
    public ulong SequenceNumber { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal SubmitMessageRecord(Proto.TransactionRecord record) : base(record)
    {
        RunningHash = record.Receipt.TopicRunningHash.Memory;
        RunningHashVersion = record.Receipt.TopicRunningHashVersion;
        SequenceNumber = record.Receipt.TopicSequenceNumber;
    }
}