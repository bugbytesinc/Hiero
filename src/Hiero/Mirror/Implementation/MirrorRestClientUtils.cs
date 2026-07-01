// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Formatting;
using Hiero.Mirror.Filters;
using System.Diagnostics;
using System.Text;
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
        var filterCount = filters.Length;
        if (filterCount == 0)
        {
            return rootPath;
        }
        var builder = CreatePathBuilder(rootPath, filterCount);
        var addSeparator = false;
        for (var i = 0; i < filterCount; i++)
        {
            AppendFilter(builder, filters[i], ref addSeparator);
        }
        return builder.ToString();
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter[] firstFilters, IMirrorQueryParameter[] secondFilters)
    {
        var firstCount = firstFilters.Length;
        var secondCount = secondFilters.Length;
        var filterCount = firstCount + secondCount;
        if (filterCount == 0)
        {
            return rootPath;
        }
        var builder = CreatePathBuilder(rootPath, filterCount);
        var addSeparator = false;
        for (var i = 0; i < firstCount; i++)
        {
            AppendFilter(builder, firstFilters[i], ref addSeparator);
        }
        for (var i = 0; i < secondCount; i++)
        {
            AppendFilter(builder, secondFilters[i], ref addSeparator);
        }
        return builder.ToString();
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter filter)
    {
        EncodeFilter(filter, out var encodedName, out var encodedValue);
        var separator = rootPath.Contains('?') ? '&' : '?';
        return $"{rootPath}{separator}{encodedName}={encodedValue}";
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter firstFilter, IMirrorQueryParameter secondFilter)
    {
        EncodeFilter(firstFilter, out var firstName, out var firstValue);
        EncodeFilter(secondFilter, out var secondName, out var secondValue);
        var separator = rootPath.Contains('?') ? '&' : '?';
        return $"{rootPath}{separator}{firstName}={firstValue}&{secondName}={secondValue}";
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter firstFilter, IMirrorQueryParameter secondFilter, IMirrorQueryParameter thirdFilter)
    {
        EncodeFilter(firstFilter, out var firstName, out var firstValue);
        EncodeFilter(secondFilter, out var secondName, out var secondValue);
        EncodeFilter(thirdFilter, out var thirdName, out var thirdValue);
        var separator = rootPath.Contains('?') ? '&' : '?';
        return $"{rootPath}{separator}{firstName}={firstValue}&{secondName}={secondValue}&{thirdName}={thirdValue}";
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter requiredFilter, IMirrorQueryParameter[] filters)
    {
        var filterCount = filters.Length;
        if (filterCount == 0)
        {
            return GenerateInitialPath(rootPath, requiredFilter);
        }
        var builder = CreatePathBuilder(rootPath, filterCount + 1);
        var addSeparator = false;
        AppendFilter(builder, requiredFilter, ref addSeparator);
        for (var i = 0; i < filterCount; i++)
        {
            AppendFilter(builder, filters[i], ref addSeparator);
        }
        return builder.ToString();
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter firstFilter, IMirrorQueryParameter secondFilter, IMirrorQueryParameter[] filters)
    {
        var filterCount = filters.Length;
        if (filterCount == 0)
        {
            return GenerateInitialPath(rootPath, firstFilter, secondFilter);
        }
        var builder = CreatePathBuilder(rootPath, filterCount + 2);
        var addSeparator = false;
        AppendFilter(builder, firstFilter, ref addSeparator);
        AppendFilter(builder, secondFilter, ref addSeparator);
        for (var i = 0; i < filterCount; i++)
        {
            AppendFilter(builder, filters[i], ref addSeparator);
        }
        return builder.ToString();
    }
    internal static string GenerateInitialPath(string rootPath, IMirrorQueryParameter requiredFilter, IMirrorQueryParameter[] firstFilters, IMirrorQueryParameter[] secondFilters)
    {
        var firstCount = firstFilters.Length;
        var secondCount = secondFilters.Length;
        var filterCount = firstCount + secondCount;
        var builder = CreatePathBuilder(rootPath, filterCount + 1);
        var addSeparator = false;
        AppendFilter(builder, requiredFilter, ref addSeparator);
        for (var i = 0; i < firstCount; i++)
        {
            AppendFilter(builder, firstFilters[i], ref addSeparator);
        }
        for (var i = 0; i < secondCount; i++)
        {
            AppendFilter(builder, secondFilters[i], ref addSeparator);
        }
        return builder.ToString();
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
        var mirrorTxId = TransactionIdFormatter.Format(txId, TransactionIdFormatStyle.Mirror);
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
        var reason = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        MirrorError[]? messages = null;
        try
        {
            messages = JsonSerializer.Deserialize(reason, MirrorJsonContext.Default.MirrorErrorListEnvelope)?.Status?.Messages;
        }
        catch
        {
            // format not known
        }
        var messageCount = messages?.Length ?? 0;
        if (messageCount > 0)
        {
            var errorMessages = messages!;
            var summary = errorMessages[0].Message ?? $"Mirror Call Failed: {response.StatusCode}";
            throw new MirrorException(summary, errorMessages, response.StatusCode);
        }
        return new MirrorException($"Mirror Call Failed: {reason}", Array.Empty<MirrorError>(), response.StatusCode);
    }
    private static StringBuilder CreatePathBuilder(string rootPath, int filterCount)
    {
        var builder = new StringBuilder(rootPath, rootPath.Length + (filterCount * 16));
        builder.Append(rootPath.Contains('?') ? '&' : '?');
        return builder;
    }
    private static void EncodeFilter(IMirrorQueryParameter filter, out string encodedName, out string encodedValue)
    {
        encodedName = HttpUtility.UrlEncode(filter.Name) ?? string.Empty;
        encodedValue = HttpUtility.UrlEncode(filter.Value) ?? string.Empty;
    }
    private static void AppendFilter(StringBuilder builder, IMirrorQueryParameter filter, ref bool addSeparator)
    {
        if (addSeparator)
        {
            builder.Append('&');
        }
        builder.Append(HttpUtility.UrlEncode(filter.Name));
        builder.Append('=');
        builder.Append(HttpUtility.UrlEncode(filter.Value));
        addSeparator = true;
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
