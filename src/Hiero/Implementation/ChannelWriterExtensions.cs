// SPDX-License-Identifier: Apache-2.0
using System.Threading.Channels;

namespace Hiero.Implementation;

/// <summary>
/// Writes streamed results into a caller-supplied channel, the one
/// way the streaming clients deliver them.
/// </summary>
internal static class ChannelWriterExtensions
{
    /// <summary>
    /// Writes the item exactly once, waiting for room when the channel
    /// is bounded and full.
    /// </summary>
    /// <returns>
    /// True once the item is written; false if the channel was completed
    /// (by the consuming side) before it could be.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// If the token is cancelled while waiting for room.
    /// </exception>
    internal static async ValueTask<bool> WriteOnceAsync<T>(this ChannelWriter<T> writer, T item, CancellationToken cancellationToken)
    {
        if (writer.TryWrite(item))
        {
            return true;
        }
        while (await writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            if (writer.TryWrite(item))
            {
                return true;
            }
        }
        return false;
    }
}
