// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Crypto;

public class TransferParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Nets_Crypto_Transfers_By_Account()
    {
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferParams
        {
            CryptoTransfers =
            [
                new CryptoTransfer(sender, -100),
                new CryptoTransfer(receiver, 40),
                new CryptoTransfer(receiver, 60)
            ]
        });

        await Assert.That(body.Transfers.AccountAmounts.Count).IsEqualTo(2);
        await Assert.That(GetAccountAmount(body.Transfers, sender)).IsEqualTo(-100);
        await Assert.That(GetAccountAmount(body.Transfers, receiver)).IsEqualTo(100);
    }

    [Test]
    public async Task CreateNetworkTransaction_Groups_Token_Transfers_By_Token()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferParams
        {
            TokenTransfers =
            [
                new TokenTransfer(tokenOne, sender, -10),
                new TokenTransfer(tokenOne, receiver, 10),
                new TokenTransfer(tokenTwo, sender, -20),
                new TokenTransfer(tokenTwo, receiver, 20)
            ]
        });

        await Assert.That(body.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(GetTokenTransferCount(body, tokenOne)).IsEqualTo(2);
        await Assert.That(GetTokenTransferCount(body, tokenTwo)).IsEqualTo(2);
    }

    [Test]
    public async Task CreateNetworkTransaction_Groups_Nft_Transfers_By_Token()
    {
        var token = new EntityId(0, 0, 2001);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferParams
        {
            NftTransfers =
            [
                new NftTransfer(new Hiero.Nft(token, 1), sender, receiver),
                new NftTransfer(new Hiero.Nft(token, 2), sender, receiver)
            ]
        });

        await Assert.That(body.TokenTransfers.Count).IsEqualTo(1);
        await Assert.That(body.TokenTransfers[0].Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.TokenTransfers[0].NftTransfers.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TransferOnlyCryptoParams_Creates_Two_Account_Amounts()
    {
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferOnlyCryptoParams(sender, receiver, 100));

        await Assert.That(body.Transfers.AccountAmounts.Count).IsEqualTo(2);
        await Assert.That(GetAccountAmount(body.Transfers, sender)).IsEqualTo(-100);
        await Assert.That(GetAccountAmount(body.Transfers, receiver)).IsEqualTo(100);
    }

    [Test]
    public async Task TransferOnlyTokenParams_Creates_One_Token_Transfer_List()
    {
        var token = new EntityId(0, 0, 2001);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferOnlyTokenParams(token, sender, receiver, 100));

        await Assert.That(body.TokenTransfers.Count).IsEqualTo(1);
        await Assert.That(body.TokenTransfers[0].Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.TokenTransfers[0].Transfers.Count).IsEqualTo(2);
    }

    [Test]
    public async Task TransferOnlyNftParams_Creates_One_Nft_Transfer()
    {
        var token = new EntityId(0, 0, 2001);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new TransferOnlyNftParams(new Hiero.Nft(token, 1), sender, receiver));

        await Assert.That(body.TokenTransfers.Count).IsEqualTo(1);
        await Assert.That(body.TokenTransfers[0].Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.TokenTransfers[0].NftTransfers.Count).IsEqualTo(1);
    }

    private static CryptoTransferTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (CryptoTransferTransactionBody)parameters.CreateNetworkTransaction();
    }

    private static long GetAccountAmount(TransferList transfers, EntityId account)
    {
        foreach (var amount in transfers.AccountAmounts)
        {
            if (amount.AccountID.AsAddress() == account)
            {
                return amount.Amount;
            }
        }
        throw new InvalidOperationException($"Account {account} was not found.");
    }

    private static int GetTokenTransferCount(CryptoTransferTransactionBody body, EntityId token)
    {
        foreach (var tokenTransfers in body.TokenTransfers)
        {
            if (tokenTransfers.Token.AsAddress() == token)
            {
                return tokenTransfers.Transfers.Count;
            }
        }
        return 0;
    }
}
