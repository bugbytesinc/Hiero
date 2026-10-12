using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestToken : IAsyncDisposable
{
    public required ReadOnlyMemory<byte> AdminPublicKey;
    public required ReadOnlyMemory<byte> AdminPrivateKey;
    public required ReadOnlyMemory<byte> GrantPublicKey;
    public required ReadOnlyMemory<byte> GrantPrivateKey;
    public required ReadOnlyMemory<byte> SuspendPublicKey;
    public required ReadOnlyMemory<byte> SuspendPrivateKey;
    public required ReadOnlyMemory<byte> PausePublicKey;
    public required ReadOnlyMemory<byte> PausePrivateKey;
    public required ReadOnlyMemory<byte> ConfiscatePublicKey;
    public required ReadOnlyMemory<byte> ConfiscatePrivateKey;
    public required ReadOnlyMemory<byte> SupplyPublicKey;
    public required ReadOnlyMemory<byte> SupplyPrivateKey;
    public required ReadOnlyMemory<byte> MetadataPublicKey;
    public required ReadOnlyMemory<byte> MetadataPrivateKey;
    public required ReadOnlyMemory<byte> RoyaltiesPublicKey;
    public required ReadOnlyMemory<byte> RoyaltiesPrivateKey;

    public required TestAccount TreasuryAccount;
    public required TestAccount RenewAccount;
    public required CreateTokenParams CreateParams;
    public required CreateTokenReceipt CreateReceipt;

    public static async Task<TestToken> CreateAsync(Action<TestToken>? customize = null, params TestAccount[] associate)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test Token Instance");
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (grantPublicKey, grantPrivateKey) = Generator.KeyPair();
        var (suspendPublicKey, suspendPrivateKey) = Generator.KeyPair();
        var (pausePublicKey, pausePrivateKey) = Generator.KeyPair();
        var (confiscatePublicKey, confiscatePrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, supplyPrivateKey) = Generator.KeyPair();
        var (metadataPublicKey, metadataPrivateKey) = Generator.KeyPair();
        var (royaltiesPublicKey, royaltiesPrivateKey) = Generator.KeyPair();
        var treasuryAccount = await TestAccount.CreateAsync();
        var renewAccount = await TestAccount.CreateAsync();
        var wholeTokens = (ulong)(Generator.Integer(10, 20) * 100000);
        var decimals = (uint)Generator.Integer(2, 5);
        var circulation = wholeTokens * (ulong)Math.Pow(10, decimals);
        var maxSupply = (long)(circulation * Generator.Double(2.1, 2.8));
        var createParams = new CreateTokenParams
        {
            Name = Generator.Code(100),
            Symbol = Generator.Code(100),
            Circulation = circulation,
            Decimals = decimals,
            Ceiling = maxSupply,
            Treasury = treasuryAccount.CreateReceipt!.Address,
            Administrator = adminPublicKey,
            GrantKycEndorsement = grantPublicKey,
            SuspendEndorsement = suspendPublicKey,
            PauseEndorsement = pausePublicKey,
            ConfiscateEndorsement = confiscatePublicKey,
            SupplyEndorsement = supplyPublicKey,
            RoyaltiesEndorsement = royaltiesPublicKey,
            MetadataEndorsement = metadataPublicKey,
            InitializeSuspended = false,
            Expiration = DateTime.UtcNow.AddDays(Generator.Integer(33, 60)),
            RenewAccount = renewAccount.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(32),
            Signatory = new Signatory(adminPrivateKey, renewAccount.PrivateKey, treasuryAccount.PrivateKey),
            Memo = Generator.Code(20)
        };
        var fixture = new TestToken
        {
            AdminPublicKey = adminPublicKey,
            AdminPrivateKey = adminPrivateKey,
            GrantPublicKey = grantPublicKey,
            GrantPrivateKey = grantPrivateKey,
            SuspendPublicKey = suspendPublicKey,
            SuspendPrivateKey = suspendPrivateKey,
            PausePublicKey = pausePublicKey,
            PausePrivateKey = pausePrivateKey,
            ConfiscatePublicKey = confiscatePublicKey,
            ConfiscatePrivateKey = confiscatePrivateKey,
            SupplyPublicKey = supplyPublicKey,
            SupplyPrivateKey = supplyPrivateKey,
            MetadataPublicKey = metadataPublicKey,
            MetadataPrivateKey = metadataPrivateKey,
            RoyaltiesPublicKey = royaltiesPublicKey,
            RoyaltiesPrivateKey = royaltiesPrivateKey,
            TreasuryAccount = treasuryAccount,
            RenewAccount = renewAccount,
            CreateParams = createParams,
            CreateReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.CreateReceipt = await client.CreateTokenAsync(fixture.CreateParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        await fixture.AssociateAccountsAsync(associate);
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test Token Instance Created");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test Token Instance");
        await TreasuryAccount.DisposeAsync();
        await RenewAccount.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test Token Instance");
    }
    public static implicit operator EntityId(TestToken fixture)
    {
        return fixture.CreateReceipt.Token;
    }
    public async Task AssociateAccountsAsync(params TestAccount[] accounts)
    {
        await using var client = await TestNetwork.CreateClientAsync();
        foreach (var account in accounts)
        {
            await client.AssociateTokenAsync(CreateReceipt!.Token, account.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, account.PrivateKey);
            });
        }
    }
}
