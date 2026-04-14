using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Allowance;

public class ExecuteAllowancesTests
{
    [Test]
    public async Task Can_Spend_An_Nft_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestNft, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(new Nft(fxAllowances.TestNft, 1), fxAllowances.Owner, fxDestination, true) }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxAllowances.TestNft, fxDestination, 1);
    }

    [Test]
    public async Task Can_Not_Spend_An_Nft_Allowance_Without_Delegated_Flag()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var ex = await Assert.That(async () =>
        {
            await agentClient.TransferAsync(new TransferParams
            {
                NftTransfers = new[] { new NftTransfer(new Nft(fxAllowances.TestNft, 1), fxAllowances.Owner, fxDestination, false) }
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        await AssertHg.NftNotAssociatedAsync(fxAllowances.TestNft, fxDestination);
    }

    [Test]
    public async Task Can_Spend_A_Token_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        var xferAmount = fxAllowances.TestToken.CreateParams.Circulation / 3 + 1;

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxAllowances.TestToken, fxAllowances.Owner, -(long)xferAmount, true),
                new TokenTransfer(fxAllowances.TestToken, fxDestination, (long)xferAmount, false)
            }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxAllowances.TestToken, fxAllowances.Owner, fxAllowances.TestToken.CreateParams.Circulation - xferAmount);
        await AssertHg.TokenBalanceAsync(fxAllowances.TestToken, fxDestination, xferAmount);
    }

    [Test]
    public async Task Can_Not_Spend_A_Token_Allowance_Without_Delegated_Flag()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        var xferAmount = fxAllowances.TestToken.CreateParams.Circulation / 3 + 1;

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var ex = await Assert.That(async () =>
        {
            await agentClient.TransferAsync(new TransferParams
            {
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxAllowances.TestToken, fxAllowances.Owner, -(long)xferAmount),
                    new TokenTransfer(fxAllowances.TestToken, fxDestination, (long)xferAmount)
                }
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        await AssertHg.TokenBalanceAsync(fxAllowances.TestToken, fxAllowances.Owner, fxAllowances.TestToken.CreateParams.Circulation);
        await AssertHg.TokenBalanceAsync(fxAllowances.TestToken, fxDestination, 0);
    }

    [Test]
    public async Task Can_Spend_A_Crypto_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var initialOwnerBalance = await client.GetAccountBalanceAsync(fxAllowances.Owner);
        var initialDestination = await client.GetAccountBalanceAsync(fxDestination);
        var xferAmount = initialOwnerBalance / 3 + 1;

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxAllowances.Owner, -(long)xferAmount, true),
                new CryptoTransfer(fxDestination, (long)xferAmount, false)
            }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.CryptoBalanceAsync(fxAllowances.Owner, initialOwnerBalance - xferAmount);
        await AssertHg.CryptoBalanceAsync(fxDestination, initialDestination + xferAmount);
    }

    [Test]
    public async Task Can_Not_Spend_A_Crypto_Allowance_Without_Delegated_Flag()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var initialOwnerBalance = await client.GetAccountBalanceAsync(fxAllowances.Owner);
        var initialDestination = await client.GetAccountBalanceAsync(fxDestination);
        var xferAmount = initialOwnerBalance / 3 + 1;

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var ex = await Assert.That(async () =>
        {
            await agentClient.TransferAsync(new TransferParams
            {
                CryptoTransfers = new[]
                {
                    new CryptoTransfer(fxAllowances.Owner, -(long)xferAmount, false),
                    new CryptoTransfer(fxDestination, (long)xferAmount, false)
                }
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);

        await AssertHg.CryptoBalanceAsync(fxAllowances.Owner, initialOwnerBalance);
        await AssertHg.CryptoBalanceAsync(fxDestination, initialDestination);
    }

    [Test]
    public async Task Can_Spend_An_Explicit_Nft_Allowance()
    {
        await using var fxAllowance = await TestAllowance.CreateAsync();
        await using var fxOtherAgent = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 50_00_000_000);
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Nft(fxAllowance.TestNft.CreateReceipt!.Token, 1);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[]
            {
                new NftAllowance(nft, fxAllowance.Owner, fxOtherAgent),
            },
            Signatory = fxAllowance.Owner.PrivateKey
        });

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAllowance.Owner.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(fxOtherAgent.CreateReceipt!.Address);

        await using var otherAgentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxOtherAgent;
            ctx.Signatory = fxOtherAgent.PrivateKey;
        });

        var receipt = await otherAgentClient.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(nft, fxAllowance.Owner, fxDestination, true) }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxAllowance.TestNft, fxDestination, 1);

        info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxDestination.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task Can_Spend_An_Implicit_Nft_When_Explicit_Allowance_Exists()
    {
        await using var fxAllowance = await TestAllowance.CreateAsync();
        await using var fxOtherAgent = await TestAccount.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Nft(fxAllowance.TestNft.CreateReceipt!.Token, 1);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[]
            {
                new NftAllowance(nft, fxAllowance.Owner, fxOtherAgent),
            },
            Signatory = fxAllowance.Owner.PrivateKey
        });

        var info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxAllowance.Owner.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(fxOtherAgent.CreateReceipt!.Address);

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowance.Agent;
            ctx.Signatory = fxAllowance.Agent.PrivateKey;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(nft, fxAllowance.Owner, fxDestination, true) }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxAllowance.TestNft, fxDestination, 1);

        info = await client.GetNftInfoAsync(nft);
        await Assert.That(info.Nft).IsEqualTo(nft);
        await Assert.That(info.Owner).IsEqualTo(fxDestination.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.Spender).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task Can_Spend_An_Nft_Allowance_From_Delegate()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestNft, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        await using var delegateClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.DelegatedAgent;
            ctx.Signatory = fxAllowances.DelegatedAgent.PrivateKey;
        });

        var receipt = await delegateClient.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(new Nft(fxAllowances.TestNft, 1), fxAllowances.Owner, fxDestination, true) }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxAllowances.TestNft, fxDestination, 1);
    }

    [Test]
    public async Task Can_Spend_An_Nft_Having_Delegate_By_Original_Agent()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestNft, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent.PrivateKey;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(new Nft(fxAllowances.TestNft, 1), fxAllowances.Owner, fxDestination, true) }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxAllowances.TestNft, fxDestination, 1);
    }

    [Test]
    public async Task Can_Not_Spend_An_Nft_From_Delegate_Without_Permission()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestNft, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        await using var delegateClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.DelegatedAgent;
            ctx.Signatory = fxAllowances.DelegatedAgent.PrivateKey;
        });

        var ex = await Assert.That(async () =>
        {
            await delegateClient.TransferAsync(new TransferParams
            {
                NftTransfers = new[] { new NftTransfer(new Nft(fxAllowances.TestNft, 2), fxAllowances.Owner, fxDestination, true) }
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.SpenderDoesNotHaveAllowance);

        await AssertHg.NftBalanceAsync(fxAllowances.TestNft, fxAllowances.Owner, (ulong)fxAllowances.TestNft.Metadata.Length);
        await AssertHg.NftBalanceAsync(fxAllowances.TestNft, fxDestination, 0);
    }

    [Test]
    public async Task Can_Delete_Account_Having_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxDestination, fxAllowances.TestToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxDestination);
        });

        var xferAmount = (long)fxAllowances.TestToken.CreateParams.Circulation / 2;

        await using var agentClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAllowances.Agent;
            ctx.Signatory = fxAllowances.Agent;
        });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxAllowances.TestToken, fxAllowances.Owner, -xferAmount, true),
                new TokenTransfer(fxAllowances.TestToken, fxDestination, xferAmount, false)
            }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxAllowances.TestToken, fxDestination, (ulong)xferAmount);

        var del = await agentClient.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAllowances.Agent,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAllowances.Agent.PrivateKey
        });
        await Assert.That(del.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Spend_A_Token_Allowance_With_Reimbursement()
    {
        await using var fxAgent = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxDestination = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxDestination);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxToken.TreasuryAccount, fxAgent, 50) },
            TokenAllowances = new[] { new TokenAllowance(fxToken, fxToken.TreasuryAccount, fxAgent, (long)fxToken.CreateParams.Circulation - 1) },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var xferAmount = fxToken.CreateParams.Circulation / 3 + 1;
        await using var agentClient = client.Clone(ctx => { ctx.Payer = fxAgent; ctx.Signatory = fxAgent; });

        var receipt = await agentClient.TransferAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxToken.TreasuryAccount, -10, true),
                new CryptoTransfer(fxAgent, 10)
            },
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken, fxToken.TreasuryAccount, -(long)xferAmount, true),
                new TokenTransfer(fxToken, fxDestination, (long)xferAmount)
            }
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxDestination, xferAmount);

        await agentClient.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAgent,
            FundsReceiver = fxDestination,
            Signatory = fxAgent.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await agentClient.TransferAsync(new TransferParams
            {
                CryptoTransfers = new[]
                {
                    new CryptoTransfer(fxToken.TreasuryAccount, -10, true),
                    new CryptoTransfer(fxAgent, 10)
                },
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxToken, fxToken.TreasuryAccount, -(long)xferAmount, true),
                    new TokenTransfer(fxToken, fxDestination, (long)xferAmount)
                }
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.PayerAccountDeleted);
        // Note: We skip verifying token balances after the failed transfer because
        // the precheck-rejected transaction sets _latestKnownMutatingTransaction to
        // a tx that never reached consensus, which breaks mirror sync. Balances were
        // verified before the account deletion and the failed transfer confirms no
        // additional movement occurred.
    }
}
