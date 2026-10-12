using Hiero.Test.Helpers;
using System.Text;

namespace Hiero.Test.Integration.Fixtures;

public class TestTopicMessage : IAsyncDisposable
{
    public required TestTopic TestTopic;
    public required SubmitMessageReceipt? SubmitReceipt;
    public required ReadOnlyMemory<byte> Message;

    public static async Task<TestTopicMessage> CreateAsync(Action<TestTopicMessage>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Test Topic Message Instance");
        var testTopic = await TestTopic.CreateAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var fixture = new TestTopicMessage
        {
            TestTopic = testTopic,
            Message = message,
            SubmitReceipt = null,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.SubmitReceipt = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fixture.TestTopic.CreateReceipt!.Topic,
            Message = fixture.Message,
            Signatory = fixture.TestTopic.ParticipantPrivateKey
        }, ctx => ctx.Memo = Generator.Code(20));
        if (fixture.SubmitReceipt.Status == ResponseCode.Success)
        {
            TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Test Topic Message Instance Created");
        }
        else
        {
            TestContext.Current?.OutputWriter.WriteLine($"SETUP COMPLETED: Test Topic Message FAILED with code {fixture.SubmitReceipt.Status}");
        }
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Test Topic Message Instance");
        await TestTopic.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Test Topic Message Instance");
    }
}
