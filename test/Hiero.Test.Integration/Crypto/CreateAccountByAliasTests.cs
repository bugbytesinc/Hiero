// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class CreateAccountByAliasTests
{
    [Test]
    public async Task Can_Create_Account_Async()
    {
        var initialPayment = 1_00_000_000;
        var (publicKey, privateKey) = Generator.KeyPair();
        var alias = new EntityId(0, 0, (Endorsement)publicKey);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, alias, initialPayment);
        await Assert.That(receipt).IsNotNull();

        // If an account was created by the alias, the receipt
        // with the address is a "child receipt" of the transfer
        // receipt and must be explictly asked for.
        var allReceipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        await Assert.That(allReceipts.Count).IsEqualTo(2);
        await Assert.That(allReceipts[0]).IsEqualTo(receipt);

        var createReceipt = allReceipts[1] as CreateAccountReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.Address).IsNotNull();
        await Assert.That(createReceipt.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createReceipt.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createReceipt.Address.AccountNum > 0).IsTrue();
        await Assert.That(createReceipt.TransactionId.ChildNonce).IsEqualTo(1);

        var createReceiptByTx = await client.GetReceiptAsync(createReceipt.TransactionId) as CreateAccountReceipt;
        await Assert.That(createReceiptByTx).IsNotNull();
        await Assert.That(createReceiptByTx!.Address).IsNotNull();
        await Assert.That(createReceiptByTx.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createReceiptByTx.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createReceiptByTx.Address).IsEqualTo(createReceipt.Address);
        await Assert.That(createReceiptByTx.TransactionId).IsEqualTo(createReceipt.TransactionId);

        await Assert.That(await client.GetAccountBalanceAsync(alias)).IsEqualTo((ulong)initialPayment);

        var infoFromAccount = await client.GetAccountInfoAsync(createReceipt.Address);
        await Assert.That(infoFromAccount.Address).IsEqualTo(createReceipt.Address);
        await Assert.That((EntityId)infoFromAccount.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAccount.Monikers).IsEmpty();
        await Assert.That(infoFromAccount.EvmAddress).IsNotNull();
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
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

        var infoFromAlias = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAlias.Address).IsEqualTo(createReceipt.Address);
        await Assert.That((EntityId)infoFromAlias.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAlias.Monikers).IsEmpty();
        await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
        await Assert.That(infoFromAlias.Deleted).IsFalse();
        await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAlias.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAlias.Balance > 0).IsTrue();
        await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAlias.Memo).IsEmpty();
        await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAccount.Ledger);
        await Assert.That(infoFromAlias.StakingInfo).IsNotNull();
        await Assert.That(infoFromAlias.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAlias.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAlias.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAlias.StakingInfo.Node).IsEqualTo(0);
    }
    [Test]
    public async Task Can_Schedule_Create_Account_Async_Defect()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var initialPayment = 1_00_000_000;
        var (publicKey, privateKey) = Generator.KeyPair();
        var alias = (EntityId)(Endorsement)publicKey;
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new TransferParams
            {
                CryptoTransfers = [
                new CryptoTransfer(fxPayer.CreateReceipt!.Address,-initialPayment),
                new CryptoTransfer(alias,initialPayment)
                ]
            },
            Payer = fxPayer,
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var signingReceipt = await client.SignScheduleAsync(receipt.Schedule, ctx => {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        await Assert.That(signingReceipt.Status).IsEqualTo(ResponseCode.Success);

        // VERSION 0.49.0 REGRESSION, THE FOLLOWING IS HOW IT USED TO WORK
        // If an account was created by the alias, the receipt
        // with the address is a "child receipt" of the transfer
        // receipt and must be explictly asked for.
        //var allReceipts = await client.GetAllReceiptsAsync(receipt.Scheduled.TransactionId);
        //await Assert.That(allReceipts.Count).IsEqualTo(2);
        //await Assert.That(allReceipts[0].TransactionId).IsEqualTo(receipt.Scheduled.TransactionId);
        // BUT NOW YOU NEED TO GET THE RECEIPT FROM THE SCHEDULE SIGN ACCOUNT
        var allReceipts = await client.GetAllReceiptsAsync(signingReceipt.TransactionId);
        await Assert.That(allReceipts.Count).IsEqualTo(2);
        await Assert.That(allReceipts[0].TransactionId).IsEqualTo(signingReceipt.TransactionId);

        var createReceipt = allReceipts[1] as CreateAccountReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.Address).IsNotNull();
        await Assert.That(createReceipt.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createReceipt.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createReceipt.Address.AccountNum > 0).IsTrue();
        await Assert.That(createReceipt.TransactionId.ChildNonce).IsEqualTo(1);

        var createReceiptByTx = await client.GetReceiptAsync(createReceipt.TransactionId) as CreateAccountReceipt;
        await Assert.That(createReceiptByTx).IsNotNull();
        await Assert.That(createReceiptByTx!.Address).IsNotNull();
        await Assert.That(createReceiptByTx.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createReceiptByTx.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createReceiptByTx.Address).IsEqualTo(createReceipt.Address);
        await Assert.That(createReceiptByTx.TransactionId).IsEqualTo(createReceipt.TransactionId);

        await Assert.That(await client.GetAccountBalanceAsync(alias)).IsEqualTo((ulong)initialPayment);

        var infoFromAccount = await client.GetAccountInfoAsync(createReceipt.Address);
        await Assert.That(infoFromAccount.Address).IsEqualTo(createReceipt.Address);
        await Assert.That((EntityId)infoFromAccount.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAccount.Monikers).IsEmpty();
        await Assert.That(infoFromAccount.EvmAddress).IsNotNull();
        await Assert.That(infoFromAccount.Deleted).IsFalse();
        await Assert.That(infoFromAccount.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAccount.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAccount.Balance > 0).IsTrue();
        await Assert.That(infoFromAccount.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAccount.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        await Assert.That(infoFromAccount.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAccount.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
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

        var infoFromAlias = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAlias.Address).IsEqualTo(createReceipt.Address);
        await Assert.That((EntityId)infoFromAlias.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAlias.Monikers).IsEmpty();
        await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
        await Assert.That(infoFromAlias.Deleted).IsFalse();
        await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAlias.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAlias.Balance > 0).IsTrue();
        await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAlias.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAlias.Memo).IsEmpty();
        await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAccount.Ledger);
        await Assert.That(infoFromAccount.StakingInfo).IsNotNull();
        await Assert.That(infoFromAccount.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAccount.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAccount.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAccount.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAccount.StakingInfo.Node).IsEqualTo(0);
    }
    [Test]
    public async Task Can_Create_Account_Via_Ed22519_Transfer_And_Get_Records()
    {
        var initialPayment = 1_00_000_000;
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();
        var alias = new EntityId(0, 0, new Endorsement(publicKey));
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, alias, initialPayment);
        await Assert.That(receipt).IsNotNull();

        // If an account was created by the alias, the receipt
        // with the address is a "child receipt" of the transfer
        // receipt and must be explictly asked for.
        var allRecords = await client.GetAllTransactionRecordsAsync(receipt.TransactionId);
        await Assert.That(allRecords.Count).IsEqualTo(2);
        await Assert.That(allRecords[0].ParentTransactionConsensus).IsNull();

        var createRecord = allRecords[1] as CreateAccountRecord;
        await Assert.That(createRecord).IsNotNull();
        await Assert.That(createRecord!.Address).IsNotNull();
        await Assert.That(createRecord.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createRecord.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createRecord.Address.AccountNum > 0).IsTrue();
        await Assert.That(createRecord.TransactionId.ChildNonce).IsEqualTo(1);
        await Assert.That(createRecord.EvmAddress).IsEqualTo(EvmAddress.None);
        // NETWORK V0.21.0 UNSUPPORTED vvvv
        // NOT IMPLEMENTED YET
        //await Assert.That(createRecord.ParentTransactionConsensus).IsEqualTo(allRecords[0].Consensus);
        await Assert.That(createRecord.ParentTransactionConsensus).IsNull();
        // NETWORK V0.21.0 UNSUPPORTED ^^^^

        var createRecordByTx = await client.GetTransactionRecordAsync(createRecord.TransactionId) as CreateAccountRecord;
        await Assert.That(createRecordByTx).IsNotNull();
        await Assert.That(createRecordByTx!.Address).IsNotNull();
        await Assert.That(createRecordByTx.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createRecordByTx.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createRecordByTx.Address).IsEqualTo(createRecord.Address);
        await Assert.That(createRecordByTx.TransactionId).IsEqualTo(createRecord.TransactionId);
        await Assert.That(createRecordByTx.EvmAddress).IsEqualTo(EvmAddress.None);
        // NETWORK V0.21.0 UNSUPPORTED vvvv
        // NOT IMPLEMENTED YET
        //await Assert.That(createRecordByTx.ParentTransactionConsensus).IsEqualTo(allRecords[0].Consensus);
        await Assert.That(createRecordByTx.ParentTransactionConsensus).IsNull();
        // NETWORK V0.21.0 UNSUPPORTED ^^^^

        await Assert.That(await client.GetAccountBalanceAsync(alias)).IsEqualTo((ulong)initialPayment);

        var infoFromAccount = await client.GetAccountInfoAsync(createRecord.Address);
        await Assert.That(infoFromAccount.Address).IsEqualTo(createRecord.Address);
        await Assert.That((EntityId)infoFromAccount.KeyAlias).IsEqualTo(alias);
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
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
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

        var infoFromAlias = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAlias.Address).IsEqualTo(createRecord.Address);
        await Assert.That((EntityId)infoFromAlias.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAlias.Monikers).IsEmpty();
        await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
        await Assert.That(infoFromAlias.Deleted).IsFalse();
        await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAlias.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAlias.Balance > 0).IsTrue();
        await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAccount.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAlias.Memo).IsEmpty();
        await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAccount.Ledger);
        await Assert.That(infoFromAlias.StakingInfo).IsNotNull();
        await Assert.That(infoFromAlias.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAlias.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAlias.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAlias.StakingInfo.Node).IsEqualTo(0);
    }
    [Test]
    public async Task Can_Create_Account_Via_Secp256k1_Transfer_And_Get_Records()
    {
        var initialPayment = 1_00_000_000;
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var alias = new EntityId(0, 0, new Endorsement(publicKey));
        var moniker = new EvmAddress(new Endorsement(publicKey));
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, alias, initialPayment);
        await Assert.That(receipt).IsNotNull();

        // If an account was created by the alias, the receipt
        // with the address is a "child receipt" of the transfer
        // receipt and must be explictly asked for.
        var allRecords = await client.GetAllTransactionRecordsAsync(receipt.TransactionId);
        await Assert.That(allRecords.Count).IsEqualTo(2);
        await Assert.That(allRecords[0].ParentTransactionConsensus).IsNull();

        var createRecord = allRecords[1] as CreateAccountRecord;
        await Assert.That(createRecord).IsNotNull();
        await Assert.That(createRecord!.Address).IsNotNull();
        await Assert.That(createRecord.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createRecord.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createRecord.Address.AccountNum > 0).IsTrue();
        await Assert.That(createRecord.TransactionId.ChildNonce).IsEqualTo(1);
        await Assert.That(createRecord.EvmAddress).IsEqualTo(moniker);
        // NETWORK V0.21.0 UNSUPPORTED vvvv
        // NOT IMPLEMENTED YET
        //await Assert.That(createRecord.ParentTransactionConsensus).IsEqualTo(allRecords[0].Consensus);
        await Assert.That(createRecord.ParentTransactionConsensus).IsNull();
        // NETWORK V0.21.0 UNSUPPORTED ^^^^

        var createRecordByTx = await client.GetTransactionRecordAsync(createRecord.TransactionId) as CreateAccountRecord;
        await Assert.That(createRecordByTx).IsNotNull();
        await Assert.That(createRecordByTx!.Address).IsNotNull();
        await Assert.That(createRecordByTx.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createRecordByTx.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createRecordByTx.Address).IsEqualTo(createRecord.Address);
        await Assert.That(createRecordByTx.TransactionId).IsEqualTo(createRecord.TransactionId);
        await Assert.That(createRecord.EvmAddress).IsEqualTo(moniker);
        // NETWORK V0.21.0 UNSUPPORTED vvvv
        // NOT IMPLEMENTED YET
        //await Assert.That(createRecordByTx.ParentTransactionConsensus).IsEqualTo(allRecords[0].Consensus);
        await Assert.That(createRecordByTx.ParentTransactionConsensus).IsNull();
        // NETWORK V0.21.0 UNSUPPORTED ^^^^

        await Assert.That(await client.GetAccountBalanceAsync(alias)).IsEqualTo((ulong)initialPayment);

        var infoFromAccount = await client.GetAccountInfoAsync(createRecord.Address);
        await Assert.That(infoFromAccount.Address).IsEqualTo(createRecord.Address);
        await Assert.That((EntityId)infoFromAccount.KeyAlias).IsEqualTo(alias);
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
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
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

        var infoFromAlias = await client.GetAccountInfoAsync(alias);
        await Assert.That(infoFromAlias.Address).IsEqualTo(createRecord.Address);
        await Assert.That((EntityId)infoFromAlias.KeyAlias).IsEqualTo(alias);
        // HIP-583 Churn
        //await Assert.That(infoFromAlias.Monikers).IsEmpty();
        await Assert.That(infoFromAlias.EvmAddress).IsNotNull();
        await Assert.That(infoFromAlias.Deleted).IsFalse();
        await Assert.That(infoFromAlias.EvmNonce).IsEqualTo(0);
        await Assert.That(infoFromAlias.Endorsement).IsEqualTo(new Endorsement(publicKey));
        await Assert.That(infoFromAlias.Balance > 0).IsTrue();
        await Assert.That(infoFromAlias.ReceiveSignatureRequired).IsFalse();
        await Assert.That(infoFromAlias.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(infoFromAccount.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(infoFromAlias.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(infoFromAlias.NftCount).IsEqualTo(0);
        // v0.53.0 Churn, now the Default is -1 as a flag for sky is the limit airdrop
        await Assert.That(infoFromAlias.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(infoFromAlias.Memo).IsEmpty();
        await Assert.That(infoFromAlias.Ledger).IsEqualTo(infoFromAccount.Ledger);
        await Assert.That(infoFromAlias.StakingInfo).IsNotNull();
        await Assert.That(infoFromAlias.StakingInfo.Declined).IsFalse();
        await Assert.That(infoFromAlias.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(infoFromAlias.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(infoFromAlias.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(infoFromAlias.StakingInfo.Node).IsEqualTo(0);
    }
}
