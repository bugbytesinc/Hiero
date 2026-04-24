// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Helpers;
using System.Numerics;

namespace Hiero.Test.Unit.Mirror;

public class MirrorFilterTests
{
    [Test]
    public async Task PageLimit_Has_Correct_Name()
    {
        var limit = Generator.Integer(1, 100);
        var filter = new PageLimit(limit);
        await Assert.That(filter.Name).IsEqualTo("limit");
    }

    [Test]
    public async Task PageLimit_Has_Correct_Value()
    {
        var limit = Generator.Integer(1, 100);
        var filter = new PageLimit(limit);
        await Assert.That(filter.Value).IsEqualTo(limit.ToString());
    }

    [Test]
    public async Task OrderBy_Ascending_Has_Correct_Name()
    {
        var filter = OrderBy.Ascending;
        await Assert.That(filter.Name).IsEqualTo("order");
    }

    [Test]
    public async Task OrderBy_Ascending_Has_Correct_Value()
    {
        var filter = OrderBy.Ascending;
        await Assert.That(filter.Value).IsEqualTo("asc");
    }

    [Test]
    public async Task OrderBy_Descending_Has_Correct_Name()
    {
        var filter = OrderBy.Descending;
        await Assert.That(filter.Name).IsEqualTo("order");
    }

    [Test]
    public async Task OrderBy_Descending_Has_Correct_Value()
    {
        var filter = OrderBy.Descending;
        await Assert.That(filter.Value).IsEqualTo("desc");
    }

