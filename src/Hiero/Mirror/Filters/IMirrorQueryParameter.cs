// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;

/// <summary>
/// Represents a single parameter appended to a Mirror Node REST
/// query string. Covers three conceptual kinds — predicate filters,
/// paging directives, and projection toggles — distinguished by
/// the marker sub-interfaces (see Phase 2 of the refactor).
/// </summary>
public interface IMirrorQueryParameter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }
}
