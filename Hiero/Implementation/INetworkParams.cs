using System.Threading;

namespace Hiero.Implementation;

/// <summary>
/// Defines the common properties required by all transaction parameter objects.
/// </summary>
internal interface INetworkParams
{
    /// <summary>
    /// Optional additional signatory (or signatories) required to authorize the transaction.
    /// This is typically not the payer, but may include the payer if necessary.
    /// </summary>
    Signatory? Signatory { get; }
    /// <summary>
    /// An optional cancellation token that can be used to interrupt the transaction
    /// submission process.
    /// </summary>
    CancellationToken? CancellationToken { get; }
    /// <summary>
    /// Constructs the corresponding network transaction object that encapsulates
    /// the intent represented by this parameter instance.
    /// </summary>
    /// <returns>
    /// A network transaction containing instructions to be submitted to the network.
    /// </returns>
    INetworkTransaction CreateNetworkTransaction();
    /// <summary>
    /// Checks the raw transaction receipt for correctness, raising an error
    /// if the transaction was not successful, otherwise returning a receipt 
    /// class if success.
    /// </summary>
    /// <param name="transactionId">
    /// The protobuf transaction id.
    /// </param>
    /// <param name="receipt">
    /// the protobuf receipt, which can indicate success or failure.
    /// </param>
    /// <returns>
    /// The specified type of receipt, if successful.
    /// </returns>
    TransactionReceipt CreateReceipt(Proto.TransactionID transactionId, Proto.TransactionReceipt receipt);
    /// <summary>
    /// Description of the operation
    /// </summary>
    public string OperationDescription { get; }
}
