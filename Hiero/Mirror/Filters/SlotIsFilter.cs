using System.Numerics;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Filter contract state data by slot ID.
/// </summary>
public class SlotIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The slot id to filter the request by.
    /// </summary>
    private readonly BigInteger _slot;
    /// <summary>
    /// Constructor requires the slot to filter the request by.
    /// </summary>
    /// <param name="slot">
    /// The ID of the slot to filter the response by.
    /// </param>
    public SlotIsFilter(BigInteger slot)
    {
        _slot = slot;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "slot";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => $"0x{Hex.FromBytes(_slot.ToByteArray(true, true)).PadLeft(64, '0')}";
}
