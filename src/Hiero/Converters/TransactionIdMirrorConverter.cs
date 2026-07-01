// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Formatting;
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// TransactionId Converter for reading and writing JSON
/// when interacting with a mirror node.
/// </summary>
public sealed class TransactionIdMirrorConverter : JsonConverter<TransactionId>
{
    /// <inheritdoc />
    public override TransactionId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            return TransactionId.None;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return TransactionIdMirrorParser.TryParse(reader.GetString(), out var id) ? id : TransactionId.None;
        }
        return TransactionIdMirrorParser.TryParse(reader.ValueSpan, out var transactionId) ? transactionId : TransactionId.None;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TransactionId txId, JsonSerializerOptions options)
    {
        if (txId == null || txId == TransactionId.None)
        {
            writer.WriteStringValue(string.Empty);
        }
        else
        {
            Span<byte> buffer = stackalloc byte[96];
            if (TransactionIdFormatter.TryFormat(txId, TransactionIdFormatStyle.Mirror, buffer, out var bytesWritten))
            {
                writer.WriteStringValue(buffer[..bytesWritten]);
            }
            else
            {
                writer.WriteStringValue(TransactionIdFormatter.Format(txId, TransactionIdFormatStyle.Mirror));
            }
        }
    }
}
