// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>balance</c> query parameter —
/// controls whether the mirror node includes balance information
/// in each returned record. Implements <see cref="IMirrorProjection"/>
/// rather than <see cref="IMirrorFilter"/>: it does not narrow
/// which records are returned, only reshapes each response payload.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>balance=true</c>; passing
/// <see cref="Exclude"/> opts out to reduce payload size when
/// the caller doesn't need the balance subtree. When balance is
/// included, token balances within it are capped at 50 entries
/// per account per HIP-367.
/// </para>
/// <para>
/// This is the first concrete <see cref="IMirrorProjection"/>
/// implementation in the SDK; the marker interface was
/// introduced in Phase 2 for exactly this category of wire
/// parameter.
/// </para>
/// </remarks>
public sealed class BalanceProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Include the balance subtree in each returned record (the
    /// server's default behavior — explicit here for call-site
    /// clarity).
    /// </summary>
    public static readonly BalanceProjectionFilter Include = new("true");
    /// <summary>
    /// Omit the balance subtree from each returned record.
    /// </summary>
    public static readonly BalanceProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "balance";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private BalanceProjectionFilter(string value) => Value = value;
}
