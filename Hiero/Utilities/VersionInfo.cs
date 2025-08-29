using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Contains version information identifying the Hedera
/// Services version and API Protobuf version implemented
/// by the node being queried.
/// </summary>
public sealed record VersionInfo
{
    /// <summary>
    /// Hedera API Protobuf version supported by this node.
    /// </summary>
    public SemanticVersion ApiProtobufVersion { get; private init; }
    /// <summary>
    /// Hedera Services Version implemented by this node.
    /// </summary>
    public SemanticVersion HederaServicesVersion { get; private init; }
    /// <summary>
    /// Internal constructor from raw results
    /// </summary>
    internal VersionInfo(Response response)
    {
        var info = response.NetworkGetVersionInfo;
        ApiProtobufVersion = new SemanticVersion(info.HapiProtoVersion);
        HederaServicesVersion = new SemanticVersion(info.HederaServicesVersion);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class VersionInfoExtensions
{
    /// <summary>
    /// Retrieves version information from the node.
    /// </summary>
    /// <param name="client">
    /// Consensus Client to QueryAsync
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// Version information regarding the gossip network node.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<VersionInfo> GetVersionInfoAsync(this ConsensusClient client, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new VersionInfo(await Engine.QueryAsync(client, new NetworkGetVersionInfoQuery(), cancellationToken, configure).ConfigureAwait(false));
    }
}