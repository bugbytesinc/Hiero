// SPDX-License-Identifier: Apache-2.0
using Hiero.BlockNode;
using Org.Hiero.Block.Api;
using ProtoBlockStream = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Implementation;

/// <summary>
/// The kind of recoverable fault detected while assembling blocks
/// from the unverified block stream.
/// </summary>
internal enum BlockStreamFaultKind
{
    /// <summary>
    /// A completed block's number does not contiguously follow the
    /// last delivered block.
    /// </summary>
    Gap,
    /// <summary>
    /// A completed block failed structural validation (or a block
    /// boundary arrived with no buffered items).
    /// </summary>
    Malformed,
    /// <summary>
    /// A completed block failed hash-chain verification: its footer's
    /// previous-block root hash does not match the root computed for
    /// the preceding verified block (or the resume checkpoint).
    /// Never recoverable by resubscription — the same node returns
    /// the same bytes.
    /// </summary>
    HashMismatch,
    /// <summary>
    /// A completed block holds content this SDK cannot hash with
    /// certainty (filtered or redacted items, or item types newer than
    /// this build), so hash-chain verification cannot be performed.
    /// Never recoverable by resubscription, and never silently dropped
    /// under any gap policy.
    /// </summary>
    Unverifiable,
}

/// <summary>
/// Details of a fault detected by the <see cref="BlockStreamAssembler"/>.
/// </summary>
internal sealed record BlockStreamFault(BlockStreamFaultKind Kind, ulong? ExpectedBlockNumber, ulong ReceivedBlockNumber, string Message);

/// <summary>
/// The outcome of processing one subscribe stream response:
/// at most one of a delivered block, a fault, or a terminal code.
/// </summary>
internal readonly record struct BlockStreamStep(Block? Delivered, BlockStreamFault? Fault, SubscribeStreamResponse.Types.Code? TerminalCode);

/// <summary>
/// The gRPC-free state machine that assembles whole blocks from the
/// subscribe stream's item frames, owning the sharp edges of the
/// unverified protocol: resend-supersedes-partial, duplicate
/// dropping, structural validation and gap detection.  The gRPC
/// loop and resubscription logic wrap this class; keeping it free
/// of transport concerns makes every protocol case unit-testable
/// with synthetic response sequences.
/// </summary>
internal sealed class BlockStreamAssembler
{
    private readonly BlockVerificationLevel _verificationLevel;
    private readonly BlockStreamGapPolicy _gapPolicy;
    private readonly List<ProtoBlockStream.BlockItem> _buffer = [];
    /// <summary>
    /// The number of the last block handed out for delivery; used
    /// for duplicate and gap detection, and carried across
    /// resubscriptions.
    /// </summary>
    public ulong? LastDeliveredBlockNumber { get; private set; }
    /// <summary>
    /// The merkle root computed for the last delivered block (hash
    /// chain verification only); the next block's footer must chain
    /// from it.  Carried across resubscriptions.
    /// </summary>
    public byte[]? LastComputedRootHash { get; private set; }

    public BlockStreamAssembler(BlockVerificationLevel verificationLevel, BlockStreamGapPolicy gapPolicy, ulong? lastDeliveredBlockNumber, byte[]? expectedPreviousRootHash = null)
    {
        _verificationLevel = verificationLevel;
        _gapPolicy = gapPolicy;
        LastDeliveredBlockNumber = lastDeliveredBlockNumber;
        LastComputedRootHash = expectedPreviousRootHash;
    }

    /// <summary>
    /// Processes one response from the subscribe stream, buffering
    /// items and emitting a completed block, a fault, or a terminal
    /// code as appropriate.
    /// </summary>
    public BlockStreamStep Process(SubscribeStreamResponse response)
    {
        switch (response.ResponseCase)
        {
            case SubscribeStreamResponse.ResponseOneofCase.Status:
                return new BlockStreamStep(null, null, response.Status);
            case SubscribeStreamResponse.ResponseOneofCase.BlockItems:
                foreach (var item in response.BlockItems.BlockItems)
                {
                    if (item.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockHeader && _buffer.Count > 0)
                    {
                        // A new header before the previous block completed:
                        // the resend supersedes the partial buffer.
                        _buffer.Clear();
                    }
                    _buffer.Add(item);
                }
                return default;
            case SubscribeStreamResponse.ResponseOneofCase.EndOfBlock:
                return CompleteBlock(response.EndOfBlock.BlockNumber);
            default:
                return default;
        }
    }

