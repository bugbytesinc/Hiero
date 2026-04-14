using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestTopic : IAsyncDisposable
{
    public required ReadOnlyMemory<byte> AdminPublicKey;
    public required ReadOnlyMemory<byte> AdminPrivateKey;
    public required ReadOnlyMemory<byte> ParticipantPublicKey;
    public required ReadOnlyMemory<byte> ParticipantPrivateKey;
    public required TestAccount RenewAccount;
    public required CreateTopicParams CreateParams;
    public required CreateTopicReceipt CreateReceipt;

    public static async Task<TestTopic> CreateAsync(Action<TestTopic>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test Topic Instance");
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (participantPublicKey, participantPrivateKey) = Generator.KeyPair();
        var renewAccount = await TestAccount.CreateAsync();
        var createParams = new CreateTopicParams
        {
            Memo = Generator.Memo(20),
            Administrator = adminPublicKey,
            Submitter = participantPublicKey,
            RenewAccount = renewAccount.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(adminPrivateKey, renewAccount.PrivateKey),
        };
        var fixture = new TestTopic
        {
            AdminPublicKey = adminPublicKey,
            AdminPrivateKey = adminPrivateKey,
            ParticipantPublicKey = participantPublicKey,
            ParticipantPrivateKey = participantPrivateKey,
            RenewAccount = renewAccount,
            CreateParams = createParams,
            CreateReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.CreateReceipt = await client.CreateTopicAsync(fixture.CreateParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test Topic Instance Created");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test Topic Instance");
        await RenewAccount.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test Topic Instance");
    }
    public static implicit operator EntityId(TestTopic fixture)
    {
        return fixture.CreateReceipt.Topic;
    }
}