    [Test]
    public async Task TimestampFilter_Name_Is_Always_Timestamp()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 123456789);
        await Assert.That(TimestampFilter.Is(ts).Name).IsEqualTo("timestamp");
        await Assert.That(TimestampFilter.After(ts).Name).IsEqualTo("timestamp");
        await Assert.That(TimestampFilter.OnOrAfter(ts).Name).IsEqualTo("timestamp");
        await Assert.That(TimestampFilter.Before(ts).Name).IsEqualTo("timestamp");
        await Assert.That(TimestampFilter.OnOrBefore(ts).Name).IsEqualTo("timestamp");
        await Assert.That(TimestampFilter.NotIs(ts).Name).IsEqualTo("timestamp");
    }

    [Test]
    public async Task TimestampFilter_Is_Has_No_Operator_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 0);
        await Assert.That(TimestampFilter.Is(ts).Value).IsEqualTo(ts.ToString());
    }

    [Test]
    public async Task TimestampFilter_After_Uses_Gt_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 123456789);
        await Assert.That(TimestampFilter.After(ts).Value).IsEqualTo($"gt:{ts}");
    }

    [Test]
    public async Task TimestampFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 123456789);
        await Assert.That(TimestampFilter.OnOrAfter(ts).Value).IsEqualTo($"gte:{ts}");
    }

    [Test]
    public async Task TimestampFilter_Before_Uses_Lt_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 999999999);
        await Assert.That(TimestampFilter.Before(ts).Value).IsEqualTo($"lt:{ts}");
    }

    [Test]
    public async Task TimestampFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 999999999);
        await Assert.That(TimestampFilter.OnOrBefore(ts).Value).IsEqualTo($"lte:{ts}");
    }

    [Test]
    public async Task TimestampFilter_NotIs_Uses_Ne_Prefix()
    {
        var ts = new ConsensusTimeStamp(1_700_000_000L, 0);
        await Assert.That(TimestampFilter.NotIs(ts).Value).IsEqualTo($"ne:{ts}");
    }

    [Test]
    public async Task AccountFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = AccountFilter.Is(entityId);
        await Assert.That(filter.Name).IsEqualTo("account.id");
    }

    [Test]
    public async Task AccountFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = AccountFilter.Is(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task AccountFilter_Name_Is_Always_AccountId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.Is(entityId).Name).IsEqualTo("account.id");
        await Assert.That(AccountFilter.After(entityId).Name).IsEqualTo("account.id");
        await Assert.That(AccountFilter.OnOrAfter(entityId).Name).IsEqualTo("account.id");
        await Assert.That(AccountFilter.Before(entityId).Name).IsEqualTo("account.id");
        await Assert.That(AccountFilter.OnOrBefore(entityId).Name).IsEqualTo("account.id");
        await Assert.That(AccountFilter.NotIs(entityId).Name).IsEqualTo("account.id");
    }

    [Test]
    public async Task AccountFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task AccountFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task AccountFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task AccountFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task AccountFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(AccountFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task EvmSenderFilter_Has_Correct_Name()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var filter = EvmSenderFilter.Is(evmAddress);
        await Assert.That(filter.Name).IsEqualTo("from");
    }

    [Test]
    public async Task EvmSenderFilter_Has_Correct_Value()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var filter = EvmSenderFilter.Is(evmAddress);
        var expected = $"0x{Hex.FromBytes(evmAddress.Bytes)}";
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task TokenFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = TokenFilter.Is(entityId);
        await Assert.That(filter.Name).IsEqualTo("token.id");
    }

    [Test]
    public async Task TokenFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = TokenFilter.Is(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task SpenderFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = SpenderFilter.Is(entityId);
        await Assert.That(filter.Name).IsEqualTo("spender.id");
    }

    [Test]
    public async Task SpenderFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = SpenderFilter.Is(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task TokenFilter_Name_Is_Always_TokenId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.Is(entityId).Name).IsEqualTo("token.id");
        await Assert.That(TokenFilter.After(entityId).Name).IsEqualTo("token.id");
        await Assert.That(TokenFilter.OnOrAfter(entityId).Name).IsEqualTo("token.id");
        await Assert.That(TokenFilter.Before(entityId).Name).IsEqualTo("token.id");
        await Assert.That(TokenFilter.OnOrBefore(entityId).Name).IsEqualTo("token.id");
        await Assert.That(TokenFilter.NotIs(entityId).Name).IsEqualTo("token.id");
    }

    [Test]
    public async Task TokenFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task TokenFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task TokenFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task TokenFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task TokenFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(TokenFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task SpenderFilter_Name_Is_Always_SpenderId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.Is(entityId).Name).IsEqualTo("spender.id");
        await Assert.That(SpenderFilter.After(entityId).Name).IsEqualTo("spender.id");
        await Assert.That(SpenderFilter.OnOrAfter(entityId).Name).IsEqualTo("spender.id");
        await Assert.That(SpenderFilter.Before(entityId).Name).IsEqualTo("spender.id");
        await Assert.That(SpenderFilter.OnOrBefore(entityId).Name).IsEqualTo("spender.id");
        await Assert.That(SpenderFilter.NotIs(entityId).Name).IsEqualTo("spender.id");
    }

    [Test]
    public async Task SpenderFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task SpenderFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task SpenderFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task SpenderFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task SpenderFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SpenderFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task SlotFilter_Has_Correct_Name()
    {
        var slot = new BigInteger(Generator.Integer(0, 1_000_000));
        var filter = SlotFilter.Is(slot);
        await Assert.That(filter.Name).IsEqualTo("slot");
    }

    [Test]
    public async Task SlotFilter_Has_Correct_Value_For_Zero()
    {
        var filter = SlotFilter.Is(BigInteger.Zero);
        await Assert.That(filter.Value).IsEqualTo("0x" + new string('0', 64));
    }

    [Test]
    public async Task SlotFilter_Has_Correct_Value_Padded_To_64_Hex_Chars()
    {
        var slot = new BigInteger(1);
        var filter = SlotFilter.Is(slot);
        var expected = "0x" + Hex.FromBytes(slot.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task SlotFilter_Has_Correct_Value_For_Large_Slot()
    {
        var slot = new BigInteger(Generator.Integer(1, 1_000_000));
        var filter = SlotFilter.Is(slot);
        var expected = "0x" + Hex.FromBytes(slot.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task SequenceNumberFilter_Name_Is_Always_SequenceNumber()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.Is(seq).Name).IsEqualTo("sequencenumber");
        await Assert.That(SequenceNumberFilter.After(seq).Name).IsEqualTo("sequencenumber");
        await Assert.That(SequenceNumberFilter.OnOrAfter(seq).Name).IsEqualTo("sequencenumber");
        await Assert.That(SequenceNumberFilter.Before(seq).Name).IsEqualTo("sequencenumber");
        await Assert.That(SequenceNumberFilter.OnOrBefore(seq).Name).IsEqualTo("sequencenumber");
        await Assert.That(SequenceNumberFilter.NotIs(seq).Name).IsEqualTo("sequencenumber");
    }

    [Test]
    public async Task SequenceNumberFilter_Is_Has_No_Operator_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.Is(seq).Value).IsEqualTo(seq.ToString());
    }

    [Test]
    public async Task SequenceNumberFilter_After_Uses_Gt_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.After(seq).Value).IsEqualTo($"gt:{seq}");
    }

    [Test]
    public async Task SequenceNumberFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.OnOrAfter(seq).Value).IsEqualTo($"gte:{seq}");
    }

    [Test]
    public async Task SequenceNumberFilter_Before_Uses_Lt_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.Before(seq).Value).IsEqualTo($"lt:{seq}");
    }

    [Test]
    public async Task SequenceNumberFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.OnOrBefore(seq).Value).IsEqualTo($"lte:{seq}");
    }

    [Test]
    public async Task SequenceNumberFilter_NotIs_Uses_Ne_Prefix()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        await Assert.That(SequenceNumberFilter.NotIs(seq).Value).IsEqualTo($"ne:{seq}");
    }

    [Test]
    public async Task BlockNumberFilter_Name_Is_Always_BlockNumber()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.Is(block).Name).IsEqualTo("block.number");
        await Assert.That(BlockNumberFilter.After(block).Name).IsEqualTo("block.number");
        await Assert.That(BlockNumberFilter.OnOrAfter(block).Name).IsEqualTo("block.number");
        await Assert.That(BlockNumberFilter.Before(block).Name).IsEqualTo("block.number");
        await Assert.That(BlockNumberFilter.OnOrBefore(block).Name).IsEqualTo("block.number");
        await Assert.That(BlockNumberFilter.NotIs(block).Name).IsEqualTo("block.number");
    }

    [Test]
    public async Task BlockNumberFilter_Is_Has_No_Operator_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.Is(block).Value).IsEqualTo(block.ToString());
    }

    [Test]
    public async Task BlockNumberFilter_After_Uses_Gt_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.After(block).Value).IsEqualTo($"gt:{block}");
    }

    [Test]
    public async Task BlockNumberFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.OnOrAfter(block).Value).IsEqualTo($"gte:{block}");
    }

    [Test]
    public async Task BlockNumberFilter_Before_Uses_Lt_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.Before(block).Value).IsEqualTo($"lt:{block}");
    }

    [Test]
    public async Task BlockNumberFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.OnOrBefore(block).Value).IsEqualTo($"lte:{block}");
    }

    [Test]
    public async Task BlockNumberFilter_NotIs_Uses_Ne_Prefix()
    {
        var block = (ulong)Generator.Integer(1, 10_000_000);
        await Assert.That(BlockNumberFilter.NotIs(block).Value).IsEqualTo($"ne:{block}");
    }

    [Test]
    public async Task FileFilter_Has_Correct_Name()
    {
        var fileId = new EntityId(0, 0, Generator.Integer(100, 200));
        await Assert.That(FileFilter.Is(fileId).Name).IsEqualTo("file.id");
    }

    [Test]
    public async Task FileFilter_Has_Correct_Value()
    {
        var fileId = new EntityId(0, 0, Generator.Integer(100, 200));
        await Assert.That(FileFilter.Is(fileId).Value).IsEqualTo(fileId.ToString());
    }

    [Test]
    public async Task NodeFilter_Name_Is_Always_NodeId()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.Is(nodeId).Name).IsEqualTo("node.id");
        await Assert.That(NodeFilter.After(nodeId).Name).IsEqualTo("node.id");
        await Assert.That(NodeFilter.OnOrAfter(nodeId).Name).IsEqualTo("node.id");
        await Assert.That(NodeFilter.Before(nodeId).Name).IsEqualTo("node.id");
        await Assert.That(NodeFilter.OnOrBefore(nodeId).Name).IsEqualTo("node.id");
    }

    [Test]
    public async Task NodeFilter_Is_Has_No_Operator_Prefix()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.Is(nodeId).Value).IsEqualTo(nodeId.ToString());
    }

    [Test]
    public async Task NodeFilter_After_Uses_Gt_Prefix()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.After(nodeId).Value).IsEqualTo($"gt:{nodeId}");
    }

    [Test]
    public async Task NodeFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.OnOrAfter(nodeId).Value).IsEqualTo($"gte:{nodeId}");
    }

    [Test]
    public async Task NodeFilter_Before_Uses_Lt_Prefix()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.Before(nodeId).Value).IsEqualTo($"lt:{nodeId}");
    }

    [Test]
    public async Task NodeFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var nodeId = (ulong)Generator.Integer(0, 100);
        await Assert.That(NodeFilter.OnOrBefore(nodeId).Value).IsEqualTo($"lte:{nodeId}");
    }

    [Test]
    public async Task SerialNumberFilter_Name_Is_Always_SerialNumber()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.Is(serial).Name).IsEqualTo("serialnumber");
        await Assert.That(SerialNumberFilter.After(serial).Name).IsEqualTo("serialnumber");
        await Assert.That(SerialNumberFilter.OnOrAfter(serial).Name).IsEqualTo("serialnumber");
        await Assert.That(SerialNumberFilter.Before(serial).Name).IsEqualTo("serialnumber");
        await Assert.That(SerialNumberFilter.OnOrBefore(serial).Name).IsEqualTo("serialnumber");
    }

    [Test]
    public async Task SerialNumberFilter_Is_Has_No_Operator_Prefix()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.Is(serial).Value).IsEqualTo(serial.ToString());
    }

    [Test]
    public async Task SerialNumberFilter_After_Uses_Gt_Prefix()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.After(serial).Value).IsEqualTo($"gt:{serial}");
    }

    [Test]
    public async Task SerialNumberFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.OnOrAfter(serial).Value).IsEqualTo($"gte:{serial}");
    }

    [Test]
    public async Task SerialNumberFilter_Before_Uses_Lt_Prefix()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.Before(serial).Value).IsEqualTo($"lt:{serial}");
    }

    [Test]
    public async Task SerialNumberFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var serial = (long)Generator.Integer(1, 100000);
        await Assert.That(SerialNumberFilter.OnOrBefore(serial).Value).IsEqualTo($"lte:{serial}");
    }

    [Test]
    public async Task ReceiverFilter_Name_Is_Always_ReceiverId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.Is(entityId).Name).IsEqualTo("receiver.id");
        await Assert.That(ReceiverFilter.After(entityId).Name).IsEqualTo("receiver.id");
        await Assert.That(ReceiverFilter.OnOrAfter(entityId).Name).IsEqualTo("receiver.id");
        await Assert.That(ReceiverFilter.Before(entityId).Name).IsEqualTo("receiver.id");
        await Assert.That(ReceiverFilter.OnOrBefore(entityId).Name).IsEqualTo("receiver.id");
        await Assert.That(ReceiverFilter.NotIs(entityId).Name).IsEqualTo("receiver.id");
    }

    [Test]
    public async Task ReceiverFilter_Is_Has_No_Operator_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.Is(entityId).Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task ReceiverFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task ReceiverFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task ReceiverFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task ReceiverFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task ReceiverFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ReceiverFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task SenderFilter_Name_Is_Always_SenderId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.Is(entityId).Name).IsEqualTo("sender.id");
        await Assert.That(SenderFilter.After(entityId).Name).IsEqualTo("sender.id");
        await Assert.That(SenderFilter.OnOrAfter(entityId).Name).IsEqualTo("sender.id");
        await Assert.That(SenderFilter.Before(entityId).Name).IsEqualTo("sender.id");
        await Assert.That(SenderFilter.OnOrBefore(entityId).Name).IsEqualTo("sender.id");
        await Assert.That(SenderFilter.NotIs(entityId).Name).IsEqualTo("sender.id");
    }

    [Test]
    public async Task SenderFilter_Is_Has_No_Operator_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.Is(entityId).Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task SenderFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task SenderFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task SenderFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task SenderFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task SenderFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(SenderFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task TokenNameFilter_Contains_Has_Correct_Name_And_Value()
    {
        var filter = TokenNameFilter.Contains("usd");
        await Assert.That(filter.Name).IsEqualTo("name");
        await Assert.That(filter.Value).IsEqualTo("usd");
    }

    [Test]
    public async Task TokenNameFilter_Contains_Rejects_Too_Short_Fragment()
    {
        await Assert.That(() => TokenNameFilter.Contains("ab")).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TokenNameFilter_Contains_Rejects_Too_Long_Fragment()
    {
        var tooLong = new string('x', 101);
        await Assert.That(() => TokenNameFilter.Contains(tooLong)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TokenNameFilter_Contains_Accepts_Minimum_Length()
    {
        var filter = TokenNameFilter.Contains("abc");
        await Assert.That(filter.Value).IsEqualTo("abc");
    }

    [Test]
    public async Task PublicKeyFilter_Is_Has_Correct_Name_And_Hex_Value()
    {
        var (pub, _) = Generator.KeyPair();
        var endorsement = new Endorsement(pub);
        var filter = PublicKeyFilter.Is(endorsement);
        await Assert.That(filter.Name).IsEqualTo("publickey");
        await Assert.That(filter.Value).IsEqualTo(Hex.FromBytes(endorsement.ToBytes(KeyFormat.Mirror)));
    }

    [Test]
    public async Task TokenTypeFilter_All_Has_Correct_Name_And_Value()
    {
        var filter = TokenTypeFilter.All;
        await Assert.That(filter.Name).IsEqualTo("type");
        await Assert.That(filter.Value).IsEqualTo("ALL");
    }

    [Test]
    public async Task TokenTypeFilter_Fungible_Has_Correct_Value()
    {
        await Assert.That(TokenTypeFilter.Fungible.Name).IsEqualTo("type");
        await Assert.That(TokenTypeFilter.Fungible.Value).IsEqualTo("FUNGIBLE_COMMON");
    }

    [Test]
    public async Task TokenTypeFilter_NonFungible_Has_Correct_Value()
    {
        await Assert.That(TokenTypeFilter.NonFungible.Name).IsEqualTo("type");
        await Assert.That(TokenTypeFilter.NonFungible.Value).IsEqualTo("NON_FUNGIBLE_UNIQUE");
    }

    [Test]
    public async Task ScheduleFilter_Name_Is_Always_ScheduleId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.Is(entityId).Name).IsEqualTo("schedule.id");
        await Assert.That(ScheduleFilter.After(entityId).Name).IsEqualTo("schedule.id");
        await Assert.That(ScheduleFilter.OnOrAfter(entityId).Name).IsEqualTo("schedule.id");
        await Assert.That(ScheduleFilter.Before(entityId).Name).IsEqualTo("schedule.id");
        await Assert.That(ScheduleFilter.OnOrBefore(entityId).Name).IsEqualTo("schedule.id");
        await Assert.That(ScheduleFilter.NotIs(entityId).Name).IsEqualTo("schedule.id");
    }

    [Test]
    public async Task ScheduleFilter_Is_Has_No_Operator_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.Is(entityId).Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task ScheduleFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task ScheduleFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task ScheduleFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task ScheduleFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task ScheduleFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ScheduleFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task ContractFilter_Name_Is_Always_ContractId_Across_Factories()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.Is(entityId).Name).IsEqualTo("contract.id");
        await Assert.That(ContractFilter.After(entityId).Name).IsEqualTo("contract.id");
        await Assert.That(ContractFilter.OnOrAfter(entityId).Name).IsEqualTo("contract.id");
        await Assert.That(ContractFilter.Before(entityId).Name).IsEqualTo("contract.id");
        await Assert.That(ContractFilter.OnOrBefore(entityId).Name).IsEqualTo("contract.id");
        await Assert.That(ContractFilter.NotIs(entityId).Name).IsEqualTo("contract.id");
    }

    [Test]
    public async Task ContractFilter_Is_Has_No_Operator_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.Is(entityId).Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task ContractFilter_After_Uses_Gt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.After(entityId).Value).IsEqualTo($"gt:{entityId}");
    }

    [Test]
    public async Task ContractFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.OnOrAfter(entityId).Value).IsEqualTo($"gte:{entityId}");
    }

    [Test]
    public async Task ContractFilter_Before_Uses_Lt_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.Before(entityId).Value).IsEqualTo($"lt:{entityId}");
    }

    [Test]
    public async Task ContractFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.OnOrBefore(entityId).Value).IsEqualTo($"lte:{entityId}");
    }

    [Test]
    public async Task ContractFilter_NotIs_Uses_Ne_Prefix()
    {
        var entityId = new EntityId(0, 0, Generator.Integer(1, 1000));
        await Assert.That(ContractFilter.NotIs(entityId).Value).IsEqualTo($"ne:{entityId}");
    }

    [Test]
    public async Task AccountBalanceFilter_Name_Is_Always_AccountBalance_Across_Factories()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.Is(tinybars).Name).IsEqualTo("account.balance");
        await Assert.That(AccountBalanceFilter.After(tinybars).Name).IsEqualTo("account.balance");
        await Assert.That(AccountBalanceFilter.OnOrAfter(tinybars).Name).IsEqualTo("account.balance");
        await Assert.That(AccountBalanceFilter.Before(tinybars).Name).IsEqualTo("account.balance");
        await Assert.That(AccountBalanceFilter.OnOrBefore(tinybars).Name).IsEqualTo("account.balance");
        await Assert.That(AccountBalanceFilter.NotIs(tinybars).Name).IsEqualTo("account.balance");
    }

    [Test]
    public async Task AccountBalanceFilter_Is_Has_No_Operator_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.Is(tinybars).Value).IsEqualTo(tinybars.ToString());
    }

    [Test]
    public async Task AccountBalanceFilter_After_Uses_Gt_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.After(tinybars).Value).IsEqualTo($"gt:{tinybars}");
    }

    [Test]
    public async Task AccountBalanceFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.OnOrAfter(tinybars).Value).IsEqualTo($"gte:{tinybars}");
    }

    [Test]
    public async Task AccountBalanceFilter_Before_Uses_Lt_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.Before(tinybars).Value).IsEqualTo($"lt:{tinybars}");
    }

    [Test]
    public async Task AccountBalanceFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.OnOrBefore(tinybars).Value).IsEqualTo($"lte:{tinybars}");
    }

    [Test]
    public async Task AccountBalanceFilter_NotIs_Uses_Ne_Prefix()
    {
        var tinybars = (long)Generator.Integer(1, 1_000_000);
        await Assert.That(AccountBalanceFilter.NotIs(tinybars).Value).IsEqualTo($"ne:{tinybars}");
    }

    [Test]
    public async Task AccountPublicKeyFilter_Is_Has_Correct_Name_And_Hex_Value()
    {
        var (pub, _) = Generator.KeyPair();
        var endorsement = new Endorsement(pub);
        var filter = AccountPublicKeyFilter.Is(endorsement);
        await Assert.That(filter.Name).IsEqualTo("account.publickey");
        await Assert.That(filter.Value).IsEqualTo(Hex.FromBytes(endorsement.ToBytes(KeyFormat.Mirror)));
    }

    [Test]
    public async Task BalanceProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(BalanceProjectionFilter.Include.Name).IsEqualTo("balance");
        await Assert.That(BalanceProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task BalanceProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(BalanceProjectionFilter.Exclude.Name).IsEqualTo("balance");
        await Assert.That(BalanceProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task BalanceProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(BalanceProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(BalanceProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task HbarTransferProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(HbarTransferProjectionFilter.Include.Name).IsEqualTo("hbar");
        await Assert.That(HbarTransferProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task HbarTransferProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(HbarTransferProjectionFilter.Exclude.Name).IsEqualTo("hbar");
        await Assert.That(HbarTransferProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task HbarTransferProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(HbarTransferProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(HbarTransferProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task InternalProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(InternalProjectionFilter.Include.Name).IsEqualTo("internal");
        await Assert.That(InternalProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task InternalProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(InternalProjectionFilter.Exclude.Name).IsEqualTo("internal");
        await Assert.That(InternalProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task InternalProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(InternalProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(InternalProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task TransactionIndexFilter_Is_Has_Correct_Name_And_Value()
    {
        var idx = Generator.Integer(0, 1000);
        var filter = TransactionIndexFilter.Is(idx);
        await Assert.That(filter.Name).IsEqualTo("transaction.index");
        await Assert.That(filter.Value).IsEqualTo(idx.ToString());
    }

    [Test]
    public async Task TransactionIndexFilter_Is_Rejects_Negative_Index()
    {
        await Assert.That(() => TransactionIndexFilter.Is(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ResultFilter_Success_Has_Correct_Name_And_Value()
    {
        await Assert.That(ResultFilter.Success.Name).IsEqualTo("result");
        await Assert.That(ResultFilter.Success.Value).IsEqualTo("success");
    }

    [Test]
    public async Task ResultFilter_Fail_Has_Correct_Value()
    {
        await Assert.That(ResultFilter.Fail.Name).IsEqualTo("result");
        await Assert.That(ResultFilter.Fail.Value).IsEqualTo("fail");
    }

    [Test]
    public async Task TransferDirectionFilter_Credit_Has_Correct_Name_And_Value()
    {
        await Assert.That(TransferDirectionFilter.Credit.Name).IsEqualTo("type");
        await Assert.That(TransferDirectionFilter.Credit.Value).IsEqualTo("credit");
    }

    [Test]
    public async Task TransferDirectionFilter_Debit_Has_Correct_Value()
    {
        await Assert.That(TransferDirectionFilter.Debit.Name).IsEqualTo("type");
        await Assert.That(TransferDirectionFilter.Debit.Value).IsEqualTo("debit");
    }

    [Test]
    public async Task TransactionTypeFilter_All_Properties_Target_TransactionType_Name()
    {
        // Spot-check a handful across the alphabetical range.
        await Assert.That(TransactionTypeFilter.AtomicBatch.Name).IsEqualTo("transactiontype");
        await Assert.That(TransactionTypeFilter.CryptoTransfer.Name).IsEqualTo("transactiontype");
        await Assert.That(TransactionTypeFilter.TokenMint.Name).IsEqualTo("transactiontype");
        await Assert.That(TransactionTypeFilter.UtilPrng.Name).IsEqualTo("transactiontype");
    }

    [Test]
    public async Task TransactionTypeFilter_Wire_Values_Match_HAPI_Enum()
    {
        // Spot-check the wire-string mapping — the conversion rule is
        // CamelCase C# identifier → ALLCAPS concatenated HAPI enum,
        // pinned per-property (no runtime reflection).
        await Assert.That(TransactionTypeFilter.AtomicBatch.Value).IsEqualTo("ATOMICBATCH");
        await Assert.That(TransactionTypeFilter.ContractCreateInstance.Value).IsEqualTo("CONTRACTCREATEINSTANCE");
        await Assert.That(TransactionTypeFilter.CryptoTransfer.Value).IsEqualTo("CRYPTOTRANSFER");
        await Assert.That(TransactionTypeFilter.TokenFeeScheduleUpdate.Value).IsEqualTo("TOKENFEESCHEDULEUPDATE");
        await Assert.That(TransactionTypeFilter.TokenMint.Value).IsEqualTo("TOKENMINT");
        await Assert.That(TransactionTypeFilter.UtilPrng.Value).IsEqualTo("UTILPRNG");
    }

    [Test]
    public async Task EvmTopicFilter_Index0_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = EvmTopicFilter.Is(0, topic);
        await Assert.That(filter.Name).IsEqualTo("topic0");
    }

    [Test]
    public async Task EvmTopicFilter_Index1_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = EvmTopicFilter.Is(1, topic);
        await Assert.That(filter.Name).IsEqualTo("topic1");
    }

    [Test]
    public async Task EvmTopicFilter_Index2_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = EvmTopicFilter.Is(2, topic);
        await Assert.That(filter.Name).IsEqualTo("topic2");
    }

    [Test]
    public async Task EvmTopicFilter_Index3_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = EvmTopicFilter.Is(3, topic);
        await Assert.That(filter.Name).IsEqualTo("topic3");
    }

    [Test]
    public async Task EvmTopicFilter_Has_Correct_Value_Padded_To_64_Hex_Chars()
    {
        var index = Generator.Integer(0, 3);
        var topic = new BigInteger(Generator.Integer(1, 1_000_000));
        var filter = EvmTopicFilter.Is(index, topic);
        var expected = "0x" + Hex.FromBytes(topic.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task EvmTopicFilter_Has_Correct_Value_For_Zero_Topic()
    {
        var filter = EvmTopicFilter.Is(0, BigInteger.Zero);
        await Assert.That(filter.Value).IsEqualTo("0x" + new string('0', 64));
    }

    [Test]
    public async Task EvmTopicFilter_Throws_For_Negative_Index()
    {
        var topic = new BigInteger(1);
        await Assert.That(() => EvmTopicFilter.Is(-1, topic)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EvmTopicFilter_Throws_For_Index_4()
    {
        var topic = new BigInteger(1);
        await Assert.That(() => EvmTopicFilter.Is(4, topic)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EvmTopicFilter_Does_Not_Throw_For_Valid_Indices()
    {
        var topic = new BigInteger(42);
        for (int index = 0; index <= 3; index++)
        {
            var filter = EvmTopicFilter.Is(index, topic);
            await Assert.That(filter.Name).IsEqualTo($"topic{index}");
        }
    }

    [Test]
    public async Task TransactionHashFilter_Name_Is_Transaction_Hash()
    {
        var evmHash = new EvmHash(new byte[32]);
        await Assert.That(TransactionHashFilter.Is(evmHash).Name).IsEqualTo("transaction.hash");
    }

    [Test]
    public async Task TransactionHashFilter_Is_EvmHash_Produces_64_Char_Hex()
    {
        var bytes = new byte[32];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i + 1);
        }
        var filter = TransactionHashFilter.Is(new EvmHash(bytes));
        await Assert.That(filter.Value).IsEqualTo("0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
    }

    [Test]
    public async Task TransactionHashFilter_Is_32_Byte_Memory_Produces_64_Char_Hex()
    {
        var bytes = new byte[32];
        bytes[0] = 0xde;
        bytes[1] = 0xad;
        bytes[2] = 0xbe;
        bytes[3] = 0xef;
        var filter = TransactionHashFilter.Is(new ReadOnlyMemory<byte>(bytes));
        await Assert.That(filter.Value).IsEqualTo("0xdeadbeef" + new string('0', 56));
        await Assert.That(filter.Value.Length).IsEqualTo(2 + 64);
    }

    [Test]
    public async Task TransactionHashFilter_Is_48_Byte_Memory_Produces_96_Char_Hex()
    {
        var bytes = new byte[48];
        Array.Fill(bytes, (byte)0xab);
        var filter = TransactionHashFilter.Is(new ReadOnlyMemory<byte>(bytes));
        await Assert.That(filter.Value).IsEqualTo("0x" + string.Concat(Enumerable.Repeat("ab", 48)));
        await Assert.That(filter.Value.Length).IsEqualTo(2 + 96);
    }

    [Test]
    public async Task TransactionHashFilter_Throws_On_Unsupported_Length()
    {
        ReadOnlyMemory<byte> tooShort = new byte[31];
        ReadOnlyMemory<byte> inBetween = new byte[40];
        ReadOnlyMemory<byte> tooLong = new byte[64];
        await Assert.That(() => TransactionHashFilter.Is(tooShort)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => TransactionHashFilter.Is(inBetween)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => TransactionHashFilter.Is(tooLong)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task BlockHashFilter_Name_Is_Block_Hash()
    {
        var evmHash = new EvmHash(new byte[32]);
        await Assert.That(BlockHashFilter.Is(evmHash).Name).IsEqualTo("block.hash");
    }

    [Test]
    public async Task BlockHashFilter_Is_EvmHash_Produces_64_Char_Hex()
    {
        var bytes = new byte[32];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i + 1);
        }
        var filter = BlockHashFilter.Is(new EvmHash(bytes));
        await Assert.That(filter.Value).IsEqualTo("0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
    }

    [Test]
    public async Task BlockHashFilter_Is_32_Byte_Memory_Produces_64_Char_Hex()
    {
        var bytes = new byte[32];
        bytes[0] = 0xde;
        bytes[1] = 0xad;
        bytes[2] = 0xbe;
        bytes[3] = 0xef;
        var filter = BlockHashFilter.Is(new ReadOnlyMemory<byte>(bytes));
        await Assert.That(filter.Value).IsEqualTo("0xdeadbeef" + new string('0', 56));
        await Assert.That(filter.Value.Length).IsEqualTo(2 + 64);
    }

    [Test]
    public async Task BlockHashFilter_Is_48_Byte_Memory_Produces_96_Char_Hex()
    {
        var bytes = new byte[48];
        Array.Fill(bytes, (byte)0xab);
        var filter = BlockHashFilter.Is(new ReadOnlyMemory<byte>(bytes));
        await Assert.That(filter.Value).IsEqualTo("0x" + string.Concat(Enumerable.Repeat("ab", 48)));
        await Assert.That(filter.Value.Length).IsEqualTo(2 + 96);
    }

    [Test]
    public async Task BlockHashFilter_Throws_On_Unsupported_Length()
    {
        ReadOnlyMemory<byte> tooShort = new byte[31];
        ReadOnlyMemory<byte> inBetween = new byte[40];
        ReadOnlyMemory<byte> tooLong = new byte[64];
        await Assert.That(() => BlockHashFilter.Is(tooShort)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => BlockHashFilter.Is(inBetween)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => BlockHashFilter.Is(tooLong)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task MessageEncodingProjectionFilter_Base64_Has_Correct_Name_And_Value()
    {
        await Assert.That(MessageEncodingProjectionFilter.Base64.Name).IsEqualTo("encoding");
        await Assert.That(MessageEncodingProjectionFilter.Base64.Value).IsEqualTo("base64");
    }

    [Test]
    public async Task MessageEncodingProjectionFilter_Utf8_Has_Correct_Value()
    {
        await Assert.That(MessageEncodingProjectionFilter.Utf8.Name).IsEqualTo("encoding");
        await Assert.That(MessageEncodingProjectionFilter.Utf8.Value).IsEqualTo("utf-8");
    }

    [Test]
    public async Task MessageEncodingProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(MessageEncodingProjectionFilter.Base64 is IMirrorProjection).IsTrue();
        await Assert.That(MessageEncodingProjectionFilter.Utf8 is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task OpcodeStackProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(OpcodeStackProjectionFilter.Include.Name).IsEqualTo("stack");
        await Assert.That(OpcodeStackProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task OpcodeStackProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(OpcodeStackProjectionFilter.Exclude.Name).IsEqualTo("stack");
        await Assert.That(OpcodeStackProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task OpcodeStackProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(OpcodeStackProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(OpcodeStackProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task OpcodeMemoryProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(OpcodeMemoryProjectionFilter.Include.Name).IsEqualTo("memory");
        await Assert.That(OpcodeMemoryProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task OpcodeMemoryProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(OpcodeMemoryProjectionFilter.Exclude.Name).IsEqualTo("memory");
        await Assert.That(OpcodeMemoryProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task OpcodeMemoryProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(OpcodeMemoryProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(OpcodeMemoryProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task OpcodeStorageProjectionFilter_Include_Has_Correct_Name_And_Value()
    {
        await Assert.That(OpcodeStorageProjectionFilter.Include.Name).IsEqualTo("storage");
        await Assert.That(OpcodeStorageProjectionFilter.Include.Value).IsEqualTo("true");
    }

    [Test]
    public async Task OpcodeStorageProjectionFilter_Exclude_Has_Correct_Value()
    {
        await Assert.That(OpcodeStorageProjectionFilter.Exclude.Name).IsEqualTo("storage");
        await Assert.That(OpcodeStorageProjectionFilter.Exclude.Value).IsEqualTo("false");
    }

    [Test]
    public async Task OpcodeStorageProjectionFilter_Implements_IMirrorProjection()
    {
        await Assert.That(OpcodeStorageProjectionFilter.Include is IMirrorProjection).IsTrue();
        await Assert.That(OpcodeStorageProjectionFilter.Exclude is IMirrorProjection).IsTrue();
    }

    [Test]
    public async Task ContractActionIndexFilter_Is_Has_Correct_Name_And_Value()
    {
        var f = ContractActionIndexFilter.Is(7);
        await Assert.That(f.Name).IsEqualTo("index");
        await Assert.That(f.Value).IsEqualTo("7");
    }

    [Test]
    public async Task ContractActionIndexFilter_After_Uses_Gt_Prefix()
    {
        await Assert.That(ContractActionIndexFilter.After(3).Value).IsEqualTo("gt:3");
    }

    [Test]
    public async Task ContractActionIndexFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        await Assert.That(ContractActionIndexFilter.OnOrAfter(3).Value).IsEqualTo("gte:3");
    }

    [Test]
    public async Task ContractActionIndexFilter_Before_Uses_Lt_Prefix()
    {
        await Assert.That(ContractActionIndexFilter.Before(3).Value).IsEqualTo("lt:3");
    }

    [Test]
    public async Task ContractActionIndexFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        await Assert.That(ContractActionIndexFilter.OnOrBefore(3).Value).IsEqualTo("lte:3");
    }

    [Test]
    public async Task ContractActionIndexFilter_NotIs_Uses_Ne_Prefix()
    {
        await Assert.That(ContractActionIndexFilter.NotIs(3).Value).IsEqualTo("ne:3");
    }

    [Test]
    public async Task ContractActionIndexFilter_Throws_On_Negative_Index()
    {
        await Assert.That(() => ContractActionIndexFilter.Is(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => ContractActionIndexFilter.After(-5)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ContractLogIndexFilter_Is_Has_Correct_Name_And_Value()
    {
        var f = ContractLogIndexFilter.Is(7);
        await Assert.That(f.Name).IsEqualTo("index");
        await Assert.That(f.Value).IsEqualTo("7");
    }

    [Test]
    public async Task ContractLogIndexFilter_After_Uses_Gt_Prefix()
    {
        await Assert.That(ContractLogIndexFilter.After(3).Value).IsEqualTo("gt:3");
    }

    [Test]
    public async Task ContractLogIndexFilter_OnOrAfter_Uses_Gte_Prefix()
    {
        await Assert.That(ContractLogIndexFilter.OnOrAfter(3).Value).IsEqualTo("gte:3");
    }

    [Test]
    public async Task ContractLogIndexFilter_Before_Uses_Lt_Prefix()
    {
        await Assert.That(ContractLogIndexFilter.Before(3).Value).IsEqualTo("lt:3");
    }

    [Test]
    public async Task ContractLogIndexFilter_OnOrBefore_Uses_Lte_Prefix()
    {
        await Assert.That(ContractLogIndexFilter.OnOrBefore(3).Value).IsEqualTo("lte:3");
    }

    [Test]
    public async Task ContractLogIndexFilter_Throws_On_Negative_Index()
    {
        await Assert.That(() => ContractLogIndexFilter.Is(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => ContractLogIndexFilter.After(-5)).Throws<ArgumentOutOfRangeException>();
    }
}
