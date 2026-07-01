// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Implementation.Formatting;

internal static class ConsensusTimeStampFormatter
{
    internal static string Format(ConsensusTimeStamp timeStamp)
    {
        Span<char> buffer = stackalloc char[64];
        return TryFormat(timeStamp, buffer, out var charsWritten)
            ? new string(buffer[..charsWritten])
            : timeStamp.Seconds.ToString("0.000000000");
    }

    internal static bool TryFormat(ConsensusTimeStamp timeStamp, Span<char> destination, out int charsWritten)
    {
        return timeStamp.Seconds.TryFormat(destination, out charsWritten, "0.000000000");
    }
}
