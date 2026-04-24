// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>internal</c> query parameter —
/// controls whether the mirror node includes child (internal)
/// transactions alongside top-level results. Implements
/// <see cref="IMirrorProjection"/>: reshapes each listing's
/// inclusion rather than narrowing which records are returned.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>internal=false</c>; passing
/// <see cref="Include"/> opts in to seeing child transactions
/// spawned by the top-level result.
/// </para>
/// <para>
/// Accepted by the two list endpoints that return contract-result
/// records: <c>/api/v1/contracts/results</c> and
/// <c>/api/v1/contracts/{id}/results</c>.
/// </para>
/// </remarks>
public sealed class InternalProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Include child transactions in each returned listing. Opt-in;
    /// the server default is to omit them.
    /// </summary>
    public static readonly InternalProjectionFilter Include = new("true");
    /// <summary>
    /// Omit child transactions from each returned listing (the
    /// server's default behavior — explicit here for call-site
    /// clarity).
    /// </summary>
    public static readonly InternalProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "internal";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private InternalProjectionFilter(string value) => Value = value;
}
