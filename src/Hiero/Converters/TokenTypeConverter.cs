// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts the mirror node token-type string (<c>FUNGIBLE_COMMON</c>/<c>NON_FUNGIBLE_UNIQUE</c>)
/// to and from a <see cref="TokenType"/>.
/// </summary>
public sealed class TokenTypeConverter : JsonConverter<TokenType>
{
    /// <inheritdoc />
    public override TokenType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return TokenType.Fungible;
        }
        if (reader.ValueTextEquals("NON_FUNGIBLE_UNIQUE"u8))
        {
            return TokenType.NonFungible;
        }
        return TokenType.Fungible;
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
