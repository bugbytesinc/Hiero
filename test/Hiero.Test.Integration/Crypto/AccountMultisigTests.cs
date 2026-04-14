// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class AccountMultisigTests
{
    [Test]
    public async Task Can_Create_Account_Async()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();
        var endorsement = new Endorsement(publicKey1, publicKey2);
        var signatory = new Signatory(privateKey1, privateKey2);
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult).IsNotNull();
        await Assert.That(createResult.Address).IsNotNull();
        await Assert.That(createResult.Address.RealmNum).IsEqualTo(0L);
        await Assert.That(createResult.Address.ShardNum).IsEqualTo(0L);
        await Assert.That(createResult.Address.AccountNum > 0).IsTrue();
        var info = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(info.Balance).IsEqualTo(initialBalance);
        await Assert.That(info.Address.RealmNum).IsEqualTo(createResult.Address.RealmNum);
        await Assert.That(info.Address.ShardNum).IsEqualTo(createResult.Address.ShardNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(createResult.Address.AccountNum);
        await Assert.That(info.Endorsement).IsEqualTo(endorsement);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        await Assert.That(info.Ledger).IsNotEqualTo(BigInteger.Zero);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);

        // Move remaining funds back to primary account.
        var from = createResult.Address;
        await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, signatory));

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = createResult.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = signatory
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(createResult.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: AccountDeleted");
    }
    [Test]
    public async Task Requires_All_Signatures_To_Transfer_Out()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();
        var endorsement = new Endorsement(publicKey1, publicKey2);
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        // Move funds back to primary account (still use Payer to pay TX Fee)
        var from = createResult.Address;
        var receipt = await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey1, privateKey2));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);

        var balance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
    [Test]
    public async Task Requires_One_Of_Two_Signatures_To_Transfer_Out()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();
        var endorsement = new Endorsement(1, publicKey1, publicKey2);
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        // Move funds back to primary account (still use Payer to pay TX Fee)
        var from = createResult.Address;
        var receipt = await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey1));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);

        var balance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
    [Test]
    public async Task Requires_Two_Sets_Of_One_Of_Two_Signatures_To_Transfer_Out()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1a, privateKey1a) = Generator.KeyPair();
        var (publicKey1b, privateKey1b) = Generator.KeyPair();
        var (publicKey2a, privateKey2a) = Generator.KeyPair();
        var (publicKey2b, privateKey2b) = Generator.KeyPair();
        var endorsement = new Endorsement(new Endorsement(1, publicKey1a, publicKey1b), new Endorsement(1, publicKey2a, publicKey2b));
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        // Fail by not providing all necessary keys (note only one of the root keys here)
        var from = createResult.Address;
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey1a));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        // Now try with proper set of signatures
        from = createResult.Address;
        await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey1b, privateKey2a));

        var balance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
    [Test]
    public async Task Change_Key_To_Requires_Two_Sets_Of_One_Of_Two_Signatures_To_Transfer_Out()
    {
        await using var fx = await TestAccount.CreateAsync();
        var (publicKey1a, privateKey1a) = Generator.KeyPair();
        var (publicKey1b, privateKey1b) = Generator.KeyPair();
        var (publicKey2a, privateKey2a) = Generator.KeyPair();
        var (publicKey2b, privateKey2b) = Generator.KeyPair();
        var endorsement = new Endorsement(new Endorsement(1, publicKey1a, publicKey1b), new Endorsement(1, publicKey2a, publicKey2b));
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fx.CreateReceipt!.Address,
            Endorsement = endorsement,
            Signatory = new Signatory(fx.PrivateKey, privateKey1a, privateKey1b, privateKey2a, privateKey2b)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        // Fail by not providing all necessary keys (note only one of the root keys here)
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, (long)fx.CreateParams.InitialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        // Now try with proper set of signatures
        await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, (long)fx.CreateParams.InitialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey1a, privateKey2b));

        var balance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
    [Test]
    public async Task Can_Change_To_Simple_Signature()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1a, privateKey1a) = Generator.KeyPair();
        var (publicKey1b, privateKey1b) = Generator.KeyPair();
        var (publicKey2a, privateKey2a) = Generator.KeyPair();
        var (publicKey2b, privateKey2b) = Generator.KeyPair();
        var (publicKey3, privateKey3) = Generator.KeyPair();
        var endorsement = new Endorsement(new Endorsement(1, publicKey1a, publicKey1b), new Endorsement(1, publicKey2a, publicKey2b));
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        // Fail by not providing all necessary keys (note only one of the root keys here)
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(createResult.Address, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey3));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = createResult.Address,
            Endorsement = publicKey3,
            Signatory = privateKey3,
        }, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, privateKey1a, privateKey2a);
        });

        // Now try with proper set of signatures
        var record = await client.TransferAsync(createResult.Address, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey3));

        var balance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
    [Test]
    public async Task Can_Rotate_To_Complex_Signature()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey1a, privateKey1a) = Generator.KeyPair();
        var (publicKey1b, privateKey1b) = Generator.KeyPair();
        var (publicKey2a, privateKey2a) = Generator.KeyPair();
        var (publicKey2b, privateKey2b) = Generator.KeyPair();
        var (publicKey3a, privateKey3a) = Generator.KeyPair();
        var (publicKey3b, privateKey3b) = Generator.KeyPair();
        var (publicKey3c, privateKey3c) = Generator.KeyPair();
        var endorsement = new Endorsement(new Endorsement(1, publicKey1a, publicKey1b), new Endorsement(1, publicKey2a, publicKey2b));
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = endorsement
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        // Fail by not providing all necessary keys (note only one of the root keys here)
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(createResult.Address, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey3a));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = createResult.Address,
            Endorsement = new Endorsement(1, publicKey3a, publicKey3b, publicKey3c),
            Signatory = privateKey3a,
        }, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, privateKey1a, privateKey2a);
        });

        // Now try with proper set of signatures
        var record = await client.TransferAsync(createResult.Address, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey3c));

        var balance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(balance).IsEqualTo(0UL);
    }
}
