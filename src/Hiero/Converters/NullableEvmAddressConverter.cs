// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Nullable EVM-address converter that preserves an explicit
/// wire-<c>null</c> as a C# <c>null</c>, rather than coercing it to
/// <see cref="EvmAddress.None"/> (the all-zeros sentinel). The plain
/// <see cref="EvmAddressConverter"/> collapses both forms — useful when
/// the server never sends null — but on endpoints that *do* send null
/// to distinguish "no address" from "the literal zero address" (e.g.
/// contract-action <c>to</c> fields, where CREATE actions can leave
/// the recipient null), use this converter.
/// </summary>
public sealed class NullableEvmAddressConverter : JsonConverter<EvmAddress?>
{
    /// <inheritdoc />
    public override EvmAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        return EvmAddress.TryParse(value, out var evmAddress) ? evmAddress : null;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmAddress? evmAddress, JsonSerializerOptions options)
    {
        if (evmAddress is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(evmAddress.Bytes.IsEmpty ?
                "0x0000000000000000000000000000000000000000" :
                evmAddress.ToString());
        }
    }
}
