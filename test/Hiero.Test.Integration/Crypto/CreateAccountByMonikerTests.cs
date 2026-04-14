// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class CreateAccountByMonikerTests
{
    [Test]
    public async Task Can_Not_Create_Account_Having_Moniker_By_Regular_Means()
    {
        var initialPayment = 1_000_000ul;
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var moniker = new EvmAddress(endorsement);
        var alias = new EntityId(0, 0, endorsement);

        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            Endorsement = endorsement,
            InitialBalance = initialPayment
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var xferReceipt1 = await client.TransferAsync(receipt.Address, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt1).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        var balance = await client.GetAccountBalanceAsync(receipt.Address);
        await Assert.That(balance).IsEqualTo(initialPayment - 1);

        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(alias, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InvalidAccountId");

        balance = await client.GetAccountBalanceAsync(receipt.Address);
        await Assert.That(balance).IsEqualTo(initialPayment - 1);

        ex = await Assert.That(async () =>
        {
            await client.TransferAsync(moniker, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InvalidAccountId");

        balance = await client.GetAccountBalanceAsync(receipt.Address);
        await Assert.That(balance).IsEqualTo(initialPayment - 1);

        var infoFromAccount = await client.GetAccountInfoAsync(receipt.Address);
        await Assert.That(infoFromAccount.Address).IsEqualTo(receipt.Address);
        // HIP-583 Churn
        //await Assert.That(infoFromAccount.Monikers).IsEmpty();
        await Assert.That(infoFromAccount.EvmAddress).IsNotNull();
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAccount.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(alias);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");

        ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(moniker);
        }).ThrowsException();
        pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");
    }

    [Test]
    public async Task Account_Via_Moniker_Transfer_Does_Create_Moniker()
    {
        var initialPayment = 20_00_000_000ul;
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var moniker = new EvmAddress(endorsement);
        var alias = new EntityId(0, 0, endorsement);

        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferAsync(TestNetwork.Payer, moniker, (long)initialPayment);
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var accountId = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: have to hydrate via being the payer, don't forget the full prefix
        // required for the system to figure out the public key part, eesh.
        var xferReceipt1 = await client.TransferAsync(accountId, TestNetwork.Payer, 1, ctx =>
        {
            ctx.Payer = accountId;
            ctx.Signatory = new Signatory(privateKey);
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
        });
        await Assert.That(xferReceipt1).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        var remainderBalance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(initialPayment > remainderBalance).IsTrue();

        var xferReceipt2 = await client.TransferAsync(alias, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt2).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        var balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(remainderBalance - 1);

        var xferReceipt3 = await client.TransferAsync(moniker, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt3).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(remainderBalance - 2);

        var infoFromAccount = await client.GetAccountInfoAsync(accountId);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(moniker);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);
    }
    [Test]
    public async Task Account_Via_Moniker_Transfer_Does_Create_Moniker_Alt()
    {
        var initialPayment = 1_000_000ul;
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var moniker = new EvmAddress(endorsement);
        var alias = new EntityId(0, 0, endorsement);

        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferAsync(TestNetwork.Payer, moniker, (long)initialPayment);
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var accountId = ((CreateAccountReceipt)receipts[1]).Address;

        // Note: have to hydrate via being the payer, don't forget the full prefix
        // required for the system to figure out the public key part, eesh.
        var xferReceipt1 = await client.TransferAsync(accountId, TestNetwork.Payer, 1, ctx =>
        {
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
            ctx.Signatory = new Signatory(ctx.Signatory!, privateKey);
        });
        await Assert.That(xferReceipt1).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        var balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 1);

        var xferReceipt2 = await client.TransferAsync(alias, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt2).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 2);

        var xferReceipt3 = await client.TransferAsync(moniker, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt3).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 3);

        var infoFromAccount = await client.GetAccountInfoAsync(accountId);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(moniker);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);
    }
    [Test]
    public async Task Account_Via_Alias_Transfer_Does_Create_Moniker()
    {
        var initialPayment = 1_000_000ul;
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var moniker = new EvmAddress(endorsement);
        var alias = new EntityId(0, 0, endorsement);

        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferAsync(TestNetwork.Payer, alias, (long)initialPayment);
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var accountId = ((CreateAccountReceipt)receipts[1]).Address;

        var xferReceipt1 = await client.TransferAsync(accountId, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt1).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        var balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 1);

        var xferReceipt2 = await client.TransferAsync(alias, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt2).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 2);

        var xferReceipt3 = await client.TransferAsync(moniker, TestNetwork.Payer, 1, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));
        await Assert.That(xferReceipt3).IsNotNull();
        await Assert.That(xferReceipt1.Status).IsEqualTo(ResponseCode.Success);

        balance = await client.GetAccountBalanceAsync(accountId);
        await Assert.That(balance).IsEqualTo(initialPayment - 3);

        var infoFromAccount = await client.GetAccountInfoAsync(accountId);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

        infoFromAccount = await client.GetAccountInfoAsync(moniker);
        await Assert.That(infoFromAccount.Address).IsEqualTo(accountId);
        await Assert.That(infoFromAccount.EvmAddress).IsEqualTo(moniker);
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(endorsement);
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAccount.Memo).IsEmpty();
        await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);
    }
    //[Test]
    //public async Task CanScheduleCreateAccountAsync()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);

    //    var initialPayment = 1_000_000ul;
    //    var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
    //    var endorsement = new Endorsement(publicKey);
    //    var moniker = new EvmAddress(endorsement);
    //    var alias = new EntityId(0, 0, endorsement);

    //    await using var client = await TestNetwork.CreateClientAsync();

    //    var receipt = await client.TransferAsync(TestNetwork.Payer, moniker, (long)initialPayment, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, new PendingParams { PendingPayer = fxPayer }));
    //    await Assert.That(receipt).IsNotNull();
    //    await Assert.That(receipt.Pending).IsNotNull();

    //    var signingReceipt = await client.LegacySignPendingTransactionAsync(receipt.Pending.Id, fxPayer);
    //    await Assert.That(signingReceipt.Status).IsEqualTo(ResponseCode.Success);

    //    // If an account was created by the moniker, the receipt
    //    // with the address is a "child receipt" of the SIGNING
    //    // TRANSACTION (note this is a change with 0.53.0)
    //    // receipt and must be explictly asked for.
    //    var allReceipts = await client.GetAllReceiptsAsync(signingReceipt.TransactionId);
    //    await Assert.That(allReceipts.Count).IsEqualTo(2);
    //    await Assert.That(allReceipts[0].TransactionId).IsEqualTo(signingReceipt.TransactionId);

    //    var createReceipt = allReceipts[1] as CreateAccountReceipt;
    //    await Assert.That(createReceipt).IsNotNull();
    //    await Assert.That(createReceipt!.Address).IsNotNull();
    //    await Assert.That(createReceipt.Address.RealmNum).IsEqualTo(0L);
    //    await Assert.That(createReceipt.Address.ShardNum).IsEqualTo(0L);
    //    await Assert.That(createReceipt.Address.AccountNum > 0).IsTrue();
    //    await Assert.That(createReceipt.TransactionId.ChildNonce).IsEqualTo(1);

    //    var createReceiptByTx = await client.GetReceiptAsync(createReceipt.TransactionId) as CreateAccountReceipt;
    //    await Assert.That(createReceiptByTx).IsNotNull();
    //    await Assert.That(createReceiptByTx!.Address).IsNotNull();
    //    await Assert.That(createReceiptByTx.Address.RealmNum).IsEqualTo(0L);
    //    await Assert.That(createReceiptByTx.Address.ShardNum).IsEqualTo(0L);
    //    await Assert.That(createReceiptByTx.Address).IsEqualTo(createReceipt.Address);
    //    await Assert.That(createReceiptByTx.TransactionId).IsEqualTo(createReceipt.TransactionId);

    //    var balances = await client.GetAccountBalancesAsync(alias);
    //    await Assert.That(balances).IsNotNull();
    //    await Assert.That(balances.Holder).IsEqualTo(createReceipt.Address);
    //    await Assert.That(balances.Crypto > 0).IsTrue();

    //    var infoFromAccount = await client.GetAccountInfoAsync(createReceipt.Address);
    //    await Assert.That(infoFromAccount.Address).IsEqualTo(createReceipt.Address);
    //    await Assert.That(infoFromAccount.KeyAlias).IsEqualTo(Endorsement.None);
    //    await Assert.That(infoFromAccount.EvmAddress).IsNotNull();
    //    await Assert.That(infoFromAccount.Deleted).IsFalse();
    //    await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
    //    // Note: not materialzed yet, so no public key because
    //    // the network has never seen the public key.
    //    await Assert.That(infoFromAccount.Endorsement).IsEqualTo(Endorsement.None);
    //    await Assert.That(infoFromAccount.Balance > 0).IsTrue();
    //    await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
    //    await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
    //    await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
    //    await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
    //    await Assert.That(infoFromAccount.AutoAssociationLimit).IsEqualTo(-1);
    //    await Assert.That(infoFromAccount.Memo).IsEmpty();
    //    await Assert.That(infoFromAccount.Ledger).IsNotEqualTo(BigInteger.Zero);
    //    await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
    //    await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
    //    await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
    //    await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
    //    await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
    //    await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
    //    await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);

    //    var infoFromAlias = await client.GetAccountInfoAsync(moniker);
    //    await Assert.That(infoFromAlias.Address).IsEqualTo(createReceipt.Address);
    //    await Assert.That(infoFromAlias.KeyAlias).IsEqualTo(Endorsement.None);
    //    await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
    //    await Assert.That(infoFromAlias.Deleted).IsFalse();
    //    await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
    //    await Assert.That(infoFromAlias.Endorsement).IsEqualTo(Endorsement.None);
    //    await Assert.That(infoFromAlias.Balance > 0).IsTrue();
    //    await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
    //    await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
    //    await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
    //    await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
    //    await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
    //    await Assert.That(infoFromAlias.Memo).IsEmpty();
    //    await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAccount.Ledger);
    //    await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
    //    await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
    //    await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
    //    await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
    //    await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
    //    await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
    //    await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);
    //}
}
