namespace Hiero.Mirror.Filters;
/// <summary>
/// Retrieve consensus messages filtered by acocunt id.
/// </summary>
public class TokenIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The account id to filter the request by.
    /// </summary>
    private readonly EntityId _token;
    /// <summary>
    /// Constructor requres the token to filter the request by.
    /// </summary>
    /// <param name="token">
    /// Payer of the token to filter the response by.
    /// </param>
    public TokenIsFilter(EntityId token)
    {
        _token = token;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "token.id";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => _token.ToString();
}
