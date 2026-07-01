// SPDX-License-Identifier: Apache-2.0
using System.Text;

namespace Hiero.Implementation.Formatting;

internal enum TransactionIdFormatStyle
{
    Standard,
    Mirror
}

internal static class TransactionIdFormatter
{
    internal static string Format(TransactionId? txId, TransactionIdFormatStyle style)
    {
        if (txId is null)
        {
            return string.Empty;
        }
        Span<byte> buffer = stackalloc byte[128];
        if (TryFormat(txId, style, buffer, out var bytesWritten))
        {
            return Encoding.ASCII.GetString(buffer[..bytesWritten]);
        }
        return FormatWithFallback(txId, style);
    }

    internal static bool TryFormat(TransactionId txId, TransactionIdFormatStyle style, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        var payer = txId.Payer;
        if (payer is null || !payer.IsShardRealmNum)
        {
            return false;
        }
        if (!Utf8Format.TryAppend(payer.ShardNum, destination, ref bytesWritten) ||
            !Utf8Format.TryAppend((byte)'.', destination, ref bytesWritten) ||
            !Utf8Format.TryAppend(payer.RealmNum, destination, ref bytesWritten) ||
            !Utf8Format.TryAppend((byte)'.', destination, ref bytesWritten) ||
            !Utf8Format.TryAppend(payer.AccountNum, destination, ref bytesWritten))
        {
            return false;
        }
        return style switch
        {
            TransactionIdFormatStyle.Standard => TryAppendStandard(txId, destination, ref bytesWritten),
            TransactionIdFormatStyle.Mirror => TryAppendMirror(txId, destination, ref bytesWritten),
            _ => false
        };
    }

    private static bool TryAppendStandard(TransactionId txId, Span<byte> destination, ref int offset)
    {
        return Utf8Format.TryAppend((byte)'@', destination, ref offset) &&
            Utf8Format.TryAppend(txId.ValidStartSeconds, destination, ref offset) &&
            Utf8Format.TryAppend((byte)'.', destination, ref offset) &&
            Utf8Format.TryAppendPaddedNineDigits(txId.ValidStartNanos, destination, ref offset) &&
            TryAppendChildNonce(txId.ChildNonce, destination, ref offset) &&
            TryAppendScheduled(txId.Scheduled, destination, ref offset);
    }

    private static bool TryAppendMirror(TransactionId txId, Span<byte> destination, ref int offset)
    {
        return Utf8Format.TryAppend((byte)'-', destination, ref offset) &&
            Utf8Format.TryAppend(txId.ValidStartSeconds, destination, ref offset) &&
            Utf8Format.TryAppend((byte)'-', destination, ref offset) &&
            Utf8Format.TryAppendPaddedNineDigits(txId.ValidStartNanos, destination, ref offset);
    }

    private static string FormatWithFallback(TransactionId txId, TransactionIdFormatStyle style)
    {
        return style switch
        {
            TransactionIdFormatStyle.Standard => FormatStandardWithFallback(txId),
            TransactionIdFormatStyle.Mirror => $"{txId.Payer}-{txId.ValidStartSeconds}-{txId.ValidStartNanos:000000000}",
            _ => string.Empty
        };
    }

    private static string FormatStandardWithFallback(TransactionId txId)
    {
        if (txId.Scheduled)
        {
            if (txId.ChildNonce != 0)
            {
                return $"{txId.Payer}@{txId.ValidStartSeconds}.{txId.ValidStartNanos:D9}:{txId.ChildNonce}-scheduled";
            }
            return $"{txId.Payer}@{txId.ValidStartSeconds}.{txId.ValidStartNanos:D9}-scheduled";
        }
        if (txId.ChildNonce != 0)
        {
            return $"{txId.Payer}@{txId.ValidStartSeconds}.{txId.ValidStartNanos:D9}:{txId.ChildNonce}";
        }
        return $"{txId.Payer}@{txId.ValidStartSeconds}.{txId.ValidStartNanos:D9}";
    }

    private static bool TryAppendChildNonce(int childNonce, Span<byte> destination, ref int offset)
    {
        if (childNonce == 0)
        {
            return true;
        }
        return Utf8Format.TryAppend((byte)':', destination, ref offset) &&
            Utf8Format.TryAppend(childNonce, destination, ref offset);
    }

    private static bool TryAppendScheduled(bool scheduled, Span<byte> destination, ref int offset)
    {
        return !scheduled || Utf8Format.TryAppend("-scheduled"u8, destination, ref offset);
    }
}
