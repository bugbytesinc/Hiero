using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Allowance;

public class DeleteAllowancesTests
{
    [Test]
    public async Task Can_Delete_An_Nft_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.RevokeNftAllowancesAsync(new RevokeNftAllowanceParams
        {
            Token = fxAllowances.TestNft,
            Owner = fxAllowances.Owner,
            SerialNumbers = [1],
            Signatory = fxAllowances.Owner.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Delete_An_Nft_Allowance_And_Get_Record()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.RevokeNftAllowancesAsync(new RevokeNftAllowanceParams
        {
            Token = fxAllowances.TestNft,
            Owner = fxAllowances.Owner,
            SerialNumbers = [1],
            Signatory = fxAllowances.Owner.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Delete_A_Crypto_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxAllowances.Owner, fxAllowances.Agent, 0) },
            Signatory = fxAllowances.Owner.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Agent_Can_Delete_A_Token_Allowance_From_Deleted_Account()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAllowances.Agent,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAllowances.Agent.PrivateKey
        });

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] { new TokenAllowance(fxAllowances.TestToken, fxAllowances.Owner, fxAllowances.Agent, 0) },
            Signatory = fxAllowances.Owner.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Agent_Can_Delete_A_Token_Allowance_With_Mirror_Node_Confirmation()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(await HasNonZeroTokenAllowanceAsync()).IsTrue();

        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAllowances.Agent,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAllowances.Agent.PrivateKey
        });

        await Assert.That(await HasNonZeroTokenAllowanceAsync()).IsTrue();

        var clearReceipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] { new TokenAllowance(fxAllowances.TestToken, fxAllowances.Owner, fxAllowances.Agent, 0) },
            Signatory = fxAllowances.Owner.PrivateKey
        });
        await Assert.That(clearReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await HasNonZeroTokenAllowanceAsync()).IsFalse();

        async Task<bool> HasNonZeroTokenAllowanceAsync()
        {
            var mirror = await TestNetwork.GetMirrorRestClientAsync();
            await foreach (var record in mirror.GetAccountTokenAllowancesAsync(fxAllowances.Owner.CreateReceipt!.Address))
            {
                if (record.Spender == fxAllowances.Agent.CreateReceipt!.Address &&
                    record.Token == fxAllowances.TestToken.CreateReceipt!.Token &&
                    record.Amount > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Revoke_Nft_Allowance()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new RevokeNftAllowanceParams
                {
                    Token = fxAllowances.TestNft,
                    Owner = fxAllowances.Owner,
                    SerialNumbers = [1],
                },
                Payer = fxPayer,
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
