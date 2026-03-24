// SPDX-License-Identifier: Apache-2.0
using Hiero;

namespace Proto;

public sealed partial class ScheduleID
{
    internal ScheduleID(EntityId schedule) : this()
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule), "Schedule Address is missing. Please check that it is not null.");
        }
        if (EntityId.None.Equals(schedule))
        {
            throw new ArgumentOutOfRangeException(nameof(schedule), "Schedule Address can not be empty or None. Please provide a valid value.");
        }
        else if (!schedule.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(schedule), "Schedule Address does not appear to be in a valid <shard>.<realm>.<num> form.");
        }
        ShardNum = schedule.ShardNum;
        RealmNum = schedule.RealmNum;
        ScheduleNum = schedule.AccountNum;
    }
    internal EntityId ToAddress()
    {
        return new EntityId(ShardNum, RealmNum, ScheduleNum);
    }
}