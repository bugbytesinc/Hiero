using Hiero;
using System;

namespace Proto;

public sealed partial class TokenID
{
    internal TokenID(EntityId token) : this()
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(token), "Token is missing. Please check that it is not null or empty.");
        }
        else if (!token.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(token), "Token Address does not appear to be in a valid <shard>.<realm>.<num> form.");
        }
        ShardNum = token.ShardNum;
        RealmNum = token.RealmNum;
        TokenNum = token.AccountNum;
    }
}
internal static class TokenIDExtensions
{
    internal static EntityId AsAddress(this TokenID? id)
    {
        if (id is not null)
        {
            return new EntityId(id.ShardNum, id.RealmNum, id.TokenNum);
        }
        return EntityId.None;
    }
}