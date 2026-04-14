using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Crypto;

public class GetInfoTests
{
    [Test]
    public async Task Can_Get_Info_For_Account_Async()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetAccountAsync(TestNetwork.Payer);
        await Assert.That(data).IsNotNull();

        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;
        var info = await client.GetAccountInfoAsync(account);
        await Assert.That(info.Address).IsNotNull();

        await Assert.That(info.Address.RealmNum).IsEqualTo(account.RealmNum);
        await Assert.That(info.Address.ShardNum).IsEqualTo(account.ShardNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(account.AccountNum);
        await Assert.That(info.Address).IsEqualTo(data.Account);
        await Assert.That(info.EvmAddress).IsEqualTo(data.EvmAddress);
        await Assert.That(info.Deleted).IsEqualTo(data.Deleted);
        await Assert.That(info.EvmNonce).IsEqualTo(data.EvmNonce);
        await Assert.That(info.Endorsement).IsEqualTo(TestNetwork.Endorsement);
        await Assert.That(info.Endorsement).IsEqualTo(data.Endorsement);
        await Assert.That(info.Balance > 0).IsTrue();
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(data.ReceiverSignatureRequired);
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
        // Need to figure this out later
        //await Assert.That(info.Alias).IsNotEqualTo(data.Alias);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(0 <= info.StakingInfo.Node).IsTrue();
    }

