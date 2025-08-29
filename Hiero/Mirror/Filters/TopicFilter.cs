using System.Numerics;

namespace Hiero.Mirror.Filters;
/// <summary>
/// Filters a request for contract log events
/// to the specified index and topic, typically
/// requires additional temporal filters when querying
/// the mirror nodes.  Multiple filters may be added
/// to the same mirror node query.
/// </summary>
public class TopicFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The topic index this query applies to.
    /// </summary>
    private readonly int _index;
    /// <summary>
    /// The topic value to filter by.
    /// </summary>
    private readonly BigInteger _topic;
    /// <summary>
    /// Constructor requres the token to filter the request by.
    /// </summary>
    /// <param name="index">
    /// The topic index this query applies to.
    /// </param>
    /// <param name="topic">
    /// The topic value to filter by.
    /// </param>
    public TopicFilter(int index, BigInteger topic)
    {
        if (index < 0 || index > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index topics must be between 0 and 3 inclusive.");
        }
        _index = index;
        _topic = topic;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => $"topic{_index}";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => "0x" + Hex.FromBytes(_topic.ToByteArray(true, true)).PadLeft(64, '0');
}
