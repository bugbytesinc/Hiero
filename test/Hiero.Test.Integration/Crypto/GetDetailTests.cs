using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Crypto;

public class GetDetailTests
{
    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Can_Get_Detail_For_Account_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;

        var detail = await client.GetAccountDetailAsync(account);
        await Assert.That(detail.Address).IsNotNull();
        await Assert.That(detail.Address.RealmNum).IsEqualTo(account.RealmNum);
        await Assert.That(detail.Address.ShardNum).IsEqualTo(account.ShardNum);
        await Assert.That(detail.Address.AccountNum).IsEqualTo(account.AccountNum);
        await Assert.That(detail.EvmAddress).IsNotNull();
        await Assert.That(detail.Deleted).IsFalse();
        await Assert.That(detail.ProxiedToAccount).IsEqualTo(0);
        await Assert.That(detail.Endorsement).IsEqualTo(TestNetwork.Endorsement);
        await Assert.That(detail.Balance > 0).IsTrue();
        await Assert.That(detail.ReceiveSignatureRequired).IsFalse();
        await Assert.That(detail.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detail.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detail.NftCount).IsEqualTo(0);
        await Assert.That(detail.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That((EntityId)detail.KeyAlias).IsEqualTo(EntityId.None);
        await Assert.That(detail.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(detail.CryptoAllowances).IsEmpty();
        await Assert.That(detail.TokenAllowances).IsEmpty();
        await Assert.That(detail.NftAllowances).IsEmpty();
    }

    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Can_Get_Detail_For_Account_Facet()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var detail = await client.GetAccountDetailAsync(fxAccount);
        await Assert.That(detail.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(detail.EvmAddress).IsNotNull();
        await Assert.That(detail.Deleted).IsFalse();
        await Assert.That(detail.ProxiedToAccount).IsEqualTo(0);
        await Assert.That(detail.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(detail.Balance).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(detail.ReceiveSignatureRequired).IsEqualTo(fxAccount.CreateParams.RequireReceiveSignature);
        await Assert.That(detail.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detail.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detail.Memo).IsEqualTo(fxAccount.CreateParams.Memo);
        await Assert.That(detail.NftCount).IsEqualTo(0);
        await Assert.That(detail.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);
        await Assert.That((EntityId)detail.KeyAlias).IsEqualTo(EntityId.None);
        await Assert.That(detail.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(detail.CryptoAllowances).IsEmpty();
        await Assert.That(detail.TokenAllowances).IsEmpty();
        await Assert.That(detail.NftAllowances).IsEmpty();
    }

    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Can_Get_Detail_For_Alias_Facet()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var detailFromAddress = await client.GetAccountDetailAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(detailFromAddress.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(detailFromAddress.EvmAddress).IsNotNull();
        await Assert.That(detailFromAddress.Deleted).IsFalse();
        await Assert.That(detailFromAddress.ProxiedToAccount).IsEqualTo(0);
        await Assert.That(detailFromAddress.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(detailFromAddress.Balance > 0).IsTrue();
        await Assert.That(detailFromAddress.ReceiveSignatureRequired).IsFalse();
        await Assert.That(detailFromAddress.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detailFromAddress.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detailFromAddress.Memo).IsEmpty();
        await Assert.That(detailFromAddress.NftCount).IsEqualTo(0);
        await Assert.That(detailFromAddress.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That((EntityId)detailFromAddress.KeyAlias).IsEqualTo(fxAccount.Alias);
        await Assert.That(detailFromAddress.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(detailFromAddress.CryptoAllowances).IsEmpty();
        await Assert.That(detailFromAddress.TokenAllowances).IsEmpty();
        await Assert.That(detailFromAddress.NftAllowances).IsEmpty();

        var detailFromAlias = await client.GetAccountDetailAsync(fxAccount.Alias);
        await Assert.That(detailFromAlias.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(detailFromAlias.EvmAddress).IsNotNull();
        await Assert.That(detailFromAlias.Deleted).IsFalse();
        await Assert.That(detailFromAlias.ProxiedToAccount).IsEqualTo(0);
        await Assert.That(detailFromAlias.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(detailFromAlias.Balance > 0).IsTrue();
        await Assert.That(detailFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(detailFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detailFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detailFromAlias.Memo).IsEmpty();
        await Assert.That(detailFromAlias.NftCount).IsEqualTo(0);
        await Assert.That(detailFromAlias.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That((EntityId)detailFromAlias.KeyAlias).IsEqualTo(fxAccount.Alias);
        await Assert.That(detailFromAlias.Ledger).IsEqualTo(detailFromAddress.Ledger);
        // NotStrictEqual: verifies different object references (not deep inequality)
        await Assert.That(detailFromAlias.CryptoAllowances).IsNotSameReferenceAs(detailFromAddress.CryptoAllowances);
        await Assert.That(detailFromAlias.TokenAllowances).IsNotSameReferenceAs(detailFromAddress.TokenAllowances);
        await Assert.That(detailFromAlias.NftAllowances).IsNotSameReferenceAs(detailFromAddress.NftAllowances);
    }

    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Can_Get_Detail_For_Gateway_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = await TestNetwork.GetConsensusNodeEndpointAsync();

        var detail = await client.GetAccountDetailAsync(account);
        await Assert.That(detail.Address).IsNotNull();
        await Assert.That(detail.Address.ShardNum).IsEqualTo(account.Node.ShardNum);
        await Assert.That(detail.Address.RealmNum).IsEqualTo(account.Node.RealmNum);
        await Assert.That(detail.Address.AccountNum).IsEqualTo(account.Node.AccountNum);
        await Assert.That(detail.EvmAddress).IsNotNull();
        await Assert.That(detail.Deleted).IsFalse();
        await Assert.That(detail.ProxiedToAccount > -1).IsTrue();
        await Assert.That(detail.Balance > 0).IsTrue();
        await Assert.That(detail.ReceiveSignatureRequired).IsFalse();
        await Assert.That(detail.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detail.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detail.NftCount).IsEqualTo(0);
        await Assert.That((EntityId)detail.KeyAlias).IsEqualTo(EntityId.None);
        await Assert.That(detail.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(detail.CryptoAllowances).IsEmpty();
        await Assert.That(detail.TokenAllowances).IsEmpty();
        await Assert.That(detail.NftAllowances).IsEmpty();
    }

    [Test]
    public async Task Get_Detail_Without_Paying_Signature_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(ctx => ctx.Signatory = null);
        var account = TestNetwork.Payer;
        var ex = await Assert.That(async () =>
        {
            await client.GetAccountDetailAsync(account);
        }).ThrowsException();
        var ioe = ex as InvalidOperationException;
        await Assert.That(ioe).IsNotNull();
        await Assert.That(ioe!.Message).StartsWith("The Payer's signatory (signing key/callback) has not been configured");
    }

    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Can_Get_Detail_For_Asset_Treasury_Account()
    {
        await using var fxAsset = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var detail = await client.GetAccountDetailAsync(fxAsset.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(detail.Address).IsEqualTo(fxAsset.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(detail.EvmAddress).IsNotNull();
        await Assert.That(detail.Deleted).IsFalse();
        await Assert.That(detail.ProxiedToAccount > -1).IsTrue();
        await Assert.That(detail.Balance > 0).IsTrue();
        await Assert.That(detail.ReceiveSignatureRequired).IsFalse();
        await Assert.That(detail.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(detail.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(detail.NftCount).IsEqualTo(fxAsset.Metadata.Length);
        await Assert.That(detail.AutoAssociationLimit).IsEqualTo(fxAsset.TreasuryAccount.CreateParams.AutoAssociationLimit);
        await Assert.That((EntityId)detail.KeyAlias).IsEqualTo(EntityId.None);
        await Assert.That(detail.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(detail.CryptoAllowances).IsEmpty();
        await Assert.That(detail.TokenAllowances).IsEmpty();
        await Assert.That(detail.NftAllowances).IsEmpty();
    }
}
