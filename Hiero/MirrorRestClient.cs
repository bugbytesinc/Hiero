using Hiero.Mirror.Implementation;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero;
/// <summary>
/// The Mirror Node REST Client.
/// </summary>
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
    /// <returns></returns>
    internal Task<HttpResponseMessage> PostPayload<TValue>(string path, TValue payload)
    {
        return _client.PostAsJsonAsync($"api/v1/{path}", payload);
    }
    /// <summary>
    /// Internal helper function to retrieve a paged items structured
    /// object, converting it into an IAsyncEnumerable for consumption.
    /// </summary>
    internal async IAsyncEnumerable<TItem> GetPagedItemsAsync<TList, TItem>(string path) where TList : Page<TItem>
    {
        var fullPath = $"api/v1/{path}";
        do
        {
            using var response = await _client.GetAsync(fullPath);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<TList>();
                if (payload is not null)
                {
                    foreach (var item in payload.GetItems())
                    {
                        yield return item;
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
                throw await CreateMirrorExceptionAsync(response);
            }
        }
        while (!string.IsNullOrWhiteSpace(fullPath));
    }
    /// <summary>
    /// Helper function to retrieve a single item from the rest api call.
    /// </summary>
    internal async Task<TItem?> GetSingleItemAsync<TItem>(string path)
    {
        using var response = await _client.GetAsync($"api/v1/{path}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TItem>();
        }
        else if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        else
        {
            throw await CreateMirrorExceptionAsync(response);
        }
    }
}
