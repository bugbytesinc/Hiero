// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Implementation;

internal static class PendingAirdropRecordExtensions
{
    internal static IReadOnlyList<AirdropAmount> AsAirdropAmountList(this RepeatedField<PendingAirdropRecord> list)
    {
        var count = list.Count;
        if (count > 0)
        {
            var result = new AirdropAmount[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = new(list[i]);
            }
            return result;
        }
        return [];
    }
}
