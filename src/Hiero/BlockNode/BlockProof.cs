// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The proof closing a block stream block.
/// </summary>
/// <remarks>
/// This SDK does not (yet) verify block proofs itself; the record
/// surfaces the proof's form and signature blob and hands the full
/// structure back through <see cref="ToProtobuf"/> for external
/// verification tooling.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockProof
{
    private readonly Com.Hedera.Hapi.Block.Stream.BlockProof _protobuf;
    /// <summary>
    /// The form this proof takes.
    /// </summary>
    public BlockProofKind Kind { get; }
    /// <summary>
    /// The number of the block this proof covers.
    /// </summary>
    public ulong BlockNumber { get; }
    /// <summary>
    /// The aggregate signature blob, populated only when
    /// <see cref="Kind"/> is <see cref="BlockProofKind.TssSigned"/>;
    /// empty otherwise.
    /// </summary>
    public ReadOnlyMemory<byte> BlockSignature { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockProof(Com.Hedera.Hapi.Block.Stream.BlockProof protobuf)
    {
        _protobuf = protobuf;
        BlockNumber = protobuf.Block;
        Kind = protobuf.ProofCase switch
        {
            Com.Hedera.Hapi.Block.Stream.BlockProof.ProofOneofCase.SignedBlockProof => BlockProofKind.TssSigned,
            Com.Hedera.Hapi.Block.Stream.BlockProof.ProofOneofCase.BlockStateProof => BlockProofKind.StateProof,
            Com.Hedera.Hapi.Block.Stream.BlockProof.ProofOneofCase.SignedRecordFileProof => BlockProofKind.SignedRecordFile,
            _ => BlockProofKind.Unknown
        };
        BlockSignature = Kind == BlockProofKind.TssSigned ? protobuf.SignedBlockProof.BlockSignature.Memory : ReadOnlyMemory<byte>.Empty;
    }
    /// <summary>
    /// Returns the underlying block stream protobuf proof — the
    /// lossless escape hatch to the full proof structure.
    /// </summary>
    public Com.Hedera.Hapi.Block.Stream.BlockProof ToProtobuf()
    {
        return _protobuf;
    }
}
