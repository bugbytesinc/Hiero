﻿using Proto;
using System;

namespace Hiero;
/// <summary>
/// Receipt produced from creating a new contract.
/// </summary>
public record SubmitMessageReceipt : TransactionReceipt
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
    /// Internal Constructor of the receipt.
    /// </summary>
    internal SubmitMessageReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        RunningHash = receipt.TopicRunningHash.Memory;
        RunningHashVersion = receipt.TopicRunningHashVersion;
        SequenceNumber = receipt.TopicSequenceNumber;
    }
}