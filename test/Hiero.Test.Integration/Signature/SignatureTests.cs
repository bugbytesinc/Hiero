using Hiero.Implementation;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.X509;
using System.Text;

namespace Hiero.Test.Integration.Signature;

public class SignatureTests
{
    private static Ed25519PrivateKeyParameters ImportPrivateEd25519Key(ReadOnlyMemory<byte> privateKey)
    {
        // DER-encoded Ed25519 private key: 16-byte prefix + 32-byte raw key
        var raw = privateKey.Span.Slice(16, 32).ToArray();
        return new Ed25519PrivateKeyParameters(raw, 0);
    }

    private static byte[] GetPublicKeyDerBytes(Ed25519PrivateKeyParameters keyParams)
    {
        return SubjectPublicKeyInfoFactory
            .CreateSubjectPublicKeyInfo(keyParams.GeneratePublicKey())
            .GetDerEncoded();
    }

    private static byte[] SignEd25519(Ed25519PrivateKeyParameters keyParams, ReadOnlySpan<byte> data)
    {
        var sig = new byte[Ed25519.SignatureSize];
        var privateBytes = new byte[Ed25519.SecretKeySize];
        keyParams.Encode(privateBytes, 0);
        var dataArray = data.ToArray();
        Ed25519.Sign(privateBytes, 0, dataArray, 0, dataArray.Length, sig, 0);
        return sig;
    }

    [Test]
    public async Task Can_Sign_Transaction_With_Extra_Signature()
    {
        var (_, privateKey) = Generator.KeyPair();
        await using var fx = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var client = await TestNetwork.CreateClientAsync();
        await AssertHg.CryptoBalanceAsync(fx, 0);

        var transferAmount = Generator.Integer(10, 100);
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, privateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.CryptoBalanceAsync(fx, (ulong)transferAmount);
    }

    [Test]
    public async Task Unrelated_Public_Keys_Can_Sign_Unrelated_Message()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();
        await using var fx = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var client = await TestNetwork.CreateClientAsync();
        await AssertHg.CryptoBalanceAsync(fx, 0);

