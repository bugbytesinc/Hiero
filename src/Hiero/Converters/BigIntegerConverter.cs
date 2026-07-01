// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts <see cref="BigInteger"/> to and from hexadecimal JSON string values, using the 0x-prefixed format.
/// </summary>
public sealed class BigIntegerConverter : JsonConverter<BigInteger>
{
    /// <inheritdoc />
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return BigInteger.Zero;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return Parse(reader.GetString());
        }
        return Parse(reader.ValueSpan);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        if (value.IsZero)
        {
            writer.WriteStringValue("0x0");
        }
        else
        {
            var byteCount = value.GetByteCount(isUnsigned: true);
            Span<byte> bytes = byteCount <= 64 ? stackalloc byte[byteCount] : new byte[byteCount];
            value.TryWriteBytes(bytes, out int bytesWritten, isUnsigned: true, isBigEndian: true);

            var hexCharCount = bytesWritten * 2;
            var bufferLength = 2 + hexCharCount;
            Span<char> buffer = bufferLength <= 130 ? stackalloc char[bufferLength] : new char[bufferLength];
            buffer[0] = '0';
            buffer[1] = 'x';
            Convert.TryToHexStringLower(bytes[..bytesWritten], buffer[2..], out _);
            if (buffer[2] == '0')
            {
                buffer[3..bufferLength].CopyTo(buffer[2..]);
                bufferLength--;
            }
            writer.WriteStringValue(buffer[..bufferLength]);
        }
    }

    private static BigInteger Parse(string? valueInHex)
    {
        if (string.IsNullOrWhiteSpace(valueInHex) || valueInHex == "0x0")
        {
            return BigInteger.Zero;
        }
        return ParseHex(valueInHex.AsSpan());
    }

    private static BigInteger Parse(ReadOnlySpan<byte> valueInHex)
    {
        if (valueInHex.Length == 0 || valueInHex.IsWhiteSpace() || valueInHex.SequenceEqual("0x0"u8))
        {
            return BigInteger.Zero;
        }
        return ParseHex(valueInHex);
    }

    private static BigInteger ParseHex(ReadOnlySpan<char> hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }
        if (hex.Length > 0)
        {
            if (hex.Length % 2 == 1)
            {
                Span<char> paddedHex = hex.Length < 128 ? stackalloc char[hex.Length + 1] : new char[hex.Length + 1];
                paddedHex[0] = '0';
                hex.CopyTo(paddedHex[1..]);
                var paddedByteCount = paddedHex.Length / 2;
                Span<byte> paddedBuffer = paddedByteCount <= 64 ? stackalloc byte[paddedByteCount] : new byte[paddedByteCount];
                if (Convert.FromHexString(paddedHex, paddedBuffer, out _, out int paddedBytesWritten) == System.Buffers.OperationStatus.Done)
                {
                    return new BigInteger(paddedBuffer[..paddedBytesWritten], true, true);
                }
            }
            else
            {
                var byteCount = hex.Length / 2;
                Span<byte> buffer = byteCount <= 64 ? stackalloc byte[byteCount] : new byte[byteCount];
                if (Convert.FromHexString(hex, buffer, out _, out int bytesWritten) == System.Buffers.OperationStatus.Done)
                {
                    return new BigInteger(buffer[..bytesWritten], true, true);
                }
            }
        }
        return BigInteger.Zero;
    }

    private static BigInteger ParseHex(ReadOnlySpan<byte> hex)
    {
        if (hex.Length >= 2 && hex[0] == (byte)'0' && (hex[1] == (byte)'x' || hex[1] == (byte)'X'))
        {
            hex = hex[2..];
        }
        if (hex.Length > 0)
        {
            if (hex.Length % 2 == 1)
            {
                Span<byte> paddedHex = hex.Length < 128 ? stackalloc byte[hex.Length + 1] : new byte[hex.Length + 1];
                paddedHex[0] = (byte)'0';
                hex.CopyTo(paddedHex[1..]);
                var paddedByteCount = paddedHex.Length / 2;
                Span<byte> paddedBuffer = paddedByteCount <= 64 ? stackalloc byte[paddedByteCount] : new byte[paddedByteCount];
                if (Convert.FromHexString(paddedHex, paddedBuffer, out _, out int paddedBytesWritten) == System.Buffers.OperationStatus.Done)
                {
                    return new BigInteger(paddedBuffer[..paddedBytesWritten], true, true);
                }
            }
            else
            {
                var byteCount = hex.Length / 2;
                Span<byte> buffer = byteCount <= 64 ? stackalloc byte[byteCount] : new byte[byteCount];
                if (Convert.FromHexString(hex, buffer, out _, out int bytesWritten) == System.Buffers.OperationStatus.Done)
                {
                    return new BigInteger(buffer[..bytesWritten], true, true);
                }
            }
        }
        return BigInteger.Zero;
    }
}
