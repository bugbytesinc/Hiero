using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Token Type Converter
/// </summary>
public sealed class TokenTypeConverter : JsonConverter<TokenType>
{
    /// <inheritdoc />
    public override TokenType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "FUNGIBLE_COMMON" => TokenType.Fungible,
            "NON_FUNGIBLE_UNIQUE" => TokenType.NonFungible,
            _ => TokenType.Fungible
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TokenType timeStamp, JsonSerializerOptions options)
    {
        writer.WriteStringValue(timeStamp switch
        {
            TokenType.Fungible => "FUNGIBLE_COMMON",
            TokenType.NonFungible => "NON_FUNGIBLE_UNIQUE",
            _ => "FUNGIBLE_COMMON"
        });
    }
}
