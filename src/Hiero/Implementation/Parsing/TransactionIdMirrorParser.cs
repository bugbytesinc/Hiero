// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hiero.Implementation.Parsing;

internal static class TransactionIdMirrorParser
{
    internal static bool TryParse(string? value, [NotNullWhen(true)] out TransactionId? transactionId)
    {
        transactionId = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        var byteCount = Encoding.UTF8.GetByteCount(value);
        byte[]? rented = null;
        Span<byte> buffer = byteCount <= 128
            ? stackalloc byte[byteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);
        try
        {
            Encoding.UTF8.GetBytes(value, buffer);
            return TryParse(buffer, out transactionId);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, [NotNullWhen(true)] out TransactionId? transactionId)
    {
        transactionId = null;
        if (value.Length == 0 || value.IsWhiteSpace())
        {
            return false;
        }
        var firstDash = value.IndexOf((byte)'-');
        if (firstDash <= 0)
        {
            return false;
        }
        var afterFirstDash = value[(firstDash + 1)..];
        var secondDashOffset = afterFirstDash.IndexOf((byte)'-');
        var secondsText = secondDashOffset >= 0 ? afterFirstDash[..secondDashOffset] : afterFirstDash;
        var nanosText = secondDashOffset >= 0 ? afterFirstDash[(secondDashOffset + 1)..] : ReadOnlySpan<byte>.Empty;
        if (secondsText.Length == 0 ||
            (secondDashOffset >= 0 && (nanosText.Length == 0 || nanosText.IndexOf((byte)'-') >= 0)) ||
            !ShardRealmNumParser.TryParse(value[..firstDash], out var payer) ||
            !Utf8Parser.TryParse(secondsText, out long seconds, out var secondsConsumed) ||
            secondsConsumed != secondsText.Length)
        {
            return false;
        }
        var nanos = 0;
        if (nanosText.Length > 0 &&
            (!Utf8Parser.TryParse(nanosText, out nanos, out var nanosConsumed) ||
            nanosConsumed != nanosText.Length))
        {
            return false;
        }
        transactionId = new TransactionId(payer, seconds, nanos);
        return true;
    }
}
