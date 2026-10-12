// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.BlockNode;
using Hiero.Test.Helpers;
using ProtoBlock = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Test.Unit.BlockNode;

public class BlockDomainTests
{
    internal static byte[] RandomHash()
    {
        return Generator.SHA384Hash().ToArray();
    }

    internal static ProtoBlock.BlockItem CreateHeaderItem(ulong number, long seconds = 1000, int nanos = 0)
    {
        return new ProtoBlock.BlockItem
        {
            BlockHeader = new ProtoBlock.Output.BlockHeader
            {
                Number = number,
                BlockTimestamp = new Proto.Timestamp { Seconds = seconds, Nanos = nanos },
                HapiProtoVersion = new Proto.SemanticVersion { Major = 0, Minor = 74, Patch = 0 },
                SoftwareVersion = new Proto.SemanticVersion { Major = 0, Minor = 74, Patch = 1 }
            }
        };
    }

    internal static ProtoBlock.BlockItem CreateTransactionItem(byte[] bytes)
    {
        return new ProtoBlock.BlockItem { SignedTransaction = ByteString.CopyFrom(bytes) };
    }

    internal static ProtoBlock.BlockItem CreateFooterItem(byte[] previousRoot)
    {
        return new ProtoBlock.BlockItem
        {
            BlockFooter = new ProtoBlock.Output.BlockFooter
            {
                PreviousBlockRootHash = ByteString.CopyFrom(previousRoot),
                RootHashOfAllBlockHashesTree = ByteString.CopyFrom(RandomHash()),
                StartOfBlockStateRootHash = ByteString.CopyFrom(RandomHash())
            }
        };
    }

    internal static ProtoBlock.BlockItem CreateProofItem(ulong number, byte[]? signature = null)
    {
        return new ProtoBlock.BlockItem
        {
            BlockProof = new ProtoBlock.BlockProof
            {
                Block = number,
                SignedBlockProof = new ProtoBlock.TssSignedBlockProof
                {
                    BlockSignature = ByteString.CopyFrom(signature ?? RandomHash())
                }
            }
        };
    }

    internal static ProtoBlock.Block CreateBlockProto(ulong number, int transactionCount = 2)
    {
        var block = new ProtoBlock.Block();
        block.Items.Add(CreateHeaderItem(number));
        for (var i = 0; i < transactionCount; i++)
        {
            block.Items.Add(CreateTransactionItem(RandomHash()));
        }
        block.Items.Add(CreateFooterItem(RandomHash()));
        block.Items.Add(CreateProofItem(number));
        return block;
    }

    [Test]
    public async Task Block_Promotes_Header_Footer_And_Proof()
    {
        var number = (ulong)Generator.Integer(100, 10000);
        var proto = CreateBlockProto(number, 3);
        var block = Block.FromProtobuf(proto);

        await Assert.That(block.Number).IsEqualTo(number);
        await Assert.That(block.Timestamp).IsEqualTo(new ConsensusTimeStamp(1000, 0));
        await Assert.That(block.HapiVersion).IsEqualTo(new SemanticVersion(0, 74, 0));
        await Assert.That(block.SoftwareVersion).IsEqualTo(new SemanticVersion(0, 74, 1));
        await Assert.That(block.Items.Count).IsEqualTo(6);
        await Assert.That(block.Footer).IsNotNull();
        await Assert.That(block.Proof).IsNotNull();
        await Assert.That(block.Proof!.Kind).IsEqualTo(BlockProofKind.TssSigned);
        await Assert.That(block.Proof.BlockNumber).IsEqualTo(number);
        await Assert.That(block.Proof.BlockSignature.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Block_Round_Trips_Losslessly()
    {
        var proto = CreateBlockProto((ulong)Generator.Integer(1, 5000));
        var block = Block.FromProtobuf(proto);
        await Assert.That(ReferenceEquals(block.ToProtobuf(), proto)).IsTrue();
        await Assert.That(block.ToProtobuf().ToByteArray()).IsEquivalentTo(proto.ToByteArray());
    }

    [Test]
    public async Task Block_Without_Header_Uses_Sentinel_Values()
    {
        var proto = new ProtoBlock.Block();
        proto.Items.Add(CreateTransactionItem(RandomHash()));
        var block = Block.FromProtobuf(proto);

        await Assert.That(block.Number).IsEqualTo(0ul);
        await Assert.That(block.Timestamp).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(block.HapiVersion).IsEqualTo(SemanticVersion.None);
        await Assert.That(block.SoftwareVersion).IsEqualTo(SemanticVersion.None);
        await Assert.That(block.Footer).IsNull();
        await Assert.That(block.Proof).IsNull();
    }

    [Test]
    public async Task Block_From_Null_Protobuf_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Block.FromProtobuf(null!));
        await Assert.That(exception!.ParamName).IsEqualTo("block");
    }

