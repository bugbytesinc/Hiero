using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Represents the parameters required to call a smart contract on the Hedera network.
/// </summary>
public class CallContractParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the contract to call.
    /// </summary>
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// The amount of gas that is allowed for the call.
    /// </summary>
    public long Gas { get; set; }
    /// <summary>
    /// For payable function calls, the amount of tinybars to send to the contract.
    /// </summary>
    public long PayableAmount { get; set; } = 0;
    /// <summary>
    /// Name of the contract function to call.
    /// </summary>
    public string MethodName { get; set; } = default!;
    /// <summary>
    /// The arguments to send with the method call.
    /// </summary>
    public object[] MethodArgs { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to invoke this contract.  Typically not used
    /// however there are some edge cases where it may send
    /// crypto to accounts that require a signature to receive
    /// funds.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the contract
    /// call.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        return new ContractCallTransactionBody()
        {
            ContractID = new ContractID(Contract),
            Gas = Gas,
            Amount = PayableAmount,
            FunctionParameters = ByteString.CopyFrom(Abi.EncodeFunctionWithArguments(MethodName, MethodArgs).Span)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Contract Call";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CallContractExtensions
{
    /// <summary>
    /// Calls a smart contract returning a receipt indicating success.  
    /// This call does not return the data emitted from the contract, to
    /// obtain that data, use a mirror node to fetch the results by transaction
    /// id or retrieve the transaction record from the mirror node
    /// via and cast the result ot a <see cref="CallContractRecord"/>.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the contract call.
    /// </param>
    /// <param name="callParameters">
    /// An object identifying the function to call, any input parameters and the 
    /// amount of gas that may be used to execute the request.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A contract transaction receipt indicating success, it does not
    /// include any output parameters returned from the contract.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> CallContractAsync(this ConsensusClient client, CallContractParams callParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(callParameters, configure);
    }
}