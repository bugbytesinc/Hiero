// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A single item within a block stream block.
/// </summary>
/// <remarks>
/// This is a thin wrapper over the underlying block stream protobuf
/// item.  The <see cref="Kind"/> discriminator identifies the content;
/// transaction bytes are promoted to <see cref="SignedTransactionBytes"/>
/// because they are the most commonly consumed payload.  All other
/// content (transaction results, state changes, traces, …) is reachable
/// losslessly through <see cref="ToProtobuf"/> — the SDK deliberately
/// does not domain-map those structures while the upstream block stream
/// format remains in flux.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockItem
{
    private readonly Com.Hedera.Hapi.Block.Stream.BlockItem _protobuf;
    /// <summary>
    /// The kind of content this item carries.
    /// </summary>
    public BlockItemKind Kind { get; }
    /// <summary>
    /// The serialized signed transaction bytes, populated only when
    /// <see cref="Kind"/> is <see cref="BlockItemKind.SignedTransaction"/>;
    /// empty otherwise.
    /// </summary>
    public ReadOnlyMemory<byte> SignedTransactionBytes { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf item.
    /// </summary>
    internal BlockItem(Com.Hedera.Hapi.Block.Stream.BlockItem protobuf)
    {
        _protobuf = protobuf;
        Kind = protobuf.ItemCase switch
        {
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.BlockHeader => BlockItemKind.BlockHeader,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.EventHeader => BlockItemKind.EventHeader,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.RoundHeader => BlockItemKind.RoundHeader,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.SignedTransaction => BlockItemKind.SignedTransaction,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.TransactionResult => BlockItemKind.TransactionResult,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.TransactionOutput => BlockItemKind.TransactionOutput,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.StateChanges => BlockItemKind.StateChanges,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.FilteredSingleItem => BlockItemKind.FilteredSingleItem,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.BlockProof => BlockItemKind.BlockProof,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.RecordFile => BlockItemKind.RecordFile,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.TraceData => BlockItemKind.TraceData,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.BlockFooter => BlockItemKind.BlockFooter,
            Com.Hedera.Hapi.Block.Stream.BlockItem.ItemOneofCase.RedactedItem => BlockItemKind.RedactedItem,
            _ => BlockItemKind.Unknown
        };
        SignedTransactionBytes = Kind == BlockItemKind.SignedTransaction ? protobuf.SignedTransaction.Memory : ReadOnlyMemory<byte>.Empty;
    }
    /// <summary>
    /// Returns the underlying block stream protobuf item — the
    /// lossless escape hatch to content the SDK does not domain-map
    /// (transaction results, state changes, traces, …).
    /// </summary>
    public Com.Hedera.Hapi.Block.Stream.BlockItem ToProtobuf()
    {
        return _protobuf;
    }
    /// <summary>
    /// Constructs a <code>BlockItem</code> from a raw block stream
    /// protobuf item (round-trip counterpart of <see cref="ToProtobuf"/>;
    /// also the entry point for constructing items to publish).
    /// </summary>
    /// <param name="item">
    /// The protobuf item to wrap.
    /// </param>
    /// <returns>
    /// A <code>BlockItem</code> exposing the protobuf's content.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the item is null.</exception>
    public static BlockItem FromProtobuf(Com.Hedera.Hapi.Block.Stream.BlockItem item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item), "Block item protobuf is missing. Please check that it is not null.");
        }
        return new BlockItem(item);
    }
}
