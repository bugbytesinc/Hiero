// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Token;

public class AirdropParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Groups_Token_Transfers_By_Token()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new AirdropParams
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
        var body = CreateBody(new AirdropParams
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
    public async Task CreateNetworkTransaction_Includes_Token_And_Nft_Airdrops()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var sender = new EntityId(0, 0, 1001);
        var receiver = new EntityId(0, 0, 1002);
        var body = CreateBody(new AirdropParams
        {
            TokenTransfers =
            [
                new TokenTransfer(tokenOne, sender, -10),
                new TokenTransfer(tokenOne, receiver, 10)
            ],
            NftTransfers =
            [
                new NftTransfer(new Hiero.Nft(tokenTwo, 1), sender, receiver)
            ]
        });

        await Assert.That(body.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(GetTokenTransferCount(body, tokenOne)).IsEqualTo(2);
        await Assert.That(GetNftTransferCount(body, tokenTwo)).IsEqualTo(1);
    }

    private static TokenAirdropTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (TokenAirdropTransactionBody)parameters.CreateNetworkTransaction();
    }

    private static int GetTokenTransferCount(TokenAirdropTransactionBody body, EntityId token)
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

    private static int GetNftTransferCount(TokenAirdropTransactionBody body, EntityId token)
    {
        foreach (var tokenTransfers in body.TokenTransfers)
        {
            if (tokenTransfers.Token.AsAddress() == token)
            {
                return tokenTransfers.NftTransfers.Count;
            }
        }
        return 0;
    }
}
