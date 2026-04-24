// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>spender.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>spender.id</c> query parameter. Each factory builds
/// the corresponding wire value.
/// </para>
/// <para>
/// Some endpoints restrict which operators they will honor at
/// the server level — for example,
/// <c>/api/v1/accounts/{id}/nfts</c> rejects <c>ne:</c>. See the
/// per-endpoint OpenAPI documentation for the exact constraints;
/// the filter itself does not enforce them.
/// </para>
/// </remarks>
public sealed class SpenderFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "spender.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private SpenderFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>spender.id</c> equals the given account.
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter Is(EntityId spender) => new(spender.ToString());
    /// <summary>
    /// Records whose <c>spender.id</c> is strictly greater than
    /// the given account (<c>gt:</c>).
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter After(EntityId spender) => new($"gt:{spender}");
    /// <summary>
    /// Records whose <c>spender.id</c> is at or greater than the
    /// given account (<c>gte:</c>).
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter OnOrAfter(EntityId spender) => new($"gte:{spender}");
    /// <summary>
    /// Records whose <c>spender.id</c> is strictly less than the
    /// given account (<c>lt:</c>).
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter Before(EntityId spender) => new($"lt:{spender}");
    /// <summary>
    /// Records whose <c>spender.id</c> is at or less than the given
    /// account (<c>lte:</c>).
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter OnOrBefore(EntityId spender) => new($"lte:{spender}");
    /// <summary>
    /// Records whose <c>spender.id</c> is not equal to the given
    /// account (<c>ne:</c>).
    /// </summary>
    /// <param name="spender">The spender account to filter by.</param>
    public static SpenderFilter NotIs(EntityId spender) => new($"ne:{spender}");
}
