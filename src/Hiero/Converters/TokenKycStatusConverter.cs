// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts the mirror node KYC-status string (<c>GRANTED</c>/<c>REVOKED</c>)
/// to and from a <see cref="TokenKycStatus"/>.
/// </summary>
public sealed class TokenKycStatusConverter : JsonConverter<TokenKycStatus>
{
    /// <inheritdoc />
    public override TokenKycStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return TokenKycStatus.NotApplicable;
        }
        if (reader.ValueTextEquals("GRANTED"u8))
        {
            return TokenKycStatus.Granted;
        }
        if (reader.ValueTextEquals("REVOKED"u8))
        {
            return TokenKycStatus.Revoked;
        }
        return TokenKycStatus.NotApplicable;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TokenKycStatus status, JsonSerializerOptions options)
    {
        writer.WriteStringValue(status switch
        {
            TokenKycStatus.Granted => "GRANTED",
            TokenKycStatus.Revoked => "REVOKED",
            TokenKycStatus.NotApplicable => "NOT_APPLICABLE",
            _ => "NOT_APPLICABLE"
        });
    }
}
