// SPDX-License-Identifier: Apache-2.0
using System.Numerics;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on an EVM event-log topic position
/// (<c>topic0</c> … <c>topic3</c>) on the contract-logs endpoints.
/// Construct via <see cref="Is(int, BigInteger)"/>; the ctor is
/// private.
/// </summary>
/// <remarks>
/// "Topic" here is the Ethereum event-log topic (one of up to four
/// indexed parameters of an emitted event), not an HCS topic id.
/// Each factory call produces one filter bound to a specific
/// <c>index</c> slot (0–3) — pass multiple instances with different
/// indices to constrain more than one topic.
/// </remarks>
public sealed class EvmTopicFilter : IMirrorFilter
{
    private readonly int _index;
    /// <summary>
    /// The query parameter name recognized by the remote mirror node
    /// — <c>topic0</c> through <c>topic3</c> depending on index.
    /// </summary>
    public string Name => $"topic{_index}";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private EvmTopicFilter(int index, string value)
    {
        _index = index;
        Value = value;
    }

    /// <summary>
    /// Records whose EVM event-log topic at position
    /// <paramref name="index"/> equals the given value.
    /// </summary>
    /// <param name="index">The topic slot, 0 through 3 inclusive.</param>
    /// <param name="topic">The topic value (padded to 32 bytes on the wire).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the 0–3 range.
    /// </exception>
    public static EvmTopicFilter Is(int index, BigInteger topic)
    {
        if (index < 0 || index > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index topics must be between 0 and 3 inclusive.");
        }
        var value = "0x" + Hex.FromBytes(topic.ToByteArray(true, true)).PadLeft(64, '0');
        return new EvmTopicFilter(index, value);
    }
}
