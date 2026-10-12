// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of HIP-1137 registered nodes.
/// </summary>
internal class RegisteredNodeDataPage : Page<RegisteredNodeData>
{
    /// <summary>
    /// List of registered nodes.
    /// </summary>
    [JsonPropertyName("registered_nodes")]
    public RegisteredNodeData[]? RegisteredNodes { get; set; }
    /// <summary>
    /// Enumerates the list of registered nodes.
    /// </summary>
    /// <returns>
    /// Enumerator of registered nodes for this paged list.
    /// </returns>
    public override RegisteredNodeData[] GetItems()
    {
        return RegisteredNodes ?? [];
    }
}
