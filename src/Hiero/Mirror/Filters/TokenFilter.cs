// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>token.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>token.id</c> query parameter. Each factory builds the
/// corresponding wire value.
/// </para>
/// <para>
/// Some endpoints restrict which operators they will honor at
/// the server level — for example,
/// <c>/api/v1/accounts/{id}/nfts</c> rejects <c>ne:</c> and
/// allows only a single occurrence of each comparison form.
/// See the per-endpoint OpenAPI documentation for the exact
/// constraints; the filter itself does not enforce them.
/// </para>
/// </remarks>
public sealed class TokenFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "token.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private TokenFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>token.id</c> equals the given entity.
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter Is(EntityId token) => new(token.ToString());
    /// <summary>
    /// Records whose <c>token.id</c> is strictly greater than the
    /// given entity (<c>gt:</c>).
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter After(EntityId token) => new($"gt:{token}");
    /// <summary>
    /// Records whose <c>token.id</c> is at or greater than the given
    /// entity (<c>gte:</c>).
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter OnOrAfter(EntityId token) => new($"gte:{token}");
    /// <summary>
    /// Records whose <c>token.id</c> is strictly less than the given
    /// entity (<c>lt:</c>).
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter Before(EntityId token) => new($"lt:{token}");
    /// <summary>
    /// Records whose <c>token.id</c> is at or less than the given
    /// entity (<c>lte:</c>).
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter OnOrBefore(EntityId token) => new($"lte:{token}");
    /// <summary>
    /// Records whose <c>token.id</c> is not equal to the given
    /// entity (<c>ne:</c>).
    /// </summary>
    /// <param name="token">The token entity to filter by.</param>
    public static TokenFilter NotIs(EntityId token) => new($"ne:{token}");
}
