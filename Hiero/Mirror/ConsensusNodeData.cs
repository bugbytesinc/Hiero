using Hiero.Converters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents consensus node information returned from the mirror node.
/// </summary>
public class ConsensusNodeData
{
    /// <summary>
    /// The consensus nodes account ID (for payment purposes).
    /// </summary>
    [JsonPropertyName("node_account_id")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// A list of gRPC endpoints this gossip node can reached through.
    /// </summary>
    [JsonPropertyName("service_endpoints")]
    public GrpcEndpointData[] Endpoints { get; set; } = default!;
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
    /// The range of time this data record
    /// is valid for.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData ValidRange { get; set; } = default!;
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConsensusNodeDataExtensions
{
    /// <summary>
    /// Retrieves the list of known Hedera Gossip Nodes.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The list of known Hedera Gossip Nodes.
    /// </returns>
    public static IAsyncEnumerable<ConsensusNodeData> GetConsensusNodesAsync(this MirrorRestClient client)
    {
        return client.GetPagedItemsAsync<ConsensusNodeDataPage, ConsensusNodeData>("network/nodes");
    }
    /// <summary>
    /// Retrieves a list of Hedera gRPC nodes known to the 
    /// mirror node that respond to a const_ask query within 
    /// the given timeout value.  This can be used to create
    /// a list of working gRPC nodes for submitting transactions.
    /// </summary>
    /// <param name="client">
    /// The Mirror Node REST ConsensusClient
    /// </param>
    /// <param name="maxTimeoutInMiliseconds">
    /// The time value threshold, that if exceeded, will result
    /// in the node not being considered active and included
    /// on this list.
    /// </param>
    /// <returns>
    /// A dictionary of gateways and the corresponding response
    /// time (in miliseconds).
    /// </returns>
    public static async Task<IReadOnlyDictionary<ConsensusNodeEndpoint, long>> GetActiveConsensusNodesAsync(this MirrorRestClient client, int maxTimeoutInMiliseconds)
    {
        var list = new List<Task<(ConsensusNodeEndpoint gatway, long response)>>();
        await foreach (var node in client.GetConsensusNodesAsync())
        {
            foreach (var endpoint in node.Endpoints)
            {
                // Solo does not include address anymore
                if (endpoint.Port == 50211 && !string.IsNullOrWhiteSpace(endpoint.Address))
                {
                    list.Add(Task.Run(async () =>
                    {
                        var uri = new Uri($"http://{endpoint.Address}:{endpoint.Port}");
                        var gateway = new ConsensusNodeEndpoint(node.Account, uri);
                        var grpClient = new ConsensusClient(cfg => cfg.Endpoint = gateway);
                        var response = -1L;
                        var task = grpClient.PingAsync();
                        if (await Task.WhenAny(task, Task.Delay(maxTimeoutInMiliseconds)) == task)
                        {
                            try
                            {
                                response = task.Result;
                            }
                            catch
                            {
                                // fall thru with -1
                            }
                        }
                        return (gateway, response);
                    }));
                }
            }
        }
        var result = new Dictionary<ConsensusNodeEndpoint, long>();
        foreach (var (gatway, response) in await Task.WhenAll(list))
        {
            if (response > -1)
            {
                result.Add(gatway, response);
            }
        }
        return result;
    }
}