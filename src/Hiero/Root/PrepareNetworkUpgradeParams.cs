// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Transaction parameters for preparing the network for a software upgrade.
/// </summary>
/// <example>
/// Step 1 of the upgrade flow: stage an upgrade image and its hash so every
/// node can validate the file before the scheduled switchover:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="PrepareNetworkUpgrade" language="csharp"/>
/// </example>
public sealed class PrepareNetworkUpgradeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// Address of the upgrade file (previously uploaded).
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// Hash of the upgrade file's contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileHash { get; set; }
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
        if (File.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(File), "The upgrade file's File Address ID is missing.");
        }
        if (FileHash.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(FileHash), "The hash of the file contents must be included.");
        }
        return new FreezeTransactionBody
        {
            UpdateFile = new FileID(File),
            FileHash = ByteString.CopyFrom(FileHash.Span),
            FreezeType = FreezeType.PrepareUpgrade
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Prepare Upgrade Command";
}
/// <summary>
/// Extension methods for preparing a network upgrade.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PrepareNetworkUpgradeExtensions
{
    /// <summary>
    /// Prepares the network for an upgrade as configured by 
    /// the specified upgrade file.  The file hash must match 
    /// the hash of the identified upgrade file stored at the 
    /// specified location.  This operation does not 
    /// immediately affect network operations.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="prepareParams">
    /// The parameters for preparing the network upgrade.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success.
    /// </returns>
    /// <remarks>
    /// This operation must be submitted by a privileged account
    /// having access rights to perform this operation.
    /// </remarks>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="PrepareNetworkUpgrade" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> PrepareNetworkUpgradeAsync(this ConsensusClient client, PrepareNetworkUpgradeParams prepareParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(prepareParams, configure);
    }
}