using Google.Protobuf.Collections;
using Hiero;

namespace Proto;

internal static class TokenAssociationExtensions
{
    internal static IReadOnlyList<Association> AsAssociationList(this RepeatedField<TokenAssociation> list)
    {
        if (list is { Count: > 0 })
        {
            var result = new List<Association>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.Add(new Association(list[i]));
            }
            return result;
        }
        return [];
    }
}