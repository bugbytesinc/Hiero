using System.Globalization;
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
        var valueInHex = reader.GetString();
        if (!string.IsNullOrWhiteSpace(valueInHex) && valueInHex != "0x0")
        {
            try
            {
                if (valueInHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    valueInHex = valueInHex[2..];
                }
                if (valueInHex.Length % 2 == 1)
                {
                    valueInHex = "0" + valueInHex;
                }
                BigInteger.Parse(valueInHex, NumberStyles.HexNumber);
                Span<byte> buffer = valueInHex.Length <= 128 ? stackalloc byte[64] : new byte[valueInHex.Length / 2];
                if (Hex.TryDecode(valueInHex.AsSpan(), buffer, out int bytesWritten) || bytesWritten == 0)
                {
                    return new BigInteger(buffer[..bytesWritten], true, true);
                }
            }
            catch
            {
                // Punt.
            }
        }
        return BigInteger.Zero;
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
            var hex = Hex.FromBytes(value.ToByteArray(true, true));
            writer.WriteStringValue($"0x{(hex.StartsWith('0') ? hex[1..] : hex)}");
        }
    }
}
