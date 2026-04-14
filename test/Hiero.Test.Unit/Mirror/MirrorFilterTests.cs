// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Test.Helpers;
using System.Numerics;

namespace Hiero.Test.Unit.Mirror;

public class MirrorFilterTests
{
    [Test]
    public async Task LimitFilter_Has_Correct_Name()
    {
        var limit = Generator.Integer(1, 100);
        var filter = new LimitFilter(limit);
        await Assert.That(filter.Name).IsEqualTo("limit");
    }

    [Test]
    public async Task LimitFilter_Has_Correct_Value()
    {
        var limit = Generator.Integer(1, 100);
        var filter = new LimitFilter(limit);
        await Assert.That(filter.Value).IsEqualTo(limit.ToString());
    }

    [Test]
    public async Task OrderByFilter_Ascending_Has_Correct_Name()
    {
        var filter = OrderByFilter.Ascending;
        await Assert.That(filter.Name).IsEqualTo("order");
    }

    [Test]
    public async Task OrderByFilter_Ascending_Has_Correct_Value()
    {
        var filter = OrderByFilter.Ascending;
        await Assert.That(filter.Value).IsEqualTo("asc");
    }

    [Test]
    public async Task OrderByFilter_Descending_Has_Correct_Name()
    {
        var filter = OrderByFilter.Descending;
        await Assert.That(filter.Name).IsEqualTo("order");
    }

    [Test]
    public async Task OrderByFilter_Descending_Has_Correct_Value()
    {
        var filter = OrderByFilter.Descending;
        await Assert.That(filter.Value).IsEqualTo("desc");
    }

