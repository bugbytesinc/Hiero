// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts a <see cref="ConsensusNodeEndpoint"/> to and from a JSON object
/// with <c>address</c> (node id) and <c>url</c> properties.
/// </summary>
public sealed class ConsensusNodeEndpointConverter : JsonConverter<ConsensusNodeEndpoint>
{
    /// <inheritdoc />
    public override ConsensusNodeEndpoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EntityId? node = null;
        string? urlAsString = null;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("address"u8))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        continue;
                    }
                    if (reader.ValueIsEscaped)
                    {
                        ShardRealmNumParser.TryParse(reader.GetString(), out node);
                    }
                    else if (reader.HasValueSequence)
                    {
                        ShardRealmNumParser.TryParse(reader.ValueSequence, out node);
                    }
                    else
                    {
                        ShardRealmNumParser.TryParse(reader.ValueSpan, out node);
                    }
                }
                else if (reader.ValueTextEquals("url"u8))
                {
                    reader.Read();
                    urlAsString = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        if (node is not null && !string.IsNullOrEmpty(urlAsString))
        {
            return new ConsensusNodeEndpoint(node, new Uri(urlAsString));
        }
        throw new JsonException("Not an appropriately formatted Consensus Node Endpoint Object.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusNodeEndpoint gateway, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        Span<byte> buffer = stackalloc byte[64];
        if (gateway.Node.TryFormat(buffer, out var bytesWritten, default, default))
        {
            writer.WriteString("address"u8, buffer[..bytesWritten]);
        }
        else
        {
            writer.WriteString("address"u8, gateway.Node.ToString());
        }
        writer.WriteString("url"u8, gateway.Uri.AbsoluteUri);
        writer.WriteEndObject();
    }
}
