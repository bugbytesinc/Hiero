using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Batch;

public class BatchTransferTests
{
    [Test]
    public async Task Can_Invoke_Single_Transfer_Inside_A_Batch()
    {
        await using var fxReceiver = await TestAccount.CreateAsync();
        var transferAmount = (long)Generator.Integer(10, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var originalBalance = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(originalBalance).IsEqualTo(fxReceiver.CreateParams.InitialBalance);

        var xferParams = new TransferParams
        {
            CryptoTransfers = [
                new CryptoTransfer(fxReceiver, transferAmount),
                new CryptoTransfer(TestNetwork.Payer, -transferAmount)
            ]
        };

        var batchParams = new BatchedTransactionParams
        {
            TransactionParams = [xferParams]
        };

        var receipt = await client.ExecuteAsync(batchParams);
        var newBalanceAfterTransfer = await client.GetAccountBalanceAsync(fxReceiver.CreateReceipt!.Address);
        await Assert.That(newBalanceAfterTransfer).IsEqualTo(originalBalance + (ulong)transferAmount);
    }

    [Test]
    public async Task Can_Pair_Allowance_With_Transfer_In_Batch()
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
    public async Task Can_Atomically_UnPause_Paused_Token_For_Transfer()
    {
        await using var fxReceiver = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxReceiver);
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = (long)circulation / 5;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        await using var client = await TestNetwork.CreateClientAsync();
        await client.PauseTokenAsync(new PauseTokenParams { Token = fxToken, Signatory = fxToken.PausePrivateKey });

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = [
                    new TokenTransfer(fxToken, fxToken.TreasuryAccount, -xferAmount),
                    new TokenTransfer(fxToken, fxReceiver, xferAmount)
                ],
                Signatory = fxToken.TreasuryAccount.PrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: TokenIsPaused");
        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var unpauseParams = new ContinueTokenParams
        {
            Token = fxToken,
            Signatory = fxToken.PausePrivateKey
        };

        var transferParams = new TransferParams
        {
            TokenTransfers = [
                new TokenTransfer(fxToken, fxToken.TreasuryAccount, -xferAmount),
                new TokenTransfer(fxToken, fxReceiver, xferAmount)
            ],
            Signatory = fxToken.TreasuryAccount.PrivateKey,
        };

        var pauseParams = new PauseTokenParams
        {
            Token = fxToken,
            Signatory = fxToken.PausePrivateKey,
        };

        var receipt = await client.ExecuteAsync(new BatchedTransactionParams
        {
            TransactionParams = [unpauseParams, transferParams, pauseParams]
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.TokenBalanceAsync(fxToken, fxReceiver, (ulong)xferAmount);

        ex = await Assert.That(async () =>
        {
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = [
                    new TokenTransfer(fxToken, fxToken.TreasuryAccount, -xferAmount),
                    new TokenTransfer(fxToken, fxReceiver, xferAmount)
                ],
                Signatory = fxToken.TreasuryAccount.PrivateKey,
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: TokenIsPaused");
        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);
        await AssertHg.TokenBalanceAsync(fxToken, fxReceiver, (ulong)xferAmount);
    }
}
