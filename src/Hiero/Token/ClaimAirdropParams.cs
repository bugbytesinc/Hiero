// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for claiming one or more pending airdrops.
/// </summary>
/// <remarks>
/// This transaction must be signed by the receiver account for each pending airdrop
/// to be claimed. If the sender has insufficient balance at claim time, the claim fails.
/// Successfully claimed airdrops are removed from state and tokens are transferred
/// to the receiver's balance.
/// </remarks>
public sealed class ClaimAirdropParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The pending airdrops to claim.
    /// Must contain between 1 and 10 entries with no duplicates.
    /// </summary>
    public IReadOnlyList<Airdrop> Airdrops { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the claim. Must match the endorsement
    /// assigned to the receiver account for each pending airdrop.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Airdrops is null)
        {
            throw new ArgumentNullException(nameof(Airdrops), "The list of airdrops must not be null.");
        }
        var result = new TokenClaimAirdropTransactionBody();
        result.PendingAirdrops.AddRange(Airdrops.Select(id => new PendingAirdropId(id)));
        if (result.PendingAirdrops.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Airdrops), "The list of airdrops must not be empty.");
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Claim Airdrop";
}
/// <summary>
/// Extension methods for claiming pending airdrops on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ClaimAirdropExtensions
{
    /// <summary>
    /// Claims a single pending airdrop.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the claim.
    /// </param>
    /// <param name="pendingAirdrop">
    /// The pending airdrop to claim.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ClaimAirdropAsync(this ConsensusClient client, Airdrop pendingAirdrop, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new ClaimAirdropParams { Airdrops = [pendingAirdrop] }, configure);
    }
    /// <summary>
    /// Claims one or more pending airdrops.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the claim.
    /// </param>
    /// <param name="claimParams">
    /// The claim parameters containing the list of pending airdrops to claim.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ClaimAirdropsAsync(this ConsensusClient client, ClaimAirdropParams claimParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(claimParams, configure);
    }
}
