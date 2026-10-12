// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A block from the Hiero block stream: the unit of consensus output
/// containing events, transactions, results, state changes and traces
/// for one consensus round, framed by a header, footer and proof.
/// </summary>
/// <remarks>
/// The universally-needed header fields (number, timestamp, versions)
/// and the footer/proof are promoted to typed properties; everything
/// else is available in wire order through <see cref="Items"/>, each
/// item exposing the underlying protobuf via
/// <see cref="BlockItem.ToProtobuf"/>.  The full block round-trips
/// losslessly through <see cref="ToProtobuf"/> and
/// <see cref="FromProtobuf"/>.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record Block
{
    private readonly Com.Hedera.Hapi.Block.Stream.Block _protobuf;
    /// <summary>
    /// The block number (monotonically increasing from genesis).
    /// </summary>
    public ulong Number { get; }
    /// <summary>
    /// The consensus timestamp of the start of this block.
    /// </summary>
    public ConsensusTimeStamp Timestamp { get; }
    /// <summary>
    /// The version of the HAPI protobuf specification the block
    /// was produced under.
    /// </summary>
    public SemanticVersion HapiVersion { get; }
    /// <summary>
    /// The version of the consensus node software that produced
    /// the block.
    /// </summary>
    public SemanticVersion SoftwareVersion { get; }
    /// <summary>
    /// All block items in wire order (header first; footer and
    /// proof last when present).
    /// </summary>
    public IReadOnlyList<BlockItem> Items { get; }
    /// <summary>
    /// The block footer carrying the chaining merkle roots, or
    /// <code>null</code> when absent (e.g. a filtered or still
    /// incomplete block).
    /// </summary>
    public BlockFooter? Footer { get; }
    /// <summary>
    /// The block proof, or <code>null</code> when the block is
    /// not yet proven or the proof was filtered out.
    /// </summary>
    public BlockProof? Proof { get; }
    /// <summary>
    /// The SHA-384 merkle root hash this client computed for the
    /// block; populated only when the block was obtained with
    /// <see cref="BlockVerificationLevel.HashChain"/> verification
    /// (or an explicitly verified query), empty otherwise.  Persist
    /// together with <see cref="Number"/> as a
    /// <see cref="BlockStreamCheckpoint"/> to resume verified
    /// streaming across process restarts.
    /// </summary>
    public ReadOnlyMemory<byte> VerifiedRootHash { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf, optionally
    /// carrying the merkle root hash this client computed and verified
    /// for the block.
    /// </summary>
    internal Block(Com.Hedera.Hapi.Block.Stream.Block protobuf, ReadOnlyMemory<byte> verifiedRootHash = default)
    {
        _protobuf = protobuf;
        VerifiedRootHash = verifiedRootHash;
        Timestamp = ConsensusTimeStamp.MinValue;
        HapiVersion = SemanticVersion.None;
        SoftwareVersion = SemanticVersion.None;
        var items = new BlockItem[protobuf.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = new BlockItem(protobuf.Items[i]);
            items[i] = item;
            switch (item.Kind)
            {
                case BlockItemKind.BlockHeader:
                    var header = item.ToProtobuf().BlockHeader;
                    Number = header.Number;
                    Timestamp = header.BlockTimestamp is not null ? header.BlockTimestamp.ToConsensusTimeStamp() : ConsensusTimeStamp.MinValue;
                    HapiVersion = header.HapiProtoVersion is not null ? new SemanticVersion(header.HapiProtoVersion) : SemanticVersion.None;
                    SoftwareVersion = header.SoftwareVersion is not null ? new SemanticVersion(header.SoftwareVersion) : SemanticVersion.None;
                    break;
                case BlockItemKind.BlockFooter:
                    Footer = new BlockFooter(item.ToProtobuf().BlockFooter);
                    break;
                case BlockItemKind.BlockProof:
                    Proof = new BlockProof(item.ToProtobuf().BlockProof);
                    break;
            }
        }
        Items = items;
    }
    /// <summary>
    /// Returns the underlying block stream protobuf — the lossless
    /// escape hatch to the complete block structure.
    /// </summary>
    public Com.Hedera.Hapi.Block.Stream.Block ToProtobuf()
    {
        return _protobuf;
    }
    /// <summary>
    /// Constructs a <code>Block</code> from a raw block stream
    /// protobuf (round-trip counterpart of <see cref="ToProtobuf"/>;
    /// also the entry point for constructing blocks to publish).
    /// </summary>
    /// <param name="block">
    /// The protobuf block to wrap.
    /// </param>
    /// <returns>
    /// A <code>Block</code> exposing the protobuf's content.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the block is null.</exception>
    public static Block FromProtobuf(Com.Hedera.Hapi.Block.Stream.Block block)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block), "Block protobuf is missing. Please check that it is not null.");
        }
        return new Block(block);
    }
}
