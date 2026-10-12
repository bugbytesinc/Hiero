// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Implementation;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero;
/// <summary>
/// The Mirror Node REST Client.
/// </summary>
/// <remarks>
/// Wraps a configured <see cref="HttpClient"/> pointed at a mirror node's
/// base address and queries its REST API for historical and current network
/// state.  The data-retrieval operations are provided as extension methods on
/// this client (one per resource family — accounts, tokens, transactions, and
/// so on); single results are returned as awaitable tasks and paged result
/// sets as <see cref="IAsyncEnumerable{T}"/> that transparently follow the
/// mirror node's "next" links.  This client performs read-only queries and
/// does not submit transactions; use <see cref="ConsensusClient"/> for that.
/// <para>
/// <b>Thread safety.</b> An instance is immutable once constructed — it holds
/// only the <see cref="HttpClient"/> it was given — so it is safe for
/// concurrent use and carries none of the configuration caveats the
/// context-configured clients do. Concurrency is bounded by the supplied
/// <see cref="HttpClient"/>, which is itself safe to share.
/// </para>
/// <para>
/// <b>Cancellation.</b> Every method takes a <see cref="CancellationToken"/>:
/// as its last parameter or, for a method that ends in a list of filters, in
/// an overload taking it just before the list. Paged results also honor
/// <see cref="TaskAsyncEnumerableExtensions.WithCancellation{T}(IAsyncEnumerable{T}, CancellationToken)"/>.
/// A cancelled call throws <see cref="OperationCanceledException"/>. A request
/// that outlasts <see cref="HttpClient.Timeout"/> throws a
/// <see cref="TaskCanceledException"/> whose inner exception is a
/// <see cref="TimeoutException"/>.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MirrorRestClient
{
    /// <summary>
    /// The underlying http client connecting to the mirror node.
    /// </summary>
    private readonly HttpClient _client;
    /// <summary>
    /// The remote mirror node endpoint url.
    /// </summary>
    public string EndpointUrl => _client.BaseAddress?.ToString() ?? string.Empty;
    /// <summary>
    /// Constructor, requires a configured HttpClient including the base url.
    /// </summary>
    /// <param name="client">
    /// A configured HttpClient object, must at least have the BaseAddress
    /// set for the mirror node client to function properly.
    /// </param>
    public MirrorRestClient(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        _client = client;
    }
    /// <summary>
    /// Returns a useful string representation of the client, and
    /// which mirror node it is configured to connect to.
    /// </summary>
    public override string ToString()
    {
        return _client.BaseAddress?.ToString() ?? "unconfigured";
    }
    /// <summary>
    /// Internal helper function to post a payload to the mirror node REST API.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of payload to post, it should be serializable as JSON.
    /// </typeparam>
    /// <param name="path">
    /// The path to post the payload to, this should be the API endpoint
    /// </param>
    /// <param name="payload">
    /// Payload to post, it will be serialized as JSON.
    /// </param>
    /// <param name="jsonTypeInfo">
    /// The source-generated JSON type info for the payload type.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the request.
    /// </param>
    /// <returns>The HTTP response message from the mirror node REST API.</returns>
    internal Task<HttpResponseMessage> PostPayload<TValue>(string path, TValue payload, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken)
    {
        return _client.PostAsJsonAsync($"api/v1/{path}", payload, jsonTypeInfo, cancellationToken);
    }
    /// <summary>
    /// Internal helper function to retrieve a paged items structured
    /// object, converting it into an IAsyncEnumerable for consumption.
    /// The token is combined with any given to
    /// <see cref="TaskAsyncEnumerableExtensions.WithCancellation{T}(IAsyncEnumerable{T}, CancellationToken)"/>,
    /// and either cancels the page request in flight.
    /// </summary>
    internal async IAsyncEnumerable<TItem> GetPagedItemsAsync<TList, TItem>(string path, JsonTypeInfo<TList> jsonTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken) where TList : Page<TItem>
    {
        var fullPath = $"api/v1/{path}";
        do
        {
            using var response = await _client.GetAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
                if (payload is not null)
                {
                    var items = payload.GetItems();
                    var count = items.Length;
                    for (var i = 0; i < count; i++)
                    {
                        yield return items[i];
                    }
                }
                // The mirror's next link is root-relative (/api/v1/...);
                // resolve it against the base address, as for the first
                // page, so a gateway's path prefix is kept.
                fullPath = payload?.Links?.Next?.TrimStart('/');
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                fullPath = null;
                break;
            }
            else
            {
                throw await CreateMirrorExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }
        while (!string.IsNullOrWhiteSpace(fullPath));
    }
    /// <summary>
    /// Helper function to retrieve a single item from the rest api call.
    /// </summary>
    internal async Task<TItem?> GetSingleItemAsync<TItem>(string path, JsonTypeInfo<TItem> jsonTypeInfo, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync($"api/v1/{path}", cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        else
        {
            throw await CreateMirrorExceptionAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }
}
