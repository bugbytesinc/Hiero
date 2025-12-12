namespace Hiero.Mirror.Filters;
/// <summary>
/// Retrieve consensus messages filtered by account id.
/// </summary>
public class SpenderIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The account id to filter the request by.
    /// </summary>
    private readonly EntityId _token;
    /// <summary>
    /// Constructor requires the token to filter the request by.
    /// </summary>
    /// <param name="token">
    /// Payer of the token to filter the response by.
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
