// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Proto;

namespace Hiero.Test.Unit.Consensus;

public class TopicInfoTests
{
    [Test]
    public async Task Constructor_Maps_Topic_Info_Response()
    {
        var runningHash = new byte[] { 0x01, 0x02, 0x03 };
        var ledgerId = new byte[] { 0x00, 0x01 };
        var response = new Response
        {
            ConsensusGetTopicInfo = new ConsensusGetTopicInfoResponse
            {
                TopicInfo = new ConsensusTopicInfo
                {
                    Memo = "topic memo",
                    RunningHash = ByteString.CopyFrom(runningHash),
                    SequenceNumber = 7,
                    ExpirationTime = new Timestamp { Seconds = 1000, Nanos = 1 },
                    AutoRenewPeriod = new Duration { Seconds = 3600 },
                    AutoRenewAccount = new AccountID(new EntityId(0, 0, 1001)),
                    LedgerId = ByteString.CopyFrom(ledgerId)
                }
            }
        };

        var result = new TopicInfo(response);

        await Assert.That(result.Memo).IsEqualTo("topic memo");
        await Assert.That(result.RunningHash.Span.SequenceEqual(runningHash)).IsTrue();
        await Assert.That(result.SequenceNumber).IsEqualTo(7UL);
        await Assert.That(result.Expiration.Seconds).IsEqualTo(1000.000000001m);
        await Assert.That(result.AutoRenewPeriod).IsEqualTo(TimeSpan.FromSeconds(3600));
        await Assert.That(result.RenewAccount).IsEqualTo(new EntityId(0, 0, 1001));
        await Assert.That(result.Ledger).IsEqualTo(new System.Numerics.BigInteger(ledgerId, true, true));
    }
}
