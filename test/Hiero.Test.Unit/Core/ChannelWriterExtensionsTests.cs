// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using System.Threading.Channels;

namespace Hiero.Test.Unit.Core;

public class ChannelWriterExtensionsTests
{
    [Test]
    public async Task Writes_Immediately_When_There_Is_Room()
    {
        var channel = Channel.CreateBounded<int>(1);
        var written = await channel.Writer.WriteOnceAsync(1, CancellationToken.None);
        await Assert.That(written).IsTrue();
        await Assert.That(channel.Reader.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Waits_For_Room_And_Writes_Exactly_Once()
    {
        var channel = Channel.CreateBounded<int>(1);
        channel.Writer.TryWrite(1);
        var write = channel.Writer.WriteOnceAsync(2, CancellationToken.None).AsTask();
        await Assert.That(write.IsCompleted).IsFalse();
        await Assert.That(await channel.Reader.ReadAsync()).IsEqualTo(1);
        await Assert.That(await write.WaitAsync(TimeSpan.FromSeconds(10))).IsTrue();
        await Assert.That(await channel.Reader.ReadAsync()).IsEqualTo(2);
        // Room again, yet nothing more arrives: the item is never written twice.
        await Task.Delay(50);
        await Assert.That(channel.Reader.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Returns_False_When_The_Channel_Is_Completed()
    {
        var channel = Channel.CreateBounded<int>(1);
        channel.Writer.TryWrite(1);
        var write = channel.Writer.WriteOnceAsync(2, CancellationToken.None).AsTask();
        channel.Writer.TryComplete();
        await Assert.That(await write.WaitAsync(TimeSpan.FromSeconds(10))).IsFalse();
    }

    [Test]
    public async Task Returns_False_At_Once_For_A_Completed_Unbounded_Channel()
    {
        var channel = Channel.CreateUnbounded<int>();
        channel.Writer.TryComplete();
        await Assert.That(await channel.Writer.WriteOnceAsync(1, CancellationToken.None)).IsFalse();
    }

    [Test]
    public async Task Throws_When_Cancelled_While_Waiting_For_Room()
    {
        var channel = Channel.CreateBounded<int>(1);
        channel.Writer.TryWrite(1);
        using var cts = new CancellationTokenSource();
        var write = channel.Writer.WriteOnceAsync(2, cts.Token).AsTask();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await write.WaitAsync(TimeSpan.FromSeconds(10)));
    }
}
