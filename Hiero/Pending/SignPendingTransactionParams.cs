using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Signing a Pending/Scheduled Transaction.
/// </summary>
public sealed class SignPendingTransactionParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The Entity Id of the pending transaction that is to be signed.
    /// </summary>
    public EntityId Pending { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the deletion.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the pending transaction
    /// signing submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new ScheduleSignTransactionBody
        {
            ScheduleID = new ScheduleID(Pending)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return TransactionReceiptExtensions.FromProtobuf(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Sign Pending Transaction";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignPendingTransactionExtensions
{
    /// <summary>
    /// Adds a signature to a pending transaction record. The Scheduled TransactionId executes 
    /// this signature completes the list of required signatures for execution.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client submitting the request to sign the pending transaction.
    /// </param>
    /// <param name="signParams">
    /// The parameters for signing the pending transaction.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A Receipt indicating success.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SignPendingTransactionAsync(this ConsensusClient client, SignPendingTransactionParams signParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(signParams, configure);
    }
}