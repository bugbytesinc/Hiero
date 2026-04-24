// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;

/// <summary>
/// Marker interface for query parameters that act as predicates —
/// they narrow the result set returned by the mirror node (for
/// example, "where <c>account.id</c> equals this value" or "where
/// <c>timestamp</c> is strictly greater than this consensus time").
/// </summary>
/// <remarks>
/// Implementations extend <see cref="IMirrorQueryParameter"/> via
/// this interface. Use this marker on method signatures that want to
/// accept predicates only, excluding paging directives
/// (<see cref="IMirrorPaging"/>) and projection toggles
/// (<see cref="IMirrorProjection"/>).
/// </remarks>
public interface IMirrorFilter : IMirrorQueryParameter
{
}
