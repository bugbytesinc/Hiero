// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Converters;
using Proto;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace Hiero.Implementation.Parsing;

internal static class EntityIdParser
{
    internal static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out EntityId? entityId)
    {
        if (value.Length > 0 && !value.IsWhiteSpace())
        {
            if (value.StartsWith("0x"))
            {
                var byteCount = value.Length / 2 - 1;
                byte[]? buffer = null;
                try
                {
                    buffer = ArrayPool<byte>.Shared.Rent(byteCount);
                    if (Convert.FromHexString(value[2..], buffer, out _, out var bytesWritten) == OperationStatus.Done)
                    {
                        return TryParseAsEvmOrLongZeroOrProtobuf(buffer.AsMemory(0, bytesWritten), out entityId);
                    }
                }
                catch (ArgumentException)
                {
                    // Unhappy case, can't figure out what this actually
                    // represents, fall thru and let it return false
                }
                finally
                {
                    if (buffer is not null)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
            else
            {
                return ShardRealmNumParser.TryParse(value, out entityId);
            }
        }
        entityId = null;
        return false;
    }
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
        if (value.Length > 0 && !value.IsWhiteSpace())
        {
            if (value.StartsWith("0x"u8))
            {
                var byteCount = value.Length / 2 - 1;
                byte[]? buffer = null;
                try
                {
                    buffer = ArrayPool<byte>.Shared.Rent(byteCount);
                    if (Convert.FromHexString(value[2..], buffer, out _, out var bytesWritten) == OperationStatus.Done)
                    {
                        return TryParseAsEvmOrLongZeroOrProtobuf(buffer.AsMemory(0, bytesWritten), out entityId);
                    }
                }
                catch (ArgumentException)
                {
                    // Unhappy case, can't figure out what this actually
                    // represents, fall thru and let it return None
                }
                finally
                {
                    if (buffer is not null)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

            }
            else
            {
                return ShardRealmNumParser.TryParse(value, out entityId);
            }
        }
        entityId = null;
        return false;
    }
    private static bool TryParseAsEvmOrLongZeroOrProtobuf(ReadOnlyMemory<byte> bytes, [NotNullWhen(true)] out EntityId? entityId)
    {
        if (bytes.Length == 20)
        {
            var span = bytes.Span;
            // Check to see if this is a long zero address.
            if (span[..12].SequenceEqual(stackalloc byte[12]))
            {
                var num = BinaryPrimitives.ReadInt64BigEndian(span.Slice(12, 8));
                if (num >= 0)
                {
                    entityId = new EntityId(0, 0, num);
                    return true;
                }
            }
            // Not a small enough number, must be a EvmAddress (EVM Payer)
            entityId = new EvmAddress(span);
            return true;
        }
        try
        {
            // Maybe the bytes are parsable as an
            // Ed25519 or ECDSA public key
            entityId = new EntityId(0, 0, new Endorsement(bytes));
            return true;
        }
        catch (ArgumentException)
        {
            // fall thru to possibility of complex key
            // encoded as protobuf instead.
        }
        try
        {
            entityId = new EntityId(0, 0, Key.Parser.ParseFrom(bytes.Span).ToEndorsement());
            return true;
        }
        catch (InvalidProtocolBufferException)
        {
            // fall thru to can't parse.
        }
        entityId = null;
        return false;
    }
}
