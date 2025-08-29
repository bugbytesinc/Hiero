using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Helper class for converting between bytes and Hex encoded string values.
/// </summary>
public static class Hex
{
    /// <summary>
    /// Parses a hex encoded string into a byte array.
    /// </summary>
    /// <param name="hex">The Hex Value</param>
    /// <param name="buffer">Span receiving the decoded bytes</param>
    /// <param name="bytesWritten">Number of bytes written into the span</param>
    /// <returns>true if successfull, false if the hex string is invalid or buffer is too small.</returns>
    public static bool TryDecode(ReadOnlySpan<char> hex, Span<byte> buffer, out int bytesWritten)
    {
        bytesWritten = 0;
        if (buffer.Length < hex.Length / 2 || hex.Length % 2 != 0)
        {
            return false;
        }
        int byteIndex = 0;
        for (int i = 0; i < hex.Length; i += 2)
        {
            int hi = HexDigit(hex[i]);
            int lo = HexDigit(hex[i + 1]);
            if (hi == -1 || lo == -1)
            {
                return false;
            }
            buffer[byteIndex++] = (byte)((hi << 4) | lo);
        }
        bytesWritten = byteIndex;
        return true;
    }
    /// <summary>
    /// Internal method to produce a hex digit value from a character.
    /// </summary>
    /// <param name="c">hex character</param>
    /// <returns>the digit</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HexDigit(char c)
    {
        if ((uint)(c - '0') <= 9) return c - '0';
        char upper = (char)(c & ~0x20);
        if ((uint)(upper - 'A') <= 5) return upper - 'A' + 10;
        return -1;
    }
    /// <summary>
    /// Converts string values encoded in Hex into bytes.
    /// </summary>
    /// <param name="hex">
    /// A string containing a series of characters in hexadecimal format.
    /// </param>
    /// <returns>
    /// A blob of bytes decoded from the hex string.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the input string is <code>null</code>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the string of characters is not valid Hex.</exception>
    public static ReadOnlyMemory<byte> ToBytes(string hex)
    {
        if (hex == null)
        {
            throw new ArgumentNullException(nameof(hex), "Hex string value cannot be null.");
        }
        else if (hex.Length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        else if (string.IsNullOrWhiteSpace(hex))
        {
            throw new ArgumentOutOfRangeException(nameof(hex), "Hex string value does not contain hex values.");
        }
        else if (hex.Length % 2 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hex), "String value does not appear to be properly encoded in Hex, found an odd number of characters.");
        }
        byte[] result = new byte[hex.Length / 2];
        if (!TryDecode(hex.AsSpan(), result, out _))
        {
            throw new ArgumentOutOfRangeException("String value does not appear to be properly encoded in Hex.");
        }
        return result;
    }
    /// <summary>
    /// Converts a blob of bytes into the corresponding hex encoded string.
    /// </summary>
    /// <param name="bytes">
    /// Blob of bytes to turn into Hex.
    /// </param>
    /// <returns>
    /// String value of the bytes in lowercase Hex.
    /// </returns>
    public static string FromBytes(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        return string.Create(bytes.Length * 2, bytes.Span, static (dest, src) => TryEncode(src, dest, out _));
    }
    /// <summary>
    /// Converts a blob of bytes into the corresponding hex encoded string, using a ReadOnlySpan for performance.
    /// </summary>
    /// <param name="bytes">
    /// Blob of bytes to turn into Hex.
    /// </param>
    /// <returns>
    /// String value of the bytes in lowercase Hex.
    /// </returns>
    public static string FromBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        return string.Create(bytes.Length * 2, bytes, static (dest, src) => TryEncode(src, dest, out _));
    }
    /// <summary>
    /// Converts a blob of bytes into the corresponding hex encoded string.
    /// </summary>
    /// <param name="bytes">
    /// Blob of bytes to turn into Hex.
    /// </param>
    /// <returns>
    /// String value of the bytes in lowercase Hex.
    /// </returns>
    public static string FromBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }
        return string.Create(bytes.Length * 2, bytes, static (dest, src) => TryEncode(src, dest, out _));
    }
    /// <summary>
    /// Computes the hex representation of a byte array and writes it to a destination span.
    /// </summary>
    /// <param name="bytes">Incomming Bytes</param>
    /// <param name="destination">Span that will Receive the lower case encoded hex char values</param>
    /// <param name="charsWritten">Number of characters written (in case the buffer is largeer than needed)</param>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten)
    {
        const string hexAlphabet = "0123456789abcdef";
        charsWritten = bytes.Length * 2;
        if (destination.Length < charsWritten)
        {
            charsWritten = 0;
            return false;
        }
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            destination[i * 2] = hexAlphabet[b >> 4];
            destination[i * 2 + 1] = hexAlphabet[b & 0xF];
        }
        return true;
    }
}