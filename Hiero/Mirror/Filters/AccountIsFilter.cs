namespace Hiero.Mirror.Filters;
/// <summary>
/// Retrieve consensus messages filtered by account id.
/// </summary>
public class AccountIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The account id to filter the request by.
    /// </summary>
    private readonly EntityId _account;
    /// <summary>
    /// Constructor requires the account to filter the request by.
    /// </summary>
    /// <param name="account">
    /// Payer of the account to filter the response by.
    /// </param>
    public AccountIsFilter(EntityId account)
    {
        _account = account;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "account.id";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => _account.ToString();
}
