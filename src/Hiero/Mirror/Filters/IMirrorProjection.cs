// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;

/// <summary>
/// Marker interface for query parameters that reshape the response
/// rather than filter or page it — for example, toggling whether a
/// balance snapshot is included, selecting a transaction-type enum,
/// or asking the mirror node for an opcode-trace sub-section
/// (stack / memory / storage).
/// </summary>
/// <remarks>
/// Implementations extend <see cref="IMirrorQueryParameter"/> via
/// this interface. Projections change the shape of each returned
/// record; filters (<see cref="IMirrorFilter"/>) change which records
/// are returned; paging directives (<see cref="IMirrorPaging"/>)
/// control pagination over those records.
/// </remarks>
public interface IMirrorProjection : IMirrorQueryParameter
{
}
