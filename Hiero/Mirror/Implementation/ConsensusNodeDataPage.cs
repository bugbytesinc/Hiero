namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of gossip nodes.
/// </summary>
internal class ConsensusNodeDataPage : Page<ConsensusNodeData>
{
    /// <summary>
    /// List of gossip nodes.
    /// </summary>
    public ConsensusNodeData[]? Nodes { get; set; }
    /// <summary>
    /// Enumerates the list of gossip nodes.
    /// </summary>
    /// <returns>
    /// Enumerator of gossip nodes for this paged list.
    /// </returns>
    public override IEnumerable<ConsensusNodeData> GetItems()
    {
        return Nodes ?? Array.Empty<ConsensusNodeData>();
    }
}