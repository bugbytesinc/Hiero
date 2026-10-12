// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Creates a Pseudo Random Number request from the Hedera Network.
/// </summary>
/// <example>
/// Ask the network for a bounded integer (returned as
/// <see cref="RangedPseudoRandomNumberRecord"/> in the mirror record);
/// omit <c>MaxValue</c> to get 48 raw bytes
/// (<see cref="BytesPseudoRandomNumberRecord"/>) instead:
/// <code source="../../../samples/DocSnippets/UtilitiesSnippets.cs" region="GenerateBoundedPrng" language="csharp"/>
/// </example>
public sealed class PseudoRandomNumberParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// If specified, the exclusive upper bound of the pseudo random number
    /// generated: the result lies in the range [0, <c>MaxValue</c>).
    /// </summary>
    public int? MaxValue { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
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
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Generate Random Number";
}
/// <summary>
/// Extension methods for generating pseudo-random numbers on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PseudoRandomNumberExtensions
{
    /// <summary>
    /// Generates a pseudo random number, which can be retrieved via the
    /// transaction's record.
    /// </summary>
    /// <param name="client">The Consensus Node Client.</param>
    /// <param name="randomParams">The parameters for generating the pseudo random number.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating the success of the operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/UtilitiesSnippets.cs" region="GeneratePrng" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> GeneratePseudoRandomNumberAsync(this ConsensusClient client, PseudoRandomNumberParams randomParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(randomParams, configure);
    }
}