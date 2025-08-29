using Google.Protobuf;

namespace Proto;

internal static class FeeScheduleExtensions
{
    internal static Hiero.FeeSchedule ToFeeSchedule(this FeeSchedule schedule)
    {
        var data = new Dictionary<string, string[]>();
        // We're doing this in a loop because the fee schedule
        // can be corrupted and have duplicate entries, we'll
        // assume the last entry wins, which may or may/not be
        // what the actual node software would use, but at this
        // time we have no idea which one is correct, and it would
        // be sad to not beable to load any of the info due to a
        // bad data file on hedera's side.  This is the best
        // work-around for this defect that we have for now.
        foreach (var entry in schedule.TransactionFeeSchedule)
        {
            data[entry.HederaFunctionality.ToString()] = entry.Fees?.Select(item => JsonFormatter.Default.Format(item)).ToArray() ?? Array.Empty<string>();
        }
        return new Hiero.FeeSchedule
        {
            Expires = schedule.ExpiryTime.ToConsensusTimeStamp(),
            Data = data
        };
    }
}