    [Test]
    public async Task Can_Get_Info_For_Account_Facet()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);
        await Assert.That(info.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(info.Balance).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(fxAccount.CreateParams.RequireReceiveSignature);
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(info.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fxAccount.CreateParams.Memo);
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        // HIP-583 Churn
        //await Assert.That(info.Monikers).IsEmpty();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Info_For_Alias_Facet()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var infoFromAddress = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(infoFromAddress.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(infoFromAddress.EvmAddress).IsNotNull();
        await Assert.That(infoFromAddress.Deleted).IsFalse();
        await Assert.That(infoFromAddress.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAddress.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(infoFromAddress.Balance > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAddress.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAddress.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAddress.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAddress.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAddress.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAddress.Memo).IsEmpty();
        await Assert.That(infoFromAddress.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAddress.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That((EntityId)infoFromAddress.KeyAlias).IsEqualTo(fxAccount.Alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAddress.Monikers).IsEmpty();
        await Assert.That(infoFromAddress.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(infoFromAddress.StakingInfo).IsNotNull();
        await Assert.That(infoFromAddress.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAddress.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAddress.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAddress.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAddress.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAddress.StakingInfo.Node).IsEqualTo(0);

        var infoFromAlias = await client.GetAccountInfoAsync(fxAccount.Alias);
        await Assert.That(infoFromAlias.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
        await Assert.That(infoFromAlias.Deleted).IsFalse();
        await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAlias.Endorsement).IsEqualTo(fxAccount.PublicKey);
        await Assert.That(infoFromAlias.Balance > 0).IsTrue();
        await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAlias.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAlias.Memo).IsEmpty();
        await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That((EntityId)infoFromAlias.KeyAlias).IsEqualTo(fxAccount.Alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAlias.Monikers).IsEmpty();
        await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAddress.Ledger);
        await Assert.That(infoFromAlias.StakingInfo).IsNotNull();
        await Assert.That(infoFromAlias.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAlias.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAlias.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAlias.StakingInfo.Node).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Info_For_Gateway_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = await TestNetwork.GetConsensusNodeEndpointAsync();
        var info = await client.GetAccountInfoAsync(account);
        var data = await (await TestNetwork.GetMirrorRestClientAsync()).GetAccountAsync(account);
        await Assert.That(info.Address).IsNotNull();
        await Assert.That(info.Address.ShardNum).IsEqualTo(account.Node.ShardNum);
        await Assert.That(info.Address.RealmNum).IsEqualTo(account.Node.RealmNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(account.Node.AccountNum);
        await Assert.That(info.Address).IsEqualTo(data!.Account);
        await Assert.That(info.EvmAddress).IsEqualTo(data.EvmAddress);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(data.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(data.EvmNonce);
        await Assert.That(Math.Abs((double)info.Balance - (double)data.Balances.Balance) < 10000000.0).IsTrue();
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(data.ReceiverSignatureRequired);
        // Begin TESTNET REGRESSION - Known defect: AutoRenewPeriod mismatch between
        // consensus node info and mirror node data. The original xUnit test used
        // Assert.Throws<EqualException> to document this known discrepancy.
        // await Assert.That(info.AutoRenewPeriod.TotalSeconds).IsEqualTo(data.AutoRenewPeriod);
        // End TESTNET REGRESSION
        await Assert.That(Math.Abs((double)info.Expiration.Seconds - (double)data.Expiration.Seconds) < 10.0).IsTrue();
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(data.Alias).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        // This is a DEFECT with SOLO, but I don't have time to investigate
        //await Assert.That(info.StakingInfo.PeriodStart.Seconds).IsEqualTo(data.StakePeriodStart.Seconds);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(data.PendingReward);
        await Assert.That(info.StakingInfo.Proxied > -1).IsTrue();
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(data.StakedNode ?? 0);
    }

    [Test]
    public async Task Get_Info_Without_Paying_Signature_Throws_Exception()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(ctx => ctx.Signatory = null);
        var account = TestNetwork.Payer;
        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(account);
        }).ThrowsException();
        var ioe = ex as InvalidOperationException;
        await Assert.That(ioe).IsNotNull();
        await Assert.That(ioe!.Message).StartsWith("The Payer's signatory (signing key/callback) has not been configured");
    }

    [Test]
    public async Task Can_Get_Info_For_Asset_Treasury_Account()
    {
        await using var fxAsset = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetAccountInfoAsync(fxAsset.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Address).IsEqualTo(fxAsset.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);
        await Assert.That(info.Balance > 0).IsTrue();
        await Assert.That(info.ReceiveSignatureRequired).IsFalse();
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(info.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.NftCount).IsEqualTo(fxAsset.Metadata.Length);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAsset.TreasuryAccount.CreateParams.AutoAssociationLimit);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        // HIP-583 Churn
        //await Assert.That(info.Monikers).IsEmpty();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Info_For_Account_Ed25519_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();
        var account = (await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = publicKey
        })).Address;
        var info = await client.GetAccountInfoAsync(account);
        await Assert.That(info.Address).IsNotNull();
        await Assert.That(info.Address.RealmNum).IsEqualTo(account.RealmNum);
        await Assert.That(info.Address.ShardNum).IsEqualTo(account.ShardNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(account.AccountNum);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);
        await Assert.That(info.Endorsement).IsEqualTo(new Endorsement(KeyType.Ed25519, publicKey));
        await Assert.That(info.Balance > 0).IsTrue();
        await Assert.That(info.ReceiveSignatureRequired).IsFalse();
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(info.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        // HIP-583 Churn
        //await Assert.That(info.Monikers).IsEmpty();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(0 <= info.StakingInfo.Node).IsTrue();
    }

    [Test]
    public async Task Can_Get_Info_For_Account_Secp256_K1_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var account = (await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        })).Address;
        var info = await client.GetAccountInfoAsync(account);
        await Assert.That(info.Address).IsNotNull();
        await Assert.That(info.Address.RealmNum).IsEqualTo(account.RealmNum);
        await Assert.That(info.Address.ShardNum).IsEqualTo(account.ShardNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(account.AccountNum);
        await Assert.That(info.Address.CastToEvmAddress()).IsEqualTo(info.EvmAddress);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);
        await Assert.That(info.Endorsement).IsEqualTo(new Endorsement(KeyType.ECDSASecp256K1, publicKey));
        await Assert.That(info.Balance > 0).IsTrue();
        await Assert.That(info.ReceiveSignatureRequired).IsFalse();
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(info.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        // HIP-583 Churn
        //await Assert.That(info.Monikers).IsEmpty();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(0 <= info.StakingInfo.Node).IsTrue();
    }

    [Test]
    public async Task Can_Cancel_Get_Info_For_Account_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;
        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(account, new CancellationToken(true));
        }).ThrowsException();
        var oce = ex as OperationCanceledException;
        await Assert.That(oce).IsNotNull();
    }
}
