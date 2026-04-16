// SPDX-License-Identifier: Apache-2.0
using Com.Hedera.Hapi.Node.Addressbook;
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for adding a new consensus node to the network address book.
/// </summary>
/// <example>
/// Register a new node. The receipt carries the network-assigned NodeId
/// which every later UpdateConsensusNode/RemoveConsensusNode call needs:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="AddConsensusNode" language="csharp"/>
/// </example>
/// <remarks>
/// This is a privileged transaction requiring Hedera governing council authorization.
/// The node will be added to network state but will not participate in consensus
/// until the next network upgrade (freeze with PREPARE_UPGRADE).
/// </remarks>
public sealed class AddConsensusNodeParams : TransactionParams<ConsensusNodeReceipt>, INetworkParams<ConsensusNodeReceipt>
{
    /// <summary>
    /// The account identifier to associate with this node.
    /// This field is REQUIRED.
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// A short description of the node. Must not exceed 100 bytes when encoded as UTF-8.
    /// This field is OPTIONAL.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Service endpoints for gossip communication with other consensus nodes.
    /// Must contain between 1 and 10 entries. This field is REQUIRED.
    /// </summary>
    public IEnumerable<Uri> GossipEndpoints { get; set; } = default!;
    /// <summary>
    /// Service endpoints for gRPC client connections.
    /// Must contain between 1 and 8 entries. This field is REQUIRED.
    /// </summary>
    public IEnumerable<Uri> ServiceEndpoints { get; set; } = default!;
    /// <summary>
    /// The DER-encoded gossip CA certificate used to sign gossip events.
    /// This field is REQUIRED and must not be empty.
    /// </summary>
    public ReadOnlyMemory<byte> GossipCaCertificate { get; set; }
    /// <summary>
    /// A SHA-384 hash of the node's gRPC TLS certificate in PEM format.
    /// This field is OPTIONAL.
    /// </summary>
    public ReadOnlyMemory<byte>? GrpcCertificateHash { get; set; }
    /// <summary>
    /// An administrative key controlled by the node operator.
    /// This key must sign this transaction and all future node update transactions.
    /// This field is REQUIRED.
    /// </summary>
    public Endorsement AdminKey { get; set; } = default!;
    /// <summary>
    /// When true, the node operator declines to receive node rewards.
    /// Defaults to false.
    /// </summary>
    public bool DeclineReward { get; set; }
    /// <summary>
    /// An optional gRPC-Web proxy endpoint for non-gRPC (e.g. browser) clients.
    /// Must be an FQDN using HTTPS. This field is OPTIONAL.
    /// </summary>
    public Uri? GrpcProxyEndpoint { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required to
    /// authorize this transaction. Must include the key matching <see cref="AdminKey"/>
    /// and governing council keys.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<ConsensusNodeReceipt>.CreateNetworkTransaction()
    {
        if (Account.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(Account), "Node account is required.");
        }
        if (AdminKey is null)
        {
            throw new ArgumentNullException(nameof(AdminKey), "Node admin key is required.");
        }
        if (GossipEndpoints is null)
        {
            throw new ArgumentNullException(nameof(GossipEndpoints), "Gossip endpoints are required.");
        }
        if (ServiceEndpoints is null)
        {
            throw new ArgumentNullException(nameof(ServiceEndpoints), "Service endpoints are required.");
        }
        var result = new NodeCreateTransactionBody
        {
            AccountId = new AccountID(Account),
            AdminKey = new Key(AdminKey),
            GossipCaCertificate = ByteString.CopyFrom(GossipCaCertificate.Span),
            DeclineReward = DeclineReward
        };
        if (!string.IsNullOrEmpty(Description))
        {
            result.Description = Description;
        }
        result.GossipEndpoint.AddRange(GossipEndpoints.Select(e => new ServiceEndpoint(e)));
        if (result.GossipEndpoint.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GossipEndpoints), "At least one gossip endpoint is required.");
        }
        result.ServiceEndpoint.AddRange(ServiceEndpoints.Select(e => new ServiceEndpoint(e)));
        if (result.ServiceEndpoint.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ServiceEndpoints), "At least one service endpoint is required.");
        }
        if (GrpcCertificateHash.HasValue)
        {
            result.GrpcCertificateHash = ByteString.CopyFrom(GrpcCertificateHash.Value.Span);
        }
        if (GrpcProxyEndpoint is not null)
        {
            result.GrpcProxyEndpoint = new ServiceEndpoint(GrpcProxyEndpoint);
        }
        return result;
    }
    ConsensusNodeReceipt INetworkParams<ConsensusNodeReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new ConsensusNodeReceipt(transactionId, receipt);
    }
    string INetworkParams<ConsensusNodeReceipt>.OperationDescription => "Create Consensus Node";
}
/// <summary>
/// Extension methods for adding consensus nodes to the network address book.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AddConsensusNodeExtensions
{
    /// <summary>
    /// Creates a new consensus node in the network address book.
    /// </summary>
    /// <remarks>
    /// This is a privileged transaction requiring Hedera governing council authorization.
    /// The node becomes active after the next network upgrade.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="createParams">
    /// The parameters describing the new node to create.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A receipt containing the newly assigned node identifier.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="AddConsensusNode" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<ConsensusNodeReceipt> AddConsensusNodeAsync(this ConsensusClient client, AddConsensusNodeParams createParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(createParams, configure);
    }
}
