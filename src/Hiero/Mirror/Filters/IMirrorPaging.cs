// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;

/// <summary>
/// Marker interface for query parameters that control paging or
/// ordering of results — they do not change which records the mirror
/// node returns, only the page size, sort direction, or cursor
/// position.
/// </summary>
/// <remarks>
/// Implementations extend <see cref="IMirrorQueryParameter"/> via
/// this interface. Distinguishing paging from filtering
/// (<see cref="IMirrorFilter"/>) and projections
/// (<see cref="IMirrorProjection"/>) lets method signatures and
/// documentation group parameters by their actual role.
/// </remarks>
public interface IMirrorPaging : IMirrorQueryParameter
{
}