    [Test]
    public async Task TimestampAfterFilter_Has_Correct_Name()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 123456789);
        var filter = new TimestampAfterFilter(timestamp);
        await Assert.That(filter.Name).IsEqualTo("timestamp");
    }

    [Test]
    public async Task TimestampAfterFilter_Has_Correct_Value()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 123456789);
        var filter = new TimestampAfterFilter(timestamp);
        await Assert.That(filter.Value).IsEqualTo($"gt:{timestamp}");
    }

    [Test]
    public async Task TimestampEqualsFilter_Has_Correct_Name()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 0);
        var filter = new TimestampEqualsFilter(timestamp);
        await Assert.That(filter.Name).IsEqualTo("timestamp");
    }

    [Test]
    public async Task TimestampEqualsFilter_Has_Correct_Value()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 0);
        var filter = new TimestampEqualsFilter(timestamp);
        await Assert.That(filter.Value).IsEqualTo(timestamp.ToString());
    }

    [Test]
    public async Task TimestampOnOrBeforeFilter_Has_Correct_Name()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 999999999);
        var filter = new TimestampOnOrBeforeFilter(timestamp);
        await Assert.That(filter.Name).IsEqualTo("timestamp");
    }

    [Test]
    public async Task TimestampOnOrBeforeFilter_Has_Correct_Value()
    {
        var timestamp = new ConsensusTimeStamp(1_700_000_000L, 999999999);
        var filter = new TimestampOnOrBeforeFilter(timestamp);
        await Assert.That(filter.Value).IsEqualTo($"lte:{timestamp}");
    }

    [Test]
    public async Task AccountIsFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new AccountIsFilter(entityId);
        await Assert.That(filter.Name).IsEqualTo("account.id");
    }

    [Test]
    public async Task AccountIsFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new AccountIsFilter(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task ContractIsFilter_Has_Correct_Name()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var filter = new ContractIsFilter(evmAddress);
        await Assert.That(filter.Name).IsEqualTo("from");
    }

    [Test]
    public async Task ContractIsFilter_Has_Correct_Value()
    {
        var bytes = Generator.KeyPair().publicKey[^20..];
        var evmAddress = new EvmAddress(bytes);
        var filter = new ContractIsFilter(evmAddress);
        var expected = $"0x{Hex.FromBytes(evmAddress.Bytes)}";
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task TokenIsFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new TokenIsFilter(entityId);
        await Assert.That(filter.Name).IsEqualTo("token.id");
    }

    [Test]
    public async Task TokenIsFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new TokenIsFilter(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task SpenderIsFilter_Has_Correct_Name()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new SpenderIsFilter(entityId);
        await Assert.That(filter.Name).IsEqualTo("spender.id");
    }

    [Test]
    public async Task SpenderIsFilter_Has_Correct_Value()
    {
        var shard = Generator.Integer(0, 10);
        var realm = Generator.Integer(0, 10);
        var num = Generator.Integer(1, 1000);
        var entityId = new EntityId(shard, realm, num);
        var filter = new SpenderIsFilter(entityId);
        await Assert.That(filter.Value).IsEqualTo(entityId.ToString());
    }

    [Test]
    public async Task SlotIsFilter_Has_Correct_Name()
    {
        var slot = new BigInteger(Generator.Integer(0, 1_000_000));
        var filter = new SlotIsFilter(slot);
        await Assert.That(filter.Name).IsEqualTo("slot");
    }

    [Test]
    public async Task SlotIsFilter_Has_Correct_Value_For_Zero()
    {
        var filter = new SlotIsFilter(BigInteger.Zero);
        await Assert.That(filter.Value).IsEqualTo("0x" + new string('0', 64));
    }

    [Test]
    public async Task SlotIsFilter_Has_Correct_Value_Padded_To_64_Hex_Chars()
    {
        var slot = new BigInteger(1);
        var filter = new SlotIsFilter(slot);
        var expected = "0x" + Hex.FromBytes(slot.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task SlotIsFilter_Has_Correct_Value_For_Large_Slot()
    {
        var slot = new BigInteger(Generator.Integer(1, 1_000_000));
        var filter = new SlotIsFilter(slot);
        var expected = "0x" + Hex.FromBytes(slot.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task SequenceAfterFilter_Has_Correct_Name()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        var filter = new SequenceAfterFilter(seq);
        await Assert.That(filter.Name).IsEqualTo("sequencenumber");
    }

    [Test]
    public async Task SequenceAfterFilter_Has_Correct_Value()
    {
        var seq = (ulong)Generator.Integer(1, 10000);
        var filter = new SequenceAfterFilter(seq);
        await Assert.That(filter.Value).IsEqualTo($"gt:{seq}");
    }

    [Test]
    public async Task TopicFilter_Index0_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = new TopicFilter(0, topic);
        await Assert.That(filter.Name).IsEqualTo("topic0");
    }

    [Test]
    public async Task TopicFilter_Index1_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = new TopicFilter(1, topic);
        await Assert.That(filter.Name).IsEqualTo("topic1");
    }

    [Test]
    public async Task TopicFilter_Index2_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = new TopicFilter(2, topic);
        await Assert.That(filter.Name).IsEqualTo("topic2");
    }

    [Test]
    public async Task TopicFilter_Index3_Has_Correct_Name()
    {
        var topic = new BigInteger(Generator.Integer(1, 100000));
        var filter = new TopicFilter(3, topic);
        await Assert.That(filter.Name).IsEqualTo("topic3");
    }

    [Test]
    public async Task TopicFilter_Has_Correct_Value_Padded_To_64_Hex_Chars()
    {
        var index = Generator.Integer(0, 3);
        var topic = new BigInteger(Generator.Integer(1, 1_000_000));
        var filter = new TopicFilter(index, topic);
        var expected = "0x" + Hex.FromBytes(topic.ToByteArray(true, true)).PadLeft(64, '0');
        await Assert.That(filter.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task TopicFilter_Has_Correct_Value_For_Zero_Topic()
    {
        var filter = new TopicFilter(0, BigInteger.Zero);
        await Assert.That(filter.Value).IsEqualTo("0x" + new string('0', 64));
    }

    [Test]
    public async Task TopicFilter_Throws_For_Negative_Index()
    {
        var topic = new BigInteger(1);
        await Assert.That(() => new TopicFilter(-1, topic)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TopicFilter_Throws_For_Index_4()
    {
        var topic = new BigInteger(1);
        await Assert.That(() => new TopicFilter(4, topic)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TopicFilter_Does_Not_Throw_For_Valid_Indices()
    {
        var topic = new BigInteger(42);
        for (int index = 0; index <= 3; index++)
        {
            var filter = new TopicFilter(index, topic);
            await Assert.That(filter.Name).IsEqualTo($"topic{index}");
        }
    }
}
