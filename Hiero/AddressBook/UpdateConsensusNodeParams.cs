using Com.Hedera.Hapi.Node.Addressbook;
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for updating an existing consensus node in the network address book.
/// </summary>
/// <remarks>
/// Any property left as <c>null</c> will remain unchanged on the node.
/// This transaction must be signed by the node's current <c>admin_key</c>.
/// Changes take effect at the next network upgrade (freeze with PREPARE_UPGRADE).
/// </remarks>
public sealed class UpdateConsensusNodeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the node to update. This field is REQUIRED.
    /// </summary>
    public ulong NodeId { get; set; }
    /// <summary>
    /// If set, replaces the node's associated account identifier.
    /// Both the current and new account keys must sign this transaction.
    /// </summary>
    public EntityId? Account { get; set; }
    /// <summary>
    /// If set, replaces the node's description. Must not exceed 100 bytes when encoded as UTF-8.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// If set, replaces the entire list of gossip endpoints.
    /// Must contain between 1 and 10 entries.
    /// </summary>
    public IEnumerable<Uri>? GossipEndpoints { get; set; }
    /// <summary>
    /// If set, replaces the entire list of gRPC service endpoints.
    /// Must contain between 1 and 8 entries.
    /// </summary>
    public IEnumerable<Uri>? ServiceEndpoints { get; set; }
    /// <summary>
    /// If set, replaces the DER-encoded gossip CA certificate.
    /// </summary>
    public ReadOnlyMemory<byte>? GossipCaCertificate { get; set; }
    /// <summary>
    /// If set, replaces the SHA-384 hash of the node's gRPC TLS certificate.
    /// </summary>
    public ReadOnlyMemory<byte>? GrpcCertificateHash { get; set; }
    /// <summary>
    /// If set, replaces the node's administrative key.
    /// Both the current and new admin keys must sign this transaction.
    /// </summary>
    public Endorsement? AdminKey { get; set; }
    /// <summary>
    /// If set, updates whether the node operator declines to receive node rewards.
    /// </summary>
    public bool? DeclineReward { get; set; }
    /// <summary>
    /// If set, replaces the gRPC-Web proxy endpoint for non-gRPC clients.
    /// </summary>
    public Uri? GrpcProxyEndpoint { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required to
    /// authorize this transaction. Must include the current node admin key.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        var result = new NodeUpdateTransactionBody
        {
            NodeId = NodeId
        };
        if (Account is not null && !Account.IsNullOrNone())
        {
            result.AccountId = new AccountID(Account);
        }
        if (Description is not null)
        {
            result.Description = Description;
        }
        if (GossipEndpoints is not null)
        {
            result.GossipEndpoint.AddRange(GossipEndpoints.Select(e => new ServiceEndpoint(e)));
            if (result.GossipEndpoint.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(GossipEndpoints), "Gossip endpoints list must not be empty when provided.");
            }
        }
        if (ServiceEndpoints is not null)
        {
            result.ServiceEndpoint.AddRange(ServiceEndpoints.Select(e => new ServiceEndpoint(e)));
            if (result.ServiceEndpoint.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ServiceEndpoints), "Service endpoints list must not be empty when provided.");
            }
        }
        if (GossipCaCertificate.HasValue)
        {
            result.GossipCaCertificate = ByteString.CopyFrom(GossipCaCertificate.Value.Span);
        }
        if (GrpcCertificateHash.HasValue)
        {
            result.GrpcCertificateHash = ByteString.CopyFrom(GrpcCertificateHash.Value.Span);
        }
        if (AdminKey is not null)
        {
            result.AdminKey = new Key(AdminKey);
        }
        if (DeclineReward.HasValue)
        {
            result.DeclineReward = DeclineReward.Value;
        }
        if (GrpcProxyEndpoint is not null)
        {
            result.GrpcProxyEndpoint = new ServiceEndpoint(GrpcProxyEndpoint);
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Update Node";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateConsensusNodeExtensions
{
    /// <summary>
    /// Updates an existing consensus node in the network address book.
    /// </summary>
    /// <remarks>
    /// Only the properties set on <paramref name="updateParams"/> will be changed.
    /// This transaction must be signed by the node's current admin key.
    /// Changes take effect at the next network upgrade.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="updateParams">
    /// The parameters specifying the node to update and the properties to change.
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
    public static Task<TransactionReceipt> UpdateConsensusNodeAsync(this ConsensusClient client, UpdateConsensusNodeParams updateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParams, configure);
    }
}
