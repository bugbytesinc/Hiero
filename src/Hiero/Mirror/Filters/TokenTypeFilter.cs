// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Paging;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>type</c> query parameter of the
/// <c>/api/v1/tokens</c> list endpoint — restricts the listing
/// to fungible, non-fungible, or (default) all tokens.
/// </summary>
/// <remarks>
/// The three values are the OpenAPI enum
/// <c>ALL | FUNGIBLE_COMMON | NON_FUNGIBLE_UNIQUE</c>. Exposed
/// as static properties (following the <see cref="OrderBy"/>
/// precedent) rather than factories because the value set is
/// closed and fully known at compile time.
/// </remarks>
public sealed class TokenTypeFilter : IMirrorFilter
{
    /// <summary>
    /// Filter matching every token — fungible and non-fungible.
    /// Equivalent to omitting the filter entirely; present for
    /// cases where explicit intent at the call site matters.
    /// </summary>
    public static readonly TokenTypeFilter All = new("ALL");
    /// <summary>
    /// Filter matching only fungible tokens.
    /// </summary>
    public static readonly TokenTypeFilter Fungible = new("FUNGIBLE_COMMON");
    /// <summary>
    /// Filter matching only non-fungible tokens.
    /// </summary>
    public static readonly TokenTypeFilter NonFungible = new("NON_FUNGIBLE_UNIQUE");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "type";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private TokenTypeFilter(string value) => Value = value;
}
