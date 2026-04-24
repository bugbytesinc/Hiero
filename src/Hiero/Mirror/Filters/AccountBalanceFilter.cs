// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>account.balance</c> query parameter
/// used by the account-list and balance-snapshot endpoints.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// The wire parameter takes a tinybar balance threshold with an
/// optional comparison-operator prefix. OpenAPI pattern
/// <c>^((gte?|lte?|eq|ne)\:)?\d{1,10}$</c> admits all six
/// operators; each factory builds the corresponding wire value.
/// </para>
/// <para>
/// Despite the dotted <c>account.balance</c> name this is a
/// predicate filter, not a projection — it narrows which accounts
/// are returned. The distinct <see cref="BalanceProjectionFilter"/>
/// covers the separate <c>balance</c> wire param, which is the
/// projection toggle for whether the response payload includes
/// the balance subtree.
/// </para>
/// </remarks>
public sealed class AccountBalanceFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "account.balance";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private AccountBalanceFilter(string value) => Value = value;

    /// <summary>
    /// Records whose balance equals the given tinybar value.
    /// </summary>
    public static AccountBalanceFilter Is(long tinybars) => new(tinybars.ToString());
    /// <summary>
    /// Records whose balance is strictly greater than the given
    /// tinybar value (<c>gt:</c>).
    /// </summary>
    public static AccountBalanceFilter After(long tinybars) => new($"gt:{tinybars}");
    /// <summary>
    /// Records whose balance is at or greater than the given
    /// tinybar value (<c>gte:</c>).
    /// </summary>
    public static AccountBalanceFilter OnOrAfter(long tinybars) => new($"gte:{tinybars}");
    /// <summary>
    /// Records whose balance is strictly less than the given
    /// tinybar value (<c>lt:</c>).
    /// </summary>
    public static AccountBalanceFilter Before(long tinybars) => new($"lt:{tinybars}");
    /// <summary>
    /// Records whose balance is at or less than the given tinybar
    /// value (<c>lte:</c>).
    /// </summary>
    public static AccountBalanceFilter OnOrBefore(long tinybars) => new($"lte:{tinybars}");
    /// <summary>
    /// Records whose balance is not equal to the given tinybar
    /// value (<c>ne:</c>).
    /// </summary>
    public static AccountBalanceFilter NotIs(long tinybars) => new($"ne:{tinybars}");
}
