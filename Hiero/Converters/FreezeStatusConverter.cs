using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Consensus Timestamp JSON Converter
/// </summary>
public sealed class FreezeStatusConverter : JsonConverter<TokenTradableStatus>
{
    /// <inheritdoc />
    public override TokenTradableStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "FROZEN" => TokenTradableStatus.Suspended,
            "UNFROZEN" => TokenTradableStatus.Tradable,
            "NOT_APPLICABLE" => TokenTradableStatus.NotApplicable,
            _ => TokenTradableStatus.NotApplicable
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TokenTradableStatus status, JsonSerializerOptions options)
    {
        writer.WriteStringValue(status switch
        {
            TokenTradableStatus.Suspended => "FROZEN",
            TokenTradableStatus.Tradable => "UNFROZEN",
            _ => "NOT_APPLICABLE"
        });
    }
}
