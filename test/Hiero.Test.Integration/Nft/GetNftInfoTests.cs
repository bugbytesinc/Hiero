using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class GetNftInfoTests
{
    [Test]
    public async Task Can_Get_Nft_Info()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);
        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Created > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Metadata.ToArray().SequenceEqual(fxNft.Metadata[0].ToArray())).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task Can_Get_Nft_Info_Having_Delegated_Allowance()
    {
        await using var fxAllowance = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxAllowance.TestNft.CreateReceipt!.Token, 1);

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAllowance.Owner.CreateReceipt!.Address);
        await Assert.That(info.Created > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Metadata.ToArray().SequenceEqual(fxAllowance.TestNft.Metadata[0].ToArray())).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(fxAllowance.DelegatedAgent.CreateReceipt!.Address);
    }

    [Test]
    public async Task Get_Nft_Info_Having_Allowance_Defect()
    {
        // https://github.com/hashgraph/hedera-services/issues/3486
        // Defect 0.27.0: tokenGetNftInfo does not return correct spenderID accountNum value.
        await using var fxAllowance = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxAllowance.TestNft.CreateReceipt!.Token, 2);

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAllowance.Owner.CreateReceipt!.Address);
        await Assert.That(info.Created > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Metadata.ToArray().SequenceEqual(fxAllowance.TestNft.Metadata[1].ToArray())).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        // THIS IS A DEFECT IN THE NETWORK
        await Assert.That(info.Spender).IsEqualTo(EntityId.None);
        // SHOULD BE
        //await Assert.That(info.Spender).IsEqualTo(fxAllowance.Agent.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Get_Nft_Info_Having_Explicit_Allowance()
    {
        await using var fxAllowance = await TestAllowance.CreateAsync();
        await using var fxOtherAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxAllowance.TestNft.CreateReceipt!.Token, 1);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[]
            {
                new NftAllowance(nft, fxAllowance.Owner, fxOtherAgent),
            },
            Signatory = fxAllowance.Owner.PrivateKey
        });

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAllowance.Owner.CreateReceipt!.Address);
        await Assert.That(info.Created > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Metadata.ToArray().SequenceEqual(fxAllowance.TestNft.Metadata[0].ToArray())).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(fxOtherAgent.CreateReceipt!.Address);
    }
}
