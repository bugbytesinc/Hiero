using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Kyc Status JSON Converter
/// </summary>
public sealed class TokenKycStatusConverter : JsonConverter<TokenKycStatus>
{
    /// <inheritdoc />
    public override TokenKycStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "GRANTED" => TokenKycStatus.Granted,
            "REVOKED" => TokenKycStatus.Revoked,
            "NOT_APPLICABLE" => TokenKycStatus.NotApplicable,
            _ => TokenKycStatus.NotApplicable
        };
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
