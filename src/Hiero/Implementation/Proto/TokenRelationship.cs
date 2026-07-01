// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;

namespace Proto;

internal static class TokenRelationshipExtensions
{
    internal static IReadOnlyList<Hiero.TokenBalance> ToBalances(this RepeatedField<TokenRelationship> list)
    {
        var count = list.Count;
        if (count > 0)
        {
            var result = new Hiero.TokenBalance[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = new Hiero.TokenBalance(list[i]);
            }
            return result;
        }
        return [];
    }
}
