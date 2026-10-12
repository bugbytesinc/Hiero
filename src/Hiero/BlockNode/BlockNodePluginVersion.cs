// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// Version information for a single plugin installed on a block node.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockNodePluginVersion
{
    /// <summary>
    /// The identifier of the plugin.
    /// </summary>
    public string PluginId { get; }
    /// <summary>
    /// The software version of the plugin.
    /// </summary>
    public SemanticVersion Version { get; }
    /// <summary>
    /// The names of the features this plugin provides.
    /// </summary>
    public IReadOnlyList<string> Features { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockNodePluginVersion(Org.Hiero.Block.Api.BlockNodeVersions.Types.PluginVersion protobuf)
    {
        PluginId = protobuf.PluginId;
        Version = protobuf.PluginSoftwareVersion is not null ? new SemanticVersion(protobuf.PluginSoftwareVersion) : SemanticVersion.None;
        Features = protobuf.PluginFeatureNames;
    }
}
