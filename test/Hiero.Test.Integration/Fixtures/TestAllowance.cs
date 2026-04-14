namespace Hiero.Test.Integration.Fixtures;

public class TestAllowance : IAsyncDisposable
{
    public required TestToken TestToken;
    public required TestNft TestNft;
    public required TestAccount Owner;
    public required TestAccount Agent;
    public required TestAccount DelegatedAgent;
    public required TransactionReceipt AllowanceReceipt;
    public required TransactionReceipt DelegationReceipt;

    public static async Task<TestAllowance> CreateAsync(Action<TestAllowance>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test Allowance Instance");
        var agent = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 50_00_000_000);
        var delegatedAgent = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 50_00_000_000);
        var owner = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 50_00_000_000);
        var testToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, owner);
        var testNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, owner);
        var fixture = new TestAllowance
        {
            TestToken = testToken,
            TestNft = testNft,
            Owner = owner,
            Agent = agent,
            DelegatedAgent = delegatedAgent,
            AllowanceReceipt = null!,
            DelegationReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        // Transfer all tokens and NFTs from treasury to owner
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers =
            [
                new TokenTransfer(fixture.TestToken.CreateReceipt!.Token, fixture.TestToken.TreasuryAccount, -(long)fixture.TestToken.CreateParams.Circulation),
                new TokenTransfer(fixture.TestToken.CreateReceipt!.Token, fixture.Owner, (long)fixture.TestToken.CreateParams.Circulation)
            ],
            NftTransfers = fixture.TestNft.MintReceipt!.SerialNumbers.Select(s =>
                new NftTransfer(new Nft(fixture.TestNft.CreateReceipt!.Token, s), fixture.TestNft.TreasuryAccount, fixture.Owner)),
            Signatory = new Signatory(fixture.TestToken.TreasuryAccount.PrivateKey, fixture.TestNft.TreasuryAccount.PrivateKey)
        });
        // Grant allowances from owner to agent
        fixture.AllowanceReceipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = [new CryptoAllowance(fixture.Owner, fixture.Agent, (long)fixture.Owner.CreateParams.InitialBalance)],
            TokenAllowances = [new TokenAllowance(fixture.TestToken.CreateReceipt!.Token, fixture.Owner, fixture.Agent, (long)fixture.TestToken.CreateParams.Circulation)],
            NftAllowances = [new NftAllowance(fixture.TestNft.CreateReceipt!.Token, fixture.Owner, fixture.Agent)],
            Signatory = fixture.Owner.PrivateKey
        });
        if (fixture.AllowanceReceipt.Status != ResponseCode.Success)
        {
            TestContext.Current?.OutputWriter.WriteLine($"SETUP COMPLETED: Test Allowance FAILED with code {fixture.AllowanceReceipt.Status}");
            return fixture;
        }
        // Delegate a specific NFT from agent to delegated agent
        fixture.DelegationReceipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = [new NftAllowance(new Nft(fixture.TestNft.CreateReceipt!.Token, 1), fixture.Owner, fixture.DelegatedAgent, fixture.Agent)],
            Signatory = fixture.Agent.PrivateKey
        });
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test Allowance Instance Created");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test Allowance Instance");
        await Agent.DisposeAsync();
        await DelegatedAgent.DisposeAsync();
        await Owner.DisposeAsync();
        await TestToken.DisposeAsync();
        await TestNft.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test Allowance Instance");
    }
}
