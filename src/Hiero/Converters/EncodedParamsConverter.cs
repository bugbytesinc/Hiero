// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts an <see cref="EncodedParams"/> to and from a <c>0x</c>-prefixed hexadecimal JSON string.
/// </summary>
public sealed class EncodedParamsConverter : JsonConverter<EncodedParams>
{
    /// <inheritdoc />
    public override EncodedParams Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return new EncodedParams(ReadOnlyMemory<byte>.Empty);
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            var valueInHex = reader.GetString();
            if (!string.IsNullOrWhiteSpace(valueInHex))
            {
                try
                {
                    var hex = valueInHex.AsSpan();
                    if (hex.StartsWith("0x"))
                    {
                        hex = hex[2..];
                    }
                    return EncodedParams.FromOwnedBytes(Convert.FromHexString(hex));
                }
                catch
                {
                    // Punt.
                }
            }
            return new EncodedParams(ReadOnlyMemory<byte>.Empty);
        }
        var valueInHexBytes = reader.ValueSpan;
        if (valueInHexBytes.StartsWith("0x"u8))
        {
            valueInHexBytes = valueInHexBytes[2..];
        }
        try
        {
            return EncodedParams.FromOwnedBytes(Convert.FromHexString(valueInHexBytes));
        }
        catch
        {
            return new EncodedParams(ReadOnlyMemory<byte>.Empty);
        }
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EncodedParams encodedParams, JsonSerializerOptions options)
    {
        var length = 2 + encodedParams.Data.Length * 2;
        char[]? rented = null;
        Span<char> buffer = length <= 256
            ? stackalloc char[length]
            : (rented = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);
        try
        {
            buffer[0] = '0';
            buffer[1] = 'x';
            Convert.TryToHexStringLower(encodedParams.Data.Span, buffer[2..], out _);
            writer.WriteStringValue(buffer);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}
