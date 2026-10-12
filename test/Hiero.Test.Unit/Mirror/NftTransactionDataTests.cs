// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class NftTransactionDataTests
{
    [Test]
    public async Task Deserializes_Full_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "consensus_timestamp":"1618591023.997420021",
            "is_approval":false,
            "nonce":0,
            "receiver_account_id":"0.0.11",
            "sender_account_id":"0.0.10",
            "transaction_id":"0.0.19789-1618591023-997420021",
            "type":"CRYPTOTRANSFER"
        }
        """;
        var data = JsonSerializer.Deserialize<NftTransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Consensus).IsEqualTo(new ConsensusTimeStamp(1_618_591_023L, 997_420_021));
        await Assert.That(data.IsApproval).IsFalse();
        await Assert.That(data.Nonce).IsEqualTo(0L);
        await Assert.That(data.Receiver).IsEqualTo(new EntityId(0, 0, 11));
        await Assert.That(data.Sender).IsEqualTo(new EntityId(0, 0, 10));
        await Assert.That(data.TransactionId.Payer).IsEqualTo(new EntityId(0, 0, 19789));
        await Assert.That(data.TransactionId.ValidStartSeconds).IsEqualTo(1_618_591_023L);
        await Assert.That(data.TransactionId.ValidStartNanos).IsEqualTo(997_420_021);
        await Assert.That(data.TransactionType).IsEqualTo("CRYPTOTRANSFER");
    }

    [Test]
    public async Task Mint_Record_Has_Null_Sender()
    {
        var json = """
        {
            "consensus_timestamp":"1700000000.000000001",
            "is_approval":false,
            "nonce":0,
            "receiver_account_id":"0.0.500",
            "transaction_id":"0.0.500-1700000000-000000000",
            "type":"TOKENMINT"
        }
        """;
        var data = JsonSerializer.Deserialize<NftTransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Sender).IsNull();
        await Assert.That(data.Receiver).IsEqualTo(new EntityId(0, 0, 500));
        await Assert.That(data.TransactionType).IsEqualTo("TOKENMINT");
    }

    [Test]
    public async Task Burn_Record_Has_Null_Receiver()
    {
        var json = """
        {
            "consensus_timestamp":"1700000001.000000002",
            "is_approval":false,
            "nonce":0,
            "sender_account_id":"0.0.500",
            "transaction_id":"0.0.500-1700000001-000000000",
            "type":"TOKENBURN"
        }
        """;
        var data = JsonSerializer.Deserialize<NftTransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Sender).IsEqualTo(new EntityId(0, 0, 500));
        await Assert.That(data.Receiver).IsNull();
        await Assert.That(data.TransactionType).IsEqualTo("TOKENBURN");
    }

    [Test]
    public async Task IsApproval_True_Is_Round_Tripped()
    {
        var json = """
        {
            "consensus_timestamp":"1700000000.000000000",
            "is_approval":true,
            "nonce":0,
            "receiver_account_id":"0.0.300",
            "sender_account_id":"0.0.200",
            "transaction_id":"0.0.100-1700000000-000000000",
            "type":"CRYPTOTRANSFER"
        }
        """;
        var data = JsonSerializer.Deserialize<NftTransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.IsApproval).IsTrue();
    }

    [Test]
    public async Task Page_Envelope_Reads_Transactions_Array()
    {
        var json = """
        {
            "transactions":[
                {"consensus_timestamp":"1700000000.000000001","is_approval":false,"nonce":0,"receiver_account_id":"0.0.11","transaction_id":"0.0.500-1700000000-000000000","type":"TOKENMINT"},
                {"consensus_timestamp":"1700000001.000000002","is_approval":false,"nonce":0,"receiver_account_id":"0.0.12","sender_account_id":"0.0.11","transaction_id":"0.0.11-1700000001-000000000","type":"CRYPTOTRANSFER"},
                {"consensus_timestamp":"1700000002.000000003","is_approval":false,"nonce":0,"sender_account_id":"0.0.12","transaction_id":"0.0.500-1700000002-000000000","type":"TOKENBURN"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.NftTransactionDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(3);
        await Assert.That(items[0].TransactionType).IsEqualTo("TOKENMINT");
        await Assert.That(items[1].TransactionType).IsEqualTo("CRYPTOTRANSFER");
        await Assert.That(items[2].TransactionType).IsEqualTo("TOKENBURN");
    }
}
