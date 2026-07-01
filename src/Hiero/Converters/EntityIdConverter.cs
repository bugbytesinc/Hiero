// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Entity ID JSON Converter, can handle both hapi ids and evm addresses.
/// </summary>
public sealed class EntityIdConverter : JsonConverter<EntityId>
{
    /// <inheritdoc />
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            return EntityId.None;
        }
        EntityId? entityId;
        if (reader.ValueIsEscaped)
        {
            return EntityIdParser.TryParse(reader.GetString().AsSpan(), out entityId) ? entityId : EntityId.None;
        }
        else if (reader.HasValueSequence)
        {
            return EntityIdParser.TryParse(reader.ValueSequence, out entityId) ? entityId : EntityId.None;
        }
        return EntityIdParser.TryParse(reader.ValueSpan, out entityId) ? entityId : EntityId.None;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[64];
        if (value.TryFormat(buffer, out var bytesWritten, default, default))
        {
            writer.WriteStringValue(buffer[..bytesWritten]);
            return;
        }
        writer.WriteStringValue(value.ToString());
    }
    /// <inheritdoc />
    public override EntityId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] EntityId value, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[64];
        if (value.TryFormat(buffer, out var bytesWritten, default, default))
        {
            writer.WritePropertyName(buffer[..bytesWritten]);
            return;
        }
        writer.WritePropertyName(value.ToString());
    }
}
