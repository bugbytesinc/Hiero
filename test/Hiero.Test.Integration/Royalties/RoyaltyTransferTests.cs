using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Royalties;

public class RoyaltyTransferTests
{
    [Test]
    public async Task Transferring_Token_Applies_Fixed_Commission()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Associate comToken with the token's treasury so it can receive
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));

        // Associate accounts with both tokens
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer comTokens and tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -200),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 200)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer token from account1 to account2 - royalty should apply
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });

        // Verify comAccount got the 100 comTokens commission
        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(100L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(100L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(50L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(50L);
    }

    [Test]
    public async Task Transferring_Token_Applies_Fixed_Commission_With_Updated_Fee_Structure()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        // Create token WITHOUT royalties initially
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Update royalties after creation
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[]
        {
            new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 100)
        }, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey, fxComAccount.PrivateKey));

        // Associate comToken with the token's treasury
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));

        // Associate accounts with both tokens
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer comTokens and tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -200),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 200)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer token from account1 to account2 - updated royalty should apply
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(100L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(100L);
    }

    [Test]
    public async Task Transferring_Token_Applies_Fractional_Commission()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new TokenRoyalty(fxComAccount, 1, 2, 1, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer 49 tokens from account1 to account2 - 50% fractional royalty => 24 to comAccount, 25 to account2
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -49),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 49)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(24L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(51L);
    }

    [Test]
    public async Task Transferring_Token_Applies_Fractional_Commission_As_Surcharge()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new TokenRoyalty(fxComAccount, 1, 2, 1, 100, true)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer 49 with surcharge: sender pays extra, receiver gets full 49, comAccount gets 24
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -49),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 49)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(27L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(49L);
        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(24L);
    }

    [Test]
    public async Task Transferring_Token_Applies_Immutable_Fixed_Commission()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.RoyaltiesEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new TokenRoyalty(fxComAccount, 1, 2, 1, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer 50 - fractional 50% royalty: comAccount gets 25, recipient gets 25
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(50L);
    }

    [Test]
    public async Task Transferring_Token_From_Treasury_Does_Not_Apply_Fixed_Commission()
    {
        // Treasury is also the royalty collector
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new TokenRoyalty(fx.TreasuryAccount, 1, 2, 1, 100)
            };
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Treasury -> account1: no royalty applied
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(100L);

        // account1 -> account2: royalty deducted from account2 to treasury
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(50L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
    }

    [Test]
    public async Task Can_Apply_A_Simple_Immutable_Token_With_Royalties()
    {
        await using var fxTreasury = await TestAccount.CreateAsync();
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxCollector = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Create the token directly (not via fixture) with immutable royalties
        var receipt = await client.CreateTokenAsync(new CreateTokenParams
        {
            Name = Generator.Code(100),
            Symbol = Generator.Code(100),
            Circulation = 100_00,
            Decimals = 2,
            Treasury = fxTreasury,
            InitializeSuspended = false,
            Expiration = DateTime.UtcNow.AddDays(90),
            RoyaltiesEndorsement = null,
            Royalties =
            [
                new TokenRoyalty(fxCollector, 1, 2, 1, 100)
            ],
            Signatory = new Signatory(fxTreasury, fxCollector),
            Memo = Generator.Code(20)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var token = receipt.Token;

        await client.AssociateTokenAsync(fxAccount1, token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2, token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(token, fxTreasury, -100),
                new TokenTransfer(token, fxAccount1, 100)
            },
            Signatory = fxTreasury.PrivateKey
        });

        // Transfer from account1 to account2 - royalty should apply
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxCollector.GetTokenBalanceAsync(token)).IsEqualTo(25L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(token)).IsEqualTo(25L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(token)).IsEqualTo(50L);
    }

    [Test]
    public async Task Royalties_Are_Applied_When_Payer_Is_Same_Account_As_Sender()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new TokenRoyalty(fxComAccount, 1, 2, 1, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync(fx =>
            fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer with payer == sender
        await client.TransferTokensAsync(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, 50, ctx =>
        {
            ctx.Payer = fxAccount1.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxAccount1.PrivateKey);
        });

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(25L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken.CreateReceipt!.Token)).IsEqualTo(50L);
    }

    [Test]
    public async Task Insufficient_Royalty_Token_Balance_Prevents_Transfer()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 200)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Only give account1 100 comTokens, but royalty requires 200
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer should fail because insufficient comToken balance
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                    new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
                },
                Signatory = fxAccount1.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InsufficientSenderAccountBalanceForCustomFee);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: InsufficientSenderAccountBalanceForCustomFee");
    }

    [Test]
    public async Task Fractional_Royalties_Appear_In_Transaction_Record()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));
        await client.AssociateTokenAsync(fxComAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxComAccount.PrivateKey));

        // Transfer from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer without royalties first - verify empty royalties in record
        var receipt1 = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -10),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 10)
            },
            Signatory = fxAccount1.PrivateKey
        });
        var record1 = await client.GetTransactionRecordAsync(receipt1.TransactionId);
        await Assert.That(record1).IsNotNull();
        await Assert.That(record1.Hash.IsEmpty).IsFalse();
        await Assert.That(record1.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record1.Royalties.Count).IsEqualTo(0);

        // Now add fractional royalty
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[]
        {
            new TokenRoyalty(fxComAccount, 1, 2, 1, 100)
        }, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));

        // Transfer again with royalties in effect
        var receipt2 = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -40),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 40)
            },
            Signatory = fxAccount1.PrivateKey
        });
        var record2 = await client.GetTransactionRecordAsync(receipt2.TransactionId);
        await Assert.That(record2).IsNotNull();
        await Assert.That(record2.Hash.IsEmpty).IsFalse();
        await Assert.That(record2.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record2.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record2.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsRoyalty(fxToken, fxAccount2, fxComAccount, 20, record2.Royalties);

        // Verify token transfers in record
        var comAccountTransfer = record2.TokenTransfers.FirstOrDefault(x => x.Account == fxComAccount.CreateReceipt!.Address);
        await Assert.That(comAccountTransfer).IsNotNull();
        await Assert.That(comAccountTransfer!.Amount).IsEqualTo(20L);
    }

    [Test]
    public async Task Fixed_Royalties_Appear_In_Transaction_Record()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Transfer comTokens and tokens from treasury to account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -200),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 200)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer token from account1 to account2 - get the record
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
            },
            Signatory = fxAccount1.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();

        // 4 token transfers: 2 for the primary token + 2 for the comToken royalty
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(4);
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Treasury_Exempted_From_Nested_Royalties()
    {
        await using var fxComAccount1 = await TestAccount.CreateAsync();
        await using var fxComAccount2 = await TestAccount.CreateAsync();
        await using var fxComToken2 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount2);
        await using var fxComToken1 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount2, fxComToken2.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount2.PrivateKey);
        }, fxComAccount1);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount1, fxComToken1.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount1.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Associate treasury with comTokens
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));

        // Associate account1 with all tokens
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));

        // Transfer from treasury - no royalties should apply
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 50)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(record.Royalties.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Transferring_Asset_Applies_Fixed_Commission_With_Updated_Fee_Structure()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        // Create NFT without royalties initially
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Update royalties after creation
        await client.UpdateRoyaltiesAsync(fxNft.CreateReceipt!.Token, new IRoyalty[]
        {
            new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 10)
        }, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.RoyaltiesPrivateKey, fxComAccount.PrivateKey));

        // Associate accounts
        await client.AssociateTokenAsync(fxNft.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Give account1 comTokens
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to account1
        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Transfer NFT from account1 to account2 - royalty should apply
        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(10L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(90L);
        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 1);
    }

    [Test]
    public async Task Transferring_Multiple_Assets_Applies_Fixed_Commission_For_Each_Asset_Transferred()
    {
        await using var fxComAccount = await TestAccount.CreateAsync();
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxNft.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Give account1 comTokens
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });

        // Transfer 2 NFTs from treasury to account1
        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));
        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 2), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Transfer 2 NFTs at once from account1 to account2 - royalty applied per NFT (2x10=20)
        await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 1), fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address),
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 2), fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address)
            },
            Signatory = fxAccount1.PrivateKey
        });

        await Assert.That(await fxComAccount.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(20L);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxComToken.CreateReceipt!.Token)).IsEqualTo(80L);
        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 2);
    }

    [Test]
    public async Task Treasury_Does_Pay_Second_Degree_Commission()
    {
        await using var fxComAccount1 = await TestAccount.CreateAsync();
        await using var fxComAccount2 = await TestAccount.CreateAsync();
        await using var fxComToken2 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount2);
        await using var fxComToken1 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount2, fxComToken2.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount2.PrivateKey);
        }, fxComAccount1);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount1, fxComToken1.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount1.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Associate treasury accounts with needed tokens
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));

        // Associate account1 with all tokens
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));

        // Associate account2 with primary token
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Associate comAccounts
        await client.AssociateTokenAsync(fxComAccount1.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxComAccount1.PrivateKey));

        // Fund account1 with tokens
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken1.CreateReceipt!.Token, fxComToken1.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken1.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken1.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken2.CreateReceipt!.Token, fxComToken2.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken2.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken2.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -50),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 50)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Transfer primary token from account1 to account2 - both levels of royalties
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -25),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 25)
            },
            Signatory = fxAccount1.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        // 6 token transfers: 2 for primary token + 2 for comToken1 royalty + 2 for comToken2 royalty
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(6);
        await Assert.That(record.Royalties.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Three_Fixed_Royalties_Plus_Royalty_When_HBar_Value_Exchanged()
    {
        await using var fxComAccount1 = await TestAccount.CreateAsync();
        await using var fxComAccount2 = await TestAccount.CreateAsync();
        await using var fxComAccount3 = await TestAccount.CreateAsync();
        await using var fxComAccount4 = await TestAccount.CreateAsync();
        await using var fxComToken1 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount2);
        await using var fxComToken2 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount3);
        await using var fxComToken3 = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount4);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxComAccount1, 1, 10, 0, EntityId.None),
                new FixedRoyalty(fxComAccount2, fxComToken1.CreateReceipt!.Token, 10),
                new FixedRoyalty(fxComAccount3, fxComToken2.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxComAccount4, fxComToken3.CreateReceipt!.Token, 30)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount1.PrivateKey, fxComAccount2.PrivateKey, fxComAccount3.PrivateKey, fxComAccount4.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync(fx =>
            fx.CreateParams.InitialBalance = 50_00_000_000UL);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx =>
            fx.CreateParams.InitialBalance = 50_00_000_000UL);
        await using var client = await TestNetwork.CreateClientAsync();

        // Associate accounts with NFT and comTokens
        await client.AssociateTokenAsync(fxNft.TreasuryAccount.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxNft.TreasuryAccount.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxNft.TreasuryAccount.CreateReceipt!.Address, fxComToken3.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken1.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken2.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken3.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));

        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Fund account1 with comTokens
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken1.CreateReceipt!.Token, fxComToken1.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken1.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken1.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken2.CreateReceipt!.Token, fxComToken2.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken2.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken2.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken3.CreateReceipt!.Token, fxComToken3.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxComToken3.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxComToken3.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to account1
        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Transfer NFT with hbar payment from account2 to account1
        var hbarPayment = 1_00_000_000L;
        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 1), fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address)
            },
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxAccount2, -hbarPayment),
                new CryptoTransfer(fxAccount1, hbarPayment)
            },
            Signatory = new Signatory(fxAccount1.PrivateKey, fxAccount2.PrivateKey)
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        // 4 token transfers from 3 fixed royalties + hbar royalty from the NftRoyalty
        await Assert.That(record.TokenTransfers.Count >= 4).IsTrue();
        await Assert.That(record.Royalties.Count >= 4).IsTrue();
    }

    [Test]
    public async Task Three_Fixed_Royalties_Plus_Royalty_When_Fungible_Token_Value_Exchanged()
    {
        await using var fxBuyer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxSeller = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor1 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor2 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor3 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor4 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxGasToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Decimals = 2;
            fx.CreateParams.Circulation = 1_000_00;
        }, fxBenefactor1, fxBenefactor2, fxBenefactor3, fxBenefactor4, fxBuyer, fxSeller);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor4, 1, 5, 50, fxGasToken.CreateReceipt!.Token),
                new FixedRoyalty(fxBenefactor1, fxGasToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor2, fxGasToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor3, fxGasToken.CreateReceipt!.Token, 40)
            };
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxBenefactor1.PrivateKey, fxBenefactor2.PrivateKey, fxBenefactor3.PrivateKey, fxBenefactor4.PrivateKey);
        }, fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Fund buyer with gas tokens
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxGasToken, fxGasToken.TreasuryAccount, -100_00),
                new TokenTransfer(fxGasToken, fxBuyer, 100_00)
            },
            Signatory = fxGasToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer purchases NFT from seller, paying 10_00 gas tokens
        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller, fxBuyer)
            },
            TokenTransfers = new[]
            {
                new TokenTransfer(fxGasToken, fxBuyer, -10_00),
                new TokenTransfer(fxGasToken, fxSeller, 10_00)
            },
            Signatory = new Signatory(fxBuyer.PrivateKey, fxSeller.PrivateKey)
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        // 6 token transfers: buyer pays 10_00, seller gets 7_20, benefactors get 20+20+40+2_00
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(6);
        await Assert.That(record.Royalties.Count).IsEqualTo(4);
        AssertHg.ContainsRoyalty(fxGasToken, fxSeller, fxBenefactor1, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxGasToken, fxSeller, fxBenefactor2, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxGasToken, fxSeller, fxBenefactor3, 40, record.Royalties);
        AssertHg.ContainsRoyalty(fxGasToken, fxSeller, fxBenefactor4, 2_00, record.Royalties);
    }

    [Test]
    public async Task Dissociating_As_Fee_Receiver_Prevents_Transfers()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxComAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxComToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxComAccount, fxAccount1, fxAccount2);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = [new FixedRoyalty(fxComAccount, fxComToken, 100)];
        }, fxAccount1, fxAccount2);
        
        await using var client = await TestNetwork.CreateClientAsync();
        //await client.AssociateTokenAsync(fxToken.TreasuryAccount, fxComToken, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount));
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = [
                new TokenTransfer(fxComToken, fxComToken.TreasuryAccount, -200),
                new TokenTransfer(fxComToken, fxAccount1, 200)
            ],
            Signatory = fxComToken.TreasuryAccount
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = [
                new TokenTransfer(fxToken, fxToken.TreasuryAccount, -100),
                new TokenTransfer(fxToken, fxAccount1, 100)
            ],
            Signatory = fxToken.TreasuryAccount
        });

        // Dissociate fee receiver from comToken
        var disReceipt = await client.DissociateTokenAsync(fxComToken, fxComAccount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxComAccount.PrivateKey));
        await Assert.That(disReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Transfer should fail because fee receiver not associated
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = [
                    new TokenTransfer(fxToken, fxAccount1, -50),
                    new TokenTransfer(fxToken, fxAccount2, 50)
                ],
                Signatory = fxAccount1.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToFeeCollector);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: TokenNotAssociatedToFeeCollector");
    }

    [Test]
    public async Task Deleting_Fee_Receiver_Prevents_Transfers()
    {
        await using var fxComAccount = await TestAccount.CreateAsync(fx =>
            fx.CreateParams.InitialBalance = (ulong)Generator.Integer(2_00_000_000, 5_00_000_000));
        await using var fxComToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxComAccount);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxComAccount, fxComToken.CreateReceipt!.Token, 100)
            };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxComAccount.PrivateKey);
        });
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxToken.TreasuryAccount.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount1.CreateReceipt!.Address, fxComToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey));
        await client.AssociateTokenAsync(fxAccount2.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey));

        // Fund account1
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxComToken.TreasuryAccount.CreateReceipt!.Address, -200),
                new TokenTransfer(fxComToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 200)
            },
            Signatory = fxComToken.TreasuryAccount.PrivateKey
        });
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, 100)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        // Delete fee receiver account
        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxComAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxComAccount.PrivateKey
        });

        // Transfer should fail because fee receiver account is deleted
        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(new TransferParams
            {
                TokenTransfers = new[]
                {
                    new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, -50),
                    new TokenTransfer(fxToken.CreateReceipt!.Token, fxAccount2.CreateReceipt!.Address, 50)
                },
                Signatory = fxAccount1.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: AccountDeleted");
    }
}