        var transferAmount = Generator.Integer(10, 100);
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, new Signatory(CustomSigner));
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.CryptoBalanceAsync(fx, (ulong)transferAmount);

        Task CustomSigner(IInvoice invoice)
        {
            var randomBytes = Generator.SHA384Hash();
            var signingKey = ImportPrivateEd25519Key(privateKey);
            var pubKeyDer = GetPublicKeyDerBytes(signingKey);
            var prefix = pubKeyDer.TakeLast(32).Take(6).ToArray();
            var signature = SignEd25519(signingKey, randomBytes.Span);
            invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Can_Embed_Messages_In_The_Signature_Map()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();
        await using var fx = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var client = await TestNetwork.CreateClientAsync();
        await AssertHg.CryptoBalanceAsync(fx, 0);

        var transferAmount = Generator.Integer(10, 100);
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, new Signatory(CustomSigner));
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.CryptoBalanceAsync(fx, (ulong)transferAmount);

        Task CustomSigner(IInvoice invoice)
        {
            var message = Encoding.ASCII.GetBytes("This is an Embedded Message");
            var signingKey = ImportPrivateEd25519Key(privateKey);
            var signature = SignEd25519(signingKey, message);
            invoice.AddSignature(KeyType.Ed25519, message, signature);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Can_Embed_Messages_In_The_Signature_Itself()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();
        await using var fx = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var client = await TestNetwork.CreateClientAsync();
        await AssertHg.CryptoBalanceAsync(fx, 0);

        var transferAmount = Generator.Integer(10, 100);
        var receipt = await client.TransferAsync(TestNetwork.Payer, fx.CreateReceipt!.Address, transferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, new Signatory(CustomSigner));
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.CryptoBalanceAsync(fx, (ulong)transferAmount);

        Task CustomSigner(IInvoice invoice)
        {
            var message = Encoding.ASCII.GetBytes("This is an Embedded Message");
            invoice.AddSignature(KeyType.Ed25519, message, message);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Signature_Map_No_Prefix_With_Trim_Of_Zero_And_One_Signature()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (_, privateKey) = Generator.Ed25519KeyPair();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, 0, default);
        var signatory = new Signatory(CustomSigner);
        await (signatory as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair).HasSingleItem();
        await Assert.That(signatureMap.SigPair[0].PubKeyPrefix.IsEmpty).IsTrue();

        Task CustomSigner(IInvoice invoice)
        {
            var signingKey = ImportPrivateEd25519Key(privateKey);
            var prefix = GetPublicKeyDerBytes(signingKey);
            var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
            invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Signature_Map_With_One_Signature_And_Trim_Limit_Includes_Prefix()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (_, privateKey) = Generator.Ed25519KeyPair();
        var trimLimit = Generator.Integer(5, 10);
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, trimLimit, default);
        var signatory = new Signatory(CustomSigner);
        await (signatory as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair).HasSingleItem();
        await Assert.That(signatureMap.SigPair[0].PubKeyPrefix.Length).IsEqualTo(trimLimit);

        Task CustomSigner(IInvoice invoice)
        {
            var signingKey = ImportPrivateEd25519Key(privateKey);
            var prefix = GetPublicKeyDerBytes(signingKey);
            var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
            invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Multiple_Keys_With_Similar_Starting_Prefixes_Still_Unique()
    {
        var prefix = Encoding.ASCII.GetBytes(Generator.String(10, 20));
        var sigCount = Generator.Integer(5, 10);
        await using var client = await TestNetwork.CreateClientAsync();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, 0, default);
        await (new Signatory(CustomSigner) as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair.Count).IsEqualTo(sigCount);
        foreach (var sig in signatureMap.SigPair)
        {
            await Assert.That(sig.PubKeyPrefix.Length).IsEqualTo(prefix.Length);
        }

        Task CustomSigner(IInvoice invoice)
        {
            var signingKey = ImportPrivateEd25519Key(Generator.Ed25519KeyPair().privateKey);
            var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
            for (int i = 0; i < sigCount; i++)
            {
                var thumbprint = (prefix.Clone() as byte[])!;
                thumbprint[thumbprint.Length - 1] = (byte)i;
                invoice.AddSignature(KeyType.Ed25519, thumbprint, signature);
            }
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Prefix_Trim_Limit_Is_Respected()
    {
        var prefix = Encoding.ASCII.GetBytes(Generator.String(10, 20));
        var sigCount = Generator.Integer(5, 10);
        await using var client = await TestNetwork.CreateClientAsync();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, prefix.Length + 10, default);
        await (new Signatory(CustomSigner) as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair.Count).IsEqualTo(sigCount);
        foreach (var sig in signatureMap.SigPair)
        {
            await Assert.That(sig.PubKeyPrefix.Length).IsEqualTo(prefix.Length);
        }

        Task CustomSigner(IInvoice invoice)
        {
            var signingKey = ImportPrivateEd25519Key(Generator.Ed25519KeyPair().privateKey);
            var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
            for (int i = 0; i < sigCount; i++)
            {
                var thumbprint = (prefix.Clone() as byte[])!;
                thumbprint[thumbprint.Length - 1] = (byte)i;
                invoice.AddSignature(KeyType.Ed25519, thumbprint, signature);
            }
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Prefix_Trim_Accounts_For_Short_Prefixes()
    {
        var sigCount = Generator.Integer(5, 10);
        var prefix = Encoding.ASCII.GetBytes(Generator.Code(sigCount + 10));
        await using var client = await TestNetwork.CreateClientAsync();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, sigCount - 3, default);
        await (new Signatory(CustomSigner) as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair.Count).IsEqualTo(sigCount);
        for (int i = 0; i < signatureMap.SigPair.Count; i++)
        {
            await Assert.That(signatureMap.SigPair[i].PubKeyPrefix.Length).IsEqualTo(i + 1);
        }
        Task CustomSigner(IInvoice invoice)
        {
            var signingKey = ImportPrivateEd25519Key(Generator.Ed25519KeyPair().privateKey);
            var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
            for (int i = 0; i < sigCount; i++)
            {
                var thumbprint = prefix.Take(1 + i).ToArray();
                invoice.AddSignature(KeyType.Ed25519, thumbprint, signature);
            }
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Duplicate_Signatures_Are_Reduced()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (_, privateKey) = Generator.Ed25519KeyPair();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, 0, default);
        var signatory = new Signatory(CustomSigner);
        await (signatory as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair).HasSingleItem();
        await Assert.That(signatureMap.SigPair[0].PubKeyPrefix.IsEmpty).IsTrue();

        Task CustomSigner(IInvoice invoice)
        {
            for (int i = 0; i < Generator.Integer(3, 5); i++)
            {
                var signingKey = ImportPrivateEd25519Key(privateKey);
                var prefix = GetPublicKeyDerBytes(signingKey);
                var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
                invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            }
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Some_Duplicate_Signatures_Are_Reduced()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var (_, privateKey1) = Generator.Ed25519KeyPair();
        var (_, privateKey2) = Generator.Ed25519KeyPair();
        var invoice = new Invoice(new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(client.CreateNewTransactionId()),
            Memo = Generator.Memo(20, 30)
        }, 0, default);
        var signatory = new Signatory(CustomSigner);
        await (signatory as ISignatory).SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true);
        var signatureMap = signedTransaction.SigMap;
        await Assert.That(signatureMap.SigPair.Count).IsEqualTo(2);

        Task CustomSigner(IInvoice invoice)
        {
            for (int i = 0; i < Generator.Integer(3, 5); i++)
            {
                var signingKey = ImportPrivateEd25519Key(privateKey1);
                var prefix = GetPublicKeyDerBytes(signingKey);
                var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
                invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            }
            for (int i = 0; i < Generator.Integer(3, 5); i++)
            {
                var signingKey = ImportPrivateEd25519Key(privateKey2);
                var prefix = GetPublicKeyDerBytes(signingKey);
                var signature = SignEd25519(signingKey, invoice.TransactionBytes.Span);
                invoice.AddSignature(KeyType.Ed25519, prefix, signature);
            }
            return Task.CompletedTask;
        }
    }
}
