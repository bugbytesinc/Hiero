// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Paging;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>name</c> query parameter of the
/// <c>/api/v1/tokens</c> list endpoint. Matches tokens whose name
/// contains the given substring (partial match).
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node server imposes two hard rules on this filter,
/// neither enforced client-side by this type:
/// </para>
/// <para>
/// - It cannot be combined with <see cref="TokenFilter"/> or
///   <see cref="AccountFilter"/> in the same request. The server
///   will reject the request if they appear together.
/// </para>
/// <para>
/// - When this filter is present the mirror node disables
///   pagination; results are ordered by <c>token.id</c> according
///   to the supplied <see cref="OrderBy"/> and
///   <see cref="PageLimit"/> behavior becomes effectively
///   best-effort.
/// </para>
/// <para>
/// OpenAPI restricts the substring to a minimum of 3 and a
/// maximum of 100 characters; the factory enforces this bound
/// locally so out-of-range values throw before the request is
/// sent.
/// </para>
/// </remarks>
public sealed class TokenNameFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "name";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private TokenNameFilter(string value) => Value = value;

    /// <summary>
    /// Matches tokens whose name contains the given substring.
    /// </summary>
    /// <param name="nameFragment">
    /// 3–100 characters of the token name to match. Values
    /// outside that bound throw
    /// <see cref="ArgumentOutOfRangeException"/>.
    /// </param>
    public static TokenNameFilter Contains(string nameFragment)
    {
        ArgumentNullException.ThrowIfNull(nameFragment);
        if (nameFragment.Length < 3 || nameFragment.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(nameFragment), "Token name fragment must be between 3 and 100 characters.");
        }
        return new TokenNameFilter(nameFragment);
    }
}
