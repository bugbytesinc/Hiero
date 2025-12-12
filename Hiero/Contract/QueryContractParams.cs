using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Provides the details of the request to the client when invoking a contract local query function.
/// </summary>
public class QueryContractParams
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
    /// The amount of GAS required to pay for returning the output 
    /// data from the contract.  This is an additional fee that is 
    /// incorporated into the max gas fee under the hood.  Gas can
    /// be set at what a mirror node would estimate as the necessary
    /// charge, and this accounts for the bytes returned.  Honestly,
    /// you should be using mirror nodes for read-only queries of
    /// the EVM anyway, it will be cheaper for your wallet.
    /// </summary>
    public long ReturnedDataGasAllowance { get; set; }
    /// <summary>
    /// Name of the contract function to call.
    /// </summary>
    public string MethodName { get; set; } = default!;
    /// <summary>
    /// The function arguments to send with the method call.
    /// </summary>
    public object[] MethodArgs { get; set; } = default!;
    /// <summary>
    /// The account that is the "message.sender" of the contract
    /// call, if not specified it is the transaction Payer.
    /// </summary>
    public EntityId MessageSender { get; set; } = default!;
    /// <summary>
    /// Throw a <see cref="ContractException"/> exception if the query
    /// call returns a code other than success.  Default is true to maintain
    /// backwards compatibility.  If set to false, the 
    /// <see cref="ContractCallResult"/> will be returned without an exception.
    /// The exception returned also includes the contract call result.
    /// Default is <code>true</code>.
    /// </summary>
    public bool ThrowOnFail { get; set; } = true;
    /// <summary>
    /// Optional Cancellation token that interrupt the query.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class QueryContractExtensions
{
    /// <summary>
    /// Calls a smart contract function locally on the gateway node.
    /// </summary>
    /// <remarks>
    /// This is performed locally on the gateway node. It cannot change the state of the contract instance 
    /// (and so, cannot spend anything from the instance's cryptocurrency account). It will not have a 
    /// consensus timestamp nor a record or a receipt. The response will contain the output returned 
    /// by the function call.  
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="queryContractParams">
    /// The parameters identifying the contract and function method to call.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The results from the local contract query call.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ContractException">If the request was accepted by the network but the contract failed for
    /// some reason.  Contains additional information returned from the contract virtual machine.  Only thrown if
    /// the <see cref="QueryContractParams.ThrowOnFail"/> is set to <code>true</code>, the default, otherwise
    /// the method returns a <see cref="ContractCallResult"/> with the same information.</exception>
    public static async Task<ContractCallResult> QueryContractAsync(this ConsensusClient client, QueryContractParams queryContractParams, Action<IConsensusContext>? configure = null)
    {
        var query = new ContractCallLocalQuery
        {
            ContractID = new ContractID(queryContractParams.Contract),
            Gas = queryContractParams.Gas + queryContractParams.ReturnedDataGasAllowance,
            FunctionParameters = ByteString.CopyFrom(Abi.EncodeFunctionWithArguments(queryContractParams.MethodName, queryContractParams.MethodArgs).Span),
            SenderId = queryContractParams.MessageSender.IsNullOrNone() ? null : new AccountID(queryContractParams.MessageSender),
            ThrowOnFail = queryContractParams.ThrowOnFail
        };
        return new ContractCallResult(await Engine.QueryAsync(client, query, queryContractParams.CancellationToken ?? default, configure));
    }
}