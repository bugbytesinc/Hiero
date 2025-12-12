using Hiero.Mirror.Filters;
using System.Diagnostics;
using System.Text.Json;
using System.Web;

namespace Hiero.Mirror.Implementation;

internal static class MirrorRestClientUtils
{
    /// <summary>
    /// Helper function to generate the root path to a mirror node query
    /// with potential filter options.
    /// </summary>
    /// <param name="rootPath">
    /// Basic root path of the rest query.
    /// </param>
    /// <param name="limit">
    /// Optional parameter to set the limit of enumerable items
    /// returned (typically managed internally to the client library)
    /// </param>
    /// <param name="filters">
    /// Optional list of filters to translate into query parameters.
    /// </param>
    /// <returns>
    /// The mirror node rest query path including optional query parameters.
    /// </returns>
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryFilter[] filters)
    {
        if (filters.Length == 0)
        {
            return rootPath;
        }
        var query = string.Join("&", filters.Select(f => $"{HttpUtility.UrlEncode(f.Name)}={HttpUtility.UrlEncode(f.Value)}"));
        return $"{rootPath}?{query}";
    }
    /// <summary>
    /// Cast an entityId into a valid entityId or evm entityId format
    /// recognizable by the mirror node.
    /// </summary>
    /// <param name="entityId">
    /// The HAPI entityId (or embedded EvmAddress or Alias)
    /// </param>
    /// <returns>
    /// Mirror node compatible addressOrEvmAddress string.
    /// </returns>
    internal static string MirrorFormat(EntityId entityId)
    {
        if (entityId == null)
        {
            return "0.0.0";
        }
        if (entityId.TryGetEvmAddress(out var evmAddress))
        {
            return Hex.FromBytes(evmAddress.Bytes);
        }
        if (entityId.TryGetKeyAlias(out var keyAlias))
        {
            return Hex.FromBytes(keyAlias.ToBytes(KeyFormat.Mirror));
        }
        return $"{entityId.ShardNum}.{entityId.RealmNum}.{entityId.AccountNum}";
    }
    /// <summary>
    /// Formats a transaction id to mirror format
    /// </summary>
    /// <param name="txId">transaction id</param>
    /// <returns>mirror search format for a transaction id</returns>
    internal static string MirrorFormat(TransactionId txId)
    {
        if (txId == null)
        {
            return "";
        }
        return $"{txId.Payer}-{txId.ValidStartSeconds}-{txId.ValidStartNanos:000000000}";
    }
    /// <summary>
    /// Helper method that extracts extra error information returned from
    /// the mirror node and attempts to sort out the reason for the failure.
    /// </summary>
    /// <param name="response">
    /// A failed HTTP Response.
    /// </param>
    /// <exception cref="MirrorException">
    /// Throws a mirror call exception with extracted details regarding the error.
    /// </exception>
    [StackTraceHidden]
    internal static async Task<Exception> CreateMirrorExceptionAsync(HttpResponseMessage response)
    {
        var reason = await response.Content.ReadAsStringAsync();
        MirrorError[]? messages = null;
        try
        {
            messages = JsonSerializer.Deserialize<MirrorErrorListEnvelope>(reason)?.Status?.Messages;
        }
        catch
        {
            // format not known
        }
        if (messages != null && messages.Length > 0)
        {
            var summary = messages[0].Message ?? $"Mirror Call Failed: {response.StatusCode}";
            throw new MirrorException(summary, messages, response.StatusCode);
        }
        return new MirrorException($"Mirror Call Failed: {reason}", Array.Empty<MirrorError>(), response.StatusCode);
    }
}
