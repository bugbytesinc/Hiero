using Proto;
using System.ComponentModel;

namespace Hiero;

/// <summary>
/// Object containing the current and next fee schedule
/// returned from the network.
/// </summary>
public sealed class FeeSchedules
{
    /// <summary>
    /// Internal constructor, used by the library to create an
    /// initialized fee schedules object.
    /// </summary>
    /// <param name="current">Current Fee Schedule</param>
    /// <param name="next">Next Fee Schedule</param>
    internal FeeSchedules(FeeSchedule? current, FeeSchedule? next)
    {
        Current = current;
        Next = next;
    }
    /// <summary>
    /// Current Fee Schedule
    /// </summary>
    public FeeSchedule? Current { get; }
    /// <summary>
    /// Fee schedule that is in effect after 
    /// the current fee schedule expires.
    /// </summary>
    public FeeSchedule? Next { get; }
}
/// <summary>
/// Contains a dictionary holding the fee calculation
/// parameters for various network functions.
/// </summary>
public sealed record FeeSchedule
{
    /// <summary>
    /// A dictionary mapping hedera functionality (by name) to 
    /// structured fee data (serialized as JSON data).
    /// </summary>
    public Dictionary<string, string[]> Data { get; internal init; } = default!;
    /// <summary>
    /// The Time at which this fee schedule expires.
    /// </summary>
    public ConsensusTimeStamp Expires { get; internal init; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FeeSchedulesExtensions
{
    /// <summary>
    /// Retrieves the metrics for calculating fees from the network.
    /// network.
    /// </summary>
    /// <remarks>
    /// NOTE: this method incurs a charge to retrieve the file from the network.
    /// </remarks>
    /// <param name="client">
    /// Consensus Client to QueryAsync
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The structure representing the metrics of the Network's Fee Schedule.
    /// </returns>
    public static async Task<FeeSchedules> GetFeeScheduleAsync(this ConsensusClient client, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        // Well known address of the fee schedule file is 0.0.111
        var file = await client.GetFileContentAsync(new EntityId(0, 0, 111), cancellationToken, configure).ConfigureAwait(false);
        var set = Proto.CurrentAndNextFeeSchedule.Parser.ParseFrom(file.ToArray());
        return new FeeSchedules(set.CurrentFeeSchedule?.ToFeeSchedule(), set.NextFeeSchedule?.ToFeeSchedule());
    }
}