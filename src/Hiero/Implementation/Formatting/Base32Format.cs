// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Implementation.Formatting;

/// <summary>
/// RFC 4648 base32 encoding: the standard alphabet, upper case, without
/// padding. This is the form in which the Mirror Node REST API spells an
/// account alias.
/// </summary>
internal static class Base32Format
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }
        var output = new char[(data.Length * 8 + 4) / 5];
        var index = 0;
        var buffer = 0;
        var bits = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                output[index++] = Alphabet[(buffer >> bits) & 0x1F];
            }
        }
        if (bits > 0)
        {
            output[index++] = Alphabet[(buffer << (5 - bits)) & 0x1F];
        }
        return new string(output, 0, index);
    }
}