    private BlockStreamStep CompleteBlock(ulong endOfBlockNumber)
    {
        var items = _buffer.ToArray();
        _buffer.Clear();
        if (items.Length == 0)
        {
            return Malformed(endOfBlockNumber, $"The stream marked block {endOfBlockNumber} complete without delivering any of its items.");
        }
        var header = items[0].ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockHeader ? items[0].BlockHeader : null;
        var blockNumber = header?.Number ?? endOfBlockNumber;
        if (_verificationLevel >= BlockVerificationLevel.Structural)
        {
            if (header is null)
            {
                return Malformed(endOfBlockNumber, $"Block {endOfBlockNumber} does not begin with a block header.");
            }
            if (header.Number != endOfBlockNumber)
            {
                return Malformed(endOfBlockNumber, $"The stream marked block {endOfBlockNumber} complete, but the buffered block's header claims number {header.Number}.");
            }
            if (items[^1].ItemCase != ProtoBlockStream.BlockItem.ItemOneofCase.BlockProof)
            {
                return Malformed(endOfBlockNumber, $"Block {endOfBlockNumber} does not end with a block proof.");
            }
            var hasFooter = false;
            for (var i = items.Length - 1; i >= 0 && !hasFooter; i--)
            {
                hasFooter = items[i].ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockFooter;
            }
            if (!hasFooter)
            {
                return Malformed(endOfBlockNumber, $"Block {endOfBlockNumber} does not contain a block footer.");
            }
        }
        if (LastDeliveredBlockNumber.HasValue && _gapPolicy != BlockStreamGapPolicy.Deliver)
        {
            if (blockNumber <= LastDeliveredBlockNumber.Value)
            {
                // A duplicate of an already-delivered block; drop silently.
                return default;
            }
            if (blockNumber > LastDeliveredBlockNumber.Value + 1)
            {
                return new BlockStreamStep(null, new BlockStreamFault(BlockStreamFaultKind.Gap, LastDeliveredBlockNumber.Value + 1, blockNumber, $"The block stream skipped ahead: expected block {LastDeliveredBlockNumber.Value + 1} but received block {blockNumber}."), null);
            }
        }
        var protobuf = new ProtoBlockStream.Block();
        protobuf.Items.AddRange(items);
        var verifiedRootHash = ReadOnlyMemory<byte>.Empty;
        if (_verificationLevel >= BlockVerificationLevel.HashChain)
        {
            var result = BlockHasher.ComputeBlockRootHash(protobuf);
            if (result.Failure == BlockHashFailure.Malformed)
            {
                return Malformed(endOfBlockNumber, $"Block {blockNumber} cannot be hash-chain verified: {result.Reason}");
            }
            if (result.Failure == BlockHashFailure.Unverifiable)
            {
                return new BlockStreamStep(null, new BlockStreamFault(BlockStreamFaultKind.Unverifiable, null, blockNumber, $"Block {blockNumber} cannot be hash-chain verified: {result.Reason}"), null);
            }
            var computed = result.RootHash!;
            // Chain continuity: the block's footer must reference the
            // root computed for the previous verified block.  Skipped
            // under Deliver policy, where gaps break the chain by design.
            if (_gapPolicy != BlockStreamGapPolicy.Deliver && LastComputedRootHash is not null)
            {
                // Structural validation ran (HashChain implies it), so the
                // footer is present.
                var previousRootHash = FindFooter(items)!.PreviousBlockRootHash;
                if (!previousRootHash.Span.SequenceEqual(LastComputedRootHash))
                {
                    return new BlockStreamStep(null, new BlockStreamFault(BlockStreamFaultKind.HashMismatch, null, blockNumber, $"Block {blockNumber} failed hash-chain verification: its previous-block root hash does not match the root computed for the preceding verified block."), null);
                }
            }
            verifiedRootHash = computed;
            LastComputedRootHash = computed;
        }
        if (!LastDeliveredBlockNumber.HasValue || blockNumber > LastDeliveredBlockNumber.Value)
        {
            LastDeliveredBlockNumber = blockNumber;
        }
        return new BlockStreamStep(new Block(protobuf, verifiedRootHash), null, null);
    }

    /// <summary>
    /// Locates the block's footer, which structural validation has
    /// already established is present (usually immediately before the
    /// closing proof).
    /// </summary>
    private static ProtoBlockStream.Output.BlockFooter? FindFooter(ProtoBlockStream.BlockItem[] items)
    {
        for (var i = items.Length - 1; i >= 0; i--)
        {
            if (items[i].ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockFooter)
            {
                return items[i].BlockFooter;
            }
        }
        return null;
    }

    private BlockStreamStep Malformed(ulong blockNumber, string message)
    {
        if (_gapPolicy == BlockStreamGapPolicy.Deliver)
        {
            // Deliver mode silently drops structurally malformed blocks
            // (including hasher-Malformed ones); the consumer observes the
            // missing number.  Unverifiable blocks never come here: they
            // always fault, whatever the gap policy.
            return default;
        }
        return new BlockStreamStep(null, new BlockStreamFault(BlockStreamFaultKind.Malformed, null, blockNumber, message), null);
    }
}
