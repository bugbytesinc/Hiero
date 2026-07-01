// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Token;

public class RelinquishTokenParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Fungible_Token_Rejections()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var body = CreateBody(new RelinquishTokenParams
        {
            Tokens = [tokenOne, tokenTwo]
        });

        await Assert.That(body.Rejections.Count).IsEqualTo(2);
        await Assert.That(body.Rejections[0].FungibleToken.AsAddress()).IsEqualTo(tokenOne);
        await Assert.That(body.Rejections[1].FungibleToken.AsAddress()).IsEqualTo(tokenTwo);
    }

    [Test]
    public async Task CreateNetworkTransaction_Maps_Nft_Rejections()
    {
        var token = new EntityId(0, 0, 2001);
        var nftOne = new Hiero.Nft(token, 1);
        var nftTwo = new Hiero.Nft(token, 2);
        var body = CreateBody(new RelinquishTokenParams
        {
            Nfts = [nftOne, nftTwo]
        });

        await Assert.That(body.Rejections.Count).IsEqualTo(2);
        await Assert.That(body.Rejections[0].Nft.AsNft()).IsEqualTo(nftOne);
        await Assert.That(body.Rejections[1].Nft.AsNft()).IsEqualTo(nftTwo);
    }

    [Test]
    public async Task CreateNetworkTransaction_Maps_Mixed_Rejections()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var nft = new Hiero.Nft(tokenTwo, 1);
        var body = CreateBody(new RelinquishTokenParams
        {
            Tokens = [tokenOne],
            Nfts = [nft]
        });

        await Assert.That(body.Rejections.Count).IsEqualTo(2);
        await Assert.That(body.Rejections[0].FungibleToken.AsAddress()).IsEqualTo(tokenOne);
        await Assert.That(body.Rejections[1].Nft.AsNft()).IsEqualTo(nft);
    }

    private static TokenRejectTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (TokenRejectTransactionBody)parameters.CreateNetworkTransaction();
    }
}
