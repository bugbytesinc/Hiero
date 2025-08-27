using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Consensus Node ConsensusNodeEndpoint JSON Converter
/// </summary>
public sealed class ConsensusNodeEndpointConverter : JsonConverter<ConsensusNodeEndpoint>
{
    /// <inheritdoc />
    public override ConsensusNodeEndpoint? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        string? address = null;
        string? urlAsString = null;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "address":
                        address = reader.GetString();
                        break;

                    case "url":
                        urlAsString = reader.GetString();
                        break;
                }
            }
        }
        if (!string.IsNullOrEmpty(urlAsString))
        {
            var uri = new Uri(urlAsString);
            if (EntityId.TryParseShardRealmNum(address, out var node))
            {
                return new ConsensusNodeEndpoint(node, uri);
            }
        }
        throw new JsonException("Not an appropriately formatted Consensus Node Endpoint Object.");
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusNodeEndpoint gateway, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("address");
        writer.WriteStringValue(gateway.Node.ToString());
        writer.WritePropertyName("url");
        writer.WriteStringValue(gateway.Uri.ToString());
        writer.WriteEndObject();
    }
}
