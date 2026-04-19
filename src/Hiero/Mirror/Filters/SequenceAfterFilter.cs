// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Retrieve topic messages after the
/// given consensus message number.
/// </summary>
public class SequenceAfterFilter : IMirrorQueryFilter
{
    /// <summary>
    /// Message sequence number filter, only return messages
    /// with sequence numbers larger than this value.
    /// </summary>
    private readonly ulong _sequenceNumber;
    /// <summary>
    /// Constructor requires the sequence number to filter by.
    /// </summary>
    /// <param name="sequenceNumber">
    /// Return only topic messages with sequence numbers larger
    /// than this target value.
    /// </param>
    public SequenceAfterFilter(ulong sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;
    }
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "sequencenumber";

    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value => $"gt:{_sequenceNumber}";
}
