using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Response Code Converter
/// </summary>
public sealed class RepsponseCodeConverter : JsonConverter<ResponseCode>
{
    /// <summary>
    /// Map of the response code text value to enum value.
    /// </summary>
    private static readonly Dictionary<string, ResponseCode> _mapDesc;
    /// <summary>
    /// Map of the response code enum value to string text value.
    /// </summary>
    private static readonly Dictionary<ResponseCode, string> _mapCode;
    /// <inheritdoc />
    public override ResponseCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (_mapDesc.TryGetValue(reader.GetString()!, out ResponseCode code))
        {
            return code;
        }
        return (ResponseCode)(-500);
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ResponseCode code, JsonSerializerOptions options)
    {
        if (_mapCode.TryGetValue(code, out string? desc))
        {
            writer.WriteStringValue(desc);
        }
        else
        {
            writer.WriteStringValue(string.Empty);
        }
    }
    /// <summary>
    /// Static setup helper function that creates the mappings
    /// between text values and enum values.
    /// </summary>
    static RepsponseCodeConverter()
    {
        _mapDesc = new Dictionary<string, ResponseCode>();
        _mapCode = new Dictionary<ResponseCode, string>();
        foreach (var field in typeof(ResponseCode).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
            {
                if (!string.IsNullOrWhiteSpace(attr.Description))
                {
                    if (field.GetValue(null) is ResponseCode code)
                    {
                        _mapDesc[attr.Description] = code;
                    }
                }
            }
        }
    }
}