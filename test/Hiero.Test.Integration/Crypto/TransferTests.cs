using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System.Numerics;

namespace Hiero.Test.Integration.Crypto;

public class TransferTests
{
    [Test]
    public async Task Can_Transfer_Crypto_To_Gateway_Node()
    {
        long fee = 0;
        long transferAmount = 10;
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(ctx => fee = ctx.FeeLimit);
        var fromAccount = TestNetwork.Payer;
        var toAddress = await TestNetwork.GetConsensusNodeEndpointAsync();
        var balanceBefore = await client.GetAccountBalanceAsync(fromAccount);
        var receipt = await client.TransferAsync(fromAccount, toAddress, transferAmount);
        var balanceAfter = await client.GetAccountBalanceAsync(fromAccount);
        var maxFee = (ulong)(3 * fee);
        await Assert.That(balanceAfter >= balanceBefore - (ulong)transferAmount - maxFee && balanceAfter <= balanceBefore - (ulong)transferAmount).IsTrue();
    }

    [Test]
    public async Task Can_Transfer_Crypto_To_New_Account()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var newBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalance).IsEqualTo(fx.CreateParams.InitialBalance);

        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(fx.CreateParams.InitialBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Multi_Transfer_Crypto_To_New_Account()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var newBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalance).IsEqualTo(fx.CreateParams.InitialBalance);

        var transfers = new TransferParams
        {
            CryptoTransfers = new[] { new CryptoTransfer(TestNetwork.Payer, -transferAmount), new CryptoTransfer(fx.CreateReceipt!.Address, transferAmount) }
        };
        var receipt = await client.TransferAsync(transfers);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(fx.CreateParams.InitialBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Get_Transfer_Record_Showing_Transfers()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var newBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalance).IsEqualTo(fx.CreateParams.InitialBalance);

        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Transfers.Count >= 3 && record.Transfers.Count <= 6).IsTrue();
        await Assert.That(record.Transfers[TestNetwork.Payer]).IsEqualTo(-transferAmount - (long)record.Fee);
        await Assert.That(record.Transfers[fx.CreateReceipt!.Address]).IsEqualTo(transferAmount);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(fx.CreateParams.InitialBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Transfer_Crypto_From_New_Account()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = fx.CreateParams.InitialBalance / 2;
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fx.CreateReceipt!.Address);
        await Assert.That(info.Balance).IsEqualTo(fx.CreateParams.InitialBalance);
        await Assert.That(info.Endorsement).IsEqualTo(new Endorsement(fx.PublicKey));

        var receipt = await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, (long)transferAmount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey));
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(fx.CreateParams.InitialBalance - (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Transfer_Crypto_From_New_Account_Via_Dictionary()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)(fx.CreateParams.InitialBalance / 2);
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fx.CreateReceipt!.Address);
        await Assert.That(info.Balance).IsEqualTo(fx.CreateParams.InitialBalance);
        await Assert.That(info.Endorsement).IsEqualTo(new Endorsement(fx.PublicKey));
        var transfers = new TransferParams
        {
            CryptoTransfers = new[] { new CryptoTransfer(fx.CreateReceipt!.Address, -transferAmount), new CryptoTransfer(TestNetwork.Payer, transferAmount) },
            Signatory = fx.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(fx.CreateParams.InitialBalance - (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Transfer_All_Crypto_From_New_Account()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetAccountInfoAsync(fx.CreateReceipt!.Address);
        await Assert.That(info.Balance).IsEqualTo(fx.CreateParams.InitialBalance);
        await Assert.That(info.Endorsement).IsEqualTo(new Endorsement(fx.PublicKey));

        var receipt = await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, (long)fx.CreateParams.InitialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey));
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(0UL);
    }

    [Test]
    public async Task Insufficient_Funds_Throws_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)(fx.CreateParams.InitialBalance * 2);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("Transfer failed with status: InsufficientAccountBalance");
        await Assert.That(tex.TransactionId).IsNotNull();
        await Assert.That(tex.Status).IsEqualTo(ResponseCode.InsufficientAccountBalance);
    }

    [Test]
    public async Task Insufficient_Fee_Throws_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)(fx.CreateParams.InitialBalance / 2);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey);
                ctx.FeeLimit = 1;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: InsufficientTxFee");
        await Assert.That(pex.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
    }

    [Test]
    public async Task Can_Send_And_Receive_Multiple_Accounts()
    {
        var fx1 = await TestAccount.CreateAsync();
        var fx2 = await TestAccount.CreateAsync();
        var payer = TestNetwork.Payer;
        var account1 = fx1.CreateReceipt!.Address;
        var account2 = fx2.CreateReceipt!.Address;
        var sig1 = new Signatory(fx1.PrivateKey);
        var sig2 = new Signatory(fx2.PrivateKey);
        var transferAmount = (long)Generator.Integer(100, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( payer, -2 * transferAmount ),
                    new CryptoTransfer(account1, transferAmount ),
                    new CryptoTransfer(account2, transferAmount )
                }
        };
        var sendReceipt = await client.TransferAsync(transfers);
        await Assert.That(sendReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await client.GetAccountBalanceAsync(account1)).IsEqualTo((ulong)transferAmount + fx1.CreateParams.InitialBalance);
        await Assert.That(await client.GetAccountBalanceAsync(account2)).IsEqualTo((ulong)transferAmount + fx2.CreateParams.InitialBalance);
        transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( account1, -transferAmount ),
                    new CryptoTransfer( account2, -transferAmount ),
                    new CryptoTransfer( payer, 2 * transferAmount )
                },
            Signatory = new Signatory(sig1, sig2)
        };
        var returnReceipt = await client.TransferAsync(transfers, ctx => ctx.FeeLimit = 1_000_000);
        await Assert.That(returnReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await client.GetAccountBalanceAsync(account1)).IsEqualTo(fx1.CreateParams.InitialBalance);
        await Assert.That(await client.GetAccountBalanceAsync(account2)).IsEqualTo(fx2.CreateParams.InitialBalance);
    }

    [Test]
    public async Task Unblanced_Multi_Transfer_Requests_Raise_Error()
    {
        var fx1 = await TestAccount.CreateAsync();
        var fx2 = await TestAccount.CreateAsync();
        var payer = TestNetwork.Payer;
        var account1 = fx1.CreateReceipt!.Address;
        var account2 = fx2.CreateReceipt!.Address;
        var sig1 = new Signatory(fx1.PrivateKey);
        var sig2 = new Signatory(fx2.PrivateKey);
        var transferAmount = (long)Generator.Integer(100, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( payer, -transferAmount ),
                    new CryptoTransfer(account1, transferAmount ),
                    new CryptoTransfer(account2, transferAmount )
                }
        };
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transfers);
        }).ThrowsException();
        var aor = ex as ArgumentOutOfRangeException;
        await Assert.That(aor).IsNotNull();
        await Assert.That(aor!.ParamName).IsEqualTo("CryptoTransfers");
        await Assert.That(aor.Message).StartsWith("The sum of crypto sends and receives does not balance.");
    }

    [Test]
    public async Task Net_Zero_Transaction_Is_Allowed()
    {
        var fx1 = await TestAccount.CreateAsync();
        var fx2 = await TestAccount.CreateAsync();
        var payer = TestNetwork.Payer;
        var account1 = fx1.CreateReceipt!.Address;
        var account2 = fx2.CreateReceipt!.Address;
        var sig1 = new Signatory(fx1.PrivateKey);
        var sig2 = new Signatory(fx2.PrivateKey);
        var transferAmount = (long)Generator.Integer(100, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( account1, 0 ),
                    new CryptoTransfer( account2, 0 ),
                },
            Signatory = sig1
        };
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transfers);
        }).ThrowsException();
        var aor = ex as ArgumentOutOfRangeException;
        await Assert.That(aor).IsNotNull();
        await Assert.That(aor!.ParamName).IsEqualTo("CryptoTransfers");
        await Assert.That(aor.Message).StartsWith($"The amount to transfer crypto to/from 0.0.{account1.AccountNum} must be a value, negative for transfers out, and positive for transfers in. A value of zero is not allowed.");

        await Assert.That(await client.GetAccountBalanceAsync(account1)).IsEqualTo(fx1.CreateParams.InitialBalance);
        await Assert.That(await client.GetAccountBalanceAsync(account2)).IsEqualTo(fx2.CreateParams.InitialBalance);
    }

    [Test]
    public async Task Null_Send_Dictionary_Raises_Error()
    {
        var fx1 = await TestAccount.CreateAsync();
        var fx2 = await TestAccount.CreateAsync();
        var payer = TestNetwork.Payer;
        var transferAmount = (long)Generator.Integer(100, 200);
        TransferParams testParams = null!;
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(testParams);
        }).ThrowsException();
        var and = ex as ArgumentNullException;
        await Assert.That(and).IsNotNull();
        await Assert.That(and!.ParamName).IsEqualTo("transfers");
        await Assert.That(and.Message).StartsWith("The transfer parameters cannot be null.");
    }

    [Test]
    public async Task Missing_Send_Dictionary_Raises_Error()
    {
        var fx1 = await TestAccount.CreateAsync();
        var payer = TestNetwork.Payer;
        var transferAmount = (long)Generator.Integer(100, 200);

        var transfers = new TransferParams { CryptoTransfers = new CryptoTransfer[] { } };
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transfers);
        }).ThrowsException();
        var aor = ex as ArgumentOutOfRangeException;
        await Assert.That(aor).IsNotNull();
        await Assert.That(aor!.ParamName).IsEqualTo("CryptoTransfers");
        await Assert.That(aor.Message).StartsWith("The list of crypto transfers can not be empty.");
    }

    [Test]
    public async Task Transaction_Id_Makes_Sense_For_Receipt()
    {
        await using var fx = await TestAccount.CreateAsync();
        // Allow generous slack for clock drift adjustment between local and network time.
        var lowerBound = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300;
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        var upperBound = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300;
        var txId = receipt.TransactionId;
        await Assert.That(txId).IsNotNull();
        await Assert.That(txId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(txId.ValidStartSeconds >= lowerBound
                       && txId.ValidStartSeconds <= upperBound).IsTrue();
        await Assert.That(txId.ValidStartNanos >= 0 && txId.ValidStartNanos <= 1_000_000_000).IsTrue();
    }

    [Test]
    public async Task Transaction_Id_Makes_Sense_For_Record()
    {
        await using var fx = await TestAccount.CreateAsync();
        var lowerBound = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300;
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        var upperBound = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300;
        var txId = receipt.TransactionId;
        await Assert.That(txId).IsNotNull();
        await Assert.That(txId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(txId.ValidStartSeconds >= lowerBound
                       && txId.ValidStartSeconds <= upperBound).IsTrue();
        await Assert.That(txId.ValidStartNanos >= 0 && txId.ValidStartNanos <= 1_000_000_000).IsTrue();
    }

    [Test]
    public async Task Transfer_Receipt_Contains_Exchange_Information()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        await Assert.That(receipt.CurrentExchangeRate).IsNotNull();
        // Well, testnet doesn't actually have good data here
        await Assert.That(receipt.CurrentExchangeRate.Expiration >= ConsensusTimeStamp.MinValue && receipt.CurrentExchangeRate.Expiration <= ConsensusTimeStamp.MaxValue).IsTrue();
        await Assert.That(receipt.NextExchangeRate).IsNotNull();
        await Assert.That(receipt.NextExchangeRate.Expiration >= ConsensusTimeStamp.MinValue && receipt.NextExchangeRate.Expiration <= ConsensusTimeStamp.MaxValue).IsTrue();
    }

    [Test]
    public async Task Transfer_Record_Contains_Exchange_Information()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount);
        await Assert.That(receipt.CurrentExchangeRate).IsNotNull();
        // Well, testnet doesn't actually have good data here
        await Assert.That(receipt.CurrentExchangeRate.Expiration >= ConsensusTimeStamp.MinValue && receipt.CurrentExchangeRate.Expiration <= DateTime.MaxValue).IsTrue();
        await Assert.That(receipt.NextExchangeRate).IsNotNull();
        await Assert.That(receipt.NextExchangeRate.Expiration >= ConsensusTimeStamp.MinValue && receipt.NextExchangeRate.Expiration <= DateTime.MaxValue).IsTrue();
    }

    [Test]
    public async Task Transfer_To_A_Topic_Raises_Error()
    {
        var fx1 = await TestAccount.CreateAsync();
        var fx2 = await TestTopic.CreateAsync();
        var payer = TestNetwork.Payer;
        var transferAmount = (long)Generator.Integer(1, (int)fx1.CreateParams.InitialBalance);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx1.CreateReceipt!.Address, fx2.CreateReceipt!.Topic, transferAmount);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Insufficient_Fee_Exception_Includes_Required_Fee_Defect()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)(fx.CreateParams.InitialBalance / 2);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey);
                ctx.FeeLimit = 1;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: InsufficientTxFee");
        await Assert.That(pex.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
        await Assert.That(pex.RequiredFee > 0).IsTrue();

        var ex2 = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey);
                ctx.FeeLimit = (long)pex.RequiredFee;
            });
        }).ThrowsException();
        var tex = ex2 as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InsufficientTxFee");

        var balance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(balance).IsEqualTo(fx.CreateParams.InitialBalance);
    }

    [Test]
    public async Task Insufficient_Fee_Exception_Includes_Required_Fee_For_Record()
    {
        await using var fx = await TestAccount.CreateAsync();
        var transferAmount = (long)(fx.CreateParams.InitialBalance / 2);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey);
                ctx.FeeLimit = 1;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: InsufficientTxFee");
        await Assert.That(pex.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
        await Assert.That(pex.RequiredFee > 0).IsTrue();

        var ex2 = await Assert.That(async () =>
        {
            await client.TransferAsync(fx.CreateReceipt!.Address, TestNetwork.Payer, transferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.PrivateKey);
                ctx.FeeLimit = (long)pex.RequiredFee;
            });
        }).ThrowsException();
        var tex = ex2 as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InsufficientTxFee");

        var balance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(balance).IsEqualTo(fx.CreateParams.InitialBalance);
    }

    [Test]
    public async Task Allows_Duplicate_Signature()
    {
        await using var fxReceiver = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var payerKey = LoadEd25519Key(TestNetwork.PrivateKey);
        var publicPrefix = payerKey.GeneratePublicKey().GetEncoded().Take(6).ToArray();

        // Custom signer that adds the same signature twice under the same prefix
        Task CustomSigner(IInvoice invoice)
        {
            var goodSignature1 = SignBytes(payerKey, invoice.TransactionBytes.ToArray());
            var goodSignature2 = SignBytes(payerKey, invoice.TransactionBytes.ToArray());
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, goodSignature1);
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, goodSignature2);
            return Task.CompletedTask;
        }

        var receipt = await client.TransferAsync(TestNetwork.Payer, fxReceiver.CreateReceipt!.Address, 100, ctx =>
        {
            ctx.Signatory = new Signatory(CustomSigner);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    private static byte[] SignBytes(Ed25519PrivateKeyParameters key, byte[] bytes)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, key);
        signer.BlockUpdate(bytes, 0, bytes.Length);
        return signer.GenerateSignature();
    }

    private static Ed25519PrivateKeyParameters LoadEd25519Key(ReadOnlyMemory<byte> privateKey)
    {
        // Match the SDK's logic: accept either raw 32-byte seed or DER-encoded form.
        if (privateKey.Length == 32)
        {
            return new Ed25519PrivateKeyParameters(privateKey.ToArray(), 0);
        }
        return (Ed25519PrivateKeyParameters)PrivateKeyFactory.CreateKey(privateKey.ToArray());
    }

    [Test]
    public async Task Inconsistent_Duplicate_Signature_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var goodKey = LoadEd25519Key(TestNetwork.PrivateKey);
        var fakeKey1 = new Ed25519PrivateKeyParameters(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32), 0);
        var fakeKey2 = new Ed25519PrivateKeyParameters(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32), 0);
        var publicPrefix = goodKey.GeneratePublicKey().GetEncoded().Take(6).ToArray();

        // Scenario 1: Good signature first, bad signature second
        Task CustomSigner(IInvoice invoice)
        {
            var goodSignature = SignBytes(goodKey, invoice.TransactionBytes.ToArray());
            var badSignature = SignBytes(fakeKey1, invoice.TransactionBytes.ToArray());
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, goodSignature);
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, badSignature);
            return Task.CompletedTask;
        }
        var ex1 = await Assert.That(async () =>
        {
            await client.TransferAsync(TestNetwork.Payer, TestNetwork.Payer, 100, ctx =>
            {
                ctx.Signatory = new Signatory(CustomSigner);
            });
        }).ThrowsException();
        var aex1 = ex1 as ArgumentException;
        await Assert.That(aex1).IsNotNull();
        await Assert.That(aex1!.Message).StartsWith("Signature with Duplicate Prefix Identifier was provided, but did not have an Identical Signature.");

        // Scenario 2: Bad signature first, good signature second
        Task CustomSignerReverse(IInvoice invoice)
        {
            var goodSignature = SignBytes(goodKey, invoice.TransactionBytes.ToArray());
            var badSignature = SignBytes(fakeKey1, invoice.TransactionBytes.ToArray());
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, badSignature);
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, goodSignature);
            return Task.CompletedTask;
        }
        var ex2 = await Assert.That(async () =>
        {
            await client.TransferAsync(TestNetwork.Payer, TestNetwork.Payer, 100, ctx =>
            {
                ctx.Signatory = new Signatory(CustomSignerReverse);
            });
        }).ThrowsException();
        var aex2 = ex2 as ArgumentException;
        await Assert.That(aex2).IsNotNull();
        await Assert.That(aex2!.Message).StartsWith("Signature with Duplicate Prefix Identifier was provided, but did not have an Identical Signature.");

        // Scenario 3: Two different bad signatures
        Task CustomSignerBothBad(IInvoice invoice)
        {
            var badSignature1 = SignBytes(fakeKey1, invoice.TransactionBytes.ToArray());
            var badSignature2 = SignBytes(fakeKey2, invoice.TransactionBytes.ToArray());
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, badSignature2);
            invoice.AddSignature(KeyType.Ed25519, publicPrefix, badSignature1);
            return Task.CompletedTask;
        }
        // Inconsistent state should be checked before signature validation
        var ex3 = await Assert.That(async () =>
        {
            await client.TransferAsync(TestNetwork.Payer, TestNetwork.Payer, 100, ctx =>
            {
                ctx.Signatory = new Signatory(CustomSignerBothBad);
            });
        }).ThrowsException();
        var aex3 = ex3 as ArgumentException;
        await Assert.That(aex3).IsNotNull();
        await Assert.That(aex3!.Message).StartsWith("Signature with Duplicate Prefix Identifier was provided, but did not have an Identical Signature.");
    }

    [Test]
    public async Task Can_Transfer_Crypto_To_New_Alias_Account()
    {
        await using var fxAlias = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var startingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);

        var transferAmount = (long)Generator.Integer(10, 100);

        var receipt = await client.TransferAsync(TestNetwork.Payer, fxAlias.Alias, transferAmount);

        var endingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        await Assert.That(endingBalance).IsEqualTo(startingBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Send_From_Alias_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxAlias = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var aliasStartingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        var transferAmount = (aliasStartingBalance) / 2 + 1;

        var accountStartingBalance = await client.GetAccountBalanceAsync(fxAccount);
        var receipt = await client.TransferAsync(fxAlias.Alias, fxAccount.CreateReceipt!.Address, (long)transferAmount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxAlias.PrivateKey));
        var accountEndingBalance = await client.GetAccountBalanceAsync(fxAccount);
        await Assert.That(accountEndingBalance).IsEqualTo(accountStartingBalance + (ulong)transferAmount);

        var aliasEndingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        await Assert.That(aliasEndingBalance).IsEqualTo(aliasStartingBalance - (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Not_Use_Alias_As_Payer()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxAlias = await TestAliasAccount.CreateAsync(fx => fx.InitialTransfer = 5_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var aliasStartingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        var transferAmount = (aliasStartingBalance) / 2 + 1;

        var accountStartingBalance = await client.GetAccountBalanceAsync(fxAccount);

        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(fxAlias.Alias, fxAccount.CreateReceipt!.Address, (long)transferAmount, ctx =>
            {
                ctx.Payer = fxAlias.Alias;
                ctx.Signatory = fxAlias.PrivateKey;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: PayerAccountNotFound");
        await Assert.That(pex.Status).IsEqualTo(ResponseCode.PayerAccountNotFound);

        var accountEndingBalance = await client.GetAccountBalanceAsync(fxAccount);
        var aliasEndingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        await Assert.That(accountEndingBalance).IsEqualTo((ulong)accountStartingBalance);
        await Assert.That(aliasEndingBalance).IsEqualTo((ulong)aliasStartingBalance);

        var receipt = await client.TransferAsync(fxAlias.Alias, fxAccount.CreateReceipt!.Address, (long)transferAmount, ctx =>
        {
            ctx.Payer = fxAlias.CreateReceipt!.Address;
            ctx.Signatory = fxAlias.PrivateKey;
        });

        accountEndingBalance = await client.GetAccountBalanceAsync(fxAccount);
        aliasEndingBalance = await client.GetAccountBalanceAsync(fxAlias.Alias);
        await Assert.That(accountEndingBalance).IsEqualTo(accountStartingBalance + (ulong)transferAmount);
        // Don't forget fees.
        await Assert.That(aliasEndingBalance < aliasStartingBalance - (ulong)transferAmount).IsTrue();
    }

    [Test]
    public async Task Transfer_With_Signaled_Cancellation_Token_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fx.CreateParams.InitialBalance);
        var transferAmount = (long)Generator.Integer(10, 100);
        var transferParams = new TransferParams
        {
            CryptoTransfers = [
                    new CryptoTransfer(TestNetwork.Payer, -transferAmount),
                    new CryptoTransfer(fx.CreateReceipt!.Address, transferAmount)
                ],
            CancellationToken = new CancellationToken(true)
        };
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transferParams);
        }).ThrowsException();
        var oce = ex as OperationCanceledException;
        await Assert.That(oce).IsNotNull();
        var currentBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(currentBalance).IsEqualTo(originalBalance);
    }

    [Test]
    public async Task Signaling_Cancel_After_Start_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fx.CreateParams.InitialBalance);
        var transferAmount = (long)Generator.Integer(10, 100);
        var cancellationSource = new CancellationTokenSource();
        var transferParams = new TransferParams
        {
            CryptoTransfers = [
                    new CryptoTransfer(TestNetwork.Payer, -transferAmount),
                    new CryptoTransfer(fx.CreateReceipt!.Address, transferAmount)
                ],
            CancellationToken = cancellationSource.Token
        };
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transferParams, ctx =>
            {
                ctx.OnSendingRequest = _ => cancellationSource.Cancel();
            });
        }).ThrowsException();
        var oce = ex as OperationCanceledException;
        await Assert.That(oce).IsNotNull();
        var currentBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(currentBalance).IsEqualTo(originalBalance);
    }

    [Test]
    public async Task Signaling_Cancel_After_Response_Before_Receipt_Does_Raise_Error_But_Still_Succeeds()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fx.CreateParams.InitialBalance);
        var transferAmount = (long)Generator.Integer(10, 100);
        var cancellationSource = new CancellationTokenSource();
        var transferParams = new TransferParams
        {
            CryptoTransfers = [
                    new CryptoTransfer(TestNetwork.Payer, -transferAmount),
                    new CryptoTransfer(fx.CreateReceipt!.Address, transferAmount)
                ],
            CancellationToken = cancellationSource.Token
        };
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transferParams, ctx =>
            {
                ctx.OnResponseReceived = (_, _) => cancellationSource.Cancel();
            });
        }).ThrowsException();
        var oce = ex as OperationCanceledException;
        await Assert.That(oce).IsNotNull();
        // Yes, there is a race condition, the balance may not be processed
        // yet by the node (consensus reached) so we want to wait long enough
        // to ensure the transaction has reached consensus, waiting for the
        // mirror node to catch up is a proxy for this.
        await TestNetwork.GetMirrorRestClientAsync();
        var currentBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(currentBalance).IsEqualTo(originalBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Signaling_Cancel_After_Consensus_Does_Not_Raise_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fx.CreateParams.InitialBalance);
        var transferAmount = (long)Generator.Integer(10, 100);
        var cancellationSource = new CancellationTokenSource();
        var transferParams = new TransferParams
        {
            CryptoTransfers = [
                    new CryptoTransfer(TestNetwork.Payer, -transferAmount),
                    new CryptoTransfer(fx.CreateReceipt!.Address, transferAmount)
                ],
            CancellationToken = cancellationSource.Token
        };
        var receipt = await client.TransferAsync(transferParams, ctx =>
        {
            // This could randomly fail, being lazy about the timing.
            ctx.OnResponseReceived = (_, _) => cancellationSource.CancelAfter(TimeSpan.FromSeconds(3));
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var currentBalance = await client.GetAccountBalanceAsync(fx.CreateReceipt!.Address);
        await Assert.That(currentBalance).IsEqualTo(originalBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Transfer_Via_Ethereum_Transaction_From_Hydrated_EVM_Account()
    {
        await using var fxReceiver = await TestAccount.CreateAsync();
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var senderInitialBalance = Generator.Integer(10, 20) * 1_00_000_000;
        var senderEndorsement = new Endorsement(publicKey);
        var senderEvmAddress = new EvmAddress(senderEndorsement);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, senderEvmAddress, senderInitialBalance);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var senderHapiAddress = ((CreateAccountReceipt)receipts[1]).Address;
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var chainId = await mirror.GetChainIdAsync();
        var balance = (await mirror.GetAccountAsync(senderEvmAddress))!.Balances.Balance;
        await Assert.That(balance).IsEqualTo(senderInitialBalance);

        var transferAmount = senderInitialBalance / 3;
        // Note: have to hydrate via being the payer, don't forget the full prefix
        // required for the system to figure out the public key part, eesh.
        await client.TransferAsync(senderHapiAddress, fxReceiver, transferAmount, ctx =>
        {
            ctx.Payer = senderHapiAddress;
            ctx.Signatory = privateKey;
            ctx.SignaturePrefixTrimLimit = int.MaxValue;
        });
        var receiverNewBalance = await client.GetAccountBalanceAsync(fxReceiver);
        await Assert.That(receiverNewBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance + (ulong)transferAmount);

        var transaction = new EvmTransactionInput
        {
            EvmNonce = 0,
            GasPrice = 0,
            GasLimit = 27_000,
            ToEvmAddress = fxReceiver.CreateReceipt!.Address.CastToEvmAddress(),
            ValueInTinybars = transferAmount,
            ChainId = chainId,
        }.RlpEncode(privateKey);

        await client.ExecuteEvmTransactionAsync(new EvmTransactionParams
        {
            Transaction = transaction,
            AdditionalGasAllowance = 10_00_000_000
        }, ctx => ctx.FeeLimit = 20_00_000_000);
        receiverNewBalance = await client.GetAccountBalanceAsync(fxReceiver);
        await Assert.That(receiverNewBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance + (ulong)transferAmount * 2);
    }

    [Test]
    public async Task Can_Transfer_Via_Ethereum_Transaction_From_Non_Hydrated_EVM_Account()
    {
        await using var fxReceiver = await TestAccount.CreateAsync();
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var senderInitialBalance = Generator.Integer(10, 20) * 1_00_000_000;
        var senderEndorsement = new Endorsement(publicKey);
        var senderEvmAddress = new EvmAddress(senderEndorsement);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferAsync(TestNetwork.Payer, senderEvmAddress, senderInitialBalance);
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var senderHapiAddress = ((CreateAccountReceipt)receipts[1]).Address;
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var chainId = await mirror.GetChainIdAsync();
        var balance = (await mirror.GetAccountAsync(senderEvmAddress))!.Balances.Balance;
        await Assert.That(balance).IsEqualTo(senderInitialBalance);

        var transferAmount = senderInitialBalance / 3;

        var transaction = new EvmTransactionInput
        {
            EvmNonce = 0,
            GasPrice = 0,
            GasLimit = 27_000,
            ToEvmAddress = fxReceiver.CreateReceipt!.Address.CastToEvmAddress(),
            ValueInTinybars = transferAmount,
            ChainId = chainId,
        }.RlpEncode(privateKey);

        await client.ExecuteEvmTransactionAsync(new EvmTransactionParams
        {
            Transaction = transaction,
            AdditionalGasAllowance = 10_00_000_000
        }, ctx => ctx.FeeLimit = 20_00_000_000);
        var receiverNewBalance = await client.GetAccountBalanceAsync(fxReceiver);
        await Assert.That(receiverNewBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance + (ulong)transferAmount);
    }
}
