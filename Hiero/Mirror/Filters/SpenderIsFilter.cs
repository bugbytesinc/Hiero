namespace Hiero.Mirror.Filters;
/// <summary>
/// Filter results by spender account ID.
/// </summary>
public class SpenderIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The account id to filter the request by.
    /// </summary>
    private readonly EntityId _token;
    /// <summary>
    /// Constructor requires the spender account to filter the request by.
    /// </summary>
    /// <param name="token">
    /// The spender account to filter the response by.
    /// </param>
    public SpenderIsFilter(EntityId token)
    {
        _token = token;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "spender.id";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => _token.ToString();
}
