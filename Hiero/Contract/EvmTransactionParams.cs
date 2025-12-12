using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Represents a transaction submitted to the hedera network through the
/// native HAPI Ethereum gateway feature.
/// </summary>
public class EvmTransactionParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The complete raw Ethereum transaction (RLP encoded type 0, 1, and 2),
    /// with the exception of the call data if <code>ExtraCallData</code>
    /// has been populated.
    /// </summary>
    /// <remarks>
    /// If it necessary to invoke the <code>ExtraCallData</code> feature,
    /// the callData for the ethereum transaction should be set to an empty
    /// string in this property. However Note: for validation of signatures,
    /// a node will reconstruct the proper ethereumData payload with the
    /// call data before attempting to validate signatures, so there may
    /// be extra work in generating the complete ethereum transaction to
    /// sign with private keys before breaking apart into components small
    /// enough to load onto a Hedera Gossip Node thru the HAPI.
    /// </remarks>
    public ReadOnlyMemory<byte> Transaction { get; set; }
    /// <summary>
    /// For large transactions where the call data cannot fit within the size
    /// of an hedera transaction, this address points to a file containing the 
    /// callData of the ethereumData. The hedera node will re-write the 
    /// ethereumData inserting the contents into the existing empty callData 
    /// element with the contents in the referenced file at time of execution. 
    /// The reconstructed ethereumData will then be checked against signatures
    /// for validation.
    /// </summary>
    public EntityId ExtraCallData { get; set; } = default!;
    /// <summary>
    /// The maximum amount of gas, in tinybars, that the payer of the hedera 
    /// ethereum transaction is willing to pay to execute the transaction.
    /// </summary>
    /// <remarks>
    /// Ordinarily the account with the ECDSA alias corresponding to the public 
    /// key that is extracted from the ethereum_data signature is responsible for 
    /// fees that result from the execution of the transaction.  If that amount of 
    /// authorized fees is not sufficient then the (hapi) payer of the transaction 
    /// can be charged, up to but not exceeding this amount.  If the ethereum_data 
    /// transaction authorized an amount that was insufficient then the (hapi) payer 
    /// will only be charged the amount needed to make up the difference. If the gas 
    /// price in the ethereum transaction was set to zero then the (hapi) payer will 
    /// be assessed the entire gas & hedera fees.
    /// </remarks>
    public long AdditionalGasAllowance { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to invoke this transaction.  Typically not used,
    /// however there are some edge cases where it may send
    /// crypto to accounts that require a signature to receive
    /// funds.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the contract call.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Transaction.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(Transaction), "The ethereum transaction must be specified.");
        }
        if (AdditionalGasAllowance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(AdditionalGasAllowance), "The Additional Gas Allowance can not be negative.");
        }
        return new EthereumTransactionBody()
        {
            EthereumData = ByteString.CopyFrom(Transaction.Span),
            CallData = ExtraCallData.IsNullOrNone() ? null : new FileID(ExtraCallData),
            MaxGasAllowance = AdditionalGasAllowance
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Contract Call";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EvmTransactionExtensions
{
    /// <summary>
    /// Submits an equivalent Ethereum transaction (native RLP encoded type 0, 1, and 2)
    /// transaction to the hedera network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the contract create.
    /// </param>
    /// <param name="transactionParams">
    /// The ethereum formatted transaction details.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success, it does not
    /// include any output parameters sent from the contract.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ExecuteEvmTransactionAsync(this ConsensusClient client, EvmTransactionParams transactionParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(transactionParams, configure);
    }
}