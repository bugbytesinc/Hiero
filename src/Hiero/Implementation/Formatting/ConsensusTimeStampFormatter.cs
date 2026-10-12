// SPDX-License-Identifier: Apache-2.0
using System.Globalization;

namespace Hiero.Implementation.Formatting;

/// <summary>
/// Writes a consensus timestamp in the HAPI <c>seconds.nanoseconds</c>
/// form the mirror node expects, always with a dot: the culture of the
/// machine running the SDK must not change a query string or a JSON value.
/// </summary>
internal static class ConsensusTimeStampFormatter
{
    internal static string Format(ConsensusTimeStamp timeStamp)
    {
        Span<char> buffer = stackalloc char[64];
        return TryFormat(timeStamp, buffer, out var charsWritten)
            ? new string(buffer[..charsWritten])
            : timeStamp.Seconds.ToString("0.000000000", CultureInfo.InvariantCulture);
    }

    internal static bool TryFormat(ConsensusTimeStamp timeStamp, Span<char> destination, out int charsWritten)
    {
        return timeStamp.Seconds.TryFormat(destination, out charsWritten, "0.000000000", CultureInfo.InvariantCulture);
    }
}