    [Test]
    public async Task BlockItem_Maps_Every_Known_Wire_Kind()
    {
        var expectations = new (ProtoBlock.BlockItem Item, BlockItemKind Kind)[]
        {
            (CreateHeaderItem(1), BlockItemKind.BlockHeader),
            (new ProtoBlock.BlockItem { EventHeader = new ProtoBlock.Input.EventHeader() }, BlockItemKind.EventHeader),
            (new ProtoBlock.BlockItem { RoundHeader = new ProtoBlock.Input.RoundHeader() }, BlockItemKind.RoundHeader),
            (CreateTransactionItem([1, 2, 3]), BlockItemKind.SignedTransaction),
            (new ProtoBlock.BlockItem { TransactionResult = new ProtoBlock.Output.TransactionResult() }, BlockItemKind.TransactionResult),
            (new ProtoBlock.BlockItem { TransactionOutput = new ProtoBlock.Output.TransactionOutput() }, BlockItemKind.TransactionOutput),
            (new ProtoBlock.BlockItem { StateChanges = new ProtoBlock.Output.StateChanges() }, BlockItemKind.StateChanges),
            (new ProtoBlock.BlockItem { FilteredSingleItem = new ProtoBlock.FilteredSingleItem() }, BlockItemKind.FilteredSingleItem),
            (CreateProofItem(1), BlockItemKind.BlockProof),
            (new ProtoBlock.BlockItem { RecordFile = new ProtoBlock.RecordFileItem() }, BlockItemKind.RecordFile),
            (new ProtoBlock.BlockItem { TraceData = new ProtoBlock.Trace.TraceData() }, BlockItemKind.TraceData),
            (CreateFooterItem([1]), BlockItemKind.BlockFooter),
            (new ProtoBlock.BlockItem { RedactedItem = new ProtoBlock.RedactedItem() }, BlockItemKind.RedactedItem),
        };
        foreach (var (protoItem, expectedKind) in expectations)
        {
            var item = new BlockItem(protoItem);
            await Assert.That(item.Kind).IsEqualTo(expectedKind);
            await Assert.That(ReferenceEquals(item.ToProtobuf(), protoItem)).IsTrue();
        }
    }

    [Test]
    public async Task BlockItem_Empty_Item_Is_Unknown()
    {
        var item = new BlockItem(new ProtoBlock.BlockItem());
        await Assert.That(item.Kind).IsEqualTo(BlockItemKind.Unknown);
        await Assert.That(item.SignedTransactionBytes.IsEmpty).IsTrue();
    }

    [Test]
    public async Task BlockItem_Promotes_Signed_Transaction_Bytes()
    {
        var bytes = RandomHash();
        var item = new BlockItem(CreateTransactionItem(bytes));
        await Assert.That(item.SignedTransactionBytes.ToArray()).IsEquivalentTo(bytes);
    }

    [Test]
    public async Task BlockFooter_Exposes_Hashes()
    {
        var previousRoot = RandomHash();
        var item = new BlockItem(CreateFooterItem(previousRoot));
        var footer = new BlockFooter(item.ToProtobuf().BlockFooter);
        await Assert.That(footer.PreviousBlockRootHash.ToArray()).IsEquivalentTo(previousRoot);
        await Assert.That(footer.RootHashOfAllBlockHashesTree.IsEmpty).IsFalse();
        await Assert.That(footer.StartOfBlockStateRootHash.IsEmpty).IsFalse();
    }

    [Test]
    public async Task BlockNodeStatus_Maps_Response_Fields()
    {
        var status = new BlockNodeStatus(new Org.Hiero.Block.Api.ServerStatusResponse
        {
            FirstAvailableBlock = 100,
            LastAvailableBlock = 2000,
            OnlyLatestState = true
        });
        await Assert.That(status.FirstAvailableBlockNumber).IsEqualTo(100ul);
        await Assert.That(status.LastAvailableBlockNumber).IsEqualTo(2000ul);
        await Assert.That(status.OnlyLatestState).IsTrue();
    }

