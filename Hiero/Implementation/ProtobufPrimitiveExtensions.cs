using Google.Protobuf.Collections;
using System.Collections.Generic;

namespace Hiero.Implementation;

internal static class ProtobufPrimitiveExtensions
{
    internal static IReadOnlyList<T> CopyToReadOnlyList<T>(this RepeatedField<T> field)
    {
        if (field is null || field.Count == 0)
        {
            return [];
        }
        var data = new T[field.Count];
        field.CopyTo(data, 0);
        return data;
    }
}
