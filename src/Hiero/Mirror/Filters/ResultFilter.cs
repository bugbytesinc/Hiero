// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Paging;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>result</c> query parameter of the
/// <c>/api/v1/transactions</c> list endpoint — restricts the
/// listing to successful or unsuccessful transactions.
/// </summary>
/// <remarks>
/// The two values are the OpenAPI enum <c>success | fail</c>.
/// Exposed as static properties (following the <see cref="OrderBy"/>
/// precedent) rather than factories because the value set is
/// closed and fully known at compile time.
/// </remarks>
public sealed class ResultFilter : IMirrorFilter
{
    /// <summary>
    /// Filter matching only transactions that reached a successful
    /// consensus result.
    /// </summary>
    public static readonly ResultFilter Success = new("success");
    /// <summary>
    /// Filter matching only transactions whose consensus result
    /// was anything other than success.
    /// </summary>
    public static readonly ResultFilter Fail = new("fail");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "result";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private ResultFilter(string value) => Value = value;
}
