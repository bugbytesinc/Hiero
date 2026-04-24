// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Projection toggle on the <c>stack</c> query parameter — controls
/// whether the mirror node populates each <see cref="OpcodeData.Stack"/>
/// array in an opcode-trace response. Implements
/// <see cref="IMirrorProjection"/>: it reshapes each record, not which
/// records are returned.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node default is <c>stack=true</c>; passing
/// <see cref="Exclude"/> opts out. Excluding stack information is a
/// useful latency/bandwidth reduction when the caller only cares
/// about the opcode sequence or the final result.
/// </para>
/// <para>
/// Accepted by
/// <c>/api/v1/contracts/results/{transactionIdOrHash}/opcodes</c>.
/// </para>
/// </remarks>
public sealed class OpcodeStackProjectionFilter : IMirrorProjection
{
    /// <summary>
    /// Populate the <c>stack</c> array on each returned opcode
    /// (the server's default behavior — explicit here for
    /// call-site clarity).
    /// </summary>
    public static readonly OpcodeStackProjectionFilter Include = new("true");
    /// <summary>
    /// Omit the <c>stack</c> array from each returned opcode.
    /// </summary>
    public static readonly OpcodeStackProjectionFilter Exclude = new("false");

    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "stack";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private OpcodeStackProjectionFilter(string value) => Value = value;
}
