// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace Hiero.Implementation.Parsing;

internal static class ShardRealmNumParser
{
    internal static bool TryParse(ReadOnlySequence<byte> seq, [NotNullWhen(true)] out EntityId? entityId)
    {
        var byteCount = seq.Length;
        if (byteCount > 0)
        {
            byte[]? rented = null;
            Span<byte> buffer = byteCount <= 64
                ? stackalloc byte[(int)byteCount]
                : (rented = ArrayPool<byte>.Shared.Rent((int)byteCount)).AsSpan(0, (int)byteCount);
            try
            {
                seq.CopyTo(buffer);
                return TryParse(buffer, out entityId);
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }
        entityId = null;
        return false;
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, [NotNullWhen(true)] out EntityId? entityId)
    {
        entityId = null;
        if (value.Length < 5)
        {
            return false;
        }
        int firstDot = value.IndexOf((byte)'.');
        if (firstDot <= 0 || firstDot >= value.Length - 3)
        {
            return false;
        }
        int secondDot = value.Slice(firstDot + 1).IndexOf((byte)'.');
        if (secondDot <= 0)
        {
            return false;
        }
        secondDot += firstDot + 1;
        if (secondDot >= value.Length - 1)
        {
            return false;
        }
        var shardText = value[..firstDot];
        var realmText = value.Slice(firstDot + 1, secondDot - firstDot - 1);
        var numText = value[(secondDot + 1)..];
        if (Utf8Parser.TryParse(shardText, out uint shard, out var shardConsumed) &&
            shardConsumed == shardText.Length &&
            Utf8Parser.TryParse(realmText, out uint realm, out var realmConsumed) &&
            realmConsumed == realmText.Length &&
            Utf8Parser.TryParse(numText, out uint num, out var numConsumed) &&
            numConsumed == numText.Length)
        {
            entityId = new EntityId(shard, realm, num);
            return true;
        }
        return false;
    }
    internal static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out EntityId? entityId)
    {
        entityId = null;
        if (value.Length < 5)
        {
            return false;
        }
        int firstDot = value.IndexOf('.');
        if (firstDot <= 0 || firstDot >= value.Length - 3)
        {
            return false;
        }
        int secondDot = value.Slice(firstDot + 1).IndexOf('.');
        if (secondDot <= 0)
        {
            return false;
        }
        secondDot += firstDot + 1;
        if (secondDot >= value.Length - 1)
        {
            return false;
        }
        if (uint.TryParse(value.Slice(0, firstDot), out uint shard) &&
            uint.TryParse(value.Slice(firstDot + 1, secondDot - firstDot - 1), out uint realm) &&
            uint.TryParse(value[(secondDot + 1)..], out uint num))
        {
            entityId = new EntityId(shard, realm, num);
            return true;
        }
        return false;
    }
}
