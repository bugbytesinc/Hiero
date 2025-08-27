using System;
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
        return Nft.TryParse(reader.GetString(), out Nft? nft) ? nft : Nft.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Nft nft, JsonSerializerOptions options)
    {
        writer.WriteStringValue(nft.ToString());
    }
    /// <inheritdoc />
    public override Nft ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Nft value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
