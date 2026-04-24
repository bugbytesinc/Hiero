// SPDX-License-Identifier: Apache-2.0
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
    /// <param name="filters">
    /// Optional list of filters to translate into query parameters.
    /// </param>
    /// <returns>
    /// The mirror node rest query path including optional query parameters.
    /// </returns>
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter[] filters)
    {
        if (filters.Length == 0)
        {
            return rootPath;
        }
        var query = string.Join("&", filters.Select(f => $"{HttpUtility.UrlEncode(f.Name)}={HttpUtility.UrlEncode(f.Value)}"));
        var separator = rootPath.Contains('?') ? '&' : '?';
        return $"{rootPath}{separator}{query}";
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
    /// Builds the path segment plus any required query string for a transaction-id-addressed
    /// mirror-node resource (e.g. <c>/api/v1/transactions/{id}</c>,
    /// <c>/api/v1/contracts/results/{id}</c>). The path segment uses the
    /// <c>{payer}-{seconds}-{nanos}</c> form that the mirror node expects.
    /// <see cref="TransactionId.Scheduled"/> and <see cref="TransactionId.ChildNonce"/>
    /// are translated into <c>scheduled=true</c> / <c>nonce={n}</c> query
    /// parameters when set, so that a round-trip through a mirror URL preserves
    /// the full identity of the transaction variant (parent, scheduled child,
    /// or nonce-indexed child).
    /// </summary>
    /// <param name="txId">transaction id</param>
    /// <returns>Hedera transaction id formatted for mirror node, plus additional query 
    /// parameters if the transaction id carries a nonce or scheduled flag.</returns>
    internal static (string, IMirrorFilter[]) MirrorFormat(TransactionId txId)
    {
        if (txId == null)
        {
            return (string.Empty, []);
        }
        var mirrorTxId = $"{txId.Payer}-{txId.ValidStartSeconds}-{txId.ValidStartNanos:000000000}";
        if (txId.ChildNonce != 0)
        {
            if (txId.Scheduled)
            {
                return (mirrorTxId, [TransactionChildNonceFilter.Is(txId.ChildNonce), ScheduledFilter.IsScheduled]);
            }
            return (mirrorTxId, [TransactionChildNonceFilter.Is(txId.ChildNonce)]);
        }
        if (txId.Scheduled)
        {
            return (mirrorTxId, [ScheduledFilter.IsScheduled]);
        }
        return (mirrorTxId, []);
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
            messages = JsonSerializer.Deserialize(reason, MirrorJsonContext.Default.MirrorErrorListEnvelope)?.Status?.Messages;
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
/// <summary>
/// Filter indicating the transaction is scheduled.
/// </summary>
/// <remarks>
/// Not intended for the public API, scheudled and nonce
/// live on the TransactionId primitive, filters are necessary
/// for the plumbing of path and query string creation.
/// </remarks>
file sealed class ScheduledFilter : IMirrorFilter
{
    public static readonly ScheduledFilter IsScheduled = new();
    public string Name => "scheduled";
    public string Value => "true";
}
/// <summary>
/// Filter identifying the child nonce of the transaction
/// </summary>
/// <remarks>
/// Not intended for the public API, scheudled and nonce
/// live on the TransactionId primitive, filters are necessary
/// for the plumbing of path and query string creation.
/// </remarks>
file sealed class TransactionChildNonceFilter : IMirrorFilter
{
    public static TransactionChildNonceFilter Is(int nonce) => new(nonce.ToString());
    private TransactionChildNonceFilter(string value) => Value = value;
    public string Name => "nonce";
    public string Value { get; }
}