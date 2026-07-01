// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Implementation;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
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
    /// <returns>The HTTP response message from the mirror node REST API.</returns>
    internal Task<HttpResponseMessage> PostPayload<TValue>(string path, TValue payload, JsonTypeInfo<TValue> jsonTypeInfo)
    {
        return _client.PostAsJsonAsync($"api/v1/{path}", payload, jsonTypeInfo);
    }
    /// <summary>
    /// Internal helper function to retrieve a paged items structured
    /// object, converting it into an IAsyncEnumerable for consumption.
    /// </summary>
    internal async IAsyncEnumerable<TItem> GetPagedItemsAsync<TList, TItem>(string path, JsonTypeInfo<TList> jsonTypeInfo) where TList : Page<TItem>
    {
        var fullPath = $"api/v1/{path}";
        do
        {
            using var response = await _client.GetAsync(fullPath).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync(jsonTypeInfo).ConfigureAwait(false);
                if (payload is not null)
                {
                    var items = payload.GetItems();
                    var count = items.Length;
                    for (var i = 0; i < count; i++)
                    {
                        yield return items[i];
                    }
                }
                fullPath = payload?.Links?.Next;
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                fullPath = null;
                break;
            }
            else
            {
                throw await CreateMirrorExceptionAsync(response).ConfigureAwait(false);
            }
        }
        while (!string.IsNullOrWhiteSpace(fullPath));
    }
    /// <summary>
    /// Helper function to retrieve a single item from the rest api call.
    /// </summary>
    internal async Task<TItem?> GetSingleItemAsync<TItem>(string path, JsonTypeInfo<TItem> jsonTypeInfo)
    {
        using var response = await _client.GetAsync($"api/v1/{path}").ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(jsonTypeInfo).ConfigureAwait(false);
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        else
        {
            throw await CreateMirrorExceptionAsync(response).ConfigureAwait(false);
        }
    }
}
