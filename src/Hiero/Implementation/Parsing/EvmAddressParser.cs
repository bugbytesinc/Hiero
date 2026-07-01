// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hiero.Implementation.Parsing;

internal static class EvmAddressParser
{
    internal static bool TryParse(string? value, [NotNullWhen(true)] out EvmAddress? evmAddress)
    {
        evmAddress = null;
        if (value is null || value.Length is not (40 or 42))
        {
            return false;
        }
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount != value.Length)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[42];
        var bytes = buffer[..byteCount];
        Encoding.UTF8.GetBytes(value, bytes);
        return TryParse(bytes, out evmAddress);
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, [NotNullWhen(true)] out EvmAddress? evmAddress)
    {
        evmAddress = null;
        if (value.Length == 42 && value[0] == (byte)'0' && value[1] == (byte)'x')
        {
            value = value[2..];
        }
        else if (value.Length != 40)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[20];
        if (Convert.FromHexString(value, buffer, out _, out int bytesWritten) == System.Buffers.OperationStatus.Done && bytesWritten == 20)
        {
            evmAddress = new EvmAddress(buffer);
            return true;
        }
        return false;
    }
}
