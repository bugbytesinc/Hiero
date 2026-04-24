// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>account.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>account.id</c> query parameter. Each factory builds
/// the corresponding wire value.
/// </para>
/// <para>
/// Some endpoints restrict which operators they will honor at
/// the server level. See the per-endpoint OpenAPI documentation
/// for the exact constraints; the filter itself does not
/// enforce them.
/// </para>
/// </remarks>
public sealed class AccountFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "account.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private AccountFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>account.id</c> equals the given entity.
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter Is(EntityId account) => new(account.ToString());
    /// <summary>
    /// Records whose <c>account.id</c> is strictly greater than
    /// the given entity (<c>gt:</c>).
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter After(EntityId account) => new($"gt:{account}");
    /// <summary>
    /// Records whose <c>account.id</c> is at or greater than the
    /// given entity (<c>gte:</c>).
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter OnOrAfter(EntityId account) => new($"gte:{account}");
    /// <summary>
    /// Records whose <c>account.id</c> is strictly less than the
    /// given entity (<c>lt:</c>).
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter Before(EntityId account) => new($"lt:{account}");
    /// <summary>
    /// Records whose <c>account.id</c> is at or less than the
    /// given entity (<c>lte:</c>).
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter OnOrBefore(EntityId account) => new($"lte:{account}");
    /// <summary>
    /// Records whose <c>account.id</c> is not equal to the given
    /// entity (<c>ne:</c>).
    /// </summary>
    /// <param name="account">The account entity to filter by.</param>
    public static AccountFilter NotIs(EntityId account) => new($"ne:{account}");
}
