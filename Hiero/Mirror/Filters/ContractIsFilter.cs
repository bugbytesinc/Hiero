namespace Hiero.Mirror.Filters;
/// <summary>
/// Retrieve consensus messages filtered by contract address.
/// </summary>
public class ContractIsFilter : IMirrorQueryFilter
{
    /// <summary>
    /// The contract address to filter the request by.
    /// </summary>
    private readonly EvmAddress _moniker;
    /// <summary>
    /// Constructor requires the address to filter the request by.
    /// </summary>
    /// <param name="moniker">
    /// Payer of the account to filter the response by.
    /// </param>
    public ContractIsFilter(EvmAddress moniker)
    {
        _moniker = moniker;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "from";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => $"0x{Hex.FromBytes(_moniker.Bytes)}";
}
