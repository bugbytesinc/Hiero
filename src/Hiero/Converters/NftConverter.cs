// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Formatting;
using Hiero.Implementation.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Nft JSON Converter
/// </summary>
public sealed class NftConverter : JsonConverter<Nft>
{
    /// <inheritdoc />
    public override Nft Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            return Nft.None;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return NftParser.TryParse(reader.GetString(), out Nft? nft) ? nft : Nft.None;
        }
        return NftParser.TryParse(reader.ValueSpan, out Nft? spanNft) ? spanNft : Nft.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Nft nft, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[96];
        if (NftFormatter.TryFormat(nft, buffer, out var bytesWritten))
        {
            writer.WriteStringValue(buffer[..bytesWritten]);
        }
        else
        {
            writer.WriteStringValue(NftFormatter.Format(nft));
        }
    }
    /// <inheritdoc />
    public override Nft ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Nft value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[96];
        if (NftFormatter.TryFormat(value, buffer, out var bytesWritten))
        {
            writer.WritePropertyName(buffer[..bytesWritten]);
        }
        else
        {
            writer.WritePropertyName(NftFormatter.Format(value));
        }
    }
}
