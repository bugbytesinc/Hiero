using System.Threading.Channels;

namespace Hiero.Test.Integration.Fixtures;

public sealed class TopicMessageCapture
{
    private readonly Channel<TopicMessage> _channel;
    private readonly Task _readTask;
    private readonly int _limit;
    private readonly List<TopicMessage> _list;

    public ChannelWriter<TopicMessage> MessageWriter => _channel.Writer;
    public IList<TopicMessage> CapturedList => _list;

    public TopicMessageCapture(int limit)
    {
        _limit = limit;
        _list = new List<TopicMessage>(limit);
        _channel = Channel.CreateBounded<TopicMessage>(Math.Min(32, limit));
        var reader = _channel.Reader;
        var writer = _channel.Writer;
        _readTask = Task.Run(async () =>
        {
            try
            {
                while (await reader.WaitToReadAsync())
                {
                    if (reader.TryRead(out TopicMessage? message))
                    {
                        _list.Add(message);
                        if (_list.Count == _limit)
                        {
                            writer.TryComplete();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                writer.TryComplete(ex);
            }
        });
    }

    public async Task<bool> TryStopAsync()
    {
        var result = _channel.Writer.TryComplete();
        await _readTask;
        return result;
    }

    public static implicit operator ChannelWriter<TopicMessage>(TopicMessageCapture adapter)
    {
        return adapter.MessageWriter;
    }

    public static async Task<TopicMessage[]> CaptureOrTimeoutAsync(MirrorGrpcClient mirror, EntityId topic, int expectedCount, int timeoutInMilliseconds)
    {
        using var cts = new CancellationTokenSource();
        var capture = new TopicMessageCapture(expectedCount);
        var subscribeTask = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = topic,
            Starting = DateTime.UtcNow.AddHours(-1),
            MessageWriter = capture,
            CancellationToken = cts.Token
        });
        cts.CancelAfter(timeoutInMilliseconds);
        await subscribeTask;
        return capture.CapturedList.ToArray();
    }
}
