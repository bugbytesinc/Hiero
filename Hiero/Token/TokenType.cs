using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero;

/// <summary>
/// The type of token.
/// </summary>
[JsonConverter(typeof(TokenTypeConverter))]
public enum TokenType
{
    /// <summary>
    /// Fungible Token
    /// </summary>
    Fungible = 0,
    /// <summary>
    /// NonFungible Token (Non-Fungible, non Divisible)
    /// </summary>
    NonFungible = 1
}