using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorTopicAndTransactionDataTests
{
    [Test]
    public async Task Can_Get_Topic()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var data = await mirror.GetTopicAsync(fxTopic.CreateReceipt!.Topic);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Topic).IsEqualTo(fxTopic.CreateReceipt!.Topic);
        await Assert.That(data.Memo).IsEqualTo(fxTopic.CreateParams.Memo);
        await Assert.That(data.Administrator).IsEqualTo(fxTopic.CreateParams.Administrator);
        await Assert.That(data.Submit).IsEqualTo(fxTopic.CreateParams.Submitter);
        await Assert.That(data.AutoRenewAccount).IsEqualTo(fxTopic.CreateParams.RenewAccount);
        await Assert.That(data.AutoRenewPeriod).IsEqualTo((long)fxTopic.CreateParams.RenewPeriod.TotalSeconds);
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        await Assert.That(data.Deleted).IsFalse();
        await Assert.That(data.TimestampRange).IsNotNull();
        await Assert.That(data.TimestampRange.Starting).IsNotNull();
    }

    [Test]
    public async Task Can_Get_Transaction_By_Consensus_Timestamp()
    {
        // Generate a known transaction we can pin down by consensus timestamp.
        await using var fxFrom = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 1_000_000_000);
        await using var fxTo = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        const long amount = 100_000_000L;
        var receipt = await client.TransferAsync(fxFrom, fxTo, amount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxFrom.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Consensus).IsNotNull();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetTransactionAsync(record.Consensus!.Value);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.TransactionId).IsEqualTo(record.TransactionId);
        await Assert.That(data.Consensus == record.Consensus!.Value).IsTrue();
        await Assert.That(data.TransactionType).IsEqualTo("CRYPTOTRANSFER");
        await Assert.That(data.Status).IsEqualTo("SUCCESS");
        await Assert.That(data.Nonce).IsEqualTo(0);
        await Assert.That(data.IsScheduled).IsFalse();
        await Assert.That(data.CryptoTransfers).IsNotNull();
        await Assert.That(data.CryptoTransfers!.Length >= 2).IsTrue();
        // Confirm both ends of our transfer are present in the crypto transfer list.
        var fromTransfer = data.CryptoTransfers!.FirstOrDefault(t => t.Account == fxFrom.CreateReceipt!.Address);
        await Assert.That(fromTransfer).IsNotNull();
        var toTransfer = data.CryptoTransfers!.FirstOrDefault(t => t.Account == fxTo.CreateReceipt!.Address);
        await Assert.That(toTransfer).IsNotNull();
        await Assert.That(toTransfer!.Amount).IsEqualTo(amount);
    }

    [Test]
    public async Task Can_Get_Transactions_Filtered_By_Account()
    {
        // Use AccountFilter.Is(...) to narrow the network-wide transaction
        // list down to a single fixture account (which has a known finite
        // history) so we don't paginate through testnet.
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 1_000_000_000);
        await using var fxRecipient = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Drive a couple of transactions so the account has more than
        // just its create record.
        await client.TransferAsync(fxAccount, fxRecipient, 50_000_000L, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });
        await client.TransferAsync(fxAccount, fxRecipient, 25_000_000L, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var transactions = new List<TransactionDetailData>();
        await foreach (var tx in mirror.GetTransactionsAsync(AccountFilter.Is(fxAccount), new PageLimit(20)))
        {
            transactions.Add(tx);
            if (transactions.Count >= 20)
            {
                break;
            }
        }

        // Account has at minimum one CRYPTOCREATEACCOUNT and two CRYPTOTRANSFER
        // records visible from the account-id perspective.
        await Assert.That(transactions.Count >= 3).IsTrue();
        await Assert.That(transactions.Any(t => t.TransactionType == "CRYPTOCREATEACCOUNT")).IsTrue();
        await Assert.That(transactions.Count(t => t.TransactionType == "CRYPTOTRANSFER") >= 2).IsTrue();
        foreach (var tx in transactions)
        {
            await Assert.That(tx.TransactionId).IsNotNull();
            await Assert.That(tx.Consensus.Seconds > 0).IsTrue();
            await Assert.That(string.IsNullOrWhiteSpace(tx.Status)).IsFalse();
        }
    }
}
