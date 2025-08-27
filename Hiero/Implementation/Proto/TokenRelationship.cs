using Google.Protobuf.Collections;
using System.Collections.Generic;

namespace Proto;

internal static class TokenRelationshipExtensions
{
    internal static IReadOnlyList<Hiero.TokenBalance> ToBalances(this RepeatedField<TokenRelationship> list)
    {
        if (list is { Count: > 0 })
        {
            var result = new List<Hiero.TokenBalance>(list.Count);
            foreach (var record in list)
            {
                result.Add(new Hiero.TokenBalance(record));
            }
            return result;
        }
        return [];
    }
}