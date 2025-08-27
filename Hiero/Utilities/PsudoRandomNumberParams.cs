using Hiero.Implementation;
using Proto;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;
/// <summary>
/// Creates a Psudo Random Number request from the Hedera Network.
/// </summary>
public sealed class PsudoRandomNumberParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// If specified, the maximum value for the psudo random number generated.
    /// </summary>
    public int? MaxValue = null;
    /// <summary>
    /// Additional private key, keys or signing callback method.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        var result = new UtilPrngTransactionBody();
        if (MaxValue.HasValue)
        {
            if (MaxValue.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MaxValue), "If specified, the maximum random value must be greater than zero.");
            }
            result.Range = MaxValue.Value;
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Generate Random Number";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PsudoRandomNumberExtensions
{
    /// <summary>
    /// Generates a psudo random number, which can be retrieved via the
    /// transaction's record.
    /// </summary>
    /// <param name="maxValue">The maximum allowed value for
    /// the generated number.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating the success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> GeneratePsudoRandomNumberAsync(this ConsensusClient client, PsudoRandomNumberParams randomParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(randomParams, configure);
    }
}