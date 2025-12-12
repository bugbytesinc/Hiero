using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// List of one or more error details returned from the mirror node.
/// </summary>
public class MirrorErrorList
{
    /// <summary>
    /// List of any error message details returned from the server.
    /// </summary>
    [JsonPropertyName("messages")]
    public MirrorError[] Messages { get; set; } = [];
}
