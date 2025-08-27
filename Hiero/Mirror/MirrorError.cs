using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Mirror Call Error Message detail object
/// </summary>
public class MirrorError
{
    /// <summary>
    /// Summary of the error
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Any detail returned from the mirror node
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
    /// <summary>
    /// Any EVM result data returned from the mirror node if applicable.
    /// </summary>
    [JsonPropertyName("data")]
    public EncodedParams Data { get; set; } = default!;
}