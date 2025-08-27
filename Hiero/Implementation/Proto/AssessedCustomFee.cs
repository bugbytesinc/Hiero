using Google.Protobuf.Collections;
using Hiero;
using System.Collections.Generic;

namespace Proto;

internal static class AssessedCustomFeeExtensions
{
    internal static IReadOnlyList<RoyaltyTransfer> AsRoyaltyTransferList(this RepeatedField<AssessedCustomFee> list)
    {
        if (list is { Count: > 0 })
        {
            var result = new List<RoyaltyTransfer>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(new RoyaltyTransfer(list[i]));
            }
            return result;
        }
        return [];
    }
}