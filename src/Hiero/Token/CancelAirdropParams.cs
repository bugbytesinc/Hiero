// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for cancelling one or more pending airdrops.
/// </summary>
/// <remarks>
/// This transaction must be signed by the sender account for each pending airdrop
/// to be cancelled. Cancelled pending airdrops are permanently removed from state.
/// </remarks>
/// <example>
/// Cancel multiple pending airdrops in one transaction. The sender signs.
/// Cancelled airdrops are removed and can no longer be claimed:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="CancelAirdropsBatch" language="csharp"/>
/// </example>
public sealed class CancelAirdropParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The pending airdrops to cancel.
    /// Must contain between 1 and 10 entries with no duplicates.
    /// </summary>
    public IReadOnlyList<Airdrop> Airdrops { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the cancellation. Must match the endorsement
    /// assigned to the sender account for each pending airdrop.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.
    /// </remarks>
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
        if (Airdrops is null)
        {
            throw new ArgumentNullException(nameof(Airdrops), "The list of airdrops must not be null.");
        }
        var result = new TokenCancelAirdropTransactionBody();
        var pendingAirdrops = result.PendingAirdrops;
        var count = Airdrops.Count;
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Airdrops), "The list of pending airdrops must not be empty.");
        }
        if (pendingAirdrops.Capacity < count)
        {
            pendingAirdrops.Capacity = count;
        }
        for (var i = 0; i < count; i++)
        {
            pendingAirdrops.Add(new PendingAirdropId(Airdrops[i]));
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Cancel Airdrop";
}
/// <summary>
/// Extension methods for cancelling pending airdrops on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CancelAirdropExtensions
{
    /// <summary>
    /// Cancels a single pending airdrop.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the cancellation.
    /// </param>
    /// <param name="airdrop">
    /// The pending airdrop to cancel.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="CancelAirdrop" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> CancelAirdropAsync(this ConsensusClient client, Airdrop airdrop, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new CancelAirdropParams { Airdrops = [airdrop] }, configure);
    }
    /// <summary>
    /// Cancels one or more pending airdrops.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the cancellation.
    /// </param>
    /// <param name="cancelParams">
    /// The cancellation parameters containing the list of pending airdrops to cancel.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="CancelAirdropsBatch" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> CancelAirdropsAsync(this ConsensusClient client, CancelAirdropParams cancelParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(cancelParams, configure);
    }
}
