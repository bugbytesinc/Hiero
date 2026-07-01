// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;

namespace Hiero.Implementation;

internal static class ProtobufPrimitiveExtensions
{
    internal static IReadOnlyList<T> CopyToReadOnlyList<T>(this RepeatedField<T> field)
    {
        var count = field?.Count ?? 0;
        if (count == 0)
        {
            return [];
        }
        var data = new T[count];
        field!.CopyTo(data, 0);
        return data;
    }
}
