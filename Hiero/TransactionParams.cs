using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// A General Marker abstract class for transactions parameters.
/// </summary>
public abstract class TransactionParams
{
    /// <remarks>
    /// Only classes from the Hashgraph assembly 
    /// may derive from this class.
    /// </remarks>
    internal TransactionParams() { }
}
/// <summary>
/// A Transaction Receipt Specific Marker abstract class for transactions parameters.
/// </summary>
public abstract class TransactionParams<TReceipt> : TransactionParams where TReceipt : TransactionReceipt
{
    /// <remarks>
    /// Only classes from the Hashgraph assembly 
    /// may derive from this class.
    /// </remarks>
    internal TransactionParams() { }
    /// <summary>
    /// Returns the corresponding NetworkParams for 
    /// these transaction parameters.
    /// </summary>
    virtual internal INetworkParams<TReceipt> GetNetworkParams()
    {
        return (INetworkParams<TReceipt>)this;
    }
}
