// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>storage</c> query parameter — controls
/// whether the mirror node populates each <see cref="OpcodeData.Storage"/>
/// dictionary in an opcode-trace response. Implements
/// <see cref="IMirrorProjection"/>: it reshapes each record, not which
/// records are returned.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>storage=false</c>; passing
/// <see cref="Include"/> opts in. Storage information is typically
/// only needed for deep debugging of SLOAD / SSTORE behavior.
/// </para>
/// <para>
/// Accepted by
/// <c>/api/v1/contracts/results/{transactionIdOrHash}/opcodes</c>.
/// </para>
/// </remarks>
public sealed class OpcodeStorageProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Populate the <c>storage</c> dictionary on each returned opcode.
    /// </summary>
    public static readonly OpcodeStorageProjectionFilter Include = new("true");
    /// <summary>
    /// Omit the <c>storage</c> dictionary from each returned opcode
    /// (the server's default behavior — explicit here for
    /// call-site clarity).
    /// </summary>
    public static readonly OpcodeStorageProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "storage";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private OpcodeStorageProjectionFilter(string value) => Value = value;
}