    [Test]
    public async Task BlockNodeStatusDetail_Maps_Response_Fields()
    {
        var response = new Org.Hiero.Block.Api.ServerStatusDetailResponse
        {
            VersionInformation = new Org.Hiero.Block.Api.BlockNodeVersions
            {
                StreamProtoVersion = new Proto.SemanticVersion { Major = 1, Minor = 2, Patch = 3 },
                BlockNodeVersion = new Proto.SemanticVersion { Major = 0, Minor = 37, Patch = 0 },
            },
            TssData = new Org.Hiero.Block.Api.TssData
            {
                LedgerId = ByteString.CopyFrom([9, 9, 9]),
                WrapsVerificationKey = ByteString.CopyFrom([8, 8]),
                ValidFromBlock = 42,
                CurrentRoster = new Org.Hiero.Block.Api.TssRoster()
            }
        };
        response.VersionInformation.InstalledPluginVersions.Add(new Org.Hiero.Block.Api.BlockNodeVersions.Types.PluginVersion
        {
            PluginId = "subscriber",
            PluginSoftwareVersion = new Proto.SemanticVersion { Major = 0, Minor = 37, Patch = 0 }
        });
        response.AvailableRanges.Add(new Org.Hiero.Block.Api.BlockRange { RangeStart = 0, RangeEnd = 500 });
        response.StoredRanges.Add(new Org.Hiero.Block.Api.BlockRange { RangeStart = 100, RangeEnd = 500 });
        response.TssData.CurrentRoster.RosterEntries.Add(new Org.Hiero.Block.Api.RosterEntry
        {
            NodeId = 3,
            Weight = 1000,
            SchnorrPublicKey = ByteString.CopyFrom([7])
        });

        var detail = new BlockNodeStatusDetail(response);
        await Assert.That(detail.StreamProtoVersion).IsEqualTo(new SemanticVersion(1, 2, 3));
        await Assert.That(detail.BlockNodeVersion).IsEqualTo(new SemanticVersion(0, 37, 0));
        await Assert.That(detail.Plugins.Count).IsEqualTo(1);
        await Assert.That(detail.Plugins[0].PluginId).IsEqualTo("subscriber");
        await Assert.That(detail.AvailableRanges[0]).IsEqualTo(new BlockRange(0, 500));
        await Assert.That(detail.StoredRanges[0]).IsEqualTo(new BlockRange(100, 500));
        await Assert.That(detail.TssData).IsNotNull();
        await Assert.That(detail.TssData!.ValidFromBlock).IsEqualTo(42ul);
        await Assert.That(detail.TssData.Roster.Count).IsEqualTo(1);
        await Assert.That(detail.TssData.Roster[0].NodeId).IsEqualTo(3ul);
        await Assert.That(detail.NodeAddressBook).IsNull();
    }

    [Test]
    public async Task StateSnapshot_Maps_Response_Fields()
    {
        var snapshot = new StateSnapshot(new Org.Hiero.Block.Api.StateSnapshotResponse
        {
            LastBlockNumber = 777,
            SnapshotReference = "https://example.com/snapshots/777"
        });
        await Assert.That(snapshot.LastBlockNumber).IsEqualTo(777ul);
        await Assert.That(snapshot.SnapshotReference).IsEqualTo("https://example.com/snapshots/777");
    }

    [Test]
    public async Task BlockContentsProof_Maps_Response_Fields()
    {
        var proofProto = new Org.Hiero.Block.Api.BlockContentsProof
        {
            BlockItem = CreateTransactionItem([1, 2, 3]),
            BlockSignature = ByteString.CopyFrom(RandomHash())
        };
        proofProto.SiblingHashes.Add(new ProtoBlock.MerkleSiblingHash
        {
            IsFirst = true,
            SiblingHash = ByteString.CopyFrom(RandomHash())
        });

        var proof = new BlockContentsProof(proofProto);
        await Assert.That(proof.Item.Kind).IsEqualTo(BlockItemKind.SignedTransaction);
        await Assert.That(proof.SiblingHashes.Count).IsEqualTo(1);
        await Assert.That(proof.SiblingHashes[0].IsFirst).IsTrue();
        await Assert.That(proof.BlockSignature.IsEmpty).IsFalse();
    }

    [Test]
    public async Task BlockNodeException_Carries_Code()
    {
        var inner = new InvalidOperationException("boom");
        var exception = new BlockNodeException("It Broke.", BlockNodeExceptionCode.NotAvailable, inner);
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotAvailable);
        await Assert.That(exception!.Message).IsEqualTo("It Broke.");
        await Assert.That(exception!.InnerException).IsEqualTo(inner);
    }
}
