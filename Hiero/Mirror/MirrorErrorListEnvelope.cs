using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Error response payload returned from the Mirror Node API.
/// </summary>
public class MirrorErrorListEnvelope
{
    /// <summary>
    /// List of one or more errors returned by the Mirror Node API.
    /// </summary>
    [JsonPropertyName("_status")]
    public MirrorErrorList Status { get; set; } = default!;
}
