// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>memory</c> query parameter — controls
/// whether the mirror node populates each <see cref="OpcodeData.Memory"/>
/// array in an opcode-trace response. Implements
/// <see cref="IMirrorProjection"/>: it reshapes each record, not which
/// records are returned.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>memory=false</c>; passing
/// <see cref="Include"/> opts in. Including memory information
/// significantly enlarges the response payload and can materially
/// increase re-execution time on the mirror node.
/// </para>
/// <para>
/// Accepted by
/// <c>/api/v1/contracts/results/{transactionIdOrHash}/opcodes</c>.
/// </para>
/// </remarks>
public sealed class OpcodeMemoryProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Populate the <c>memory</c> array on each returned opcode.
    /// </summary>
    public static readonly OpcodeMemoryProjectionFilter Include = new("true");
    /// <summary>
    /// Omit the <c>memory</c> array from each returned opcode
    /// (the server's default behavior — explicit here for
    /// call-site clarity).
    /// </summary>
    public static readonly OpcodeMemoryProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "memory";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private OpcodeMemoryProjectionFilter(string value) => Value = value;
}
