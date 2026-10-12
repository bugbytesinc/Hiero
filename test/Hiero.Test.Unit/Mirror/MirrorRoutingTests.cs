// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;
using System.Net;
using System.Text;

namespace Hiero.Test.Unit.Mirror;

/// <summary>
/// Which URLs the mirror client actually requests: paging behind a
/// gateway path, and the transaction variants each route can select.
/// </summary>
public class MirrorRoutingTests
{
    private static readonly TransactionId Parent = new(new EntityId(0, 0, 2), 1_700_000_000, 5);

    [Test]
    public async Task Later_Pages_Keep_The_Gateway_Path_Prefix()
    {
        var mirror = new RecordingMirror(request => request.RequestUri!.Query.Contains("timestamp=lt")
            ? """{"transactions":[],"links":{"next":null}}"""
            : """{"transactions":[],"links":{"next":"/api/v1/transactions?limit=1&timestamp=lt:1700000000.000000000"}}""");
        var client = new MirrorRestClient(new HttpClient(mirror) { BaseAddress = new Uri("https://gateway.example.com/v1/key/") });
        await foreach (var _ in client.GetTransactionsAsync())
        {
        }
        await Assert.That(mirror.Paths).IsEquivalentTo(["/v1/key/api/v1/transactions", "/v1/key/api/v1/transactions"]);
    }

    [Test]
    public async Task A_Scheduled_Id_Is_Rejected_By_Routes_That_Cannot_Select_It()
    {
        var client = new MirrorRestClient(new HttpClient(new RecordingMirror(_ => "{}")) { BaseAddress = new Uri("https://mirror.example.com/") });
        var scheduled = new TransactionId(Parent.Payer, Parent.ValidStartSeconds, Parent.ValidStartNanos, scheduled: true);
        await Assert.ThrowsAsync<ArgumentException>(async () => await client.GetContractResultByTransactionIdAsync(scheduled));
        await Assert.ThrowsAsync<ArgumentException>(async () => await client.GetContractOpcodesByTransactionIdAsync(scheduled));
        await Assert.ThrowsAsync<ArgumentException>(async () => await Task.Run(() => client.GetContractActionsByTransactionIdAsync(scheduled)));
    }

    [Test]
    public async Task A_Child_Id_Is_Sent_With_Its_Nonce_Only_Where_The_Route_Accepts_One()
    {
        var mirror = new RecordingMirror(_ => """{"contract_id":"0.0.1001"}""");
        var client = new MirrorRestClient(new HttpClient(mirror) { BaseAddress = new Uri("https://mirror.example.com/") });
        var child = new TransactionId(Parent.Payer, Parent.ValidStartSeconds, Parent.ValidStartNanos, childNonce: 2);
        await client.GetContractResultByTransactionIdAsync(child);
        await Assert.That(mirror.Queries[0]).Contains("nonce=2");
        await Assert.ThrowsAsync<ArgumentException>(async () => await client.GetContractOpcodesByTransactionIdAsync(child));
        await Assert.ThrowsAsync<ArgumentException>(async () => await Task.Run(() => client.GetContractActionsByTransactionIdAsync(child)));
    }

    private sealed class RecordingMirror(Func<HttpRequestMessage, string> respond) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public List<string> Queries { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            Queries.Add(request.RequestUri.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respond(request), Encoding.UTF8, "application/json")
            });
        }
    }
}
