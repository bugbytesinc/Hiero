// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class TopicDataTests
{
    private const string Ed25519Key = "cb16747add8c887e598334e543437ecd4a4d103b0609640f470b8068851b1070";
    private const string EcdsaKey = "02ce1f1664d33ee3b91321ef318693ec1da5fa6012ec76823ee82bcb689788978a";

    [Test]
    public async Task Topic_Reads_Its_Hip991_Royalties()
    {
        var json = $$"""
        {
            "admin_key": null,
            "auto_renew_account": "0.0.10938174",
            "auto_renew_period": 7776000,
            "created_timestamp": "1791480886.510917639",
            "custom_fees": {
                "created_timestamp": "1791480886.510917639",
                "fixed_fees": [
                    { "amount": 100, "collector_account_id": "0.0.1001", "denominating_token_id": null },
                    { "amount": 7, "collector_account_id": "0.0.1001", "denominating_token_id": "0.0.2002" }
                ]
            },
            "deleted": false,
            "fee_exempt_key_list": [
                { "_type": "ED25519", "key": "{{Ed25519Key}}" },
                { "_type": "ECDSA_SECP256K1", "key": "{{EcdsaKey}}" }
            ],
            "fee_schedule_key": { "_type": "ED25519", "key": "{{Ed25519Key}}" },
            "memo": "fees",
            "submit_key": null,
            "timestamp": { "from": "1791480886.510917639", "to": null },
            "topic_id": "0.0.10938175"
        }
        """;
        var topic = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.TopicData)!;

        var royalties = topic.Royalties!;
        await Assert.That(royalties.Created).IsEqualTo(new ConsensusTimeStamp(1791480886, 510917639));
        await Assert.That(royalties.FixedFees.Length).IsEqualTo(2);
        await Assert.That(royalties.FixedFees[0].Amount).IsEqualTo(100L);
        await Assert.That(royalties.FixedFees[0].Collector).IsEqualTo(new EntityId(0, 0, 1001));
        await Assert.That(royalties.FixedFees[0].FeeToken).IsNull();
        await Assert.That(royalties.FixedFees[1].FeeToken).IsEqualTo(new EntityId(0, 0, 2002));
        // A topic has fixed fees only; the other lists read as empty, not null.
        await Assert.That(royalties.FractionalFees.Length).IsEqualTo(0);
        await Assert.That(royalties.RoyaltyFees.Length).IsEqualTo(0);

        var ed25519 = new Endorsement(KeyType.Ed25519, Convert.FromHexString(Ed25519Key));
        await Assert.That(topic.RoyaltiesEndorsement).IsEqualTo(ed25519);
        await Assert.That(topic.RoyaltyExemptEndorsements.Length).IsEqualTo(2);
        await Assert.That(topic.RoyaltyExemptEndorsements[0]).IsEqualTo(ed25519);
        await Assert.That(topic.RoyaltyExemptEndorsements[1]).IsEqualTo(new Endorsement(KeyType.ECDSASecp256K1, Convert.FromHexString(EcdsaKey)));
    }

    [Test]
    public async Task Topic_Without_Royalties()
    {
        // As served by testnet for a topic created with no custom fees.
        var json = """
        {
            "custom_fees": { "created_timestamp": "1791480886.510917639", "fixed_fees": [] },
            "fee_exempt_key_list": [],
            "fee_schedule_key": null,
            "topic_id": "0.0.10938175"
        }
        """;
        var topic = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.TopicData)!;
        await Assert.That(topic.Royalties!.FixedFees.Length).IsEqualTo(0);
        await Assert.That(topic.RoyaltiesEndorsement).IsNull();
        await Assert.That(topic.RoyaltyExemptEndorsements.Length).IsEqualTo(0);

        // Mirror nodes predating HIP-991 omit the fields entirely.
        var older = JsonSerializer.Deserialize("""{"topic_id":"0.0.1"}""", MirrorJsonContext.Default.TopicData)!;
        await Assert.That(older.Royalties).IsNull();
        await Assert.That(older.RoyaltiesEndorsement).IsNull();
        await Assert.That(older.RoyaltyExemptEndorsements.Length).IsEqualTo(0);
    }
}
