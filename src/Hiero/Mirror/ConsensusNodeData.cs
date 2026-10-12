// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents consensus node information returned from the mirror node.
/// </summary>
public class ConsensusNodeData
{
    /// <summary>
    /// The consensus node's account ID (for payment purposes).
    /// </summary>
    [JsonPropertyName("node_account_id")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The administrative key authorized to update or delete the
    /// node's address-book entry.
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// The ids of the HIP-1137 registered nodes (block nodes, mirror
    /// nodes and the like) this consensus node is associated with.
    /// </summary>
    [JsonPropertyName("associated_registered_nodes")]
    public long[] AssociatedRegisteredNodes { get; set; } = [];
    /// <summary>
    /// Whether the node declines staking rewards.
    /// </summary>
    [JsonPropertyName("decline_reward")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool DeclineReward { get; set; }
    /// <summary>
    /// A list of gRPC endpoints this gossip node can be reached through.
    /// </summary>
    [JsonPropertyName("service_endpoints")]
    public GrpcEndpointData[] Endpoints { get; set; } = default!;
    /// <summary>
    /// The node's gRPC-Web proxy endpoint (HIP-1046), for clients such
    /// as browsers that cannot speak native gRPC; null when the node
    /// publishes none.
    /// </summary>
    [JsonPropertyName("grpc_proxy_endpoint")]
    public GrpcEndpointData? GrpcProxyEndpoint { get; set; }
    /// <summary>
    /// Memo associated with the address book
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;
    /// <summary>
    /// File ID associated with this node.
    /// </summary>
    [JsonPropertyName("file_id")]
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The minimum stake (rewarded or not rewarded) this 
    /// node must reach before having non-zero consensus weight.
    /// </summary>
    [JsonPropertyName("min_stake")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long MinimumStake { get; set; }
    /// <summary>
    /// The maximum stake (rewarded or not rewarded) this node 
    /// can have as consensus weight
    /// </summary>
    [JsonPropertyName("max_stake")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long MaximumStake { get; set; }
    /// <summary>
    /// Memo associated with this node.
    /// </summary>
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = default!;
    /// <summary>
    /// The Node's ID Number
    /// </summary>
    [JsonPropertyName("node_id")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long NodeId { get; set; }
    /// <summary>
    /// hex encoded hash of the node's TLS certificate
    /// </summary>
    [JsonPropertyName("node_cert_hash")]
    public string CertificateHash { get; set; } = default!;
    /// <summary>
    /// hex encoded X509 RSA public key used to 
    /// verify stream file signature
    /// </summary>
    [JsonPropertyName("public_key")]
    public string PublicKey { get; set; } = default!;
    /// <summary>
    /// The total tinybars earned by this node per whole 
    /// hbar in the last staking period
    /// </summary>
    [JsonPropertyName("reward_rate_start")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long RewardRateStart { get; set; }
    /// <summary>
    /// The node consensus weight at the 
    /// beginning of the staking period
    /// </summary>
    [JsonPropertyName("stake")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Stake { get; set; }
    /// <summary>
    /// The sum (balance + stakedToMe) for all accounts 
    /// staked to this node with declineReward=true at 
    /// the beginning of the staking period
    /// </summary>
    [JsonPropertyName("stake_not_rewarded")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakeNotRewarded { get; set; }
    /// <summary>
    /// The sum (balance + staked) for all accounts staked 
    /// to the node that are not declining rewards at the 
    /// beginning of the staking period
    /// </summary>
    [JsonPropertyName("stake_rewarded")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakeRewarded { get; set; }
    /// <summary>
    /// The staking period the stake figures were taken from.
    /// </summary>
    [JsonPropertyName("staking_period")]
    public TimestampRangeData? StakingPeriod { get; set; }
    /// <summary>
    /// The range of time this data record
    /// is valid for.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData ValidRange { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying consensus node data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConsensusNodeDataExtensions
{
    /// <summary>
    /// Enumerates the consensus (gossip) nodes published by the
    /// network's address book, reading from
    /// <c>/api/v1/network/nodes</c>. Use <see cref="FileFilter"/> to
    /// narrow to a specific address-book file (<c>0.0.101</c> or
    /// <c>0.0.102</c>) or <see cref="NodeFilter"/> to narrow by node id.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="FileFilter"/>, <see cref="NodeFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of consensus-node records.
    /// </returns>
    public static IAsyncEnumerable<ConsensusNodeData> GetConsensusNodesAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("network/nodes", filters);
        return client.GetPagedItemsAsync<ConsensusNodeDataPage, ConsensusNodeData>(path, MirrorJsonContext.Default.ConsensusNodeDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetConsensusNodesAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<ConsensusNodeData> GetConsensusNodesAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetConsensusNodesAsync(CancellationToken.None, filters);
    }
    /// <summary>
    /// Client-side wrapper over <see cref="GetConsensusNodesAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    /// that pings the gRPC endpoints returned from
    /// <c>/api/v1/network/nodes</c> and returns only the endpoints that
    /// respond within <paramref name="maxTimeoutInMilliseconds"/>.
    /// Only endpoints that carry an IPv4 address and use port 50211
    /// (plaintext) or 50212 (TLS) are probed; endpoints published with
    /// only a domain name or on another port (as some local networks
    /// do) are skipped. Results are keyed per endpoint, so a node can
    /// appear more than once.
    /// Useful for building a dynamic gateway list for
    /// <c>ConsensusClient</c> transaction submission.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="maxTimeoutInMilliseconds">
    /// The time value threshold, that if exceeded, will result
    /// in the node not being considered active and included
    /// on this list.
    /// </param>
    /// <param name="transport">
    /// Which transport(s) to return endpoints for.  Defaults to
    /// <see cref="ConsensusNodeTransport.Plaintext"/> (unencrypted port 50211) to
    /// preserve historical behavior.  <see cref="ConsensusNodeTransport.Tls"/>
    /// returns encrypted (port 50212) endpoints, each carrying the node's
    /// certificate hash so the client's default channel factory can pin the TLS
    /// connection; a node with no usable certificate hash is excluded from the
    /// TLS results.  <see cref="ConsensusNodeTransport.All"/> probes and returns
    /// both.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token that cancels the node listing and every ping. A
    /// cancelled call throws rather than returning the nodes probed so far.
    /// </param>
    /// <returns>
    /// A dictionary of gateways and the corresponding response
    /// time (in milliseconds).
    /// </returns>
    public static async Task<IReadOnlyDictionary<ConsensusNodeEndpoint, long>> GetActiveConsensusNodesAsync(this MirrorRestClient client, int maxTimeoutInMilliseconds, ConsensusNodeTransport transport = ConsensusNodeTransport.Plaintext, CancellationToken cancellationToken = default)
    {
        var wantPlaintext = transport is ConsensusNodeTransport.Plaintext or ConsensusNodeTransport.All;
        var wantTls = transport is ConsensusNodeTransport.Tls or ConsensusNodeTransport.All;
        var list = new List<Task<(ConsensusNodeEndpoint gateway, long response)>>();
        await foreach (var node in client.GetConsensusNodesAsync(cancellationToken).ConfigureAwait(false))
        {
            // The certificate hash is published per node, not per endpoint; parse
            // it once and attach it to any TLS endpoint built for this node.
            var hasCertHash = TryParseCertificateHash(node.CertificateHash, out var certHash);
            foreach (var endpoint in node.Endpoints)
            {
                // Solo does not include address anymore
                var address = endpoint.Address;
                if (string.IsNullOrWhiteSpace(address))
                {
                    continue;
                }
                if (wantPlaintext && endpoint.Port == 50211)
                {
                    var gateway = new ConsensusNodeEndpoint(node.Account, new Uri($"http://{address}:{endpoint.Port}"));
                    list.Add(ProbeGatewayAsync(gateway, maxTimeoutInMilliseconds, cancellationToken));
                }
                // A TLS endpoint can't be validated without a hash to pin against,
                // so exclude nodes whose address-book entry lacks a usable one.
                if (wantTls && endpoint.Port == 50212 && hasCertHash)
                {
                    var gateway = new ConsensusNodeEndpoint(node.Account, new Uri($"https://{address}:{endpoint.Port}"), certHash);
                    list.Add(ProbeGatewayAsync(gateway, maxTimeoutInMilliseconds, cancellationToken));
                }
            }
        }
        var responses = await Task.WhenAll(list).ConfigureAwait(false);
        // A cancelled ping reads as an unresponsive node; don't hand back a
        // list that silently left out nodes the caller never let us reach.
        cancellationToken.ThrowIfCancellationRequested();
        var result = new Dictionary<ConsensusNodeEndpoint, long>(responses.Length);
        foreach (var (gateway, response) in responses)
        {
            if (response > -1)
            {
                result.Add(gateway, response);
            }
        }
        return result;

        static async Task<(ConsensusNodeEndpoint gateway, long response)> ProbeGatewayAsync(ConsensusNodeEndpoint gateway, int maxTimeoutInMilliseconds, CancellationToken cancellationToken)
        {
            await using var grpcClient = new ConsensusClient(cfg => cfg.Endpoint = gateway);
            // The ping itself is cancelled at the timeout: a ping merely
            // abandoned would keep retrying an unresponsive node in the
            // background for the client's whole retry window.
            using var pingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pingCancellation.CancelAfter(maxTimeoutInMilliseconds);
            try
            {
                return (gateway, await grpcClient.PingAsync(pingCancellation.Token).ConfigureAwait(false));
            }
            catch
            {
                return (gateway, -1L);
            }
        }
    }
    /// <summary>
    /// Parses the mirror node's hex-encoded <c>node_cert_hash</c> value (with or
    /// without a <c>0x</c> prefix) into raw bytes suitable for TLS certificate
    /// pinning.  Returns <c>false</c> for a null, empty, or unparseable value —
    /// such a node has no basis for TLS validation and should be excluded rather
    /// than trusted.
    /// </summary>
    /// <param name="value">The hex-encoded certificate hash from the address book.</param>
    /// <param name="hash">The decoded hash bytes when parsing succeeds.</param>
    /// <returns>True when a non-empty hash was decoded.</returns>
    internal static bool TryParseCertificateHash(string? value, out ReadOnlyMemory<byte> hash)
    {
        hash = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        var span = value.AsSpan().Trim();
        if (span.StartsWith("0x") || span.StartsWith("0X"))
        {
            span = span[2..];
        }
        if (span.Length == 0 || (span.Length & 1) != 0)
        {
            return false;
        }
        try
        {
            hash = Convert.FromHexString(span);
        }
        catch (FormatException)
        {
            hash = default;
            return false;
        }
        return !hash.IsEmpty;
    }
}
