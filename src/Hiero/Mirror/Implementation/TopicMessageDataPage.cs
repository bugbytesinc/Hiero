// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Contains a paged list of topic message information.
/// </summary>
internal class TopicMessageDataPage : Page<TopicMessageData>
{
    /// <summary>
    /// List of topic messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public TopicMessageData[]? Messages { get; set; }
    /// <summary>
    /// Enumerates the list of messages.
    /// </summary>
    /// <returns>
    /// An enumerator listing the messages in the list.
    /// </returns>
    public override IEnumerable<TopicMessageData> GetItems()
    {
        return Messages ?? Array.Empty<TopicMessageData>();
    }
}