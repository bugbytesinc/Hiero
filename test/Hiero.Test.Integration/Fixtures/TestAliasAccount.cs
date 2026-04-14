using Hiero.Mirror;
using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestAliasAccount : IHasCryptoBalance, IHasTokenBalance, IAsyncDisposable
{
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;
    public required long InitialTransfer;
    public required EntityId Alias;
    public required TransactionReceipt TransferReceipt;
    public required CreateAccountReceipt CreateReceipt;

    public static async Task<TestAliasAccount> CreateAsync(Action<TestAliasAccount>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Pay to Alias Account Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var alias = new EntityId(0, 0, new Endorsement(publicKey));
        var initialTransfer = (long)Generator.Integer(1_00_000_000, 2_00_000_000);
        var fixture = new TestAliasAccount
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Alias = alias,
            InitialTransfer = initialTransfer,
            TransferReceipt = null!,
            CreateReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.TransferReceipt = await client.TransferAsync(TestNetwork.Payer, fixture.Alias, fixture.InitialTransfer, ctx => ctx.Memo = Generator.Code(20));
        var allReceipts = await client.GetAllReceiptsAsync(fixture.TransferReceipt.TransactionId);
        fixture.CreateReceipt = allReceipts.OfType<CreateAccountReceipt>().First();
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Pay to Alias Account Instance");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Pay to Alias Account Instance");
        if (CreateReceipt is null)
        {
            return;
        }
        try
        {
            await using var client = await TestNetwork.CreateClientAsync();
            var balance = await client.GetAccountBalanceAsync(CreateReceipt.Address);
            if (balance > 0_00_120_000)
            {
                await client.TransferAsync(CreateReceipt.Address, TestNetwork.Payer, (long)balance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, PrivateKey));
            }
        }
        catch
        {
            // OK, we tried.
        }
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Pay to Alias Account Instance");
    }
    public async Task<ulong> GetCryptoBalanceAsync()
    {
        if (CreateReceipt is null)
        {
            return 0;
        }
        await using var client = await TestNetwork.CreateClientAsync();
        return await client.GetAccountBalanceAsync(CreateReceipt.Address);
    }
    public async Task<long?> GetTokenBalanceAsync(EntityId token)
    {
        if (CreateReceipt is null)
        {
            return null;
        }
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        return await mirror.GetAccountTokenBalanceAsync(CreateReceipt.Address, token);
    }
    public async Task<TokenHoldingData[]> GetTokenBalancesAsync()
    {
        if (CreateReceipt is null)
        {
            return [];
        }
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var list = new List<TokenHoldingData>();
        await foreach (var record in mirror.GetAccountTokenHoldingsAsync(CreateReceipt.Address))
        {
            list.Add(record);
        }
        return list.ToArray();
    }
    public static implicit operator EntityId(TestAliasAccount fixture)
    {
        if (fixture.CreateReceipt is null)
        {
            throw new InvalidOperationException("Test Alias Account Fixture does not represent an actual account.");
        }
        return fixture.CreateReceipt.Address;
    }
    public static implicit operator Signatory(TestAliasAccount fixture)
    {
        return new Signatory(fixture.PrivateKey);
    }
}
