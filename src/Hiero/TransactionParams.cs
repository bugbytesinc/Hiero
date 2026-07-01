// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// Non-generic base marker for all transaction parameter classes,
/// allowing them to be referenced uniformly regardless of the receipt
/// type they produce.
/// </summary>
public abstract class TransactionParams
{
    /// <remarks>
    /// Only classes from the Hiero assembly 
    /// may derive from this class.
    /// </remarks>
    internal TransactionParams() { }
}
/// <summary>
/// Base class for transaction parameters, parameterized by the
/// <typeparamref name="TReceipt"/> type returned when the transaction is
/// executed.  Derived parameter classes are passed to the client's
/// <c>ExecuteAsync</c> and <c>SubmitAsync</c> methods.
/// </summary>
/// <typeparam name="TReceipt">
/// The <see cref="TransactionReceipt"/> subtype produced when a transaction
/// built from these parameters reaches consensus.
/// </typeparam>
public abstract class TransactionParams<TReceipt> : TransactionParams where TReceipt : TransactionReceipt
{
    /// <remarks>
    /// Only classes from the Hiero assembly 
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
