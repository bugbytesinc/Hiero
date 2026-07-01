// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero;

namespace Proto;

internal static class AssessedCustomFeeExtensions
{
    internal static IReadOnlyList<RoyaltyTransfer> AsRoyaltyTransferList(this RepeatedField<AssessedCustomFee> list)
    {
        var count = list?.Count ?? 0;
        if (count > 0)
        {
            var result = new RoyaltyTransfer[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = new RoyaltyTransfer(list![i]);
            }
            return result;
        }
        return [];
    }
}
