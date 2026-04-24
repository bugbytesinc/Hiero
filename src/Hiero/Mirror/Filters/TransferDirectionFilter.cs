// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Paging;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>type</c> query parameter of the
/// <c>/api/v1/transactions</c> list endpoint — narrows the
/// listing to transactions that credit or debit the filtered
/// account.
/// </summary>
/// <remarks>
/// <para>
/// The two values are the OpenAPI enum <c>credit | debit</c>.
/// Exposed as static properties (matching the <see cref="OrderBy"/>
/// precedent) rather than factories because the value set is
/// closed and fully known at compile time.
/// </para>
/// <para>
/// The class name intentionally avoids a literal <c>TypeFilter</c>
/// — the wire parameter name <c>type</c> is reused on other
/// endpoints (e.g., <c>/api/v1/tokens</c>, which has
/// <see cref="TokenTypeFilter"/>) with entirely different
/// semantics, so a descriptive class name keeps them straight.
/// </para>
/// </remarks>
public sealed class TransferDirectionFilter : IMirrorFilter
{
    /// <summary>
    /// Filter matching transactions whose net effect on the
    /// target account is positive (a credit).
    /// </summary>
    public static readonly TransferDirectionFilter Credit = new("credit");
    /// <summary>
    /// Filter matching transactions whose net effect on the
    /// target account is negative (a debit).
    /// </summary>
    public static readonly TransferDirectionFilter Debit = new("debit");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "type";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private TransferDirectionFilter(string value) => Value = value;
}
