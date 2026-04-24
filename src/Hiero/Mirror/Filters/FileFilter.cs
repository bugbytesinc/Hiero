// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>file.id</c> query parameter of the
/// <c>/api/v1/network/nodes</c> endpoint — filters the consensus-node
/// listing to nodes registered in a specific address-book file
/// (typically <c>0.0.101</c> or <c>0.0.102</c>). Construct via
/// <see cref="Is(EntityId)"/>; the ctor is private.
/// </summary>
/// <remarks>
/// Unlike the comparison-operator filters in this namespace,
/// <c>file.id</c> accepts only an equality match on the wire —
/// the mirror-node schema for
/// <c>/api/v1/network/nodes</c> exposes no comparison-operator
/// palette on this parameter. Only an <see cref="Is(EntityId)"/>
/// factory is provided; there is no <c>After</c> / <c>Before</c> /
/// <c>NotIs</c> counterpart. In practice the live network's
/// address book contains just the two files
/// (<c>0.0.101</c> for consensus nodes, <c>0.0.102</c> for the
/// gossip-node listing), so a comparison palette would be
/// superfluous even if the wire schema allowed it.
/// </remarks>
public sealed class FileFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "file.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private FileFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>file.id</c> equals the given entity.
    /// </summary>
    /// <param name="file">The address-book file entity to filter by.</param>
    public static FileFilter Is(EntityId file) => new(file.ToString());
}
