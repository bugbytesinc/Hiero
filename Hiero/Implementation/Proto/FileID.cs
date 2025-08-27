using Hiero;
using System;

namespace Proto;

public sealed partial class FileID
{
    internal FileID(EntityId file) : this()
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file), "File is missing. Please check that it is not null.");
        }
        else if (!file.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(file), "File Address does not appear to be in a valid <shard>.<realm>.<num> form.");
        }
        ShardNum = file.ShardNum;
        RealmNum = file.RealmNum;
        FileNum = file.AccountNum;
    }
}

internal static class FileIDExtensions
{
    internal static EntityId AsAddress(this FileID? id)
    {
        if (id is not null)
        {
            return new EntityId(id.ShardNum, id.RealmNum, id.FileNum);
        }
        return EntityId.None;
    }
}