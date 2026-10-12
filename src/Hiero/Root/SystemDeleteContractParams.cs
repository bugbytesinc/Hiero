// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Transaction parameters for administratively deleting a contract from the network.
/// </summary>
/// <example>
/// Superuser-only: remove a contract even if it's immutable. Restorable via
/// <see cref="SystemRestoreContractParams"/> within a grace window:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SystemDeleteContract" language="csharp"/>
/// </example>
public sealed class SystemDeleteContractParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The address of the contract to delete.
    /// </summary>
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// Optional additional signatories.
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
        return new SystemDeleteTransactionBody
        {
            ContractID = new ContractID(Contract)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "System Delete Contract";
}
/// <summary>
/// Extension methods for administratively deleting a contract from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SystemDeleteContractExtensions
{
    /// <summary>
    /// Removes a contract from the network via Administrative Delete
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="deleteParams">
    /// The parameters for the system delete operation, including the contract address to delete.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the contract deletion.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the delete request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SystemDeleteContract" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SystemDeleteContractAsync(this ConsensusClient client, SystemDeleteContractParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}