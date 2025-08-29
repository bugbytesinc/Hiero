using Google.Protobuf;
using Hiero;

namespace Proto;

public sealed partial class ContractID
{
    internal ContractID(EntityId contract) : this()
    {
        if (contract is null)
        {
            throw new ArgumentNullException(nameof(contract), "Contract Address is missing. Please check that it is not null.");
        }
        ShardNum = contract.ShardNum;
        RealmNum = contract.RealmNum;
        if (contract.TryGetEvmAddress(out var evmAddress))
        {
            EvmAddress = ByteString.CopyFrom(evmAddress.Bytes);
        }
        else if (contract.TryGetKeyAlias(out var _))
        {
            throw new ArgumentOutOfRangeException(nameof(contract), "Contract Address does not appear to be a valid <shard>.<realm>.<num> or EVM Address.");
        }
        else
        {
            ContractNum = contract.AccountNum;
        }
    }
}

internal static class ContractIDExtensions
{
    internal static EntityId AsAddress(this ContractID? id)
    {
        if (id is null)
        {
            return EntityId.None;
        }
        if (id.ContractCase == ContractID.ContractOneofCase.EvmAddress)
        {
            return new EntityId(id.ShardNum, id.RealmNum, new EvmAddress(id.EvmAddress.Span));
        }
        return new EntityId(id.ShardNum, id.RealmNum, id.ContractNum);
    }
}