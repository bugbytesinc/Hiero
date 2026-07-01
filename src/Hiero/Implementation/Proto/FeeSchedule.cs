// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using System.Runtime.InteropServices;

namespace Proto;

internal static class FeeScheduleExtensions
{
    internal static Hiero.FeeSchedule ToFeeSchedule(this FeeSchedule schedule)
    {
        var transactionFeeSchedule = schedule.TransactionFeeSchedule;
        var count = transactionFeeSchedule.Count;
        var data = new Dictionary<string, string[]>(count);
        // We're doing this in a loop because the fee schedule
        // can be corrupted and have duplicate entries, we'll
        // assume the last entry wins, which may or may/not be
        // what the actual node software would use, but at this
        // time we have no idea which one is correct, and it would
        // be sad to not beable to load any of the info due to a
        // bad data file on hedera's side.  This is the best
        // work-around for this defect that we have for now.
        foreach (var entry in transactionFeeSchedule)
        {
            var fees = entry.Fees;
            var feeCount = fees.Count;
            var feeData = feeCount == 0 ? [] : new string[feeCount];
            for (var i = 0; i < feeCount; i++)
            {
                feeData[i] = JsonFormatter.Default.Format(fees[i]);
            }
            ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(data, entry.HederaFunctionality.ToString(), out _);
            slot = feeData;
        }
        return new Hiero.FeeSchedule
        {
            Expires = schedule.ExpiryTime.ToConsensusTimeStamp(),
            Data = data
        };
    }
}
