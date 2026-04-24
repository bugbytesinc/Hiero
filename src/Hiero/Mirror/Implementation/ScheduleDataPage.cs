// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of schedule records.
/// </summary>
internal class ScheduleDataPage : Page<ScheduleData>
{
    /// <summary>
    /// List of schedule records.
    /// </summary>
    [JsonPropertyName("schedules")]
    public ScheduleData[]? Schedules { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<ScheduleData> GetItems()
    {
        return Schedules ?? Array.Empty<ScheduleData>();
    }
}
