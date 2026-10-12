// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class TransactionDataTests
{
    [Test]
    public async Task Status_Deserializes_To_A_ResponseCode()
    {
        // The mirror reports this as the "result" field.  It used to surface as
        // a raw string, so the same concept read two different ways depending
        // on which client produced the object.
        var json = """{"result":"SUCCESS","nonce":0}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Failure_Status_Deserializes_To_Matching_Code()
    {
        var json = """{"result":"INSUFFICIENT_TX_FEE"}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data!.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
    }

    [Test]
    public async Task Unknown_Status_Does_Not_Fail_The_Surrounding_Row()
    {
        // The network gains response codes over time.  A row carrying one this
        // build predates must still deserialize — losing a whole page of mirror
        // results to an unrecognised status would be a poor trade.
        var json = """{"result":"SOME_FUTURE_STATUS_CODE","nonce":3}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Status).IsEqualTo(ResponseCode.Unrecognized);
        await Assert.That(data.Nonce).IsEqualTo(3L);
    }

    [Test]
    public async Task Missing_Status_Reads_As_Ok_Not_Unrecognized()
    {
        // Worth pinning: an absent "result" leaves the property at its default,
        // which is Ok (0) — the converter never runs.  That is distinct from a
        // status that was present but unreadable.
        var json = """{"nonce":1}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data!.Status).IsEqualTo(ResponseCode.Ok);
    }

    [Test]
    public async Task Inner_Batch_Transaction_Reads_Its_Batch_Key()
    {
        // An inner transaction of a mainnet atomic batch, as served by mirror
        // node v0.164 (transfers trimmed). Read through the source-generated
        // context the client itself uses.
        var json = """
        {
            "transactions": [
                {
                    "batch_key": { "_type": "ED25519", "key": "cb16747add8c887e598334e543437ecd4a4d103b0609640f470b8068851b1070" },
                    "bytes": null,
                    "charged_tx_fee": 6529140,
                    "consensus_timestamp": "1791474650.109033825",
                    "entity_id": "0.0.1456985",
                    "high_volume": false,
                    "high_volume_pricing_multiplier": 0,
                    "max_fee": "200000000",
                    "max_custom_fees": [],
                    "memo_base64": "",
                    "name": "CONTRACTCALL",
                    "nft_transfers": [],
                    "node": null,
                    "nonce": 0,
                    "parent_consensus_timestamp": "1791474650.109033823",
                    "result": "SUCCESS",
                    "scheduled": false,
                    "staking_reward_transfers": [],
                    "token_transfers": [],
                    "transaction_hash": "kpLUGzRtBtqCqCP9SXKe/oJk830wCEVCQC2OQZlv8RFMbfXzlW7Wrwheuupg1Enp",
                    "transaction_id": "0.0.4807807-1791474643-479639200",
                    "transfers": [
                        { "account": "0.0.802", "amount": 6529140, "is_approval": false },
                        { "account": "0.0.1456985", "amount": -2000000000000, "is_approval": false }
                    ],
                    "valid_duration_seconds": "120",
                    "valid_start_timestamp": "1791474643.479639200"
                }
            ]
        }
        """;
        var data = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.TransactionDetailByIdResponse)!.Transactions![0];

        var expectedKey = new Endorsement(KeyType.Ed25519, Convert.FromHexString("cb16747add8c887e598334e543437ecd4a4d103b0609640f470b8068851b1070"));
        await Assert.That(data.BatchEndorsement).IsEqualTo(expectedKey);
        await Assert.That(data.ParentConsensus).IsEqualTo(new ConsensusTimeStamp(1791474650, 109033823));
        await Assert.That(data.HighVolume).IsFalse();
        await Assert.That(data.HighVolumePricingMultiplier).IsEqualTo(0L);
        await Assert.That(data.MaxCustomFees!.Length).IsEqualTo(0);
        await Assert.That(data.AssetTransfers!.Length).IsEqualTo(0);
        await Assert.That(data.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Batch_Key_And_Pricing_Multiplier_Are_Null_When_Absent()
    {
        // The outer batch and ordinary transactions report a null batch key;
        // transactions predating HIP-1313 carry no pricing multiplier.
        var json = """{"batch_key":null,"high_volume_pricing_multiplier":null,"name":"ATOMICBATCH","nonce":0}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data!.BatchEndorsement).IsNull();
        await Assert.That(data.HighVolumePricingMultiplier).IsNull();

        var bare = JsonSerializer.Deserialize<TransactionData>("""{"nonce":0}""");
        await Assert.That(bare!.BatchEndorsement).IsNull();
        await Assert.That(bare.HighVolumePricingMultiplier).IsNull();
    }

    [Test]
    public async Task High_Volume_Transaction_Reads_Its_Multiplier()
    {
        var json = """{"high_volume":true,"high_volume_pricing_multiplier":4000}""";
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data!.HighVolume).IsTrue();
        await Assert.That(data.HighVolumePricingMultiplier).IsEqualTo(4000L);
    }

    [Test]
    public async Task Max_Custom_Fees_Read_Payer_Amount_And_Denomination()
    {
        var json = """
        {
            "max_custom_fees": [
                { "account_id": "0.0.1001", "amount": 100, "denominating_token_id": null },
                { "account_id": "0.0.1001", "amount": 2500, "denominating_token_id": "0.0.2002" }
            ]
        }
        """;
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        var limits = data!.MaxCustomFees!;
        await Assert.That(limits.Length).IsEqualTo(2);
        await Assert.That(limits[0].Account).IsEqualTo(new EntityId(0, 0, 1001));
        await Assert.That(limits[0].Amount).IsEqualTo(100L);
        await Assert.That(limits[0].Token).IsNull();
        await Assert.That(limits[1].Amount).IsEqualTo(2500L);
        await Assert.That(limits[1].Token).IsEqualTo(new EntityId(0, 0, 2002));
    }

    [Test]
    public async Task List_Rows_Carry_Nft_Transfers()
    {
        // The transaction list endpoint reports NFT transfers too, so they
        // are read on the base type, not only on the detail subclass.
        var json = """
        {
            "transactions": [
                {
                    "name": "CRYPTOTRANSFER",
                    "nft_transfers": [
                        { "is_approval": true, "receiver_account_id": "0.0.3003", "sender_account_id": "0.0.4004", "serial_number": 7, "token_id": "0.0.5005" }
                    ]
                }
            ],
            "links": { "next": null }
        }
        """;
        var page = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.TransactionDataPage);
        var transfer = page!.GetItems()[0].AssetTransfers![0];
        await Assert.That(transfer.Token).IsEqualTo(new EntityId(0, 0, 5005));
        await Assert.That(transfer.SerialNumber).IsEqualTo(7L);
        await Assert.That(transfer.Sender).IsEqualTo(new EntityId(0, 0, 4004));
        await Assert.That(transfer.Receiver).IsEqualTo(new EntityId(0, 0, 3003));
        await Assert.That(transfer.IsAllowance).IsTrue();
    }
}
