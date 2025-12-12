using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for updating the royalties (custom fees) associated with a token.
/// </summary>
public sealed class UpdateRoyaltiesParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The ID of the token definition to update.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The list of royalties to apply to token transactions, may
    /// be a blank list or null, this list replaces the previous 
    /// list of royalties in full.
    /// </summary>
    public IReadOnlyList<IRoyalty>? Royalties { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to the royalty key for the token if it is not already
    /// set as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the token
    /// royalties update submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        var result = new TokenFeeScheduleUpdateTransactionBody
        {
            TokenId = new TokenID(Token)
        };
        // Note: Null & Empty are Valid, they will clear the list of fees.
        if (Royalties is { Count: > 0 })
        {
            foreach (var royalty in Royalties)
            {
                result.CustomFees.Add(royalty.ToCustomFee());
            }
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Royalties Update";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateRoyaltiesExtensions
{
    /// <summary>
    /// Updates (replaces) the royalties (custom fees) associated with 
    /// a token, must be signed by the RoyaltiesEndorsment private key(s).
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="token">
    /// The address of the token definition to update.
    /// </param>
    /// <param name="royalties">
    /// The list of royalties to apply to token transactions, may
    /// be a blank list or null, this list replaces the previous 
    /// list of royalties in full.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateRoyaltiesAsync(this ConsensusClient client, EntityId token, IReadOnlyList<IRoyalty>? royalties, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new UpdateRoyaltiesParams { Token = token, Royalties = royalties }, configure);
    }
    /// <summary>
    /// Updates (replaces) the royalties (custom fees) associated with 
    /// a token, must be signed by the RoyaltiesEndorsment private key(s).
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="updateRoyaltiesParams">
    /// The parameters containing the token and royalties to update.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateRoyaltiesAsync(this ConsensusClient client, UpdateRoyaltiesParams updateRoyaltiesParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateRoyaltiesParams, configure);
    }
}