// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero;

namespace Proto;

internal static class TokenAssociationExtensions
{
    internal static IReadOnlyList<Association> AsAssociationList(this RepeatedField<TokenAssociation> list)
    {
        var count = list?.Count ?? 0;
        if (count > 0)
        {
            var result = new Association[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = new Association(list![i]);
            }
            return result;
        }
        return [];
    }
}
