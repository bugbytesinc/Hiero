// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// Detailed status of a block node: software and protocol versions,
/// installed plugins, the block ranges it can serve and store, TSS
/// verification bootstrap data, and consensus address books.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockNodeStatusDetail
{
    /// <summary>
    /// The version of the block stream protocol the node speaks.
    /// </summary>
    public SemanticVersion StreamProtoVersion { get; }
    /// <summary>
    /// The block node software version.
    /// </summary>
    public SemanticVersion BlockNodeVersion { get; }
    /// <summary>
    /// Versions of the plugins installed on the node.
    /// </summary>
    public IReadOnlyList<BlockNodePluginVersion> Plugins { get; }
    /// <summary>
    /// The block ranges this node can currently serve.
    /// </summary>
    public IReadOnlyList<BlockRange> AvailableRanges { get; }
    /// <summary>
    /// The block ranges this node persistently stores.
    /// </summary>
    public IReadOnlyList<BlockRange> StoredRanges { get; }
    /// <summary>
    /// TSS proof verification bootstrap data (ledger id, WRAPS
    /// verification key, roster), or <code>null</code> when the
    /// node does not provide it.
    /// </summary>
    public TssData? TssData { get; }
    /// <summary>
    /// The consensus network address book as known to the block
    /// node, or <code>null</code> when not provided.  Exposed as
    /// the raw protobuf — this is advanced surface primarily for
    /// verification tooling.
    /// </summary>
    public Proto.NodeAddressBook? NodeAddressBook { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockNodeStatusDetail(Org.Hiero.Block.Api.ServerStatusDetailResponse protobuf)
    {
        var versions = protobuf.VersionInformation;
        StreamProtoVersion = versions?.StreamProtoVersion is not null ? new SemanticVersion(versions.StreamProtoVersion) : SemanticVersion.None;
        BlockNodeVersion = versions?.BlockNodeVersion is not null ? new SemanticVersion(versions.BlockNodeVersion) : SemanticVersion.None;
        if (versions is not null)
        {
            var plugins = new BlockNodePluginVersion[versions.InstalledPluginVersions.Count];
            for (var i = 0; i < plugins.Length; i++)
            {
                plugins[i] = new BlockNodePluginVersion(versions.InstalledPluginVersions[i]);
            }
            Plugins = plugins;
        }
        else
        {
            Plugins = [];
        }
        AvailableRanges = ConvertRanges(protobuf.AvailableRanges);
        StoredRanges = ConvertRanges(protobuf.StoredRanges);
        TssData = protobuf.TssData is not null ? new TssData(protobuf.TssData) : null;
        NodeAddressBook = protobuf.NodeAddressBook;

        static IReadOnlyList<BlockRange> ConvertRanges(IReadOnlyList<Org.Hiero.Block.Api.BlockRange> source)
        {
            var ranges = new BlockRange[source.Count];
            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = new BlockRange(source[i].RangeStart, source[i].RangeEnd);
            }
            return ranges;
        }
    }
}
