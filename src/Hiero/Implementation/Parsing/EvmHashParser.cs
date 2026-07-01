// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hiero.Implementation.Parsing;

internal static class EvmHashParser
{
    internal static bool TryParse(string? value, [NotNullWhen(true)] out EvmHash? hash)
    {
        hash = null;
        if (value is null || value.Length is not (64 or 66))
        {
            return false;
        }
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount != value.Length)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[66];
        var bytes = buffer[..byteCount];
        Encoding.UTF8.GetBytes(value, bytes);
        return TryParse(bytes, out hash);
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, [NotNullWhen(true)] out EvmHash? hash)
    {
        hash = null;
        if (value.Length == 66 && value[0] == (byte)'0' && value[1] == (byte)'x')
        {
            value = value[2..];
        }
        else if (value.Length != 64)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[32];
        if (Convert.FromHexString(value, buffer, out _, out int bytesWritten) == System.Buffers.OperationStatus.Done && bytesWritten == 32)
        {
            hash = new EvmHash(buffer);
            return true;
        }
        return false;
    }
}
