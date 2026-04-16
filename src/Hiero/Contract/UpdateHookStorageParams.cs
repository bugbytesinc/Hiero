// SPDX-License-Identifier: Apache-2.0
using Com.Hedera.Hapi.Node.Hooks;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Represents the parameters required to update the storage
/// of an EVM hook on the Hedera network.
/// </summary>
/// <example>
/// Rewrite one or more storage slots on an EVM hook. Useful for maintaining
/// allow/deny lists or other state consumed by hook logic:
/// <code source="../../../samples/DocSnippets/ContractSnippets.cs" region="UpdateHookStorage" language="csharp"/>
/// </example>
public sealed class UpdateHookStorageParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identity of the hook whose storage is being updated.
    /// </summary>
    public Hook Hook { get; set; } = default!;
    /// <summary>
    /// The storage updates to apply to the hook.
    /// </summary>
    public IEnumerable<HookStorageEntry>? StorageUpdates { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the storage update. Typically the
    /// hook owner's key or its admin key.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that can interrupt the
    /// hook storage update.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Hook is null)
        {
            throw new ArgumentNullException(nameof(Hook), "Hook ID is missing. Please check that it is not null.");
        }
        var body = new HookStoreTransactionBody
        {
            HookId = new Proto.HookId(Hook)
        };
        if (StorageUpdates is not null)
        {
            foreach (var update in StorageUpdates.ToProto())
            {
                body.StorageUpdates.Add(update);
            }
        }
        return body;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Update Hook Storage";
}
/// <summary>
/// Extension methods for updating EVM hook storage.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateHookStorageExtensions
{
    /// <summary>
    /// Update the storage of an EVM hook on the Hedera network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="updateParameters">
    /// The parameters identifying the hook and storage updates to apply.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the update request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/ContractSnippets.cs" region="UpdateHookStorage" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateHookStorageAsync(this ConsensusClient client, UpdateHookStorageParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParameters, configure);
    }
}
