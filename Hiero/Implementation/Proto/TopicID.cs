using Hiero;
using System;

namespace Proto;

public sealed partial class TopicID
{
    internal TopicID(EntityId topic) : this()
    {
        if (topic is null)
        {
            throw new ArgumentNullException(nameof(topic), "Topic Address is missing. Please check that it is not null.");
        }
        if (topic.TryGetEvmAddress(out _) || topic.TryGetKeyAlias(out _))
        {
            throw new ArgumentOutOfRangeException(nameof(topic), "Topic Address does not appear to be in a valid <shard>.<realm>.<num> form.");
        }
        else
        {
            ShardNum = topic.ShardNum;
            RealmNum = topic.RealmNum;
            TopicNum = topic.AccountNum;
        }
    }
}

internal static class TopicIDExtensions
{
    internal static EntityId AsAddress(this TopicID? id)
    {
        if (id is not null)
        {
            return new EntityId(id.ShardNum, id.RealmNum, id.TopicNum);
        }
        return EntityId.None;
    }
}