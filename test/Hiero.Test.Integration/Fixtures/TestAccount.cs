using Hiero.Mirror;
using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestAccount : IHasCryptoBalance, IHasTokenBalance, IAsyncDisposable
{
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;
    public required CreateAccountParams CreateParams;
    public required CreateAccountReceipt CreateReceipt;
    public static async Task<TestAccount> CreateAsync(Action<TestAccount>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test Account Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var createParams = new CreateAccountParams
        {
            Endorsement = publicKey,
            InitialBalance = (ulong)Generator.Integer(10, 20),
            Memo = Generator.Memo(20, 40),
            AutoAssociationLimit = Generator.Integer(5, 10)
        };
        var fixture = new TestAccount
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            CreateParams = createParams,
            CreateReceipt = null!
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.CreateReceipt = await client.CreateAccountAsync(createParams);
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test Account Instance");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test Account Instance");
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
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test Account Instance");
    }
    public async Task<ulong> GetCryptoBalanceAsync()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        return await client.GetAccountBalanceAsync(CreateReceipt.Address);
    }
    public async Task<long?> GetTokenBalanceAsync(EntityId token)
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        return await mirror.GetAccountTokenBalanceAsync(CreateReceipt.Address, token);
    }
    public async Task<TokenHoldingData[]> GetTokenBalancesAsync()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var list = new List<TokenHoldingData>();
        await foreach (var record in mirror.GetAccountTokenHoldingsAsync(CreateReceipt.Address))
        {
            list.Add(record);
        }
        return list.ToArray();
    }
    public static implicit operator EntityId(TestAccount fixture)
    {
        return fixture.CreateReceipt.Address;
    }
    public static implicit operator Signatory(TestAccount fixture)
    {
        return new Signatory(fixture.PrivateKey);
    }
}