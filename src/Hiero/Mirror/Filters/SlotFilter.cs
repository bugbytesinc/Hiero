// SPDX-License-Identifier: Apache-2.0
using System.Numerics;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>slot</c> query parameter for contract
/// state reads. The wire format is a 32-byte 0x-prefixed hex string.
/// Construct via <see cref="Is(BigInteger)"/>; the ctor is private.
/// </summary>
/// <remarks>
/// <para>
/// The <c>slot</c> query parameter on
/// <c>/api/v1/contracts/{contractIdOrAddress}/state</c> accepts
/// only an equality match — the mirror-node schema exposes no
/// comparison-operator palette for storage-slot addresses. Only
/// an <see cref="Is(BigInteger)"/> factory is provided; there is
/// no <c>After</c> / <c>Before</c> / <c>NotIs</c> counterpart.
/// </para>
/// <para>
/// The factory normalizes the <see cref="BigInteger"/> to the
/// server's canonical wire form: a 32-byte (64-hex-digit)
/// big-endian zero-padded hex string with an <c>0x</c> prefix.
/// Callers supplying a slot value as an opaque 32-byte integer
/// need no further formatting.
/// </para>
/// </remarks>
public sealed class SlotFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "slot";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private SlotFilter(string value) => Value = value;

    /// <summary>
    /// Records for the given 32-byte storage slot.
    /// </summary>
    /// <param name="slot">The slot position as a big integer.</param>
    public static SlotFilter Is(BigInteger slot) =>
        new($"0x{Hex.FromBytes(slot.ToByteArray(true, true)).PadLeft(64, '0')}");
}
