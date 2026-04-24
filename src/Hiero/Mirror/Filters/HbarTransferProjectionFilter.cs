// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>hbar</c> query parameter —
/// controls whether the mirror node includes the HBAR-transfer
/// subtree in each returned contract-result record. Implements
/// <see cref="IMirrorProjection"/>: it does not narrow which
/// records are returned, only reshapes each response payload.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>hbar=true</c>; passing
/// <see cref="Exclude"/> opts out to reduce payload size when
/// the caller doesn't need the HBAR-transfer list.
/// </para>
/// <para>
/// Accepted by three endpoints that return contract-result
/// records: <c>/api/v1/contracts/results</c>,
/// <c>/api/v1/contracts/results/{transactionIdOrHash}</c>, and
/// <c>/api/v1/contracts/{id}/results/{timestamp}</c>.
/// </para>
/// </remarks>
public sealed class HbarTransferProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Include the HBAR-transfer subtree in each returned
    /// contract-result record (the server's default behavior —
    /// explicit here for call-site clarity).
    /// </summary>
    public static readonly HbarTransferProjectionFilter Include = new("true");
    /// <summary>
    /// Omit the HBAR-transfer subtree from each returned
    /// contract-result record.
    /// </summary>
    public static readonly HbarTransferProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "hbar";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private HbarTransferProjectionFilter(string value) => Value = value;
}
