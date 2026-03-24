// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Implementation;

internal static class PendingAirdropRecordExtensions
{
    internal static IReadOnlyList<AirdropAmount> AsAirdropAmountList(this RepeatedField<PendingAirdropRecord> list)
    {
        if (list is { Count: > 0 })
        {
            var result = new AirdropAmount[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = new(list[i]);
            }
            return result;
        }
        return [];
    }
}