// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts the mirror node freeze-status string (<c>FROZEN</c>/<c>UNFROZEN</c>)
/// to and from a <see cref="TokenTradableStatus"/>.
/// </summary>
public sealed class FreezeStatusConverter : JsonConverter<TokenTradableStatus>
{
    /// <inheritdoc />
    public override TokenTradableStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return TokenTradableStatus.NotApplicable;
        }
        if (reader.ValueTextEquals("FROZEN"u8))
        {
            return TokenTradableStatus.Suspended;
        }
        if (reader.ValueTextEquals("UNFROZEN"u8))
        {
            return TokenTradableStatus.Tradable;
        }
        return TokenTradableStatus.NotApplicable;
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
