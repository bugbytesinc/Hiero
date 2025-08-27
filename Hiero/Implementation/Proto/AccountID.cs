using Google.Protobuf;
using Hiero;
using System;

namespace Proto;

public sealed partial class AccountID
{
    internal AccountID(EntityId account) : this()
    {
        if (account is null)
        {
            throw new ArgumentNullException(nameof(account), "Account Address/Alias is missing. Please check that it is not null.");
        }
        ShardNum = account.ShardNum;
        RealmNum = account.RealmNum;
        if (account.TryGetKeyAlias(out var keyAlias))
        {
            Alias = new Key(keyAlias).ToByteString();
        }
        else if (account.TryGetEvmAddress(out var evmAddress))
        {
            // See https://github.com/hashgraph/hedera-services/issues/4606
            Alias = ByteString.CopyFrom(evmAddress.Bytes);
        }
        else
        {
            AccountNum = account.AccountNum;
        }
    }
}

internal static class AccountIDExtensions
{
    internal static EntityId AsAddress(this AccountID? accountId)
    {
        if (accountId is not null)
        {
            if (accountId.AccountCase == AccountID.AccountOneofCase.AccountNum)
            {
                return new EntityId(accountId.ShardNum, accountId.RealmNum, accountId.AccountNum);
            }
            if (accountId.AccountCase == AccountID.AccountOneofCase.Alias)
            {
                if (accountId.Alias.Length == 20)
                {
                    // BEGIN NETWORK DEFECT: Should we not be using EvmAddress instead?
                    // See https://github.com/hashgraph/hedera-services/issues/4606
                    return new EntityId(accountId.ShardNum, accountId.RealmNum, new EvmAddress(accountId.Alias.Span));
                }
                return new EntityId(accountId.ShardNum, accountId.RealmNum, Key.Parser.ParseFrom(accountId.Alias.Span).ToEndorsement());
            }
        }
        return EntityId.None;
    }
}