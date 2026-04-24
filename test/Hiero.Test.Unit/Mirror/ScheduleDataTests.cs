// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class ScheduleDataTests
{
    [Test]
    public async Task Deserializes_Full_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "admin_key":null,
            "consensus_timestamp":"1234567890.000000001",
            "creator_account_id":"0.0.10",
            "deleted":false,
            "executed_timestamp":"1234567900.000000001",
            "expiration_time":"1234567999.000000000",
            "memo":"created on 02/10/2021",
            "payer_account_id":"0.0.11",
            "schedule_id":"0.0.200",
            "transaction_body":"Kd6tvu8=",
            "wait_for_expiry":false
        }
        """;
        var data = JsonSerializer.Deserialize<ScheduleData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Administrator).IsNull();
        await Assert.That(data.Created).IsEqualTo(new ConsensusTimeStamp(1_234_567_890L, 1));
        await Assert.That(data.Creator).IsEqualTo(new EntityId(0, 0, 10));
        await Assert.That(data.Deleted).IsFalse();
        await Assert.That(data.Executed).IsEqualTo(new ConsensusTimeStamp(1_234_567_900L, 1));
        await Assert.That(data.Expiration).IsEqualTo(new ConsensusTimeStamp(1_234_567_999L, 0));
        await Assert.That(data.Memo).IsEqualTo("created on 02/10/2021");
        await Assert.That(data.Payer).IsEqualTo(new EntityId(0, 0, 11));
        await Assert.That(data.Schedule).IsEqualTo(new EntityId(0, 0, 200));
        await Assert.That(data.TransactionBody.Length).IsEqualTo(5);
        await Assert.That(data.DelayExecution).IsFalse();
    }

    [Test]
    public async Task Unexecuted_Schedule_Has_Null_Execution_Timestamp()
    {
        var json = """
        {
            "consensus_timestamp":"1700000000.000000001",
            "creator_account_id":"0.0.10",
            "deleted":false,
            "executed_timestamp":null,
            "expiration_time":null,
            "memo":null,
            "payer_account_id":"0.0.11",
            "schedule_id":"0.0.200",
            "transaction_body":"Kd6tvu8=",
            "wait_for_expiry":true
        }
        """;
        var data = JsonSerializer.Deserialize<ScheduleData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Executed).IsNull();
        await Assert.That(data.Expiration).IsNull();
        await Assert.That(data.Memo).IsNull();
        await Assert.That(data.DelayExecution).IsTrue();
    }

    [Test]
    public async Task Deserializes_Nested_Signatures_Array()
    {
        var json = """
        {
            "consensus_timestamp":"1234567890.000000001",
            "creator_account_id":"0.0.10",
            "deleted":false,
            "payer_account_id":"0.0.11",
            "schedule_id":"0.0.200",
            "signatures":[
                {"consensus_timestamp":"1234567891.000000002","public_key_prefix":"AAEBAwuqAwzB","signature":"3q2+7wABAQMLqgMMwQ==","type":"ED25519"},
                {"consensus_timestamp":"1234567892.000000003","public_key_prefix":"AAEBAwuqAwzC","signature":"3q2+7wABAQMLqgMMwg==","type":"ECDSA_SECP256K1"}
            ],
            "transaction_body":"Kd6tvu8=",
            "wait_for_expiry":false
        }
        """;
        var data = JsonSerializer.Deserialize<ScheduleData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Signatures).IsNotNull();
        await Assert.That(data.Signatures!.Length).IsEqualTo(2);
        await Assert.That(data.Signatures[0].KeyType).IsEqualTo("ED25519");
        await Assert.That(data.Signatures[0].Consensus).IsEqualTo(new ConsensusTimeStamp(1_234_567_891L, 2));
        await Assert.That(data.Signatures[0].PublicKeyPrefix.Length).IsGreaterThan(0);
        await Assert.That(data.Signatures[0].Signature.Length).IsGreaterThan(0);
        await Assert.That(data.Signatures[1].KeyType).IsEqualTo("ECDSA_SECP256K1");
    }

    [Test]
    public async Task Page_Envelope_Reads_Schedules_Array()
    {
        var json = """
        {
            "schedules":[
                {"consensus_timestamp":"1700000000.000000001","creator_account_id":"0.0.10","deleted":false,"payer_account_id":"0.0.11","schedule_id":"0.0.200","transaction_body":"Kd6tvu8=","wait_for_expiry":false},
                {"consensus_timestamp":"1700000001.000000002","creator_account_id":"0.0.10","deleted":true,"payer_account_id":"0.0.12","schedule_id":"0.0.201","transaction_body":"Kd6tvu8=","wait_for_expiry":false}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.ScheduleDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].Schedule).IsEqualTo(new EntityId(0, 0, 200));
        await Assert.That(items[0].Deleted).IsFalse();
        await Assert.That(items[1].Schedule).IsEqualTo(new EntityId(0, 0, 201));
        await Assert.That(items[1].Deleted).IsTrue();
    }
}
