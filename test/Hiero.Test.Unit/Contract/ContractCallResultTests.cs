// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Unit.Contract;

public class ContractCallResultTests
{
    [Test]
    public async Task Constructor_Materializes_Events_Topics_And_Nonces()
    {
        var bloom = new byte[] { 1, 2, 3 };
        var resultData = new byte[] { 4, 5, 6 };
        var input = new byte[] { 7, 8, 9 };
        var eventBloom = new byte[] { 10, 11 };
        var topic1 = new byte[] { 12, 13 };
        var topic2 = new byte[] { 14, 15 };
        var eventData = new byte[] { 16, 17, 18 };

        var log = new ContractLoginfo
        {
            ContractID = new ContractID { ShardNum = 1, RealmNum = 2, ContractNum = 3 },
            Bloom = ByteString.CopyFrom(eventBloom),
            Data = ByteString.CopyFrom(eventData)
        };
        log.Topic.Add(ByteString.CopyFrom(topic1));
        log.Topic.Add(ByteString.CopyFrom(topic2));

        var functionResult = new ContractFunctionResult
        {
            ContractID = new ContractID { ShardNum = 1, RealmNum = 2, ContractNum = 3 },
            ContractCallResult = ByteString.CopyFrom(resultData),
            Bloom = ByteString.CopyFrom(bloom),
            GasUsed = 123,
            Gas = 456,
            Amount = 789,
            SenderId = new AccountID { ShardNum = 1, RealmNum = 2, AccountNum = 4 },
            FunctionParameters = ByteString.CopyFrom(input)
        };
        functionResult.LogInfo.Add(log);
        functionResult.ContractNonces.Add(new ContractNonceInfo
        {
            ContractId = new ContractID { ShardNum = 1, RealmNum = 2, ContractNum = 5 },
            Nonce = 6
        });

        var callResult = new ContractCallResult(functionResult);

        await Assert.That(callResult.Contract).IsEqualTo(new EntityId(1, 2, 3));
        await Assert.That(callResult.Result.Data.ToArray().SequenceEqual(resultData)).IsTrue();
        await Assert.That(callResult.Bloom.ToArray().SequenceEqual(bloom)).IsTrue();
        await Assert.That(callResult.Input.Data.ToArray().SequenceEqual(input)).IsTrue();
        await Assert.That(callResult.Events).Count().IsEqualTo(1);
        await Assert.That(callResult.Events[0].Contract).IsEqualTo(new EntityId(1, 2, 3));
        await Assert.That(callResult.Events[0].Bloom.ToArray().SequenceEqual(eventBloom)).IsTrue();
        await Assert.That(callResult.Events[0].Topics).Count().IsEqualTo(2);
        await Assert.That(callResult.Events[0].Topics[0].ToArray().SequenceEqual(topic1)).IsTrue();
        await Assert.That(callResult.Events[0].Topics[1].ToArray().SequenceEqual(topic2)).IsTrue();
        await Assert.That(callResult.Events[0].Data.Data.ToArray().SequenceEqual(eventData)).IsTrue();
        await Assert.That(callResult.Nonces[new EntityId(1, 2, 5)]).IsEqualTo(6);
    }
}
