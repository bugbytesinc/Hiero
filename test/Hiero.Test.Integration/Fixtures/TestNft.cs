using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestNft : IAsyncDisposable
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
    public required ReadOnlyMemory<byte> RoyaltiesPublicKey;
    public required ReadOnlyMemory<byte> RoyaltiesPrivateKey;
    public required ReadOnlyMemory<byte> MetadataPublicKey;
    public required ReadOnlyMemory<byte> MetadataPrivateKey;

    public required TestAccount TreasuryAccount;
    public required TestAccount RenewAccount;
    public required CreateNftParams CreateParams;
    public required CreateTokenReceipt CreateReceipt;
    public required ReadOnlyMemory<byte>[] Metadata;
    public required NftMintReceipt MintReceipt;

    public static async Task<TestNft> CreateAsync(Action<TestNft>? customize = null, params TestAccount[] associate)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test NFT Instance");
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (grantPublicKey, grantPrivateKey) = Generator.KeyPair();
        var (suspendPublicKey, suspendPrivateKey) = Generator.KeyPair();
        var (pausePublicKey, pausePrivateKey) = Generator.KeyPair();
        var (confiscatePublicKey, confiscatePrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, supplyPrivateKey) = Generator.KeyPair();
        var (royaltiesPublicKey, royaltiesPrivateKey) = Generator.KeyPair();
        var (metadataPublicKey, metadataPrivateKey) = Generator.KeyPair();
        var treasuryAccount = await TestAccount.CreateAsync();
        var renewAccount = await TestAccount.CreateAsync();
        var metadata = Enumerable.Range(1, Generator.Integer(3, 9)).Select(_ => Generator.SHA384Hash()).ToArray();
        var maxSupply = (long)(Generator.Integer(10, 20) * 1000);
        var createParams = new CreateNftParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(100),
            Treasury = treasuryAccount.CreateReceipt!.Address,
            Ceiling = maxSupply,
            Administrator = adminPublicKey,
            GrantKycEndorsement = grantPublicKey,
            SuspendEndorsement = suspendPublicKey,
            PauseEndorsement = pausePublicKey,
            ConfiscateEndorsement = confiscatePublicKey,
            SupplyEndorsement = supplyPublicKey,
            RoyaltiesEndorsement = royaltiesPublicKey,
            MetadataEndorsement = metadataPublicKey,
            InitializeSuspended = false,
            Expiration = Generator.TruncatedFutureDate(800, 1400),
            RenewAccount = renewAccount.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(adminPrivateKey, renewAccount.PrivateKey, treasuryAccount.PrivateKey),
            Memo = Generator.Code(20)
        };
        var fixture = new TestNft
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
            RoyaltiesPublicKey = royaltiesPublicKey,
            RoyaltiesPrivateKey = royaltiesPrivateKey,
            MetadataPublicKey = metadataPublicKey,
            MetadataPrivateKey = metadataPrivateKey,
            TreasuryAccount = treasuryAccount,
            RenewAccount = renewAccount,
            CreateParams = createParams,
            CreateReceipt = null!,
            Metadata = metadata,
            MintReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.CreateReceipt = await client.CreateNftAsync(fixture.CreateParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        await fixture.AssociateAccountsAsync(associate);
        if (fixture.Metadata is { Length: > 0 })
        {
            fixture.MintReceipt = await client.MintNftsAsync(new MintNftParams
            {
                Token = fixture.CreateReceipt.Token,
                Metadata = fixture.Metadata,
                Signatory = supplyPrivateKey,
            });
        }
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test NFT Instance Created");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test NFT Instance");
        await TreasuryAccount.DisposeAsync();
        await RenewAccount.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test NFT Instance");
    }
    public static implicit operator EntityId(TestNft fixture)
    {
        return fixture.CreateReceipt.Token;
    }
    public async Task AssociateAccountsAsync(params TestAccount[] accounts)
    {
        await using var client = await TestNetwork.CreateClientAsync();
        foreach (var account in accounts)
        {
            await client.AssociateTokenAsync(account.CreateReceipt!.Address, CreateReceipt!.Token, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, account.PrivateKey);
            });
        }
    }
}
