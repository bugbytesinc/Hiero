// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System.Net;

namespace Hiero.Test.Unit.BlockNode;

/// <summary>
/// An HttpMessageHandler that impersonates a block node's gRPC server,
/// returning scripted responses per method path.  Used to unit test
/// the BlockNodeClient's request/response mapping and retry behavior
/// without a network.
/// </summary>
internal sealed class FakeBlockNodeHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<(IMessage[] Messages, StatusCode Status)>> _scripts = [];
    private readonly Dictionary<string, int> _callCounts = [];
    private readonly Dictionary<string, List<byte[]>> _requests = [];

    /// <summary>
    /// The captured request payloads (unframed protobuf bytes, one per
    /// call) sent to the given method path, in call order.
    /// </summary>
    public IReadOnlyList<byte[]> Requests(string methodPath)
    {
        return _requests.TryGetValue(methodPath, out var list) ? list : [];
    }

    public int CallCount(string methodPath)
    {
        return _callCounts.TryGetValue(methodPath, out var count) ? count : 0;
    }

    /// <summary>
    /// Enqueues a scripted response for the given method path; responses
    /// are consumed in order, and the last script repeats once the queue
    /// is exhausted... (it is re-enqueued after use).
    /// </summary>
    public void Enqueue(string methodPath, StatusCode status, params IMessage[] messages)
    {
        if (!_scripts.TryGetValue(methodPath, out var queue))
        {
            queue = new Queue<(IMessage[], StatusCode)>();
            _scripts[methodPath] = queue;
        }
        queue.Enqueue((messages, status));
    }

    public GrpcChannel CreateChannel()
    {
        return GrpcChannel.ForAddress("http://fake-block-node:40840", new GrpcChannelOptions { HttpHandler = this });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        _callCounts[path] = CallCount(path) + 1;
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            if (body.Length >= 5)
            {
                if (!_requests.TryGetValue(path, out var list))
                {
                    list = [];
                    _requests[path] = list;
                }
                list.Add(body[5..]);
            }
        }
        if (!_scripts.TryGetValue(path, out var queue) || queue.Count == 0)
        {
            throw new InvalidOperationException($"No scripted response for '{path}'.");
        }
        var (messages, status) = queue.Count == 1 ? queue.Peek() : queue.Dequeue();
        using var buffer = new MemoryStream();
        Span<byte> length = stackalloc byte[4];
        foreach (var message in messages)
        {
            var payload = message.ToByteArray();
            buffer.WriteByte(0);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length, (uint)payload.Length);
            buffer.Write(length);
            buffer.Write(payload);
        }
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Version = HttpVersion.Version20,
            Content = new ByteArrayContent(buffer.ToArray())
        };
        response.Content.Headers.Add("Content-Type", "application/grpc");
        response.TrailingHeaders.Add("grpc-status", ((int)status).ToString());
        return response;
    }
}
