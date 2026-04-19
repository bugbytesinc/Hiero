using Google.Protobuf;
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Proto;

namespace Hiero.Test.Integration.Network;

public class ExternalTransactionTests
{
    [Test]
    public async Task Can_Transfer_Crypto_Via_External_Transaction_With_No_Signatories()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var senderInitialBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverInitialBalance = await client.GetAccountBalanceAsync(fxReceiver);

        var xferAmount = (long)(fxSender.CreateParams.InitialBalance / 2);

        var noSignClient = client.Clone(ctx =>
        {
            ctx.Payer = null;
            ctx.Signatory = null;
        });

        var txid = client.CreateNewTransactionId(ctx => ctx.Payer = fxSender);
        var transfers = new Proto.TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -xferAmount
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = xferAmount
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new Proto.CryptoTransferTransactionBody { Transfers = transfers }
        };
        var invoice = new Invoice(body, 6, default);
        var senderSignatory = new Signatory(fxSender.PrivateKey) as ISignatory;
        await senderSignatory.SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true).ToByteString().Memory;

        var receipt = await noSignClient.ExecuteExternalTransactionAsync(signedTransaction);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsEqualTo(txid);

        var senderFinalBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverFinalBalance = await client.GetAccountBalanceAsync(fxReceiver);

        await Assert.That(senderFinalBalance < (ulong)((long)senderInitialBalance - xferAmount)).IsTrue();
        await Assert.That(receiverFinalBalance).IsEqualTo(receiverInitialBalance + (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Crypoto_Via_External_Unsigned_Transaction_Local_Payer()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var senderInitialBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverInitialBalance = await client.GetAccountBalanceAsync(fxReceiver);

        var xferAmount = (long)(fxSender.CreateParams.InitialBalance / 2);

        var senderClient = client.Clone(ctx =>
        {
            ctx.Payer = fxSender;
            ctx.Signatory = fxSender.PrivateKey;
        });

        var txid = senderClient.CreateNewTransactionId();
        var transfers = new Proto.TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -xferAmount
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = xferAmount
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new Proto.CryptoTransferTransactionBody { Transfers = transfers }
        };
        var signedTransaction = new SignedTransaction
        {
            BodyBytes = body.ToByteString()
        };

        var receipt = await senderClient.ExecuteExternalTransactionAsync(signedTransaction.ToByteArray());
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsEqualTo(txid);

        var senderFinalBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverFinalBalance = await client.GetAccountBalanceAsync(fxReceiver);

        await Assert.That(senderFinalBalance < (ulong)((long)senderInitialBalance - xferAmount)).IsTrue();
        await Assert.That(receiverFinalBalance).IsEqualTo(receiverInitialBalance + (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Crypoto_Via_Signed_External_Transaction_With_Local_Payer()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var senderInitialBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverInitialBalance = await client.GetAccountBalanceAsync(fxReceiver);

        var xferAmount = (long)(fxSender.CreateParams.InitialBalance / 2);

        var txid = client.CreateNewTransactionId();
        var transfers = new Proto.TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -xferAmount
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = xferAmount
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new CryptoTransferTransactionBody { Transfers = transfers }
        };
        var invoice = new Invoice(body, 32, default);
        var senderSignatory = new Signatory(fxSender.PrivateKey) as ISignatory;
        await senderSignatory.SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true).ToByteString().Memory;

        var receipt = await client.ExecuteExternalTransactionAsync(signedTransaction);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsEqualTo(txid);

        var senderFinalBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverFinalBalance = await client.GetAccountBalanceAsync(fxReceiver);

        await Assert.That(senderFinalBalance).IsEqualTo(senderInitialBalance - (ulong)xferAmount);
        await Assert.That(receiverFinalBalance).IsEqualTo(receiverInitialBalance + (ulong)xferAmount);
    }

    [Test]
    public async Task Empty_Protobuf_Array_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.ExecuteExternalTransactionAsync(ReadOnlyMemory<byte>.Empty);
        }).ThrowsException();
        var ae = ex as ArgumentOutOfRangeException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("SignedTransactionBytes");
        await Assert.That(ae.Message).StartsWith("Missing Signed Transaction Bytes (was empty).");
    }

    [Test]
    public async Task Empty_Body_Bytes_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var signedTx = new SignedTransaction();

        var ex = await Assert.That(async () =>
        {
            await client.ExecuteExternalTransactionAsync(signedTx.ToByteArray());
        }).ThrowsException();
        var ae = ex as ArgumentOutOfRangeException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("SignedTransactionBytes");
        await Assert.That(ae.Message).StartsWith("Missing Signed Transaction Bytes (was empty).");
    }

    [Test]
    public async Task Unknown_Transaction_Body_Type_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var signedTx = new SignedTransaction
        {
            BodyBytes = new Proto.TransactionID
            {
                AccountID = new Proto.AccountID(new EntityId(0, 0, Generator.Integer(100, 200))),
                TransactionValidStart = new Proto.Timestamp { Seconds = Generator.Integer(1000, 9999) }
            }.ToByteString()
        };

        var ex = await Assert.That(async () =>
        {
            await client.ExecuteExternalTransactionAsync(signedTx.ToByteArray());
        }).ThrowsException();
        var ae = ex as ArgumentOutOfRangeException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("SignedTransactionBytes");
        await Assert.That(ae.Message).StartsWith("Unrecognized Transaction Type, unable to determine which Hedera Network Service Type should process transaction.");
    }

    [Test]
    public async Task Invalid_Gateway_Raises_Error()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var nullEndpointClient = client.Clone(ctx => ctx.Endpoint = null);

        var txid = client.CreateNewTransactionId();
        var transfers = new TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -1
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = 1
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new CryptoTransferTransactionBody { Transfers = transfers }
        };
        var invoice = new Invoice(body, 10, default);
        var senderSignatory = new Signatory(fxSender.PrivateKey) as ISignatory;
        await senderSignatory.SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true).ToByteString().Memory;

        var receipt = await client.ExecuteExternalTransactionAsync(signedTransaction, ctx => ctx.SignaturePrefixTrimLimit = 10);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsEqualTo(txid);

        var ex = await Assert.That(async () =>
        {
            await nullEndpointClient.ExecuteExternalTransactionAsync(signedTransaction);
        }).ThrowsException();
        var ioe = ex as InvalidOperationException;
        await Assert.That(ioe).IsNotNull();
        await Assert.That(ioe!.Message).StartsWith("The Network Gateway Node has not been configured. Please check that 'Gateway' is set in the Client context and is compatible with this external transaction.");
    }

    [Test]
    public async Task Gateway_Mismatch_Raises_Error()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var mismatchClient = client.Clone(ctx =>
        {
            var old = ctx.Endpoint!;
            ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(old.Node.ShardNum, old.Node.RealmNum, old.Node.AccountNum + 1), old.Uri);
        });

        var txid = client.CreateNewTransactionId();
        var transfers = new TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -1
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = 1
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new CryptoTransferTransactionBody { Transfers = transfers }
        };
        var invoice = new Invoice(body, 10, default);
        var senderSignatory = new Signatory(fxSender.PrivateKey) as ISignatory;
        await senderSignatory.SignAsync(invoice);
        var signedTransaction = invoice.GenerateSignedTransactionFromSignatures(true).ToByteString().Memory;

        var receipt = await client.ExecuteExternalTransactionAsync(signedTransaction, ctx => ctx.SignaturePrefixTrimLimit = 10);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsEqualTo(txid);

        var ex = await Assert.That(async () =>
        {
            await mismatchClient.ExecuteExternalTransactionAsync(signedTransaction);
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("SignedTransactionBytes");
        await Assert.That(ae.Message).StartsWith("The configured Gateway is not compatible with the Node Account ID of this transaction.");
    }

    [Test]
    public async Task Submitting_With_No_Signatures_Fails()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();
        await using var client = TestNetwork.CreateClient(gateway);

        var noSignClient = client.Clone(ctx =>
        {
            ctx.Payer = fxSender;
            ctx.Signatory = null;
        });
        var txid = client.CreateNewTransactionId();
        var transfers = new TransferList();
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxSender.CreateReceipt!.Address),
            Amount = -1
        });
        transfers.AccountAmounts.Add(new Proto.AccountAmount
        {
            AccountID = new Proto.AccountID(fxReceiver.CreateReceipt!.Address),
            Amount = 1
        });
        var body = new Proto.TransactionBody
        {
            TransactionID = new Proto.TransactionID(txid),
            NodeAccountID = new Proto.AccountID(gateway),
            TransactionFee = 5_00_000_000,
            TransactionValidDuration = new Proto.Duration { Seconds = 180 },
            Memo = Generator.Code(20),
            CryptoTransfer = new CryptoTransferTransactionBody { Transfers = transfers }
        };
        var signedTransaction = new SignedTransaction
        {
            BodyBytes = body.ToByteString()
        };
        var ex = await Assert.That(async () =>
        {
            await client.ExecuteExternalTransactionAsync(signedTransaction.ToByteArray());
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("External Transaction Submission failed with status: InvalidSignature");
    }

    [Test]
    public async Task Bogus_Protobuf_Bytes_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var signedTx = Generator.SHA384Hash();

        var ex = await Assert.That(async () =>
        {
            await client.ExecuteExternalTransactionAsync(signedTx);
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("SignedTransactionBytes");
        await Assert.That(ae.Message).StartsWith("Signed Transaction Bytes not recognized as valid Protobuf.");
    }

    [Test]
    public async Task Can_Create_External_Transfer_Transaction()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var noSignClient = client.Clone(ctx => { ctx.Payer = null; ctx.Signatory = null; });

        var xferAmount = Generator.Integer(500, 5_00_000_000);

        var bytes = await noSignClient.PrepareExternalTransactionAsync(new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fxSender, -xferAmount),
                new CryptoTransfer(fxReceiver, xferAmount)
                ]
        }, ctx =>
        {
            ctx.Payer = fxSender;
            ctx.Signatory = fxSender.PrivateKey;
        });
        var transaction = SignedTransaction.Parser.ParseFrom(bytes.ToArray());
        await Assert.That(transaction.BodyBytes.IsEmpty).IsFalse();
        await Assert.That(transaction.SigMap).IsNotNull();
        var transactionBody = TransactionBody.Parser.ParseFrom(transaction.BodyBytes);
        await Assert.That(transactionBody).IsNotNull();
        await Assert.That(transactionBody.DataCase).IsEqualTo(TransactionBody.DataOneofCase.CryptoTransfer);
        var cryptoTransfers = transactionBody.CryptoTransfer;
        await Assert.That(cryptoTransfers).IsNotNull();
        var hbarTransfers = cryptoTransfers.Transfers;
        await Assert.That(hbarTransfers).IsNotNull();
        await Assert.That(hbarTransfers.AccountAmounts.Count).IsEqualTo(2);
        var sigMap = transaction.SigMap;
        await Assert.That(sigMap.SigPair).HasSingleItem();
    }

    [Test]
    public async Task Can_Create_External_Transfer_Transaction_With_No_Signatures()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var noSignClient = client.Clone(ctx => { ctx.Payer = null; ctx.Signatory = null; });

        var xferAmount = Generator.Integer(500, 5_00_000_000);

        var bytes = await noSignClient.PrepareExternalTransactionAsync(new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fxSender, -xferAmount),
                new CryptoTransfer(fxReceiver, xferAmount)
                ]
        }, ctx =>
        {
            ctx.Payer = fxSender;
        });
        var transaction = SignedTransaction.Parser.ParseFrom(bytes.ToArray());
        await Assert.That(transaction.BodyBytes.IsEmpty).IsFalse();
        await Assert.That(transaction.SigMap).IsNull();
        var transactionBody = TransactionBody.Parser.ParseFrom(transaction.BodyBytes);
        await Assert.That(transactionBody).IsNotNull();
        await Assert.That(transactionBody.DataCase).IsEqualTo(TransactionBody.DataOneofCase.CryptoTransfer);
        var cryptoTransfers = transactionBody.CryptoTransfer;
        await Assert.That(cryptoTransfers).IsNotNull();
        var hbarTransfers = cryptoTransfers.Transfers;
        await Assert.That(hbarTransfers).IsNotNull();
        await Assert.That(hbarTransfers.AccountAmounts.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Can_Transfer_Crypoto_Via_Prepared_Transfer_Transaction()
    {
        await using var fxSender = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxReceiver = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var noSignClient = client.Clone(ctx => { ctx.Payer = null; ctx.Signatory = null; });

        var senderInitialBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverInitialBalance = await client.GetAccountBalanceAsync(fxReceiver);

        var xferAmount = (long)(fxSender.CreateParams.InitialBalance / 2);

        var signedBytes = await noSignClient.PrepareExternalTransactionAsync(new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fxSender, -xferAmount),
                new CryptoTransfer(fxReceiver, xferAmount)
                ]
        }, ctx =>
        {
            ctx.Payer = fxSender;
            ctx.Signatory = fxSender.PrivateKey;
        });

        var receipt = await noSignClient.ExecuteExternalTransactionAsync(signedBytes, ctx =>
        {
            ctx.Payer = null;
            ctx.Signatory = null;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var senderFinalBalance = await client.GetAccountBalanceAsync(fxSender);
        var receiverFinalBalance = await client.GetAccountBalanceAsync(fxReceiver);

        await Assert.That(senderFinalBalance < (ulong)((long)senderInitialBalance - xferAmount)).IsTrue();
        await Assert.That(receiverFinalBalance).IsEqualTo(receiverInitialBalance + (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Pair_Allowance_With_External_Transfer_In_Batch()
    {
        await using var fxSource = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        var transferAmount = (long)fxSource.CreateParams.InitialBalance / 2;
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance);

        var allowanceParams = new BatchedTransactionMetadata
        {
            Payer = fxSource.CreateReceipt!.Address,
            TransactionParams = new AllowanceParams
            {
                CryptoAllowances = [
                    new CryptoAllowance(fxSource, TestNetwork.Payer, transferAmount)
                ],
                Signatory = fxSource.PrivateKey
            },
        };

        var xferParams = new ExternalTransactionParams
        {
            SignedTransactionBytes = await client.PrepareExternalTransactionAsync(new BatchedTransactionMetadata
            {
                TransactionParams = new TransferParams
                {
                    CryptoTransfers = [
                        new CryptoTransfer(fxSource, -transferAmount, true),
                        new CryptoTransfer(fxReceiver, transferAmount)
                    ]
                },
            }),
        };

        var batchParams = new BatchedTransactionParams
        {
            TransactionParams = [allowanceParams, xferParams]
        };

        var receipt = await client.ExecuteAsync(batchParams);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(originalBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Pair_External_Allowance_With_Transfer_In_Batch()
    {
        await using var fxSource = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        var transferAmount = (long)fxSource.CreateParams.InitialBalance / 2;
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance);

        var allowanceParams = new ExternalTransactionParams
        {
            SignedTransactionBytes = await client.PrepareExternalTransactionAsync(new BatchedTransactionMetadata
            {
                Payer = fxSource.CreateReceipt!.Address,
                TransactionParams = new AllowanceParams
                {
                    CryptoAllowances = [
                        new CryptoAllowance(fxSource, TestNetwork.Payer, transferAmount)
                    ],
                    Signatory = fxSource.PrivateKey
                },
            }),
        };

        var xferParams = new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fxSource, -transferAmount, true),
                new CryptoTransfer(fxReceiver, transferAmount)
            ]
        };

        var batchParams = new BatchedTransactionParams
        {
            TransactionParams = [allowanceParams, xferParams]
        };

        var receipt = await client.ExecuteAsync(batchParams);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(originalBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Create_Simple_Prepared_Crypto_Transfer()
    {
        var fromAccount = new EntityId(0, 0, Generator.Integer(500, 600));
        var toAccount = new EntityId(0, 0, Generator.Integer(601, 700));
        var quantity = (long)Generator.Integer(500, 600);
        await using var client = await TestNetwork.CreateClientAsync();
        await using var noSignClient = client.Clone(ctx => ctx.Signatory = null);
        var signedTransactionBytes = await noSignClient.PrepareExternalTransactionAsync(new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fromAccount, -quantity),
                new CryptoTransfer(toAccount, quantity),
                ]
        });
        await Assert.That(signedTransactionBytes.IsEmpty).IsFalse();
    }
}
