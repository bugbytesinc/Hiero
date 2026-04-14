using Hiero.Test.Helpers;
using System.Text;

namespace Hiero.Test.Integration.Fixtures;

public class TestFile : IAsyncDisposable
{
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;
    public required CreateFileParams CreateParams;
    public required FileReceipt CreateReceipt;

    public static async Task<TestFile> CreateAsync(Action<TestFile>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test File Instance");
        var (publicKey, privateKey) = Generator.KeyPair();
        var createParams = new CreateFileParams
        {
            Memo = Generator.Memo(20),
            Expiration = Generator.TruncateToSeconds(DateTime.UtcNow.AddSeconds(7890000)),
            Endorsements = [publicKey],
            Contents = Encoding.Unicode.GetBytes("Hello From .NET" + Generator.Code(50)).Take(48).ToArray(),
            Signatory = privateKey
        };
        var fixture = new TestFile
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            CreateParams = createParams,
            CreateReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.CreateReceipt = await client.CreateFileAsync(fixture.CreateParams, ctx =>
        {
            ctx.Memo = Generator.Code(20);
        });
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test File Instance Created");
        return fixture;
    }
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
    public static implicit operator EntityId(TestFile fixture)
    {
        return fixture.CreateReceipt.File;
    }
}
