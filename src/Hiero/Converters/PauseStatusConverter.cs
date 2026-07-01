// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts the mirror node pause-status string (<c>PAUSED</c>/<c>UNPAUSED</c>)
/// to and from a <see cref="TokenTradableStatus"/>.
/// </summary>
public sealed class PauseStatusConverter : JsonConverter<TokenTradableStatus>
{
    /// <inheritdoc />
    public override TokenTradableStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return TokenTradableStatus.NotApplicable;
        }
        if (reader.ValueTextEquals("PAUSED"u8))
        {
            return TokenTradableStatus.Suspended;
        }
        if (reader.ValueTextEquals("UNPAUSED"u8))
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
            TokenTradableStatus.Suspended => "PAUSED",
            TokenTradableStatus.Tradable => "UNPAUSED",
            _ => "NOT_APPLICABLE"
        });
    }
}
